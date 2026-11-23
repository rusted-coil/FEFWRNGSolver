using System.Collections.Immutable;

namespace FEFWRNGSolver.Domain;

internal static partial class GrowthCatalog
{
    // 成長率は HP・力・魔力・技・速さ・幸運・守備・魔防・魅力 の順。
    internal static readonly ImmutableArray<ClassGrowthData> Classes =
    [
        new("JID_無職", "平民・貴族", default) { Tier = ClassTier.Beginner },
        new("JID_闘士", "闘士", new(10, 10, 0, 0, 0, 0, 0, 0, 0)) { Tier = ClassTier.Beginner },
        new("JID_猟兵", "猟兵", new(10, 0, 0, 10, 10, 0, 0, 0, 0)) { Tier = ClassTier.Beginner },
        new("JID_兵士", "兵士", new(10, 5, 0, 5, -5, 0, 10, 0, 0)) { Tier = ClassTier.Beginner },
        new("JID_飛駝兵", "飛駝兵", new(10, 0, 0, 0, 10, 5, 0, 5, 0), AllowedAnimalType: AnimalType.Ornius) { Tier = ClassTier.Beginner },
        new("JID_呪い師", "呪い師", new(5, -5, 15, 5, 0, 0, -5, 10, 0)) { Tier = ClassTier.Beginner },

        new("JID_剣士", "剣士", new(10, 0, 0, 10, 15, 5, 0, 0, 5)) { Tier = ClassTier.Intermediate },
        new("JID_ブリガンド", "ブリガンド", new(15, 15, -5, 0, 5, 0, 5, 0, 5)) { Tier = ClassTier.Intermediate },
        new("JID_セスタス", "セスタス", new(15, 10, -5, 5, 10, 0, 10, 0, 5)) { Tier = ClassTier.Intermediate },
        new("JID_アーチャー", "アーチャー", new(10, 0, 0, 15, 10, 5, 5, 0, 5)) { Tier = ClassTier.Intermediate },
        new("JID_ローグ", "ローグ", new(10, 0, 0, 10, 15, 5, 0, 5, 0)) { Tier = ClassTier.Intermediate },
        new("JID_重装歩兵", "重装歩兵", new(10, 10, -5, 5, -5, 0, 25, -5, 5)) { Tier = ClassTier.Intermediate },
        new("JID_軽騎兵", "軽騎兵", new(10, 5, -5, 0, 5, 5, 5, 0, 10), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Intermediate },
        new("JID_戦車兵", "戦車兵", new(10, 5, -5, 15, -5, 5, 10, 0, 10), AllowedAnimalType: AnimalType.Horse, AnimalMultiplier: 2) { Tier = ClassTier.Intermediate },
        new("JID_騎甲駝兵", "騎甲駝兵", new(10, 0, -5, 5, 10, 5, 10, 0, 5), AllowedAnimalType: AnimalType.Ornius) { Tier = ClassTier.Intermediate },
        new("JID_天翼兵", "天翼兵", new(10, 0, 5, 5, 0, 5, 5, 5, 10), FemaleOnly: true, AllowedAnimalType: AnimalType.Pegasus) { Tier = ClassTier.Intermediate },
        new("JID_シャーマン", "シャーマン", new(5, -5, 15, 10, 10, 10, -10, 10, 0)) { Tier = ClassTier.Intermediate },
        new("JID_プリースト", "プリースト", new(5, -5, 10, 5, 5, 15, -10, 15, 10)) { Tier = ClassTier.Intermediate },

        new("JID_シドー", "シドー", new(10, 5, 0, 10, 15, 5, 0, -5, 5)) { Tier = ClassTier.Advanced },
        new("JID_ウォーリアー", "ウォーリアー", new(20, 20, -5, 0, 0, 0, 5, -5, 5)) { Tier = ClassTier.Advanced },
        new("JID_鍛冶師", "鍛冶師", new(15, 10, 10, 5, 0, 0, 10, 10, 5)) { Tier = ClassTier.Advanced },
        new("JID_スナイパー", "スナイパー", new(10, 5, 0, 20, 10, 5, 5, 5, 5)) { Tier = ClassTier.Advanced },
        new("JID_フォレストナイト", "フォレストナイト", new(10, 5, 0, 10, 10, 5, 5, 5, 5), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Advanced },
        new("JID_レンジャー", "レンジャー", new(10, 5, 0, 15, 15, 15, 0, 10, 0)) { Tier = ClassTier.Advanced },
        new("JID_ガーディアン", "ガーディアン", new(10, 10, 0, 5, 0, 5, 10, 15, 5)) { Tier = ClassTier.Advanced },
        new("JID_ヘヴィアーマー", "ヘヴィアーマー", new(15, 15, -5, 5, -10, 0, 30, -10, 5)) { Tier = ClassTier.Advanced },
        new("JID_バーディンガー", "バーディンガー", new(10, 10, -5, 0, 0, 5, 5, 10, 10), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Advanced },
        new("JID_カタフラクト", "カタフラクト", new(15, 15, -5, 0, -10, 0, 10, -5, 5), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Advanced },
        new("JID_戦象兵", "戦象兵", new(20, 10, -5, 20, -10, 5, 15, -5, 10), AllowedAnimalType: AnimalType.Elephant) { Tier = ClassTier.Advanced },
        new("JID_カラドリオス", "カラドリオス", new(15, 0, 10, 5, 10, 5, 10, 10, 5), AllowedAnimalType: AnimalType.Ornius) { Tier = ClassTier.Advanced },
        new("JID_ドラグーン", "ドラグーン", new(15, 5, 0, 0, 5, 10, 10, 10, 5), AllowedAnimalType: AnimalType.Bau) { Tier = ClassTier.Advanced },
        new("JID_ウァテス", "ウァテス", new(10, -5, 20, 15, 5, 10, -10, 15, 0)) { Tier = ClassTier.Advanced },
        new("JID_ビショップ", "ビショップ", new(10, -5, 15, 5, 0, 20, -10, 20, 10)) { Tier = ClassTier.Advanced },
        new("JID_トルバドール", "トルバドール", new(10, 0, 15, 5, 0, 15, 5, 15, 10), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Advanced },
        new("JID_踊り子", "踊り子", new(15, 5, 0, 10, 25, 10, 0, 5, 20)) { Tier = ClassTier.Advanced },

        new("JID_ソードマスター", "ソードマスター", new(15, 10, 0, 15, 20, 5, 0, 0, 5)) { Tier = ClassTier.Master },
        new("JID_ハイエピタフ", "ハイエピタフ", new(10, 15, 15, 5, 10, 5, 5, 5, 5), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Master },
        new("JID_バトルマスター", "バトルマスター", new(25, 25, -5, -5, 5, -5, 10, -5, 5)) { Tier = ClassTier.Master },
        new("JID_バトルモンク", "バトルモンク", new(15, 10, 0, 0, 15, 5, 5, 15, 5)) { Tier = ClassTier.Master },
        new("JID_マスターアーチ", "マスターアーチ", new(15, 5, 0, 20, 15, 5, 5, 5, 5)) { Tier = ClassTier.Master },
        new("JID_ボウナイト", "ボウナイト", new(10, 10, 0, 10, 15, 5, 5, 0, 5), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Master },
        new("JID_シャドーシーカー", "シャドーシーカー", new(15, 5, 0, 15, 25, 15, 0, 10, 0)) { Tier = ClassTier.Master },
        new("JID_ホーリーランサー", "ホーリーランサー", new(15, 15, 0, 5, 5, 5, 10, 15, 5)) { Tier = ClassTier.Master },
        new("JID_フォートレス", "フォートレス", new(25, 20, -5, 5, -15, 0, 30, -10, 5)) { Tier = ClassTier.Master },
        new("JID_オリハルディア", "オリハルディア", new(15, 15, -5, 0, 5, 5, 5, 5, 10), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Master },
        new("JID_グレートナイト", "グレートナイト", new(20, 20, -5, 0, -10, 0, 15, -10, 5), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Master },
        new("JID_聖天翼兵", "聖天翼兵", new(15, 5, -5, 5, 10, 15, 5, 15, 10), FemaleOnly: true, AllowedAnimalType: AnimalType.Pegasus) { Tier = ClassTier.Master },
        new("JID_ドラゴンマスター", "ドラゴンマスター", new(15, 5, 0, 0, 5, 10, 10, 10, 5), AllowedAnimalType: AnimalType.Bau) { Tier = ClassTier.Master },
        new("JID_ドルイド", "ドルイド", new(10, -5, 30, 15, 10, 10, -10, 20, 0)) { Tier = ClassTier.Master },
        new("JID_ワイズマン", "ワイズマン", new(10, -5, 20, 0, 5, 20, -10, 25, 10)) { Tier = ClassTier.Master },
        new("JID_ヴァルキュリウム", "ヴァルキュリウム", new(10, 0, 15, 5, 5, 15, 5, 10, 10), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Master },

        new("JID_ザ・ブレイド", "ザ・ブレイド", new(15, 10, 10, 20, 20, 5, 5, 5, 10)) { Tier = ClassTier.Divine },
        new("JID_ジ・アプサラス", "ジ・アプサラス", new(15, 5, 10, 15, 25, 10, 5, 5, 30)) { Tier = ClassTier.Divine },
        new("JID_ジ・エターナル", "ジ・エターナル", new(30, 30, 0, 0, 10, 0, 15, 0, 10)) { Tier = ClassTier.Divine },
        new("JID_ザ・ゴッドハンド", "ザ・ゴッドハンド", new(15, 10, 10, 5, 15, 5, 10, 15, 10)) { Tier = ClassTier.Divine },
        new("JID_ザ・カラミティ", "ザ・カラミティ", new(15, 5, 15, 20, 25, 15, 5, 15, 5)) { Tier = ClassTier.Divine },
        new("JID_ザ・トライデント", "ザ・トライデント", new(15, 15, 5, 10, 10, 5, 15, 15, 10)) { Tier = ClassTier.Divine },
        new("JID_ザ・キャバリアー", "ザ・キャバリアー", new(20, 15, 5, 10, 10, 5, 15, 10, 15), AllowedAnimalType: AnimalType.Horse) { Tier = ClassTier.Divine },
        new("JID_ジ・アヴァタール", "ジ・アヴァタール", new(10, -5, 30, 5, 10, 20, -5, 30, 10)) { Tier = ClassTier.Divine },
    ];
}
