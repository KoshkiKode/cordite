using System;

namespace CorditeWars.Tests.Systems.Networking;

/// <summary>
/// In-memory mock transport for integration testing the lockstep protocol
/// without real networking. Implements the same event interface as NetworkTransport.
/// Packets are routed synchronously between paired instances for deterministic test execution.
/// </summary>
public class LoopbackTransport
{
    private LoopbackTransport? _paired;
    private bool _isConnected;

    /// <summary>The local peer ID for this transport instance.</summary>
    public int LocalPeerId { get; private set; }

    /// <summary>Whether this transport is the host.</summary>
    public bool IsHost { get; private set; }

    /// <summary>Whether this transport is currently connected to its pair.</summary>
    public bool IsConnected => _isConnected;

    // ── Events matching NetworkTransport interface ────────────────────

    /// <summary>Fired when a command packet is received. Args: senderPeerId, data.</summary>
    public event Action<int, byte[]>? CommandReceived;

    /// <summary>Fired when a checksum packet is received. Args: senderPeerId, data.</summary>
    public event Action<int, byte[]>? ChecksumReceived;

    /// <summary>Fired when a remote peer connects. Argument is the peer ID.</summary>
    public event Action<long>? PeerConnected;

    /// <summary>Fired when a remote peer disconnects. Argument is the peer ID.</summary>
    public event Action<long>? PeerDisconnected;

    /// <summary>Fired when this client successfully connects to the host.</summary>
    public event Action? ConnectedToHost;

    // ── Constructor ──────────────────────────────────────────────────

    private LoopbackTransport(int localPeerId, bool isHost)
    {
        LocalPeerId = localPeerId;
        IsHost = isHost;
        _isConnected = false;
    }

    // ── Factory ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a connected pair of LoopbackTransport instances.
    /// The host has peer ID 1, the client has peer ID 2.
    /// </summary>
    /// <returns>A tuple of (host, client) transports linked together.</returns>
    public static (LoopbackTransport host, LoopbackTransport client) CreatePair()
    {
        var host = new LoopbackTransport(localPeerId: 1, isHost: true);
        var client = new LoopbackTransport(localPeerId: 2, isHost: false);

        host._paired = client;
        client._paired = host;

        return (host, client);
    }

    // ── Command Broadcasting ─────────────────────────────────────────

    /// <summary>
    /// Broadcasts a command packet to the paired transport.
    /// Synchronously invokes the paired transport's CommandReceived event.
    /// </summary>
    public void BroadcastCommand(byte[] data)
    {
        if (_paired is null || !_isConnected) return;

        _paired.CommandReceived?.Invoke(LocalPeerId, data);
    }

    /// <summary>
    /// Sends a command packet to a specific peer (routes to paired transport).
    /// </summary>
    public void SendCommand(int targetPeerId, byte[] data)
    {
        if (_paired is null || !_isConnected) return;

        // In a pair, there's only one other peer
        _paired.CommandReceived?.Invoke(LocalPeerId, data);
    }

    // ── Checksum Broadcasting ────────────────────────────────────────

    /// <summary>
    /// Broadcasts a checksum packet to the paired transport.
    /// Synchronously invokes the paired transport's ChecksumReceived event.
    /// </summary>
    public void BroadcastChecksum(byte[] data)
    {
        if (_paired is null || !_isConnected) return;

        _paired.ChecksumReceived?.Invoke(LocalPeerId, data);
    }

    /// <summary>
    /// Sends a checksum packet to a specific peer (routes to paired transport).
    /// </summary>
    public void SendChecksum(int targetPeerId, byte[] data)
    {
        if (_paired is null || !_isConnected) return;

        _paired.ChecksumReceived?.Invoke(LocalPeerId, data);
    }

    // ── Test Control ─────────────────────────────────────────────────

    /// <summary>
    /// Simulates a connection being established.
    /// Fires PeerConnected on both sides and ConnectedToHost on the client.
    /// </summary>
    public void SimulateConnect()
    {
        if (_paired is null) return;

        _isConnected = true;
        _paired._isConnected = true;

        // Host sees client connect
        if (IsHost)
        {
            PeerConnected?.Invoke(_paired.LocalPeerId);
            _paired.ConnectedToHost?.Invoke();
            _paired.PeerConnected?.Invoke(LocalPeerId);
        }
        else
        {
            // Client initiates: notify both sides
            _paired.PeerConnected?.Invoke(LocalPeerId);
            ConnectedToHost?.Invoke();
            PeerConnected?.Invoke(_paired.LocalPeerId);
        }
    }

    /// <summary>
    /// Simulates a disconnection. Fires PeerDisconnected on the paired transport.
    /// </summary>
    public void SimulateDisconnect()
    {
        if (_paired is null) return;

        _isConnected = false;

        // Notify the paired transport that this peer disconnected
        _paired.PeerDisconnected?.Invoke(LocalPeerId);
    }
}
