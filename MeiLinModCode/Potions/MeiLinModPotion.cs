using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Potions;

[Pool(typeof(MeiLinModPotionPool))]
public abstract class MeiLinModPotion : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"{GetType().ToSnakeCaseAssetStem()}.png".PotionImagePath(),
        OutlinePath: $"{GetType().ToSnakeCaseAssetStem()}_outline.png".PotionImagePath());
}
