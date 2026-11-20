using System.Collections.Immutable;

namespace FEFWRNGSolver.Domain;

public readonly record struct StatGrowthRates(short Hp = 0, short Strength = 0, short Magic = 0,
    short Dexterity = 0, short Speed = 0, short Luck = 0, short Defense = 0, short Resistance = 0, short Charm = 0)
{
    internal ImmutableArray<short> InStatOrder() =>
        [Hp, Strength, Magic, Dexterity, Speed, Luck, Defense, Resistance, Charm];
}

public enum UnitGender { Male, Female }
public enum AnimalType { Horse, Ornius, Pegasus, Bau, Elephant }

/// <summary>ムウの個人成長率には「成長の兆し」を含めて登録します。</summary>
public sealed record UnitGrowthData(string Id, string Name, UnitGender Gender, StatGrowthRates PersonalRates,
    bool HasPrePartThreeGuarantee = true)
{
    public byte GetGuaranteedStatCount(bool isPartThree) => HasPrePartThreeGuarantee && !isPartThree ? (byte)2 : (byte)0;
}

public sealed record ClassGrowthData(string Id, string Name, StatGrowthRates Modifiers, bool FemaleOnly = false,
    AnimalType? AllowedAnimalType = null, int AnimalMultiplier = 1)
{
    public bool IsAvailableTo(UnitGrowthData unit) => !FemaleOnly || unit.Gender == UnitGender.Female;
    public bool CanDeploy(AnimalGrowthData animal) => AllowedAnimalType == animal.Type;
}

public readonly record struct AssignedAnimal(string AnimalId, int Affection);
public sealed record UnitGrowthSetup(string UnitId, string ClassId, AssignedAnimal? Animal = null,
    GrowthSkillBonus SkillBonus = GrowthSkillBonus.None, bool IsPartThree = false);

public sealed class AnimalGrowthData
{
    public const int MaxAffection = 5;
    public string Id { get; }
    public string Name { get; }
    public AnimalType Type { get; }
    public ImmutableArray<StatGrowthRates> RatesByAffection { get; }

    /// <param name="ratesByAffection">友好度0～5の順で、その段階で適用する補正の全量を指定します。</param>
    public AnimalGrowthData(string id, string name, AnimalType type, params ReadOnlySpan<StatGrowthRates> ratesByAffection)
    {
        if (ratesByAffection.Length != MaxAffection + 1)
            throw new ArgumentException("友好度0～5の成長率補正を6個指定してください。", nameof(ratesByAffection));
        Id = id;
        Name = name;
        Type = type;
        RatesByAffection = [.. ratesByAffection];
    }

    public StatGrowthRates GetAt(int affection)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(affection);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(affection, MaxAffection);
        return RatesByAffection[affection];
    }
}

public sealed class GrowthRateMasterData(ImmutableArray<UnitGrowthData> units, ImmutableArray<ClassGrowthData> classes,
    ImmutableArray<AnimalGrowthData> animals)
{
    public static GrowthRateMasterData Default { get; } = new(GrowthCatalog.Units, GrowthCatalog.Classes, GrowthCatalog.Animals);
    public ImmutableDictionary<string, UnitGrowthData> Units { get; } = units.ToImmutableDictionary(row => row.Id);
    public ImmutableDictionary<string, ClassGrowthData> Classes { get; } = classes.ToImmutableDictionary(row => row.Id);
    public ImmutableDictionary<string, AnimalGrowthData> Animals { get; } = animals.ToImmutableDictionary(row => row.Id);
    public ImmutableArray<UnitGrowthData> UnitRoster => units;

    public IEnumerable<ClassGrowthData> GetClassesFor(UnitGrowthData unit) =>
        classes.Where(job => job.IsAvailableTo(unit));

    public IEnumerable<AnimalGrowthData> GetAnimalsFor(ClassGrowthData job) => animals.Where(job.CanDeploy);

    public ImmutableArray<short> Calculate(UnitGrowthSetup setup)
    {
        var unit = Units[setup.UnitId];
        var currentClass = Classes[setup.ClassId];
        if (!currentClass.IsAvailableTo(unit))
            throw new ArgumentException($"{currentClass.Name}は女性限定の兵種です。", nameof(setup));

        StatGrowthRates animalRates = default;
        if (setup.Animal is { } assigned)
        {
            var animal = Animals[assigned.AnimalId];
            if (!currentClass.CanDeploy(animal))
                throw new ArgumentException($"{currentClass.Name}には{animal.Name}を配備できません。", nameof(setup));
            animalRates = animal.GetAt(assigned.Affection);
        }
        return EffectiveGrowthRates.Calculate(unit.PersonalRates.InStatOrder().AsSpan(), setup.SkillBonus,
            currentClass.Modifiers, animalRates, currentClass.AnimalMultiplier);
    }
}
