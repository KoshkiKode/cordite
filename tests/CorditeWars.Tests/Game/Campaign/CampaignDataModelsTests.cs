using CorditeWars.Game.Campaign;

namespace CorditeWars.Tests.Game.Campaign;

public class CampaignDataModelsTests
{
    [Fact]
    public void TypedObjectiveData_Defaults_AreExpected()
    {
        var objective = new TypedObjectiveData();

        Assert.Equal(string.Empty, objective.Type);
        Assert.Equal(string.Empty, objective.Label);
        Assert.Equal(string.Empty, objective.TargetId);
        Assert.Equal(1, objective.Count);
        Assert.Equal(0, objective.Ticks);
        Assert.True(objective.Required);
    }

    [Fact]
    public void TypedObjectiveData_AssignedValues_ArePreserved()
    {
        var objective = new TypedObjectiveData
        {
            Type = "survive_ticks",
            Label = "Hold for 300 ticks",
            TargetId = "base_a",
            Count = 3,
            Ticks = 300,
            Required = false
        };

        Assert.Equal("survive_ticks", objective.Type);
        Assert.Equal("Hold for 300 ticks", objective.Label);
        Assert.Equal("base_a", objective.TargetId);
        Assert.Equal(3, objective.Count);
        Assert.Equal(300, objective.Ticks);
        Assert.False(objective.Required);
    }

    [Fact]
    public void FactionCampaign_Defaults_AreExpected()
    {
        var campaign = new FactionCampaign();

        Assert.Equal(string.Empty, campaign.FactionId);
        Assert.Equal(string.Empty, campaign.CampaignName);
        Assert.Equal(string.Empty, campaign.Commander);
        Assert.Equal(string.Empty, campaign.Description);
        Assert.Empty(campaign.Missions);
    }

    [Fact]
    public void FactionCampaign_Missions_AreRetained()
    {
        var mission1 = new CampaignMission { Id = "a_01", Name = "First Mission" };
        var mission2 = new CampaignMission { Id = "a_02", Name = "Second Mission" };

        var campaign = new FactionCampaign
        {
            FactionId = "arcloft",
            CampaignName = "Rise of Arcloft",
            Commander = "Admiral K",
            Description = "Campaign description",
            Missions = [mission1, mission2]
        };

        Assert.Equal("arcloft", campaign.FactionId);
        Assert.Equal("Rise of Arcloft", campaign.CampaignName);
        Assert.Equal("Admiral K", campaign.Commander);
        Assert.Equal("Campaign description", campaign.Description);
        Assert.Equal(2, campaign.Missions.Count);
        Assert.Same(mission1, campaign.Missions[0]);
        Assert.Same(mission2, campaign.Missions[1]);
    }
}
