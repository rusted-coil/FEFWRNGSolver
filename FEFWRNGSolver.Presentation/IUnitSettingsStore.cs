using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

public sealed record UnitSettings(string ClassId, string AnimalId, int Affection, GrowthSkillBonus SkillBonus, uint? GrowthSeed);

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
