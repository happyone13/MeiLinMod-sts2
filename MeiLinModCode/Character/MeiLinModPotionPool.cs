using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Character;

public class MeiLinModPotionPool : TypeListPotionPoolModel
{
    //public override string EnergyColorName => MeiLinMod.CharacterId;
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => MeiLinMod.Color;
}
