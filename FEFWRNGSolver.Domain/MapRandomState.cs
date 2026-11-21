namespace FEFWRNGSolver.Domain;

/// <summary>セーブの Map RNG state を X, Y, Z, W の順に保持します。</summary>
public readonly record struct MapRandomState(uint X, uint Y, uint Z, uint W)
{
    /// <summary>マップ乱数を1回進めた状態を返します。新しい W が今回の32ビットの出力です。</summary>
    public MapRandomState Next() => new(Y, Z, W, Xorshift128.NextWord(X, W));

    /// <summary>マップ乱数を指定回数進めた状態を返します。0回なら元の状態を返します。</summary>
    public MapRandomState Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var state = this;
        for (int i = 0; i < count; i++)
            state = state.Next();
        return state;
    }
}
