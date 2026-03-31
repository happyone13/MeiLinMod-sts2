using HarmonyLib;
using MeiLinMod.MeiLinModCode.Services;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(SfxCmd))]
public static class SfxCmdMeiLinAudioPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(SfxCmd))
            .Where(m => m.Name is nameof(SfxCmd.Play) or nameof(SfxCmd.PlayDeath));
    }

    [HarmonyPrefix]
    private static bool Prefix(MethodBase __originalMethod, object[] __args)
    {
        if (__originalMethod.Name == nameof(SfxCmd.Play))
            return HandlePlay(__args);

        if (__originalMethod.Name == nameof(SfxCmd.PlayDeath))
            return HandlePlayDeath(__args);

        return true;
    }

    private static bool HandlePlay(object[] args)
    {
        string? sfx = args.OfType<string>().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(sfx))
            return true;

        float volume = 1f;
        var volumeObj = args.FirstOrDefault(a => a is float or double or int);
        if (volumeObj is float f)
            volume = f;
        else if (volumeObj is double d)
            volume = (float)d;
        else if (volumeObj is int i)
            volume = i;

        if (MeiLinAudioService.ShouldSuppressDefaultSfx(sfx))
            return false;

        if (MeiLinAudioService.TryPlayFromSfxCmd(sfx, volume))
            return false;

        return true;
    }

    private static bool HandlePlayDeath(object[] args)
    {
        var player = args.OfType<Player>().FirstOrDefault();
        if (MeiLinAudioService.TryPlayDeath(player))
            return false;

        return true;
    }
}
