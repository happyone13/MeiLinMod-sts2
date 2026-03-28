using BaseLib.Abstracts;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;

namespace MeiLinMod.MeiLinModCode.Potions;

[Pool(typeof(MeiLinModPotionPool))]
public abstract class MeiLinModPotion : CustomPotionModel;