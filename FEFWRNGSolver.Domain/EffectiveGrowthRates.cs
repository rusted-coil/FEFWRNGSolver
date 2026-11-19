using System.Collections.Immutable;

namespace FEFWRNGSolver.Domain;

public enum GrowthSkillBonus
{
    None,
    CharioteersPath35,
    CharioteersPath45,
    ElephantRidersPath45
}

public static class EffectiveGrowthRates
{
    /// <summary>内部番号順の9能力に各補正を加えます。兵種の動物係数は動物の補正に掛けます。</summary>
    public static ImmutableArray<short> Calculate(ReadOnlySpan<short> baseRates, GrowthSkillBonus skillBonus,
        StatGrowthRates classModifiers = default, StatGrowthRates animalModifiers = default, int animalMultiplier = 1)
    {
        if (baseRates.Length != 9)
            throw new ArgumentException("成長率は内部番号順に9個指定してください。", nameof(baseRates));
        if (animalMultiplier is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(animalMultiplier), "動物係数は1または2を指定してください。");

        // 戦車兵は各段階と基本の兵種表の差分。https://annasnotebook.net/ja/mechanics/charioteer/
        // 戦象兵は騎乗動物を外した実測値。https://forums.serenesforest.net/topic/108370-mounts-give-bonus-stats-and-growth-rates/page/2/
        StatGrowthRates skill = skillBonus switch
        {
            GrowthSkillBonus.None => default,
            GrowthSkillBonus.CharioteersPath35 => new(Hp: 10, Strength: 5, Magic: 5, Dexterity: 5,
                Speed: 5, Defense: 10, Charm: 5),
            GrowthSkillBonus.CharioteersPath45 => new(Hp: 10, Strength: 15, Magic: 5, Dexterity: 10,
                Speed: 5, Luck: 5, Defense: 20, Resistance: 5, Charm: 10),
            GrowthSkillBonus.ElephantRidersPath45 => new(Hp: 10, Strength: 10, Dexterity: 10, Defense: 10, Resistance: 10),
            _ => throw new ArgumentOutOfRangeException(nameof(skillBonus))
        };
        var skillRates = skill.InStatOrder();
        var classRates = classModifiers.InStatOrder();
        var animalRates = animalModifiers.InStatOrder();
        var rates = ImmutableArray.CreateBuilder<short>(baseRates.Length);
        for (int i = 0; i < baseRates.Length; i++)
        {
            int total = baseRates[i] + classRates[i] + animalRates[i] * animalMultiplier + skillRates[i];
            rates.Add(checked((short)Math.Max(0, total)));
        }
        return rates.MoveToImmutable();
    }
}
