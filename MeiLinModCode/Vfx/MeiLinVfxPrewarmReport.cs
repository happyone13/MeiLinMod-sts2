namespace MeiLinMod.MeiLinModCode.Vfx;

public readonly record struct MeiLinVfxPrewarmReport(int Requested, int Loaded)
{
    public int Failed => Math.Max(0, Requested - Loaded);

    public static MeiLinVfxPrewarmReport Empty => new(0, 0);

    public static MeiLinVfxPrewarmReport operator +(
        MeiLinVfxPrewarmReport left,
        MeiLinVfxPrewarmReport right) =>
        new(left.Requested + right.Requested, left.Loaded + right.Loaded);
}
