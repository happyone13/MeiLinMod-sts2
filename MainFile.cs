using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using System;
using System.Linq;

namespace MeiLinMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MeiLinMod"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();

        LogPatchStatus(harmony, typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer));
        LogPatchStatus(harmony, typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained));
        LogPatchStatus(harmony, typeof(DustyTome), nameof(DustyTome.SetupForPlayer));
        LogPatchStatus(harmony, typeof(DustyTome), nameof(DustyTome.AfterObtained));
        LogPatchStatus(harmony, typeof(SpineAnimationAccess), nameof(SpineAnimationAccess.SetAnimation), typeof(string), typeof(bool), typeof(int));
        LogPatchStatus(harmony, typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), typeof(string), typeof(bool), typeof(int));
    }

    private static void LogPatchStatus(Harmony harmony, Type type, string methodName)
    {
        var method = AccessTools.Method(type, methodName);
        LogPatchStatus(harmony, method, type, methodName);
    }

    private static void LogPatchStatus(Harmony harmony, Type type, string methodName, params Type[] argumentTypes)
    {
        var method = AccessTools.Method(type, methodName, argumentTypes);
        LogPatchStatus(harmony, method, type, $"{methodName}({string.Join(",", argumentTypes.Select(t => t.Name))})");
    }

    private static void LogPatchStatus(Harmony harmony, System.Reflection.MethodInfo? method, Type type, string methodName)
    {
        if (method == null)
        {
            Logger.Info($"[Harmony] target not found: {type.FullName}.{methodName}");
            return;
        }

        var patchInfo = Harmony.GetPatchInfo(method);
        var mine = patchInfo == null
            ? 0
            : patchInfo.Prefixes.Count(p => p.owner == harmony.Id) +
              patchInfo.Postfixes.Count(p => p.owner == harmony.Id) +
              patchInfo.Transpilers.Count(p => p.owner == harmony.Id) +
              patchInfo.Finalizers.Count(p => p.owner == harmony.Id);

        Logger.Info($"[Harmony] {type.Name}.{methodName}: patchedByMe={mine}");
    }
}
