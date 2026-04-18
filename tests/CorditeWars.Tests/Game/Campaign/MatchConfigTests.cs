using CorditeWars.Game;
using CorditeWars.Game.Campaign;
using CorditeWars.Game.World;

namespace CorditeWars.Tests.Game.Campaign;

public class MatchConfigTests
{
    [Fact]
    public void PlayerConfig_Defaults_AreExpected()
    {
        var config = new PlayerConfig();

        Assert.Equal(0, config.PlayerId);
        Assert.Equal(string.Empty, config.FactionId);
        Assert.False(config.IsAI);
        Assert.Equal(0, config.AIDifficulty);
        Assert.Equal(string.Empty, config.PlayerName);
    }

    [Fact]
    public void CampaignMatchContext_Defaults_AreExpected()
    {
        var context = new CampaignMatchContext();

        Assert.Equal(string.Empty, context.FactionId);
        Assert.Equal(string.Empty, context.MissionId);
        Assert.Equal(0, context.MissionNumber);
        Assert.Equal(string.Empty, context.MissionName);
        Assert.Empty(context.Objectives);
        Assert.Empty(context.TypedObjectives);
        Assert.Null(context.AllowedBuildingIds);
    }

    [Fact]
    public void MatchConfig_Defaults_AreExpected()
    {
        var config = new MatchConfig();

        Assert.Equal(string.Empty, config.MapId);
        Assert.Empty(config.PlayerConfigs);
        Assert.Equal(0UL, config.MatchSeed);
        Assert.Equal(1, config.GameSpeed);
        Assert.True(config.FogOfWar);
        Assert.Equal(5000, config.StartingCordite);
        Assert.Equal(WinCondition.DestroyHQ, config.WinCondition);
        Assert.Null(config.MapGeneration);
        Assert.False(config.IsTutorial);
        Assert.Equal(1, config.TutorialMission);
        Assert.Equal(-1, config.LocalPlayerId);
        Assert.Null(config.Campaign);
    }

    [Fact]
    public void MatchConfig_AssignedValues_ArePreserved()
    {
        var campaign = new CampaignMatchContext
        {
            FactionId = "arcloft",
            MissionId = "arcloft_03",
            MissionNumber = 3,
            MissionName = "Siege of Ember Gate",
            Objectives = ["Secure bridge", "Destroy HQ"],
            TypedObjectives =
            [
                new TypedObjectiveData { Type = "destroy_unit", Label = "Destroy HQ", TargetId = "hq_enemy", Count = 1 }
            ],
            AllowedBuildingIds = ["hq", "barracks", "factory"]
        };

        var mapGen = new MapGenConfig
        {
            Width = 256,
            Height = 192,
            PlayerCount = 4,
            Biome = "rocky",
            Seed = 12345UL,
            PropDensity = 0.75,
            CorditeNodesPerPlayer = 4,
            ElevationZoneCount = 9,
            GenerateRivers = false,
            GeneratePaths = true
        };

        var config = new MatchConfig
        {
            MapId = "campaign_map_03",
            PlayerConfigs =
            [
                new PlayerConfig { PlayerId = 1, FactionId = "arcloft", PlayerName = "Commander" },
                new PlayerConfig { PlayerId = 2, FactionId = "bastion", PlayerName = "CPU", IsAI = true, AIDifficulty = 2 }
            ],
            MatchSeed = 999UL,
            GameSpeed = 2,
            FogOfWar = false,
            StartingCordite = 8000,
            WinCondition = WinCondition.KillAllUnits,
            MapGeneration = mapGen,
            IsTutorial = true,
            TutorialMission = 3,
            LocalPlayerId = 1,
            Campaign = campaign
        };

        Assert.Equal("campaign_map_03", config.MapId);
        Assert.Equal(2, config.PlayerConfigs.Length);
        Assert.Equal(999UL, config.MatchSeed);
        Assert.Equal(2, config.GameSpeed);
        Assert.False(config.FogOfWar);
        Assert.Equal(8000, config.StartingCordite);
        Assert.Equal(WinCondition.KillAllUnits, config.WinCondition);
        Assert.Same(mapGen, config.MapGeneration);
        Assert.True(config.IsTutorial);
        Assert.Equal(3, config.TutorialMission);
        Assert.Equal(1, config.LocalPlayerId);
        Assert.Same(campaign, config.Campaign);
        Assert.Equal("Destroy HQ", config.Campaign!.TypedObjectives[0].Label);
        Assert.Contains("factory", config.Campaign.AllowedBuildingIds!);
    }
}
