using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch]
public static class BaseLibPersistCompatPatch
{
    private const string LegacyCombatStateTypeName = "MegaCrit.Sts2.Core.Combat.CombatState";

    private static readonly string? CombatStateTypeName =
        AccessTools.Property(typeof(CardModel), nameof(CardModel.CombatState))?.PropertyType.FullName;

    private static readonly bool RequiresCompat =
        !string.Equals(CombatStateTypeName, LegacyCombatStateTypeName, StringComparison.Ordinal);

    [HarmonyPrepare]
    public static bool Prepare()
    {
        if (!RequiresCompat)
            return false;

        MainFile.Logger.Warn(
            $"[BaseLibPersistCompat] Enabling compatibility fallback. CardModel.CombatState type is '{CombatStateTypeName ?? "null"}'.");
        return true;
    }
}

[HarmonyPatch]
public static class BaseLibPersistPatchIsPersistCompatPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        Type? type = AccessTools.TypeByName("BaseLib.Patches.Features.PersistPatch");
        return type == null ? null : AccessTools.Method(type, "IsPersist", [typeof(CardModel)]);
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        return BaseLibPersistCompatPatch.Prepare() && TargetMethod() != null;
    }

    [HarmonyPrefix]
    private static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}

[HarmonyPatch]
public static class BaseLibPersistVarCompatPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        Type? type = AccessTools.TypeByName("BaseLib.Cards.Variables.PersistVar");
        return type == null ? null : AccessTools.Method(type, "PersistCount", [typeof(CardModel), typeof(int)]);
    }

    [HarmonyPrepare]
    private static bool Prepare()
    {
        return BaseLibPersistCompatPatch.Prepare() && TargetMethod() != null;
    }

    [HarmonyPrefix]
    private static bool Prefix(int basePersist, ref int __result)
    {
        __result = Math.Max(0, basePersist);
        return false;
    }
}
