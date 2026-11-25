using System.Collections.Immutable;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

// null はユニット設定の継承。None と空の動物IDはそれぞれ補正無し・未配備の明示指定。
public sealed record PredictionClassSettings(GrowthSkillBonus? SkillBonus = null, string? AnimalId = null,
    int Affection = AnimalGrowthData.MaxAffection);

public sealed record UnitSettings(string ClassId, string AnimalId, int Affection, GrowthSkillBonus SkillBonus, uint? GrowthSeed,
    bool IsPartThree = false)
{
    public bool ConsiderClassChanges { get; init; }
    public ImmutableHashSet<string> PredictionClassIds { get; init; } = [];
    public ImmutableDictionary<string, PredictionClassSettings> PredictionSettings { get; init; } =
        ImmutableDictionary<string, PredictionClassSettings>.Empty;
}

public interface IUnitSettingsStore
{
    UnitSettings? Load(string unitId);
    void Save(string unitId, UnitSettings settings);
}

internal sealed class MemoryUnitSettingsStore : IUnitSettingsStore
{
    private readonly Dictionary<string, UnitSettings> m_Settings = new();

    public UnitSettings? Load(string unitId) => m_Settings.GetValueOrDefault(unitId);
    public void Save(string unitId, UnitSettings settings) => m_Settings[unitId] = settings;
}
