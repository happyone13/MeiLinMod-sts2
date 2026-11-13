using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class MeiLinDeathAnimationCuePatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.Animation.DeathCue";
    public static bool IsCritical => false;
    public static string Description => "Map MeiLin game-over death cue to its Spine animation";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(ModCreatureVisualPlayback), nameof(ModCreatureVisualPlayback.TryPlayCue),
            typeof(NCreatureVisuals), typeof(CharacterModel), typeof(string), typeof(ReadOnlySpan<string>), typeof(bool))
    ];

    public static void Prefix(CharacterModel? character, ref string primaryCue)
    {
        if (MeiLinTarget.IsTarget(character) &&
            string.Equals(primaryCue, "die", StringComparison.OrdinalIgnoreCase))
        {
            primaryCue = "death";
        }
    }
}
