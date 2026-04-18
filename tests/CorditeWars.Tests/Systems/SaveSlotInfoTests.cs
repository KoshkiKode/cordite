using CorditeWars.Systems.Persistence;

namespace CorditeWars.Tests.Systems;

public class SaveSlotInfoTests
{
    [Fact]
    public void SaveSlotInfo_Defaults_AreExpected()
    {
        var slot = new SaveSlotInfo();

        Assert.Equal(string.Empty, slot.SlotName);
        Assert.Equal(string.Empty, slot.MapId);
        Assert.Equal(string.Empty, slot.MapDisplayName);
        Assert.Equal(0UL, slot.CurrentTick);
        Assert.Equal(string.Empty, slot.SaveTimestamp);
        Assert.Equal(0, slot.PlayerCount);
        Assert.Equal(string.Empty, slot.Version);
    }

    [Fact]
    public void SaveSlotInfo_AssignedValues_ArePreserved()
    {
        var slot = new SaveSlotInfo
        {
            SlotName = "slot_1",
            MapId = "crossroads",
            MapDisplayName = "Crossroads",
            CurrentTick = 1200UL,
            SaveTimestamp = "2026-01-01T00:00:00Z",
            PlayerCount = 3,
            Version = "0.2.0"
        };

        Assert.Equal("slot_1", slot.SlotName);
        Assert.Equal("crossroads", slot.MapId);
        Assert.Equal("Crossroads", slot.MapDisplayName);
        Assert.Equal(1200UL, slot.CurrentTick);
        Assert.Equal("2026-01-01T00:00:00Z", slot.SaveTimestamp);
        Assert.Equal(3, slot.PlayerCount);
        Assert.Equal("0.2.0", slot.Version);
    }
}
