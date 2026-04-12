using System.Collections.Generic;
using System.Linq;
using CorditeWars.Core;
using CorditeWars.Game.Units;
using CorditeWars.Systems.Superweapon;
using CorditeWars.Systems.Pathfinding;

namespace CorditeWars.Tests.Systems;

/// <summary>
/// Tests for <see cref="SuperweaponSystem"/>, <see cref="PlayerSuperweaponState"/>,
/// and the superweapon catalogue.
/// </summary>
public class SuperweaponSystemTests
{
    // ── Helpers ─────────────────────────────────────────────────────────

    private static SimUnit MakeUnit(int id, int playerId, FixedVector2 pos, int hp = 100)
    {
        return new SimUnit
        {
            UnitId    = id,
            PlayerId  = playerId,
            Movement  = new MovementState { Position = pos },
            Health    = FixedPoint.FromInt(hp),
            MaxHealth = FixedPoint.FromInt(hp),
            IsAlive   = true,
            Category  = UnitCategory.Infantry,
            Weapons   = new List<WeaponData>(),
            WeaponCooldowns = new List<FixedPoint>(),
            Profile   = MovementProfile.Infantry(),
            Radius    = FixedPoint.One
        };
    }

    /// <summary>Returns the first weapon ID registered for a player (test helper).</summary>
    private static string GetWeaponId(SuperweaponSystem sys, int playerId)
        => sys.GetPlayerWeapons(playerId).First().Data.Id;

    // ═══════════════════════════════════════════════════════════════════════
    // Catalogue
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Catalogue_ContainsArcloftWeapons()
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons("arcloft"));
        Assert.True(weapons.Count >= 1);
        Assert.All(weapons, w => Assert.Equal("arcloft", w.FactionId));
    }

    [Fact]
    public void Catalogue_ContainsBastionWeapons()
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons("bastion"));
        Assert.True(weapons.Count >= 1);
        Assert.All(weapons, w => Assert.Equal("bastion", w.FactionId));
    }

    [Fact]
    public void GetData_ReturnsKnownEntry()
    {
        var data = SuperweaponSystem.GetData("arcloft_orbital_strike");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.OrbitalStrike, data!.Type);
    }

    [Fact]
    public void GetData_ReturnsNull_ForUnknownId()
    {
        Assert.Null(SuperweaponSystem.GetData("nonexistent_weapon"));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Registration
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RegisterPlayer_AssignsWeapon()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        Assert.NotNull(sys.GetPlayerWeapons(1).FirstOrDefault());
    }

    [Fact]
    public void RegisterPlayer_UnknownWeapon_NoState()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "does_not_exist");
        Assert.Null(sys.GetPlayerWeapons(1).FirstOrDefault());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Cooldown
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void NewWeapon_StartsOnCooldown_NotReady()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        Assert.False(sys.IsAnyReady(1));
    }

    [Fact]
    public void Tick_ReducesCooldown()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        var state = sys.GetPlayerWeapons(1).First()!;
        int initial = state.CooldownRemaining;

        sys.Tick();
        Assert.Equal(initial - 1, state.CooldownRemaining);
    }

    [Fact]
    public void Weapon_BecomesReady_AfterFullCooldown()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        var state = sys.GetPlayerWeapons(1).First()!;
        int ticks = state.CooldownRemaining;

        for (int i = 0; i < ticks; i++)
            sys.Tick();

        Assert.True(sys.IsAnyReady(1));
    }

    [Fact]
    public void ChargePercent_IsZero_WhenFreshCooldown()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        Assert.Equal(0f, sys.GetChargePercent(1, GetWeaponId(sys, 1)), precision: 2);
    }

    [Fact]
    public void ChargePercent_IsOne_WhenReady()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        var state = sys.GetPlayerWeapons(1).First()!;
        int ticks = state.CooldownRemaining;
        for (int i = 0; i < ticks; i++)
            sys.Tick();
        Assert.Equal(1f, sys.GetChargePercent(1, GetWeaponId(sys, 1)), precision: 2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TryActivate — not ready
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TryActivate_WhenNotReady_DoesNotFire()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, new List<SimUnit>());
        Assert.False(result.DidFire);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TryActivate — ready / hits units
    // ═══════════════════════════════════════════════════════════════════════

    private static void MakeReady(SuperweaponSystem sys, int playerId)
    {
        // Tick each registered weapon to full charge independently
        foreach (var state in sys.GetPlayerWeapons(playerId))
        {
            int ticks = state.CooldownRemaining;
            for (int i = 0; i < ticks; i++)
                sys.Tick();
        }
    }

    [Fact]
    public void TryActivate_WhenReady_Fires()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        MakeReady(sys, 1);

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, new List<SimUnit>());
        Assert.True(result.DidFire);
    }

    [Fact]
    public void TryActivate_ArmsWeapon_AfterFiring()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        MakeReady(sys, 1);

        sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, new List<SimUnit>());
        Assert.False(sys.IsAnyReady(1)); // back on cooldown
    }

    [Fact]
    public void TryActivate_HitsEnemiesInAoE()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(playerId: 1, "arcloft_orbital_strike");
        MakeReady(sys, 1);

        var target = new FixedVector2(FixedPoint.FromInt(10), FixedPoint.FromInt(10));
        var units = new List<SimUnit>
        {
            // inside AoE (player 2 — enemy)
            MakeUnit(id: 10, playerId: 2,
                pos: new FixedVector2(FixedPoint.FromInt(10), FixedPoint.FromInt(10))),
            // outside AoE
            MakeUnit(id: 11, playerId: 2,
                pos: new FixedVector2(FixedPoint.FromInt(50), FixedPoint.FromInt(50)))
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), target, units);
        Assert.True(result.DidFire);
        Assert.Contains(10, result.HitUnitIds);
        Assert.DoesNotContain(11, result.HitUnitIds);
    }

    [Fact]
    public void TryActivate_DoesNotHitFriendlyUnits()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        MakeReady(sys, 1);

        var units = new List<SimUnit>
        {
            // friendly inside AoE
            MakeUnit(id: 5, playerId: 1,
                pos: FixedVector2.Zero)
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, units);
        Assert.DoesNotContain(5, result.HitUnitIds);
    }

    [Fact]
    public void TryActivate_EMP_SetsFlag()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_emp_blast");
        MakeReady(sys, 1);

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, new List<SimUnit>());
        Assert.True(result.IsEMP);
        Assert.True(result.EMPDurationTicks > 0);
    }

    [Fact]
    public void TryActivate_ReinforcementDrop_SpawnsUnits()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "bastion_reinforcement_drop");
        MakeReady(sys, 1);

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, new List<SimUnit>());
        Assert.True(result.DidFire);
        Assert.True(result.SpawnedUnitTypeIds.Count > 0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Ironmarch — Seismic Charge
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Catalogue_ContainsIronmarchWeapons()
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons("ironmarch"));
        Assert.True(weapons.Count >= 1);
        Assert.All(weapons, w => Assert.Equal("ironmarch", w.FactionId));
    }

    [Fact]
    public void SeismicCharge_InCatalogue_WithCorrectType()
    {
        var data = SuperweaponSystem.GetData("ironmarch_seismic_charge");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.SeismicCharge, data!.Type);
    }

    [Fact]
    public void SeismicCharge_HitsEnemiesInAoE()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "ironmarch_seismic_charge");
        MakeReady(sys, 1);

        var target = new FixedVector2(FixedPoint.FromInt(10), FixedPoint.FromInt(10));
        var units = new List<SimUnit>
        {
            MakeUnit(10, 2, new FixedVector2(FixedPoint.FromInt(10), FixedPoint.FromInt(10))), // inside
            MakeUnit(11, 2, new FixedVector2(FixedPoint.FromInt(100), FixedPoint.FromInt(100))) // far away
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), target, units);
        Assert.True(result.DidFire);
        Assert.Contains(10, result.HitUnitIds);
        Assert.DoesNotContain(11, result.HitUnitIds);
    }

    [Fact]
    public void SeismicCharge_DamageIs250()
    {
        var data = SuperweaponSystem.GetData("ironmarch_seismic_charge")!;
        Assert.Equal(250, data.Damage.ToInt());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Kragmore — Artillery Salvo
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Catalogue_ContainsKragmoreWeapons()
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons("kragmore"));
        Assert.True(weapons.Count >= 1);
        Assert.All(weapons, w => Assert.Equal("kragmore", w.FactionId));
    }

    [Fact]
    public void ArtillerySalvo_InCatalogue_WithCorrectType()
    {
        var data = SuperweaponSystem.GetData("kragmore_artillery_salvo");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.ArtillerySalvo, data!.Type);
    }

    [Fact]
    public void ArtillerySalvo_HitsMultipleEnemiesInArea()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "kragmore_artillery_salvo");
        MakeReady(sys, 1);

        // AoE radius = 11 cells; pack 3 enemies inside and 1 clearly outside
        var units = new List<SimUnit>
        {
            MakeUnit(10, 2, new FixedVector2(FixedPoint.FromInt(1), FixedPoint.Zero)),
            MakeUnit(11, 2, new FixedVector2(FixedPoint.Zero,      FixedPoint.FromInt(1))),
            MakeUnit(12, 2, new FixedVector2(FixedPoint.FromInt(2), FixedPoint.FromInt(2))),
            MakeUnit(20, 2, new FixedVector2(FixedPoint.FromInt(50), FixedPoint.Zero)), // outside AoE (radius=11)
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, units);
        Assert.True(result.DidFire);
        Assert.True(result.HitUnitIds.Count >= 3);
        Assert.DoesNotContain(20, result.HitUnitIds);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Stormrend — Lightning Cascade
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Catalogue_ContainsStormrendWeapons()
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons("stormrend"));
        Assert.True(weapons.Count >= 1);
        Assert.All(weapons, w => Assert.Equal("stormrend", w.FactionId));
    }

    [Fact]
    public void LightningCascade_InCatalogue_WithCorrectType()
    {
        var data = SuperweaponSystem.GetData("stormrend_lightning_cascade");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.LightningCascade, data!.Type);
        Assert.True(data.ChainCount > 0);
    }

    [Fact]
    public void LightningCascade_HitsChainedTargets()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "stormrend_lightning_cascade");
        MakeReady(sys, 1);

        // 4 enemies clustered close together
        var units = new List<SimUnit>
        {
            MakeUnit(10, 2, new FixedVector2(FixedPoint.FromInt(1), FixedPoint.Zero)),
            MakeUnit(11, 2, new FixedVector2(FixedPoint.FromInt(2), FixedPoint.Zero)),
            MakeUnit(12, 2, new FixedVector2(FixedPoint.FromInt(3), FixedPoint.Zero)),
            MakeUnit(13, 2, new FixedVector2(FixedPoint.FromInt(4), FixedPoint.Zero)),
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, units);
        Assert.True(result.DidFire);
        Assert.True(result.HitUnitIds.Count >= 2, $"Expected chain ≥2 hits, got {result.HitUnitIds.Count}");
    }

    [Fact]
    public void LightningCascade_NeverHitsSameUnitTwice()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "stormrend_lightning_cascade");
        MakeReady(sys, 1);

        var units = new List<SimUnit>
        {
            MakeUnit(10, 2, FixedVector2.Zero),
            MakeUnit(11, 2, new FixedVector2(FixedPoint.FromInt(1), FixedPoint.Zero)),
            MakeUnit(12, 2, new FixedVector2(FixedPoint.FromInt(2), FixedPoint.Zero)),
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, units);
        var hitSet = new HashSet<int>(result.HitUnitIds);
        Assert.Equal(result.HitUnitIds.Count, hitSet.Count); // no duplicates
    }

    [Fact]
    public void LightningCascade_DoesNotHitFriendlyUnits()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "stormrend_lightning_cascade");
        MakeReady(sys, 1);

        var units = new List<SimUnit>
        {
            MakeUnit(5, 1, FixedVector2.Zero), // friendly — must not be hit
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, units);
        Assert.Empty(result.HitUnitIds);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Valkyr — Carpet Bomb Run
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Catalogue_ContainsValkyrWeapons()
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons("valkyr"));
        Assert.True(weapons.Count >= 1);
        Assert.All(weapons, w => Assert.Equal("valkyr", w.FactionId));
    }

    [Fact]
    public void CarpetBombRun_InCatalogue_WithCorrectType()
    {
        var data = SuperweaponSystem.GetData("valkyr_carpet_bomb_run");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.CarpetBombRun, data!.Type);
        Assert.True(data.StripLength > FixedPoint.Zero);
        Assert.True(data.StripHalfWidth > FixedPoint.Zero);
    }

    [Fact]
    public void CarpetBombRun_HitsEnemiesInsideStrip()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "valkyr_carpet_bomb_run");
        MakeReady(sys, 1);

        // Strip runs along X; half-width=4, half-length=12, centred at origin
        var units = new List<SimUnit>
        {
            // inside strip (X ≤ 12, Y ≤ 4)
            MakeUnit(10, 2, new FixedVector2(FixedPoint.FromInt(5), FixedPoint.FromInt(2))),
            MakeUnit(11, 2, new FixedVector2(FixedPoint.FromInt(-8), FixedPoint.FromInt(-3))),
            // outside strip — too wide
            MakeUnit(20, 2, new FixedVector2(FixedPoint.FromInt(1), FixedPoint.FromInt(10))),
            // outside strip — too long
            MakeUnit(21, 2, new FixedVector2(FixedPoint.FromInt(20), FixedPoint.Zero)),
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, units);
        Assert.True(result.DidFire);
        Assert.Contains(10, result.HitUnitIds);
        Assert.Contains(11, result.HitUnitIds);
        Assert.DoesNotContain(20, result.HitUnitIds);
        Assert.DoesNotContain(21, result.HitUnitIds);
    }

    [Fact]
    public void CarpetBombRun_DoesNotHitFriendlyUnits()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "valkyr_carpet_bomb_run");
        MakeReady(sys, 1);

        var units = new List<SimUnit>
        {
            MakeUnit(5, 1, FixedVector2.Zero), // friendly — inside strip but should not be hit
        };

        var result = sys.TryActivate(1, GetWeaponId(sys, 1), FixedVector2.Zero, units);
        Assert.Empty(result.HitUnitIds);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // All 6 factions have weapons in the catalogue
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("arcloft")]
    [InlineData("bastion")]
    [InlineData("ironmarch")]
    [InlineData("kragmore")]
    [InlineData("stormrend")]
    [InlineData("valkyr")]
    public void AllFactions_HaveAtLeastOneWeapon(string factionId)
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons(factionId));
        Assert.True(weapons.Count >= 1, $"Faction '{factionId}' has no superweapon in the catalogue");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Each faction has exactly 2 superweapons: 1 building + 1 activated
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("arcloft")]
    [InlineData("bastion")]
    [InlineData("ironmarch")]
    [InlineData("kragmore")]
    [InlineData("stormrend")]
    [InlineData("valkyr")]
    public void AllFactions_HaveExactlyTwoWeapons(string factionId)
    {
        var weapons = new List<SuperweaponData>(SuperweaponSystem.GetFactionWeapons(factionId));
        Assert.Equal(2, weapons.Count);
    }

    [Theory]
    [InlineData("arcloft")]
    [InlineData("bastion")]
    [InlineData("ironmarch")]
    [InlineData("kragmore")]
    [InlineData("stormrend")]
    [InlineData("valkyr")]
    public void AllFactions_HaveOneBuildingSuperweapon(string factionId)
    {
        var building = SuperweaponSystem.GetFactionBuildingWeapon(factionId);
        Assert.NotNull(building);
        Assert.Equal(SuperweaponCategory.BuildingSuperweapon, building!.Category);
        Assert.False(string.IsNullOrEmpty(building.RequiredBuildingTypeId),
            $"BuildingSuperweapon for '{factionId}' has no RequiredBuildingTypeId");
    }

    [Theory]
    [InlineData("arcloft")]
    [InlineData("bastion")]
    [InlineData("ironmarch")]
    [InlineData("kragmore")]
    [InlineData("stormrend")]
    [InlineData("valkyr")]
    public void AllFactions_HaveOneActivatedAbility(string factionId)
    {
        var ability = SuperweaponSystem.GetFactionActivatedAbility(factionId);
        Assert.NotNull(ability);
        Assert.Equal(SuperweaponCategory.ActivatedAbility, ability!.Category);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // New building superweapons — catalogue presence and category
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SiegeVolley_InCatalogue_AsBuildingSuperweapon()
    {
        var data = SuperweaponSystem.GetData("ironmarch_siege_volley");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.SiegeVolley, data!.Type);
        Assert.Equal(SuperweaponCategory.BuildingSuperweapon, data.Category);
        Assert.Equal("ironmarch_siege_works", data.RequiredBuildingTypeId);
    }

    [Fact]
    public void SiegeVolley_Fires_CircularAoE()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "ironmarch_siege_volley");
        MakeReady(sys, 1);

        var target = FixedVector2.Zero;
        var units = new List<SimUnit>
        {
            MakeUnit(10, 2, new FixedVector2(FixedPoint.FromInt(3), FixedPoint.Zero)), // inside (AoE=6)
            MakeUnit(11, 2, new FixedVector2(FixedPoint.FromInt(50), FixedPoint.Zero)), // outside
        };
        var result = sys.TryActivate(1, "ironmarch_siege_volley", target, units);
        Assert.True(result.DidFire);
        Assert.Contains(10, result.HitUnitIds);
        Assert.DoesNotContain(11, result.HitUnitIds);
    }

    [Fact]
    public void IndustrialBarrage_InCatalogue_AsBuildingSuperweapon()
    {
        var data = SuperweaponSystem.GetData("kragmore_industrial_barrage");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.IndustrialBarrage, data!.Type);
        Assert.Equal(SuperweaponCategory.BuildingSuperweapon, data.Category);
        Assert.Equal("kragmore_war_forge", data.RequiredBuildingTypeId);
    }

    [Fact]
    public void IndustrialBarrage_Fires_WideCircularAoE()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "kragmore_industrial_barrage");
        MakeReady(sys, 1);

        var units = new List<SimUnit>
        {
            MakeUnit(10, 2, new FixedVector2(FixedPoint.FromInt(10), FixedPoint.Zero)),  // inside AoE=14
            MakeUnit(11, 2, new FixedVector2(FixedPoint.FromInt(50), FixedPoint.Zero)),  // outside
        };
        var result = sys.TryActivate(1, "kragmore_industrial_barrage", FixedVector2.Zero, units);
        Assert.True(result.DidFire);
        Assert.Contains(10, result.HitUnitIds);
        Assert.DoesNotContain(11, result.HitUnitIds);
    }

    [Fact]
    public void TempestNode_InCatalogue_AsBuildingSuperweapon()
    {
        var data = SuperweaponSystem.GetData("stormrend_tempest_node");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.TempestNode, data!.Type);
        Assert.Equal(SuperweaponCategory.BuildingSuperweapon, data.Category);
        Assert.Equal("stormrend_storm_capacitor", data.RequiredBuildingTypeId);
        Assert.Equal(16, data.ChainCount);
    }

    [Fact]
    public void TempestNode_Fires_ChainLightning_UpTo16Targets()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "stormrend_tempest_node");
        MakeReady(sys, 1);

        // 10 enemies clustered within arc radius of each other
        var units = new List<SimUnit>();
        for (int i = 0; i < 10; i++)
            units.Add(MakeUnit(10 + i, 2, new FixedVector2(FixedPoint.FromInt(i + 1), FixedPoint.Zero)));

        var result = sys.TryActivate(1, "stormrend_tempest_node", FixedVector2.Zero, units);
        Assert.True(result.DidFire);
        Assert.True(result.HitUnitIds.Count >= 2);
        Assert.Equal(result.HitUnitIds.Count, result.HitUnitIds.Distinct().Count()); // no duplicates
    }

    [Fact]
    public void PrecisionPayload_InCatalogue_AsBuildingSuperweapon()
    {
        var data = SuperweaponSystem.GetData("valkyr_precision_payload");
        Assert.NotNull(data);
        Assert.Equal(SuperweaponType.PrecisionPayload, data!.Type);
        Assert.Equal(SuperweaponCategory.BuildingSuperweapon, data.Category);
        Assert.Equal("valkyr_launch_bay", data.RequiredBuildingTypeId);
        Assert.Equal(450, data.Damage.ToInt());
    }

    [Fact]
    public void PrecisionPayload_Fires_SmallAoE_ExtremelyHighDamage()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "valkyr_precision_payload");
        MakeReady(sys, 1);

        var target = FixedVector2.Zero;
        var units = new List<SimUnit>
        {
            MakeUnit(10, 2, new FixedVector2(FixedPoint.FromInt(2), FixedPoint.Zero)), // inside (AoE=4)
            MakeUnit(11, 2, new FixedVector2(FixedPoint.FromInt(20), FixedPoint.Zero)), // outside
        };
        var result = sys.TryActivate(1, "valkyr_precision_payload", target, units);
        Assert.True(result.DidFire);
        Assert.Contains(10, result.HitUnitIds);
        Assert.DoesNotContain(11, result.HitUnitIds);
        // Damage must be 450
        Assert.All(result.DamagePerUnit, d => Assert.Equal(450, d.ToInt()));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Multi-weapon player registration
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Player_CanHoldTwoWeapons()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        sys.RegisterPlayer(1, "arcloft_emp_blast");

        var weapons = sys.GetPlayerWeapons(1).ToList();
        Assert.Equal(2, weapons.Count);
    }

    [Fact]
    public void TwoWeapons_HaveIndependentCooldowns()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "arcloft_orbital_strike");
        sys.RegisterPlayer(1, "arcloft_emp_blast");

        // Tick until the faster weapon is ready (emp_blast = 2700 ticks, orbital = 2700 too — use emp and orbital)
        // Use bastion: missile=2400, reinforcement=2100 — different cooldowns
        var sys2 = new SuperweaponSystem();
        sys2.RegisterPlayer(1, "bastion_reinforcement_drop"); // 2100 ticks
        sys2.RegisterPlayer(1, "bastion_missile_barrage");    // 2400 ticks

        var rdState = sys2.GetState(1, "bastion_reinforcement_drop")!;
        var mbState = sys2.GetState(1, "bastion_missile_barrage")!;

        // Tick 2100 times — reinforcement_drop ready, missile_barrage still not
        for (int i = 0; i < 2100; i++) sys2.Tick();

        Assert.True(rdState.IsReady);
        Assert.False(mbState.IsReady);
    }

    [Fact]
    public void FiringOneWeapon_DoesNotArmOther()
    {
        var sys = new SuperweaponSystem();
        sys.RegisterPlayer(1, "bastion_reinforcement_drop"); // 2100
        sys.RegisterPlayer(1, "bastion_missile_barrage");    // 2400

        // Make reinforcement_drop ready
        var rdState = sys.GetState(1, "bastion_reinforcement_drop")!;
        for (int i = 0; i < rdState.CooldownRemaining; i++) sys.Tick();

        // Fire reinforcement_drop
        sys.TryActivate(1, "bastion_reinforcement_drop", FixedVector2.Zero, new List<SimUnit>());

        // reinforcement_drop should be back on cooldown; missile_barrage independent
        Assert.False(rdState.IsReady);
    }
}
