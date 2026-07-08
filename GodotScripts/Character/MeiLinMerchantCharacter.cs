using Godot;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace MeiLinMod.MeiLinModCode.Character;

[GlobalClass]
[ScriptPath("res://GodotScripts/Character/MeiLinMerchantCharacter.cs")]
public partial class MeiLinMerchantCharacter : NMerchantCharacter
{
    public override void _Ready()
    {
        PlayAnimation("idle", loop: true);
    }
}
