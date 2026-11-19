using System.Collections.Immutable;
using System.Globalization;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

public sealed record GrowthOption(string Id, string Name);

public sealed class GrowthSetupViewModel
{
    private static readonly GrowthOption m_Unassigned = new("", "配備なし");
    private readonly GrowthRateMasterData m_Master = GrowthRateMasterData.Default;
    private string m_UnitId = "";
    private string m_ClassId = "";
    private string m_AnimalId = "";
    private int m_Affection = AnimalGrowthData.MaxAffection;

    internal GrowthSetupViewModel()
    {
        var japanese = StringComparer.Create(CultureInfo.GetCultureInfo("ja-JP"), ignoreCase: false);
        var sortedUnits = m_Master.UnitRoster.OrderBy(unit => unit.Name, japanese);
        Units = [new("", "手入力"), .. sortedUnits.Select(unit => new GrowthOption(unit.Id, unit.Name))];
    }

    public IReadOnlyList<GrowthOption> Units { get; }
    public IReadOnlyList<GrowthOption> Classes { get; private set; } = [];
    public IReadOnlyList<GrowthOption> Animals { get; private set; } = [m_Unassigned];
    public static IReadOnlyList<int> AffectionLevels { get; } = [.. Enumerable.Range(0, AnimalGrowthData.MaxAffection + 1)];
    public bool IsManual => m_UnitId.Length == 0;
    public bool HasAnimal => m_AnimalId.Length > 0;

    public string UnitId
    {
        get => m_UnitId;
        set
        {
            if (m_UnitId == value)
                return;
            var classes = value.Length == 0 ? [] : m_Master.GetClassesFor(m_Master.Units[value])
                .Select(job => new GrowthOption(job.Id, job.Name)).ToArray();
            m_UnitId = value;
            Classes = classes;
            if (!Classes.Any(job => job.Id == ClassId))
                ClassId = classes.FirstOrDefault()?.Id ?? "";
        }
    }

    public string ClassId
    {
        get => m_ClassId;
        set
        {
            if (m_ClassId == value)
                return;
            bool available = IsManual ? value.Length == 0 : Classes.Any(job => job.Id == value);
            if (!available)
                throw new ArgumentException("このユニットが選択できる兵種を指定してください。", nameof(value));
            m_ClassId = value;
            Animals = value.Length == 0 ? [m_Unassigned] :
                [m_Unassigned, .. m_Master.GetAnimalsFor(m_Master.Classes[value]).Select(a => new GrowthOption(a.Id, a.Name))];
            if (!Animals.Any(animal => animal.Id == AnimalId))
                AnimalId = "";
        }
    }

    public string AnimalId
    {
        get => m_AnimalId;
        set
        {
            if (m_AnimalId == value)
                return;
            if (!Animals.Any(animal => animal.Id == value))
                throw new ArgumentException("この兵種が配備できる動物を指定してください。", nameof(value));
            m_AnimalId = value;
        }
    }

    public int Affection
    {
        get => m_Affection;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, AnimalGrowthData.MaxAffection);
            m_Affection = value;
        }
    }

    internal ImmutableArray<short> GetEffectiveRates(GrowthSkillBonus skillBonus)
    {
        AssignedAnimal? animal = HasAnimal ? new(AnimalId, Affection) : null;
        return m_Master.Calculate(new(UnitId, ClassId, animal, skillBonus));
    }
}
