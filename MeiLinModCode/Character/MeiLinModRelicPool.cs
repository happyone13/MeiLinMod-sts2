using BaseLib.Abstracts;
using Godot;

namespace MeiLinMod.MeiLinModCode.Character;

public class MeiLinModRelicPool : CustomRelicPoolModel
{
    //public override string EnergyColorName => MeiLinMod.CharacterId;
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => MeiLinMod.Color;
}