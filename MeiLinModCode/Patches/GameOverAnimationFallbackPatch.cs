using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), [typeof(string), typeof(bool), typeof(int)])]
public static class GameOverAnimationFallbackOnMegaStatePatch
{
    private static bool _fallbackInProgress;

    [HarmonyPostfix]
    public static void SetAnimationPostfix(MegaAnimationState __instance, string __0, bool __1, int __2, ref MegaTrackEntry? __result)
    {
        if (_fallbackInProgress)
            return;

        if (!string.Equals(__0, "die", System.StringComparison.Ordinal))
        {
            return;
        }

        if (__result != null)
        {
            return;
        }

        try
        {
            _fallbackInProgress = true;
            __result = __instance.SetAnimation("death", __1, __2);
            if (__result != null)
            {
                MainFile.Logger.Info("[GameOverAnimationFallbackPatch] Fallback to 'death' after missing 'die' animation.");
            }
        }
        finally
        {
            _fallbackInProgress = false;
        }
    }
}
