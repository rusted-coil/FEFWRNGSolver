namespace FEFWRNGSolver.Domain;

internal static class Xorshift128
{
    public static uint NextWord(uint x, uint w)
    {
        uint t = x ^ (x << 11);
        return w ^ (w >> 19) ^ t ^ (t >> 8);
    }
}
