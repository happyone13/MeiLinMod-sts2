using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot.Bridge;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;

namespace MeiLinMod.MeiLinModCode.Potions;

[Pool(typeof(MeiLinModPotionPool))]
public abstract class MeiLinModPotion : CustomPotionModel
{
    public override string CustomPackedImagePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
    public override string CustomPackedOutlinePath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".PotionImagePath();
}
