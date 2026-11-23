using System.Collections.Immutable;
using FEFWRNGSolver.Domain;

namespace FEFWRNGSolver.Presentation;

public sealed record GrowthOption(string Id, string Name);
public sealed record PredictionClassOption(string Id, string Name, bool IsAvailable);
public sealed record PredictionClassGroup(string Name, IReadOnlyList<PredictionClassOption> Classes);

public sealed class GrowthSetupViewModel
{
    private readonly LocalizationCatalog m_Text;
    private readonly GrowthRateMasterData m_Master;
    private LocalizedOptions? m_Options;
    private string m_UnitId = "";
    private string m_ClassId = "";
    private string m_AnimalId = "";
    private int m_Affection = AnimalGrowthData.MaxAffection;

    internal GrowthSetupViewModel(LocalizationCatalog text, GrowthRateMasterData master) => (m_Text, m_Master) = (text, master);

    private sealed record LocalizedOptions(string Language, string UnitId, string ClassId,
        GrowthOption[] Units, GrowthOption[] Classes, GrowthOption[] Animals, PredictionClassGroup[] PredictionClassGroups);

    private LocalizedOptions Options
    {
        get
        {
            if (m_Options is { } cached && cached.Language == m_Text.Code && cached.UnitId == UnitId && cached.ClassId == ClassId)
                return cached;
            var sortedUnits = m_Master.UnitRoster.Select(GetUnitOption)
                .OrderBy(u => u.Name, StringComparer.Create(m_Text.Culture, ignoreCase: false));
            GrowthOption[] units = [new("", m_Text["SYS_UNIT_UNSPECIFIED"]), .. sortedUnits];
            GrowthOption[] classes = IsManual ? [] : [.. m_Master.GetClassesFor(m_Master.Units[UnitId])
                .Select(job => new GrowthOption(job.Id, m_Text.Job(job.Id)))];
            GrowthOption unassigned = new("", m_Text["SYS_ANIMAL_UNASSIGNED"]);
            GrowthOption[] animals = ClassId.Length == 0 ? [unassigned] : [unassigned, .. m_Master.GetAnimalsFor(m_Master.Classes[ClassId])
                .Select(a => new GrowthOption(a.Id, m_Text[GetAnimalNameKey(a.Id)]))];
            return m_Options = new(m_Text.Code, UnitId, ClassId, units, classes, animals, GetPredictionClassGroups());
        }
    }

    public IReadOnlyList<GrowthOption> Units => Options.Units;
    public IReadOnlyList<GrowthOption> Classes => Options.Classes;
    public IReadOnlyList<GrowthOption> Animals => Options.Animals;
    public IReadOnlyList<PredictionClassGroup> PredictionClassGroups => Options.PredictionClassGroups;

    private PredictionClassGroup[] GetPredictionClassGroups()
    {
        var unit = SelectedUnit;
        var byTier = m_Master.ClassRoster.ToLookup(job => job.Tier, job =>
            new PredictionClassOption(job.Id, m_Text.Job(job.Id), unit is not null && job.IsAvailableTo(unit)));
        return
        [
            new(m_Text["SYS_CLASS_TIER_BEGINNER"], [.. byTier[ClassTier.Beginner]]),
            new(m_Text["SYS_CLASS_TIER_INTERMEDIATE"], [.. byTier[ClassTier.Intermediate]]),
            new(m_Text["SYS_CLASS_TIER_ADVANCED"], [.. byTier[ClassTier.Advanced]]),
            new(m_Text["SYS_CLASS_TIER_MASTER"], [.. byTier[ClassTier.Master]]),
            new(m_Text["SYS_CLASS_TIER_DIVINE"], [.. byTier[ClassTier.Divine]])
        ];
    }

    private GrowthOption GetUnitOption(UnitGrowthData unit)
    {
        var name = m_Text.Character(unit.Id);
        bool male = unit.Gender == UnitGender.Male;
        if (unit.Id.EndsWith(male ? "/M" : "/F", StringComparison.Ordinal))
        {
            var otherId = unit.Id[..^2] + (male ? "/F" : "/M");
            if (m_Master.Units.ContainsKey(otherId) && m_Text.Character(otherId) == name)
                name = m_Text.Format(male ? "SYS_UNIT_MALE_FORMAT" : "SYS_UNIT_FEMALE_FORMAT", name);
        }
        return new(unit.Id, name);
    }

    public static IReadOnlyList<int> AffectionLevels { get; } = [.. Enumerable.Range(0, AnimalGrowthData.MaxAffection + 1)];
    public bool IsManual => m_UnitId.Length == 0;
    internal UnitGrowthData? SelectedUnit => m_Master.Units.GetValueOrDefault(UnitId);
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
            if (value.Length != 0 && !m_Master.Units.ContainsKey(value))
                throw new KeyNotFoundException(m_Text["SYS_UNKNOWN_UNIT"]);
            m_UnitId = value;
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
                throw new ArgumentException(m_Text["SYS_INVALID_CLASS"], nameof(value));
            SetClass(value);
            Changed?.Invoke();
        }
    }

    private void SetClass(string id)
    {
        m_ClassId = id;
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
                throw new ArgumentException(m_Text["SYS_INVALID_ANIMAL"], nameof(value));
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

    internal ImmutableArray<short> GetEffectiveRates(GrowthSkillBonus skillBonus) => GetEffectiveRates(skillBonus, ClassId);

    internal ImmutableArray<short> GetEffectiveRates(GrowthSkillBonus skillBonus, string classId)
    {
        AssignedAnimal? animal = HasAnimal && m_Master.Classes[classId].CanDeploy(m_Master.Animals[AnimalId])
            ? new(AnimalId, Affection) : null;
        return m_Master.Calculate(new(UnitId, classId, animal, skillBonus));
    }

    // 保存済みの個体IDと、表示名のシステムメッセージを対応付ける。
    private static string GetAnimalNameKey(string id) => id switch
    {
        "野生の馬" => "SYS_ANIMAL_WILD_HORSE",
        "汗血馬" => "SYS_ANIMAL_FERGHANAN_HORSE",
        "黒馬" => "SYS_ANIMAL_BLACK_HORSE",
        "モノケロース" => "SYS_ANIMAL_MONOCEROS",
        "RID_ロシナン" => "SYS_ANIMAL_ROCINANTE",
        "ロシナンの兄弟" => "SYS_ANIMAL_ROCINANTE_SIBLING",
        "野生のオルニウス" => "SYS_ANIMAL_WILD_ORNIUS",
        "白羽のオルニウス" => "SYS_ANIMAL_WHITE_ORNIUS",
        "メガニウス" => "SYS_ANIMAL_MEGANIUS",
        "赤羽のメガニウス" => "SYS_ANIMAL_RED_MEGANIUS",
        "マジニウス" => "SYS_ANIMAL_MAGONIUS",
        "黒羽のマジニウス" => "SYS_ANIMAL_BLACK_MAGONIUS",
        "野生のペガサス" => "SYS_ANIMAL_WILD_PEGASUS",
        "ダークペガサス" => "SYS_ANIMAL_DARK_PEGASUS",
        "ファルコン" => "SYS_ANIMAL_FALICORN",
        "レッドファルコン" => "SYS_ANIMAL_RED_FALICORN",
        "RID_ブーケパラス" => "SYS_ANIMAL_BUCEPHALUS",
        "ブーケパラスの子" => "SYS_ANIMAL_BUCEPHALUS_FOAL",
        "野生のバウ" => "SYS_ANIMAL_WILD_BAU",
        "レッドバウ" => "SYS_ANIMAL_RED_BAU",
        "ブラックバウ" => "SYS_ANIMAL_BLACK_BAU",
        "サラミス戦象" => "SYS_ANIMAL_SALAMIS_WAR_ELEPHANT",
        _ => id
    };
}
