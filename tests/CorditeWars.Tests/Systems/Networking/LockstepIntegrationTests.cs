using System;
using System.Collections.Generic;
using CorditeWars.Core;
using CorditeWars.Systems.Networking;

namespace CorditeWars.Tests.Systems.Networking;

/// <summary>
/// Integration tests for the lockstep networking flow using LoopbackTransport.
/// Exercises command synchronization, tick advancement gating, checksum exchange,
/// and graceful disconnect handling without real network I/O.
/// </summary>
public class LockstepIntegrationTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Lightweight lockstep state tracker that mirrors LockstepManager logic
    /// but works without Godot Node dependencies. Subscribes to LoopbackTransport events.
    /// </summary>
    private sealed class TestLockstepPeer
    {
        public int LocalPlayerId { get; }
        public int PlayerCount { get; }
        public int InputDelay { get; }
        public bool IsHost { get; }

        private readonly LoopbackTransport _transport;
        private readonly SortedList<int, SortedList<ulong, List<GameCommand>>> _commandBuffers = new();
        private readonly SortedList<int, SortedList<ulong, bool>> _confirmedTicks = new();
        private readonly SortedList<int, SortedList<ulong, uint>> _remoteChecksums = new();
        private readonly SortedList<ulong, uint> _localChecksums = new();

        public bool DesyncDetected { get; private set; }
        public ulong DesyncTick { get; private set; }
        public bool PeerConnectedFired { get; private set; }
        public bool ConnectedToHostFired { get; private set; }
        public bool PeerDisconnectedFired { get; private set; }
        public long DisconnectedPeerId { get; private set; }

        public TestLockstepPeer(int localPlayerId, int playerCount, bool isHost, int inputDelay, LoopbackTransport transport)
        {
            LocalPlayerId = localPlayerId;
            PlayerCount = playerCount;
            IsHost = isHost;
            InputDelay = inputDelay;
            _transport = transport;

            // Initialize per-player structures
            for (int p = 0; p < playerCount; p++)
            {
                _commandBuffers[p] = new SortedList<ulong, List<GameCommand>>();
                _confirmedTicks[p] = new SortedList<ulong, bool>();
                _remoteChecksums[p] = new SortedList<ulong, uint>();
            }

            // Pre-confirm tick 0 for all players
            for (int p = 0; p < playerCount; p++)
            {
                _confirmedTicks[p][0] = true;
            }

            // Subscribe to transport events
            _transport.CommandReceived += OnCommandReceived;
            _transport.ChecksumReceived += OnChecksumReceived;
            _transport.PeerConnected += OnPeerConnected;
            _transport.ConnectedToHost += OnConnectedToHost;
            _transport.PeerDisconnected += OnPeerDisconnected;
        }

        public void SubmitLocalCommand(GameCommand cmd, ulong currentTick)
        {
            ulong scheduledTick = currentTick + (ulong)InputDelay;
            cmd.ScheduledTick = scheduledTick;
            cmd.PlayerId = LocalPlayerId;

            // Buffer locally
            AddCommandToBuffer(LocalPlayerId, scheduledTick, cmd);

            // Serialize and broadcast
            byte[] data = CommandSerializer.Serialize(cmd);
            _transport.BroadcastCommand(data);
        }

        public void ConfirmLocalTick(ulong tick)
        {
            if (!_confirmedTicks[LocalPlayerId].ContainsKey(tick))
            {
                _confirmedTicks[LocalPlayerId][tick] = true;
            }

            // Send tick-confirm sentinel
            byte[] data = new byte[13];
            WriteInt(data, 0, LocalPlayerId);
            WriteUlong(data, 4, tick);
            data[12] = 0xFF;
            _transport.BroadcastCommand(data);
        }

        public bool CanAdvanceTick(ulong tick)
        {
            for (int p = 0; p < PlayerCount; p++)
            {
                if (!_confirmedTicks[p].ContainsKey(tick))
                    return false;
            }
            return true;
        }

        public List<GameCommand> GetCommandsForTick(ulong tick)
        {
            var merged = new List<GameCommand>();

            for (int p = 0; p < PlayerCount; p++)
            {
                var playerBuffer = _commandBuffers[p];
                if (playerBuffer.TryGetValue(tick, out var commands))
                {
                    for (int c = 0; c < commands.Count; c++)
                    {
                        merged.Add(commands[c]);
                    }
                    playerBuffer.Remove(tick);
                }
            }

            // Sort deterministically: PlayerId → CommandType
            merged.Sort((a, b) =>
            {
                int cmp = a.PlayerId.CompareTo(b.PlayerId);
                if (cmp != 0) return cmp;

                return ((int)a.Type).CompareTo((int)b.Type);
            });

            return merged;
        }

        public void SubmitChecksum(ulong tick, uint checksum)
        {
            _localChecksums[tick] = checksum;

            byte[] data = CommandSerializer.SerializeChecksum(LocalPlayerId, tick, checksum);
            _transport.BroadcastChecksum(data);

            CheckForDesync(tick);
        }

        // ── Event Handlers ───────────────────────────────────────────

        private void OnCommandReceived(int senderPeerId, byte[] data)
        {
            // Check for tick-confirm sentinel
            if (data.Length == 13 && data[12] == 0xFF)
            {
                int playerId = ReadInt(data, 0);
                ulong tick = ReadUlong(data, 4);

                if (playerId >= 0 && playerId < PlayerCount)
                {
                    if (!_confirmedTicks[playerId].ContainsKey(tick))
                    {
                        _confirmedTicks[playerId][tick] = true;
                    }
                }
                return;
            }

            // Normal command packet
            GameCommand cmd = CommandSerializer.Deserialize(data);
            int cmdPlayerId = cmd.PlayerId;
            ulong cmdTick = cmd.ScheduledTick;

            if (cmdPlayerId >= 0 && cmdPlayerId < PlayerCount)
            {
                AddCommandToBuffer(cmdPlayerId, cmdTick, cmd);
            }
        }

        private void OnChecksumReceived(int senderPeerId, byte[] data)
        {
            var (sendingPlayerId, tick, checksum) = CommandSerializer.DeserializeChecksum(data);

            if (sendingPlayerId < 0 || sendingPlayerId >= PlayerCount) return;

            if (!_remoteChecksums.ContainsKey(sendingPlayerId))
                _remoteChecksums[sendingPlayerId] = new SortedList<ulong, uint>();

            _remoteChecksums[sendingPlayerId][tick] = checksum;

            CheckForDesync(tick);
        }

        private void OnPeerConnected(long peerId)
        {
            PeerConnectedFired = true;
        }

        private void OnConnectedToHost()
        {
            ConnectedToHostFired = true;
        }

        private void OnPeerDisconnected(long peerId)
        {
            PeerDisconnectedFired = true;
            DisconnectedPeerId = peerId;
        }

        // ── Internal Helpers ─────────────────────────────────────────

        private void AddCommandToBuffer(int playerId, ulong tick, GameCommand cmd)
        {
            var playerBuffer = _commandBuffers[playerId];
            if (!playerBuffer.TryGetValue(tick, out var list))
            {
                list = new List<GameCommand>(4);
                playerBuffer[tick] = list;
            }
            list.Add(cmd);
        }

        private void CheckForDesync(ulong tick)
        {
            if (!_localChecksums.TryGetValue(tick, out uint localHash))
                return;

            for (int i = 0; i < _remoteChecksums.Count; i++)
            {
                var remoteByTick = _remoteChecksums.Values[i];
                if (remoteByTick.TryGetValue(tick, out uint remoteHash))
                {
                    if (remoteHash != localHash)
                    {
                        DesyncDetected = true;
                        DesyncTick = tick;
                        return;
                    }
                }
            }
        }

        private static void WriteInt(byte[] buf, int offset, int value)
        {
            buf[offset] = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
            buf[offset + 2] = (byte)((value >> 16) & 0xFF);
            buf[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteUlong(byte[] buf, int offset, ulong value)
        {
            buf[offset] = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
            buf[offset + 2] = (byte)((value >> 16) & 0xFF);
            buf[offset + 3] = (byte)((value >> 24) & 0xFF);
            buf[offset + 4] = (byte)((value >> 32) & 0xFF);
            buf[offset + 5] = (byte)((value >> 40) & 0xFF);
            buf[offset + 6] = (byte)((value >> 48) & 0xFF);
            buf[offset + 7] = (byte)((value >> 56) & 0xFF);
        }

        private static int ReadInt(byte[] buf, int offset)
        {
            return buf[offset]
                | (buf[offset + 1] << 8)
                | (buf[offset + 2] << 16)
                | (buf[offset + 3] << 24);
        }

        private static ulong ReadUlong(byte[] buf, int offset)
        {
            return (ulong)buf[offset]
                | ((ulong)buf[offset + 1] << 8)
                | ((ulong)buf[offset + 2] << 16)
                | ((ulong)buf[offset + 3] << 24)
                | ((ulong)buf[offset + 4] << 32)
                | ((ulong)buf[offset + 5] << 40)
                | ((ulong)buf[offset + 6] << 48)
                | ((ulong)buf[offset + 7] << 56);
        }
    }

    private static FixedVector2 Vec(int x, int y) =>
        new(FixedPoint.FromInt(x), FixedPoint.FromInt(y));

    // ── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public void LobbyCreation_HostAndClientConnect_PeerConnectedFires()
    {
        // Arrange
        var (hostTransport, clientTransport) = LoopbackTransport.CreatePair();
        var host = new TestLockstepPeer(0, 2, isHost: true, inputDelay: 6, hostTransport);
        var client = new TestLockstepPeer(1, 2, isHost: false, inputDelay: 6, clientTransport);

        // Act
        hostTransport.SimulateConnect();

        // Assert
        Assert.True(host.PeerConnectedFired, "Host should see PeerConnected");
        Assert.True(client.PeerConnectedFired, "Client should see PeerConnected");
        Assert.True(client.ConnectedToHostFired, "Client should see ConnectedToHost");
    }

    [Fact]
    public void CommandSynchronization_HostCommandReceivedByClient_AtCorrectTick()
    {
        // Arrange
        var (hostTransport, clientTransport) = LoopbackTransport.CreatePair();
        hostTransport.SimulateConnect();

        var host = new TestLockstepPeer(0, 2, isHost: true, inputDelay: 6, hostTransport);
        var client = new TestLockstepPeer(1, 2, isHost: false, inputDelay: 6, clientTransport);

        // Act — host submits a move command at tick 0
        var cmd = new MoveCommand
        {
            UnitIds = new List<int> { 1, 2 },
            TargetPosition = Vec(10, 20)
        };
        host.SubmitLocalCommand(cmd, currentTick: 0);

        // Assert — client should have the command at tick 6 (inputDelay)
        var clientCmds = client.GetCommandsForTick(6);
        Assert.Single(clientCmds);
        Assert.IsType<MoveCommand>(clientCmds[0]);
        Assert.Equal(6UL, clientCmds[0].ScheduledTick);
        Assert.Equal(0, clientCmds[0].PlayerId);

        var moveCmd = (MoveCommand)clientCmds[0];
        Assert.Equal(Vec(10, 20), moveCmd.TargetPosition);
    }

    [Fact]
    public void TickAdvancementGating_BlockedUntilAllConfirm_ThenTrue()
    {
        // Arrange
        var (hostTransport, clientTransport) = LoopbackTransport.CreatePair();
        hostTransport.SimulateConnect();

        var host = new TestLockstepPeer(0, 2, isHost: true, inputDelay: 6, hostTransport);
        var client = new TestLockstepPeer(1, 2, isHost: false, inputDelay: 6, clientTransport);

        // Act & Assert — tick 1 not confirmed by either yet
        Assert.False(host.CanAdvanceTick(1), "Should be blocked before any confirmation");

        // Host confirms tick 1
        host.ConfirmLocalTick(1);

        // Still blocked — client hasn't confirmed
        Assert.False(host.CanAdvanceTick(1), "Should be blocked until ALL confirm");

        // Client confirms tick 1
        client.ConfirmLocalTick(1);

        // Now both should be able to advance
        Assert.True(host.CanAdvanceTick(1), "Host should advance after all confirm");
        Assert.True(client.CanAdvanceTick(1), "Client should advance after all confirm");
    }

    [Fact]
    public void ChecksumMatch_MatchingChecksums_NoDesyncDetected()
    {
        // Arrange
        var (hostTransport, clientTransport) = LoopbackTransport.CreatePair();
        hostTransport.SimulateConnect();

        var host = new TestLockstepPeer(0, 2, isHost: true, inputDelay: 6, hostTransport);
        var client = new TestLockstepPeer(1, 2, isHost: false, inputDelay: 6, clientTransport);

        // Act — both submit identical checksums for tick 10
        host.SubmitChecksum(10, 0xABCD1234);
        client.SubmitChecksum(10, 0xABCD1234);

        // Assert — no desync on either side
        Assert.False(host.DesyncDetected, "Host should not detect desync with matching checksums");
        Assert.False(client.DesyncDetected, "Client should not detect desync with matching checksums");
    }

    [Fact]
    public void ChecksumMismatch_DifferentChecksums_DesyncDetected()
    {
        // Arrange
        var (hostTransport, clientTransport) = LoopbackTransport.CreatePair();
        hostTransport.SimulateConnect();

        var host = new TestLockstepPeer(0, 2, isHost: true, inputDelay: 6, hostTransport);
        var client = new TestLockstepPeer(1, 2, isHost: false, inputDelay: 6, clientTransport);

        // Act — submit different checksums for tick 10
        host.SubmitChecksum(10, 0xABCD1234);
        client.SubmitChecksum(10, 0xDEADBEEF);

        // Assert — at least one side detects desync
        bool anyDesync = host.DesyncDetected || client.DesyncDetected;
        Assert.True(anyDesync, "Desync should be detected when checksums mismatch");

        // The side that receives the mismatched checksum should flag it
        if (host.DesyncDetected)
            Assert.Equal(10UL, host.DesyncTick);
        if (client.DesyncDetected)
            Assert.Equal(10UL, client.DesyncTick);
    }

    [Fact]
    public void GracefulDisconnect_PeerDropsMidMatch_NoException()
    {
        // Arrange
        var (hostTransport, clientTransport) = LoopbackTransport.CreatePair();
        hostTransport.SimulateConnect();

        var host = new TestLockstepPeer(0, 2, isHost: true, inputDelay: 6, hostTransport);
        var client = new TestLockstepPeer(1, 2, isHost: false, inputDelay: 6, clientTransport);

        // Submit some commands first to ensure state is active
        var cmd = new MoveCommand
        {
            UnitIds = new List<int> { 1 },
            TargetPosition = Vec(5, 5)
        };
        host.SubmitLocalCommand(cmd, currentTick: 0);

        // Act — client disconnects mid-match
        var exception = Record.Exception(() =>
        {
            clientTransport.SimulateDisconnect();
        });

        // Assert — no crash
        Assert.Null(exception);
        Assert.True(host.PeerDisconnectedFired, "Host should see PeerDisconnected");
    }

    [Fact]
    public void DeterministicOrdering_CommandsFromMultiplePlayers_ReturnedInOrder()
    {
        // Arrange
        var (hostTransport, clientTransport) = LoopbackTransport.CreatePair();
        hostTransport.SimulateConnect();

        var host = new TestLockstepPeer(0, 2, isHost: true, inputDelay: 6, hostTransport);
        var client = new TestLockstepPeer(1, 2, isHost: false, inputDelay: 6, clientTransport);

        // Act — both players submit commands at the same tick
        var hostCmd = new MoveCommand
        {
            UnitIds = new List<int> { 1 },
            TargetPosition = Vec(10, 10)
        };
        host.SubmitLocalCommand(hostCmd, currentTick: 0);

        var clientCmd = new AttackMoveCommand
        {
            UnitIds = new List<int> { 2 },
            TargetPosition = Vec(20, 20)
        };
        client.SubmitLocalCommand(clientCmd, currentTick: 0);

        // Both commands are scheduled at tick 6
        // Get commands from host's perspective (has both local + received)
        var hostCommands = host.GetCommandsForTick(6);

        // Assert — deterministic order: PlayerId 0 before PlayerId 1
        Assert.Equal(2, hostCommands.Count);
        Assert.Equal(0, hostCommands[0].PlayerId); // Host's command first (lower PlayerId)
        Assert.Equal(1, hostCommands[1].PlayerId); // Client's command second

        // Verify from client's perspective too
        var clientCommands = client.GetCommandsForTick(6);
        Assert.Equal(2, clientCommands.Count);
        Assert.Equal(0, clientCommands[0].PlayerId);
        Assert.Equal(1, clientCommands[1].PlayerId);
    }
}
