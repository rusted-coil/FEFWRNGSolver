namespace FEFWRNGSolver.Domain;

public static class CertificationExam
{
    /// <summary>指定した成長seedで資格試験に合格できる最低の合格率（1～100％）を返します。</summary>
    public static int GetMinimumPassRate(uint growthSeed)
    {
        var random = new GrowthRandom(growthSeed);
        random.Next31();
        uint roll = random.Next31() % 1_000_000;
        // 合格条件は roll < 合格率 × 10,000 なので、境界値と等しければ次の1％が必要。
        return (int)(roll / 10_000) + 1;
    }
}
