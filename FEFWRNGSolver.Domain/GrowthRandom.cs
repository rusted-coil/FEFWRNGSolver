namespace FEFWRNGSolver.Domain;

internal struct GrowthRandom
{
    private uint m_X;
    private uint m_Y;
    private uint m_Z;
    private uint m_W;

    public GrowthRandom(uint growthSeed)
    {
        m_X = Expand(growthSeed, 0);
        m_Y = Expand(m_X, 1);
        m_Z = Expand(m_Y, 2);
        m_W = Expand(m_Z, 3);
    }

    private static uint Expand(uint value, uint index) =>
        unchecked(1812433253u * (value ^ (value >> 30)) + index);

    public uint Next31()
    {
        uint next = Xorshift128.NextWord(m_X, m_W);
        (m_X, m_Y, m_Z, m_W) = (m_Y, m_Z, m_W, next);
        return next & 0x7FFFFFFFu;
    }
}
