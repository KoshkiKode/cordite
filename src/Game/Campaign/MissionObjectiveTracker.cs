using System.Collections.Generic;
using CorditeWars.Core;
using CorditeWars.Game.Buildings;
using CorditeWars.Game.Units;

namespace CorditeWars.Game.Campaign;

public enum ObjectiveType
{
    BuildBuilding,
    MaintainUnitType,
    SurviveTimer,
    DestroyBuildingType,
    /// <summary>Destroy a specified number of enemy units of the given type.</summary>
    DestroyUnitType,
    AccumulateCordite,
    /// <summary>A specific unit type must remain alive (count &gt;= 1). Fails if 0 remain.</summary>
    EscortUnit,
    /// <summary>Survive for the specified number of ticks without all units being destroyed.</summary>
    DefendPosition,
    /// <summary>Complete after surviving for the specified number of ticks (same as SurviveTimer).</summary>
    ReachLocation
}

public sealed class TypedObjective
{
    public ObjectiveType Type       { get; set; }
    public string        Label      { get; set; } = string.Empty;
    public string        TargetId   { get; set; } = string.Empty;
    public int           Count      { get; set; } = 1;
    public int           Ticks      { get; set; }
    public bool          IsComplete { get; set; }
    public bool          IsFailed   { get; set; }
    public bool          Required   { get; set; } = true;
    internal int _destroyedCount;
}

public sealed class MissionSessionContext
{
    public IList<BuildingInstance> AllBuildings  { get; set; } = new List<BuildingInstance>();
    public IList<UnitNode3D>       AliveUnits    { get; set; } = new List<UnitNode3D>();
    public FixedPoint              PlayerCordite { get; set; }
}

public sealed class MissionObjectiveTracker
{
    private readonly List<TypedObjective> _objectives = new();
    private ulong _startTick;

    public IReadOnlyList<TypedObjective> Objectives => _objectives;

    public bool AllPrimaryObjectivesComplete
    {
        get
        {
            for (int i = 0; i < _objectives.Count; i++)
                if (_objectives[i].Required && !_objectives[i].IsComplete)
                    return false;
            return _objectives.Count > 0;
        }
    }

    public bool AnyObjectiveFailed
    {
        get
        {
            for (int i = 0; i < _objectives.Count; i++)
                if (_objectives[i].IsFailed)
                    return true;
            return false;
        }
    }

    public void Initialize(List<TypedObjective> objectives, ulong startTick)
    {
        _objectives.Clear();
        _objectives.AddRange(objectives);
        _startTick = startTick;
    }

    public void NotifyBuildingDestroyed(string buildingTypeId)
    {
        for (int i = 0; i < _objectives.Count; i++)
        {
            var obj = _objectives[i];
            if (obj.Type == ObjectiveType.DestroyBuildingType &&
                obj.TargetId == buildingTypeId && !obj.IsComplete)
            {
                obj._destroyedCount++;
                if (obj._destroyedCount >= obj.Count)
                    obj.IsComplete = true;
            }
        }
    }

    /// <summary>
    /// Called whenever an enemy unit is destroyed. Advances DestroyUnitType objectives.
    /// </summary>
    public void NotifyUnitDestroyed(string unitTypeId)
    {
        for (int i = 0; i < _objectives.Count; i++)
        {
            var obj = _objectives[i];
            if (obj.Type == ObjectiveType.DestroyUnitType &&
                obj.TargetId == unitTypeId && !obj.IsComplete)
            {
                obj._destroyedCount++;
                if (obj._destroyedCount >= obj.Count)
                    obj.IsComplete = true;
            }
        }
    }

    public void Tick(int playerId, MissionSessionContext ctx, ulong currentTick)
    {
        for (int i = 0; i < _objectives.Count; i++)
        {
            var obj = _objectives[i];
            if (obj.IsComplete || obj.IsFailed) continue;

            switch (obj.Type)
            {
                case ObjectiveType.BuildBuilding:
                {
                    int count = 0;
                    for (int b = 0; b < ctx.AllBuildings.Count; b++)
                    {
                        var bldg = ctx.AllBuildings[b];
                        if (bldg.PlayerId == playerId &&
                            bldg.BuildingTypeId == obj.TargetId &&
                            bldg.IsConstructed)
                            count++;
                    }
                    if (count >= obj.Count)
                        obj.IsComplete = true;
                    break;
                }
                case ObjectiveType.MaintainUnitType:
                {
                    int count = 0;
                    // Check mobile units
                    for (int u = 0; u < ctx.AliveUnits.Count; u++)
                    {
                        var unit = ctx.AliveUnits[u];
                        if (unit.PlayerId == playerId && unit.UnitTypeId == obj.TargetId)
                            count++;
                    }
                    // Also check buildings (e.g. "keep command_center alive")
                    if (count < obj.Count)
                    {
                        for (int b = 0; b < ctx.AllBuildings.Count; b++)
                        {
                            var bldg = ctx.AllBuildings[b];
                            if (bldg.PlayerId == playerId && bldg.BuildingTypeId == obj.TargetId &&
                                bldg.Health > FixedPoint.Zero)
                                count++;
                        }
                    }
                    if (count >= obj.Count)
                        obj.IsComplete = true;
                    break;
                }
                case ObjectiveType.SurviveTimer:
                {
                    if (currentTick - _startTick >= (ulong)obj.Ticks)
                        obj.IsComplete = true;
                    break;
                }
                case ObjectiveType.AccumulateCordite:
                {
                    if (ctx.PlayerCordite >= FixedPoint.FromInt(obj.Count))
                        obj.IsComplete = true;
                    break;
                }
                // DestroyBuildingType is handled via NotifyBuildingDestroyed
                case ObjectiveType.EscortUnit:
                {
                    int count = 0;
                    for (int u = 0; u < ctx.AliveUnits.Count; u++)
                    {
                        var unit = ctx.AliveUnits[u];
                        if (unit.PlayerId == playerId && unit.UnitTypeId == obj.TargetId)
                            count++;
                    }
                    if (count >= obj.Count)
                        obj.IsComplete = true;
                    else if (count == 0 && obj.Required)
                        obj.IsFailed = true;
                    break;
                }
                case ObjectiveType.DefendPosition:
                case ObjectiveType.ReachLocation:
                {
                    // Implemented as survive-for-N-ticks
                    if (currentTick - _startTick >= (ulong)obj.Ticks)
                        obj.IsComplete = true;
                    break;
                }
            }
        }
    }
}
