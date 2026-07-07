using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class GameOverAnimationFallbackOnMegaStatePatch : IPatchMethod
{
    private static bool _fallbackInProgress;

    public static string PatchId => "meilin_game_over_animation_fallback";

    public static bool IsCritical => false;

    public static string Description => "Fallback missing die animation to death animation";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<MegaAnimationState>(
            nameof(MegaAnimationState.SetAnimation),
            [typeof(string), typeof(bool), typeof(int)])
    ];

    public static Exception? Finalizer(MegaAnimationState __instance, string __0, bool __1, int __2, Exception? __exception)
    {
        if (__exception == null || _fallbackInProgress)
            return __exception;

        if (!string.Equals(__0, "die", System.StringComparison.Ordinal))
        {
            return __exception;
        }

        try
        {
            _fallbackInProgress = true;
            __instance.SetAnimation("death", __1, __2);
            MainFile.Logger.Info("[GameOverAnimationFallbackPatch] Fallback to 'death' instead of missing 'die' animation.");
            return null;
        }
        catch
        {
            return __exception;
        }
        finally
        {
            _fallbackInProgress = false;
        }
    }
}
