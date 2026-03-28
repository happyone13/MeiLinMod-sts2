using BaseLib.Abstracts;
using BaseLib.Extensions;
using MeiLinMod.MeiLinModCode.Extensions;

namespace MeiLinMod.MeiLinModCode.Powers;

public abstract class MeiLinModPower : CustomPowerModel
{
    // Prefer same-name power icon; fall back to default power.png when missing.
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePathOrDefault();
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePathOrDefault();
}
