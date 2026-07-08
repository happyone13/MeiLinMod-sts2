using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Character;

public class MeiLinModCardPool : TypeListCardPoolModel
{
    public override string Title => MeiLinMod.CharacterId; //This is not a display name.
    //public override string EnergyColorName => MeiLinMod.CharacterId;
    public override string EnergyColorName => "ironclad";
    public override Color DeckEntryCardColor => new("E83D3D");
    public override bool IsColorless => false;
}
