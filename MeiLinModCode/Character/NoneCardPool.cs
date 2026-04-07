using BaseLib.Abstracts;
using Godot;

namespace MeiLinMod.MeiLinModCode.Character;

// Cards in this pool are kept in code but excluded from the playable card pools.
public class NoneCardPool : CustomCardPoolModel
{
    public override string Title => "MEILINMOD_NONE_POOL";
    public override string EnergyColorName => "none";
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => new("FFFFFF");
    public override bool IsColorless => true;
}
