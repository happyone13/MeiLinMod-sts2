using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(DustyTome), nameof(DustyTome.AfterObtained))]
public static class DustyTomeAfterObtainedPatch
{
    [HarmonyPrefix]
    public static void Prefix(DustyTome __instance)
    {
        var owner = __instance.Owner;
        var ownerCharacter = owner?.Character?.Id.Entry ?? "null";
        MainFile.Logger.Info(
            $"[DustyTomeAfterObtainedPatch] enter: ownerCharacter={ownerCharacter}, ancientCardBefore={__instance.AncientCard?.Entry ?? "null"}");

        if (__instance.AncientCard != null || owner == null || owner.Character == null)
        {
            return;
        }

        var setupMethod = AccessTools.Method(typeof(DustyTome), nameof(DustyTome.SetupForPlayer));
        if (setupMethod == null)
        {
            MainFile.Logger.Info("[DustyTomeAfterObtainedPatch] repair failed: SetupForPlayer method not found.");
            return;
        }

        try
        {
            setupMethod.Invoke(__instance, [owner]);
            MainFile.Logger.Info(
                $"[DustyTomeAfterObtainedPatch] repaired via SetupForPlayer: ancientCardAfter={__instance.AncientCard?.Entry ?? "null"}");
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Info($"[DustyTomeAfterObtainedPatch] repair failed while invoking SetupForPlayer: {e.GetType().Name}: {e.Message}");
        }
    }
}
