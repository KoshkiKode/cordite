using System.Collections.Generic;
using CorditeWars.Core;

namespace CorditeWars.Systems.Superweapon;

/// <summary>
/// Broad classification for a superweapon ability.
/// </summary>
public enum SuperweaponCategory
{
    /// <summary>
    /// Activated directly from the HUD ability panel — no building prerequisite.
    /// Equivalent to a General's Power in classic RTS games.
    /// </summary>
    ActivatedAbility,

    /// <summary>
    /// Requires the player to construct <see cref="SuperweaponData.RequiredBuildingTypeId"/>
    /// before the ability can be activated. The weapon recharges as long as the building stands.
    /// Equivalent to a superweapon structure (Particle Cannon, Scud Storm, etc.).
    /// </summary>
    BuildingSuperweapon
}

/// <summary>
/// Type of strategic ability (superweapon). Determines the area-effect,
/// damage, and visual applied when the ability fires.
/// </summary>
public enum SuperweaponType
{
    // ── Arcloft ─────────────────────────────────────────────────────────
    /// <summary>Arcloft (Building): high-velocity kinetic penetrator dropped from orbit via Orbital Relay.</summary>
    OrbitalStrike,
    /// <summary>Arcloft (Activated): electromagnetic pulse that disables all enemy units in a wide radius.</summary>
    EMPBlast,

    // ── Bastion ──────────────────────────────────────────────────────────
    /// <summary>Bastion (Building): rapid multi-missile barrage from the Missile Battery.</summary>
    MissileBarrage,
    /// <summary>Bastion (Activated): airdrop of reinforcement infantry at the target location.</summary>
    ReinforcementDrop,

    // ── Generic ──────────────────────────────────────────────────────────
    /// <summary>Generic fallback — high-damage area explosion.</summary>
    Airstrike,

    // ── Ironmarch ────────────────────────────────────────────────────────
    /// <summary>Ironmarch (Activated): engineer teams detonate buried seismic charges.</summary>
    SeismicCharge,
    /// <summary>Ironmarch (Building): Siege Works fires a hypersonic kinetic volley at the target zone.</summary>
    SiegeVolley,

    // ── Kragmore ─────────────────────────────────────────────────────────
    /// <summary>Kragmore (Activated): off-map heavy artillery fires a rolling barrage.</summary>
    ArtillerySalvo,
    /// <summary>Kragmore (Building): War Forge unleashes industrial-scale mortar bombardment.</summary>
    IndustrialBarrage,

    // ── Stormrend ─────────────────────────────────────────────────────────
    /// <summary>Stormrend (Activated): lightning bolt that chains between up to 8 nearby enemies.</summary>
    LightningCascade,
    /// <summary>Stormrend (Building): Storm Capacitor erupts in a massive multi-arc discharge (up to 16 targets).</summary>
    TempestNode,

    // ── Valkyr ───────────────────────────────────────────────────────────
    /// <summary>Valkyr (Activated): bomber squadron strafing run along a long linear strip.</summary>
    CarpetBombRun,
    /// <summary>Valkyr (Building): Launch Bay releases a single precision-guided munition with extreme damage.</summary>
    PrecisionPayload
}

/// <summary>
/// Immutable data that describes a superweapon ability (cooldown, range, effect).
/// </summary>
public sealed class SuperweaponData
{
    public string Id          { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string FactionId   { get; init; } = string.Empty;
    public SuperweaponType Type { get; init; }

    /// <summary>Whether this is a HUD general power or a structure-based superweapon.</summary>
    public SuperweaponCategory Category { get; init; }

    /// <summary>
    /// For <see cref="SuperweaponCategory.BuildingSuperweapon"/>: the building type that must
    /// be constructed before this weapon can be activated. Empty for activated abilities.
    /// </summary>
    public string RequiredBuildingTypeId { get; init; } = string.Empty;

    /// <summary>Cooldown in simulation ticks (30 ticks = 1 s).</summary>
    public int CooldownTicks  { get; init; }

    /// <summary>Maximum range from any owned building, in grid cells.</summary>
    public FixedPoint Range   { get; init; }

    /// <summary>Radius of the area effect, in grid cells.</summary>
    public FixedPoint AreaOfEffect { get; init; }

    /// <summary>Base damage applied to each unit in the area.</summary>
    public FixedPoint Damage  { get; init; }

    /// <summary>
    /// Maximum number of chain-arc targets for <see cref="SuperweaponType.LightningCascade"/>
    /// and <see cref="SuperweaponType.TempestNode"/>. 0 = unused.
    /// </summary>
    public int ChainCount { get; init; }

    /// <summary>
    /// Half-width of the bomb strip for <see cref="SuperweaponType.CarpetBombRun"/>, in grid cells.
    /// 0 = unused.
    /// </summary>
    public FixedPoint StripHalfWidth { get; init; }

    /// <summary>
    /// Length of the bomb strip for <see cref="SuperweaponType.CarpetBombRun"/>, in grid cells.
    /// 0 = unused.
    /// </summary>
    public FixedPoint StripLength { get; init; }
}

/// <summary>
/// Per-player runtime state for a superweapon: cooldown countdown and ready status.
/// </summary>
public sealed class PlayerSuperweaponState
{
    public int PlayerId          { get; init; }
    public SuperweaponData Data  { get; }
    public int CooldownRemaining { get; private set; }

    /// <summary>True when the weapon is ready to fire.</summary>
    public bool IsReady => CooldownRemaining <= 0;

    public PlayerSuperweaponState(int playerId, SuperweaponData data)
    {
        PlayerId = playerId;
        Data = data;
        // Start on full cooldown so there's no immediate firing at match start
        CooldownRemaining = data.CooldownTicks;
    }

    /// <summary>Decrements the cooldown by one tick.</summary>
    public void Tick()
    {
        if (CooldownRemaining > 0)
            CooldownRemaining--;
    }

    /// <summary>Arms the cooldown after the weapon fires.</summary>
    public void Arm() => CooldownRemaining = Data.CooldownTicks;

    /// <summary>
    /// Fraction of cooldown completed [0.0 – 1.0]. Used for HUD progress bars.
    /// </summary>
    public float ChargePercent => Data.CooldownTicks > 0
        ? 1f - ((float)CooldownRemaining / Data.CooldownTicks)
        : 1f;
}

/// <summary>
/// Result of a superweapon firing: which units were hit, how much damage, any
/// secondary effects (EMP duration, unit spawns, etc.).
/// </summary>
public sealed class SuperweaponResult
{
    /// <summary>Unit IDs that took direct damage.</summary>
    public List<int> HitUnitIds { get; init; } = new();

    /// <summary>Damage dealt to each hit unit (parallel to HitUnitIds).</summary>
    public List<FixedPoint> DamagePerUnit { get; init; } = new();

    /// <summary>True if an EMP effect was applied.</summary>
    public bool IsEMP { get; init; }

    /// <summary>Duration of EMP stun in simulation ticks (0 if not EMP).</summary>
    public int EMPDurationTicks { get; init; }

    /// <summary>Unit type IDs spawned by ReinforcementDrop (empty otherwise).</summary>
    public List<string> SpawnedUnitTypeIds { get; init; } = new();

    /// <summary>The world position targeted.</summary>
    public FixedVector2 TargetPosition { get; init; }

    /// <summary>True if the weapon was actually fired (cooldown was ready).</summary>
    public bool DidFire { get; init; }

    /// <summary>The weapon ID that fired (for event routing).</summary>
    public string WeaponId { get; init; } = string.Empty;
}

/// <summary>
/// Manages all superweapon states for all players. Each player may hold multiple
/// weapons (one BuildingSuperweapon + one ActivatedAbility). Processes cooldown
/// ticks and resolves ability activations.
///
/// Deterministic: uses FixedPoint math only. No System.Random.
/// </summary>
public sealed class SuperweaponSystem
{
    // player id → weapon id → weapon state
    private readonly Dictionary<int, Dictionary<string, PlayerSuperweaponState>> _states = new();

    // ── Built-in ability catalogue ───────────────────────────────────

    private static readonly Dictionary<string, SuperweaponData> _catalogue = new()
    {
        ["arcloft_orbital_strike"] = new SuperweaponData
        {
            Id = "arcloft_orbital_strike",
            DisplayName = "Orbital Strike",
            Description = "The Orbital Relay fires a high-velocity kinetic penetrator from orbit. Devastates a small target zone.",
            FactionId = "arcloft",
            Type = SuperweaponType.OrbitalStrike,
            Category = SuperweaponCategory.BuildingSuperweapon,
            RequiredBuildingTypeId = "arcloft_orbital_relay",
            CooldownTicks = 2700, // 90 s — building superweapons charge slower
            Range          = FixedPoint.FromInt(80),
            AreaOfEffect   = FixedPoint.FromInt(5),
            Damage         = FixedPoint.FromInt(350)
        },
        ["arcloft_emp_blast"] = new SuperweaponData
        {
            Id = "arcloft_emp_blast",
            DisplayName = "EMP Blast",
            Description = "HQ-directed electromagnetic pulse disables all enemy units in a wide radius for 5 seconds.",
            FactionId = "arcloft",
            Type = SuperweaponType.EMPBlast,
            Category = SuperweaponCategory.ActivatedAbility,
            CooldownTicks = 2700, // 90 s
            Range          = FixedPoint.FromInt(100),
            AreaOfEffect   = FixedPoint.FromInt(12),
            Damage         = FixedPoint.FromInt(20)
        },
        ["bastion_missile_barrage"] = new SuperweaponData
        {
            Id = "bastion_missile_barrage",
            DisplayName = "Missile Barrage",
            Description = "The Missile Battery launches saturation bombardment with cluster munitions over a large target area.",
            FactionId = "bastion",
            Type = SuperweaponType.MissileBarrage,
            Category = SuperweaponCategory.BuildingSuperweapon,
            RequiredBuildingTypeId = "bastion_missile_battery",
            CooldownTicks = 2400, // 80 s
            Range          = FixedPoint.FromInt(90),
            AreaOfEffect   = FixedPoint.FromInt(10),
            Damage         = FixedPoint.FromInt(130)
        },
        ["bastion_reinforcement_drop"] = new SuperweaponData
        {
            Id = "bastion_reinforcement_drop",
            DisplayName = "Reinforcement Drop",
            Description = "Airdrop of four elite infantry squads at the target location. No building required.",
            FactionId = "bastion",
            Type = SuperweaponType.ReinforcementDrop,
            Category = SuperweaponCategory.ActivatedAbility,
            CooldownTicks = 2100, // 70 s
            Range          = FixedPoint.FromInt(100),
            AreaOfEffect   = FixedPoint.Zero,
            Damage         = FixedPoint.Zero
        },

        // ── Ironmarch ────────────────────────────────────────────────────────
        ["ironmarch_seismic_charge"] = new SuperweaponData
        {
            Id = "ironmarch_seismic_charge",
            DisplayName = "Seismic Charge",
            Description = "Engineer teams detonate buried charges beneath the target. Devastating ground-shaking explosion " +
                          "that flattens everything in a wide radius.",
            FactionId = "ironmarch",
            Type = SuperweaponType.SeismicCharge,
            Category = SuperweaponCategory.ActivatedAbility,
            CooldownTicks = 2400, // 80 s
            Range          = FixedPoint.FromInt(85),
            AreaOfEffect   = FixedPoint.FromInt(9),
            Damage         = FixedPoint.FromInt(250)
        },
        ["ironmarch_siege_volley"] = new SuperweaponData
        {
            Id = "ironmarch_siege_volley",
            DisplayName = "Siege Volley",
            Description = "The Siege Works unleashes a hypersonic kinetic volley at the designated target zone. " +
                          "Extreme single-strike damage against fortified positions.",
            FactionId = "ironmarch",
            Type = SuperweaponType.SiegeVolley,
            Category = SuperweaponCategory.BuildingSuperweapon,
            RequiredBuildingTypeId = "ironmarch_siege_works",
            CooldownTicks = 3000, // 100 s
            Range          = FixedPoint.FromInt(75),
            AreaOfEffect   = FixedPoint.FromInt(6),
            Damage         = FixedPoint.FromInt(400)
        },

        // ── Kragmore ─────────────────────────────────────────────────────────
        ["kragmore_artillery_salvo"] = new SuperweaponData
        {
            Id = "kragmore_artillery_salvo",
            DisplayName = "Artillery Salvo",
            Description = "Off-map heavy artillery fires a rolling barrage over a large area, shredding armor and infantry alike.",
            FactionId = "kragmore",
            Type = SuperweaponType.ArtillerySalvo,
            Category = SuperweaponCategory.ActivatedAbility,
            CooldownTicks = 1650, // 55 s
            Range          = FixedPoint.FromInt(95),
            AreaOfEffect   = FixedPoint.FromInt(11),
            Damage         = FixedPoint.FromInt(140)
        },
        ["kragmore_industrial_barrage"] = new SuperweaponData
        {
            Id = "kragmore_industrial_barrage",
            DisplayName = "Industrial Barrage",
            Description = "The War Forge fires a continuous industrial-scale mortar bombardment over an enormous area, " +
                          "overwhelming any defender with sheer volume of fire.",
            FactionId = "kragmore",
            Type = SuperweaponType.IndustrialBarrage,
            Category = SuperweaponCategory.BuildingSuperweapon,
            RequiredBuildingTypeId = "kragmore_war_forge",
            CooldownTicks = 2700, // 90 s
            Range          = FixedPoint.FromInt(90),
            AreaOfEffect   = FixedPoint.FromInt(14),
            Damage         = FixedPoint.FromInt(110)
        },

        // ── Stormrend ────────────────────────────────────────────────────────
        ["stormrend_lightning_cascade"] = new SuperweaponData
        {
            Id = "stormrend_lightning_cascade",
            DisplayName = "Lightning Cascade",
            Description = "A focused lightning bolt strikes the target, then arcs between up to 8 nearby enemies " +
                          "for full damage — lethal against clustered formations.",
            FactionId = "stormrend",
            Type = SuperweaponType.LightningCascade,
            Category = SuperweaponCategory.ActivatedAbility,
            CooldownTicks = 1350, // 45 s
            Range          = FixedPoint.FromInt(90),
            AreaOfEffect   = FixedPoint.FromInt(7),
            Damage         = FixedPoint.FromInt(200),
            ChainCount     = 8
        },
        ["stormrend_tempest_node"] = new SuperweaponData
        {
            Id = "stormrend_tempest_node",
            DisplayName = "Tempest Node",
            Description = "The Storm Capacitor erupts in a massive multi-arc discharge, chaining between up to 16 enemies " +
                          "across a wider radius — capable of wiping entire armies.",
            FactionId = "stormrend",
            Type = SuperweaponType.TempestNode,
            Category = SuperweaponCategory.BuildingSuperweapon,
            RequiredBuildingTypeId = "stormrend_storm_capacitor",
            CooldownTicks = 3300, // 110 s
            Range          = FixedPoint.FromInt(95),
            AreaOfEffect   = FixedPoint.FromInt(10),
            Damage         = FixedPoint.FromInt(180),
            ChainCount     = 16
        },

        // ── Valkyr ───────────────────────────────────────────────────────────
        ["valkyr_carpet_bomb_run"] = new SuperweaponData
        {
            Id = "valkyr_carpet_bomb_run",
            DisplayName = "Carpet Bomb Run",
            Description = "A Valkyr bomber squadron makes a strafing run over the target zone, laying a 24-cell-long " +
                          "strip of high-explosive ordnance.",
            FactionId = "valkyr",
            Type = SuperweaponType.CarpetBombRun,
            Category = SuperweaponCategory.ActivatedAbility,
            CooldownTicks = 1800, // 60 s
            Range          = FixedPoint.FromInt(100),
            AreaOfEffect   = FixedPoint.FromInt(4),
            Damage         = FixedPoint.FromInt(160),
            StripHalfWidth = FixedPoint.FromInt(4),
            StripLength    = FixedPoint.FromInt(24)
        },
        ["valkyr_precision_payload"] = new SuperweaponData
        {
            Id = "valkyr_precision_payload",
            DisplayName = "Precision Payload",
            Description = "The Sky Launch Bay releases a single precision-guided munition from high altitude. " +
                          "Extreme single-strike damage against a pinpoint target.",
            FactionId = "valkyr",
            Type = SuperweaponType.PrecisionPayload,
            Category = SuperweaponCategory.BuildingSuperweapon,
            RequiredBuildingTypeId = "valkyr_launch_bay",
            CooldownTicks = 3000, // 100 s
            Range          = FixedPoint.FromInt(100),
            AreaOfEffect   = FixedPoint.FromInt(4),
            Damage         = FixedPoint.FromInt(450)
        }
    };

    /// <summary>Returns the catalogue entry for a superweapon by id, or null.</summary>
    public static SuperweaponData? GetData(string id)
        => _catalogue.TryGetValue(id, out var d) ? d : null;

    /// <summary>Returns all catalogue entries for a faction.</summary>
    public static IEnumerable<SuperweaponData> GetFactionWeapons(string factionId)
    {
        foreach (var kv in _catalogue)
        {
            if (kv.Value.FactionId == factionId)
                yield return kv.Value;
        }
    }

    /// <summary>
    /// Returns the default (first registered) superweapon ID for a faction, or an empty
    /// string if the faction has no entry in the catalogue. This eliminates hardcoded
    /// faction→weapon switches in callers such as <see cref="GameSession"/>.
    /// </summary>
    public static string GetDefaultWeaponForFaction(string factionId)
    {
        foreach (var kv in _catalogue)
        {
            if (kv.Value.FactionId == factionId)
                return kv.Key;
        }
        return string.Empty;
    }

    /// <summary>Returns the first <see cref="SuperweaponCategory.BuildingSuperweapon"/> entry for a faction, or null.</summary>
    public static SuperweaponData? GetFactionBuildingWeapon(string factionId)
    {
        foreach (var kv in _catalogue)
        {
            if (kv.Value.FactionId == factionId &&
                kv.Value.Category  == SuperweaponCategory.BuildingSuperweapon)
                return kv.Value;
        }
        return null;
    }

    /// <summary>Returns the first <see cref="SuperweaponCategory.ActivatedAbility"/> entry for a faction, or null.</summary>
    public static SuperweaponData? GetFactionActivatedAbility(string factionId)
    {
        foreach (var kv in _catalogue)
        {
            if (kv.Value.FactionId == factionId &&
                kv.Value.Category  == SuperweaponCategory.ActivatedAbility)
                return kv.Value;
        }
        return null;
    }

    /// <summary>
    /// A large-but-safe squared-distance sentinel for "not yet found" comparisons.
    /// Represents 10 000 cells² (distance 100 cells) — well beyond any map diagonal
    /// and safe from FixedPoint overflow in comparisons (not arithmetic).
    /// </summary>
    private static readonly FixedPoint _distanceSentinel = FixedPoint.FromInt(10_000);

    // ── Player Registration ──────────────────────────────────────────

    /// <summary>
    /// Registers a weapon for a player. Each player may hold multiple weapons
    /// (one BuildingSuperweapon + one ActivatedAbility). Registering the same
    /// weaponId again replaces the previous state for that weapon.
    /// </summary>
    public void RegisterPlayer(int playerId, string weaponId)
    {
        if (!_catalogue.TryGetValue(weaponId, out var data)) return;
        if (!_states.TryGetValue(playerId, out var weapons))
            _states[playerId] = weapons = new Dictionary<string, PlayerSuperweaponState>();
        weapons[weaponId] = new PlayerSuperweaponState(playerId, data);
    }

    // ── Tick ─────────────────────────────────────────────────────────

    /// <summary>
    /// Decrements all player cooldowns by one tick.
    /// Call once per simulation tick.
    /// </summary>
    public void Tick()
    {
        foreach (var weapons in _states.Values)
            foreach (var state in weapons.Values)
                state.Tick();
    }

    // ── Activation ───────────────────────────────────────────────────

    /// <summary>
    /// Attempts to fire a specific superweapon for <paramref name="playerId"/> targeting
    /// <paramref name="target"/>. Returns a <see cref="SuperweaponResult"/> with
    /// hit details, or a non-fired result if the weapon is on cooldown or not registered.
    ///
    /// Callers are responsible for applying damage from the result.
    /// </summary>
    public SuperweaponResult TryActivate(
        int playerId,
        string weaponId,
        FixedVector2 target,
        IReadOnlyList<CorditeWars.Systems.Pathfinding.SimUnit> allUnits)
    {
        if (!_states.TryGetValue(playerId, out var weapons) ||
            !weapons.TryGetValue(weaponId, out var state) ||
            !state.IsReady)
            return new SuperweaponResult { TargetPosition = target, DidFire = false, WeaponId = weaponId };

        state.Arm();

        var result = new SuperweaponResult
        {
            TargetPosition   = target,
            DidFire          = true,
            WeaponId         = weaponId,
            IsEMP            = state.Data.Type == SuperweaponType.EMPBlast,
            EMPDurationTicks = state.Data.Type == SuperweaponType.EMPBlast ? 150 : 0 // 5 s
        };

        switch (state.Data.Type)
        {
            case SuperweaponType.ReinforcementDrop:
                for (int i = 0; i < 4; i++)
                    result.SpawnedUnitTypeIds.Add("bastion_soldier");
                return result;

            case SuperweaponType.LightningCascade:
            case SuperweaponType.TempestNode:
                return ResolveLightningChain(playerId, target, allUnits, state.Data, result);

            case SuperweaponType.CarpetBombRun:
                return ResolveCarpetBombRun(playerId, target, allUnits, state.Data, result);

            default:
                // Circular AoE: OrbitalStrike, EMPBlast, MissileBarrage, SeismicCharge,
                //               ArtillerySalvo, SiegeVolley, IndustrialBarrage,
                //               PrecisionPayload, Airstrike
                return ResolveCircularAoE(playerId, target, allUnits, state.Data, result);
        }
    }

    // ── Special activation resolvers ─────────────────────────────────

    private static SuperweaponResult ResolveCircularAoE(
        int playerId,
        FixedVector2 target,
        IReadOnlyList<CorditeWars.Systems.Pathfinding.SimUnit> allUnits,
        SuperweaponData data,
        SuperweaponResult result)
    {
        FixedPoint aoeSq = data.AreaOfEffect * data.AreaOfEffect;
        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            if (!unit.IsAlive || unit.PlayerId == playerId) continue;

            FixedVector2 diff = unit.Movement.Position - target;
            FixedPoint distSq = diff.X * diff.X + diff.Y * diff.Y;
            if (distSq > aoeSq) continue;

            result.HitUnitIds.Add(unit.UnitId);
            result.DamagePerUnit.Add(data.Damage);
        }
        return result;
    }

    /// <summary>
    /// Resolves a chain-lightning strike (LightningCascade or TempestNode): strikes the closest
    /// enemy to the target, then chains up to <see cref="SuperweaponData.ChainCount"/> times to the
    /// nearest remaining enemy within <see cref="SuperweaponData.AreaOfEffect"/> of
    /// each struck unit. Each hit unit takes full Damage; no unit is hit twice.
    /// </summary>
    private static SuperweaponResult ResolveLightningChain(
        int playerId,
        FixedVector2 target,
        IReadOnlyList<CorditeWars.Systems.Pathfinding.SimUnit> allUnits,
        SuperweaponData data,
        SuperweaponResult result)
    {
        int maxChains = data.ChainCount > 0 ? data.ChainCount : 8;
        FixedPoint arcRadiusSq = data.AreaOfEffect * data.AreaOfEffect;

        var hitSet = new System.Collections.Generic.HashSet<int>();

        // Find closest enemy to target as first strike
        int firstId = -1;
        FixedPoint bestDistSq = _distanceSentinel;
        for (int i = 0; i < allUnits.Count; i++)
        {
            var u = allUnits[i];
            if (!u.IsAlive || u.PlayerId == playerId) continue;
            FixedVector2 d2 = u.Movement.Position - target;
            FixedPoint dSq = d2.X * d2.X + d2.Y * d2.Y;
            if (dSq < bestDistSq) { bestDistSq = dSq; firstId = u.UnitId; }
        }

        if (firstId < 0) return result; // no enemies in range

        // Build a lookup for fast position queries
        var posLookup = new System.Collections.Generic.Dictionary<int, FixedVector2>();
        for (int i = 0; i < allUnits.Count; i++)
            if (allUnits[i].IsAlive && allUnits[i].PlayerId != playerId)
                posLookup[allUnits[i].UnitId] = allUnits[i].Movement.Position;

        int current = firstId;
        for (int chain = 0; chain <= maxChains; chain++)
        {
            if (!posLookup.ContainsKey(current)) break;

            hitSet.Add(current);
            result.HitUnitIds.Add(current);
            result.DamagePerUnit.Add(data.Damage);

            // Find nearest un-hit enemy within arc radius of current unit
            FixedVector2 currentPos = posLookup[current];
            int nextId = -1;
            FixedPoint nextDistSq = arcRadiusSq + FixedPoint.One; // just beyond radius

            foreach (var kv in posLookup)
            {
                if (hitSet.Contains(kv.Key)) continue;
                FixedVector2 dv = kv.Value - currentPos;
                FixedPoint dsq = dv.X * dv.X + dv.Y * dv.Y;
                if (dsq <= arcRadiusSq && dsq < nextDistSq)
                {
                    nextDistSq = dsq;
                    nextId = kv.Key;
                }
            }

            if (nextId < 0) break; // no more nearby targets
            current = nextId;
        }

        return result;
    }

    /// <summary>
    /// Resolves a CarpetBombRun: hits all enemies in a rectangular strip centred
    /// on <paramref name="target"/>. The strip runs along the X-axis with half-width
    /// <see cref="SuperweaponData.StripHalfWidth"/> and length
    /// <see cref="SuperweaponData.StripLength"/>.
    /// </summary>
    private static SuperweaponResult ResolveCarpetBombRun(
        int playerId,
        FixedVector2 target,
        IReadOnlyList<CorditeWars.Systems.Pathfinding.SimUnit> allUnits,
        SuperweaponData data,
        SuperweaponResult result)
    {
        FixedPoint halfLen    = data.StripLength    > FixedPoint.Zero ? data.StripLength    / FixedPoint.FromInt(2) : FixedPoint.FromInt(12);
        FixedPoint halfWidth  = data.StripHalfWidth > FixedPoint.Zero ? data.StripHalfWidth : FixedPoint.FromInt(4);

        for (int i = 0; i < allUnits.Count; i++)
        {
            var unit = allUnits[i];
            if (!unit.IsAlive || unit.PlayerId == playerId) continue;

            FixedVector2 diff = unit.Movement.Position - target;
            // Strip is axis-aligned along X; |dx| <= halfLen, |dz| <= halfWidth
            if (diff.X < -halfLen || diff.X > halfLen) continue;
            if (diff.Y < -halfWidth || diff.Y > halfWidth) continue;

            result.HitUnitIds.Add(unit.UnitId);
            result.DamagePerUnit.Add(data.Damage);
        }

        return result;
    }

    // ── Queries ──────────────────────────────────────────────────────

    /// <summary>Returns the state for a specific weapon of a player, or null.</summary>
    public PlayerSuperweaponState? GetState(int playerId, string weaponId)
    {
        if (!_states.TryGetValue(playerId, out var weapons)) return null;
        return weapons.TryGetValue(weaponId, out var s) ? s : null;
    }

    /// <summary>Returns all weapon states for a player (may be 0, 1, or 2).</summary>
    public IEnumerable<PlayerSuperweaponState> GetPlayerWeapons(int playerId)
    {
        if (!_states.TryGetValue(playerId, out var weapons)) yield break;
        foreach (var state in weapons.Values) yield return state;
    }

    /// <summary>Returns true if the specified weapon is ready to fire.</summary>
    public bool IsReady(int playerId, string weaponId)
    {
        if (!_states.TryGetValue(playerId, out var weapons)) return false;
        return weapons.TryGetValue(weaponId, out var s) && s.IsReady;
    }

    /// <summary>Returns true if any of the player's weapons is ready (used for tick-ready notifications).</summary>
    public bool IsAnyReady(int playerId)
    {
        if (!_states.TryGetValue(playerId, out var weapons)) return false;
        foreach (var s in weapons.Values)
            if (s.IsReady) return true;
        return false;
    }

    /// <summary>Returns the charge percentage [0,1] for a specific weapon.</summary>
    public float GetChargePercent(int playerId, string weaponId)
    {
        if (!_states.TryGetValue(playerId, out var weapons)) return 0f;
        return weapons.TryGetValue(weaponId, out var s) ? s.ChargePercent : 0f;
    }

    public IReadOnlyDictionary<int, Dictionary<string, PlayerSuperweaponState>> AllStates => _states;
}
