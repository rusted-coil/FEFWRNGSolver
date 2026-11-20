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
        Units = [new("", "指定無し"), .. sortedUnits.Select(unit => new GrowthOption(unit.Id, unit.Name))];
    }

    public IReadOnlyList<GrowthOption> Units { get; }
    public IReadOnlyList<GrowthOption> Classes { get; private set; } = [];
    public IReadOnlyList<GrowthOption> Animals { get; private set; } = [m_Unassigned];
    public static IReadOnlyList<int> AffectionLevels { get; } = [.. Enumerable.Range(0, AnimalGrowthData.MaxAffection + 1)];
    public bool IsManual => m_UnitId.Length == 0;
    public bool HasAnimal => m_AnimalId.Length > 0;
    internal event Action? Changed;
    internal event Action? UnitChanged;

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
            UnitChanged?.Invoke();
        }
    }

    internal void RestoreSelection(UnitSettings? settings)
    {
        var job = Classes.FirstOrDefault(job => job.Id == settings?.ClassId) ?? Classes.FirstOrDefault();
        SetClass(job?.Id ?? "");
        m_AnimalId = Animals.FirstOrDefault(animal => animal.Id == settings?.AnimalId)?.Id ?? "";
        m_Affection = settings is { Affection: >= 0 and <= AnimalGrowthData.MaxAffection }
            ? settings.Affection : AnimalGrowthData.MaxAffection;
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
            SetClass(value);
            Changed?.Invoke();
        }
    }

    private void SetClass(string id)
    {
        m_ClassId = id;
        Animals = id.Length == 0 ? [m_Unassigned] :
            [m_Unassigned, .. m_Master.GetAnimalsFor(m_Master.Classes[id]).Select(a => new GrowthOption(a.Id, a.Name))];
        if (!Animals.Any(animal => animal.Id == AnimalId))
            m_AnimalId = "";
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
            Changed?.Invoke();
        }
    }

    public int Affection
    {
        get => m_Affection;
        set
        {
            if (m_Affection == value)
                return;
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, AnimalGrowthData.MaxAffection);
            m_Affection = value;
            Changed?.Invoke();
        }
    }

    internal ImmutableArray<short> GetEffectiveRates(GrowthSkillBonus skillBonus)
    {
        AssignedAnimal? animal = HasAnimal ? new(AnimalId, Affection) : null;
        return m_Master.Calculate(new(UnitId, ClassId, animal, skillBonus));
    }
}
