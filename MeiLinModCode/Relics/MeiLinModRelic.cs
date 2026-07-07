using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Relics;

[Pool(typeof(MeiLinModRelicPool))]
public abstract class MeiLinModRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{GetType().ToSnakeCaseAssetStem()}.png".RelicImagePath(),
        IconOutlinePath: $"{GetType().ToSnakeCaseAssetStem()}_outline.png".RelicImagePath(),
        BigIconPath: $"{GetType().ToSnakeCaseAssetStem()}.png".BigRelicImagePath());
}
