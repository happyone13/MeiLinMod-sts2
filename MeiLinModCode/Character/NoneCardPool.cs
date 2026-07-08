using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Character;

// Cards in this pool are kept in code but excluded from the playable card pools.
public class NoneCardPool : TypeListCardPoolModel
{
    public override string Title => "MEILINMOD_NONE_POOL";
    public override string EnergyColorName => "none";
    public override Color DeckEntryCardColor => new("FFFFFF");
    public override bool IsColorless => true;
}
