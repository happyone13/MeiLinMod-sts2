using MeiLinMod.MeiLinModCode.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Powers;

public abstract class MeiLinModPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{GetType().ToSnakeCaseAssetStem()}.png".PowerImagePathOrDefault(),
        BigIconPath: $"{GetType().ToSnakeCaseAssetStem()}.png".BigPowerImagePathOrDefault());
}
