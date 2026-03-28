using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(SpineAnimationAccess), nameof(SpineAnimationAccess.SetAnimation), [typeof(string), typeof(bool), typeof(int)])]
public static class GameOverAnimationFallbackPatch
{
    [HarmonyPrefix]
    public static void SetAnimationPrefix(SpineAnimationAccess __instance, ref string __0)
    {
        if (!string.Equals(__0, "die", System.StringComparison.Ordinal))
        {
            return;
        }

        __0 = "death";
        MainFile.Logger.Info("[GameOverAnimationFallbackPatch] Replaced requested animation 'die' with 'death'.");
    }
}

[HarmonyPatch(typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), [typeof(string), typeof(bool), typeof(int)])]
public static class GameOverAnimationFallbackOnMegaStatePatch
{
    [HarmonyPrefix]
    public static void SetAnimationPrefix(ref string __0)
    {
        if (!string.Equals(__0, "die", System.StringComparison.Ordinal))
        {
            return;
        }

        __0 = "death";
        MainFile.Logger.Info("[GameOverAnimationFallbackPatch] Replaced requested animation 'die' with 'death' at MegaAnimationState.");
    }
}
