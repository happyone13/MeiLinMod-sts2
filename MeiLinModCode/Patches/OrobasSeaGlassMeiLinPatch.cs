using System.Collections.Generic;
using System.Linq;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class OrobasSeaGlassMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_orobas_sea_glass_character";
    public static bool IsCritical => false;
    public static string Description => "Allow Orobas Sea Glass to target MeiLin when MeiLin is unlocked";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<Orobas>("GenerateInitialOptions")
    ];

    public static void Postfix(Orobas __instance, ref IReadOnlyList<EventOption> __result)
    {
        var owner = __instance.Owner;
        if (owner == null)
            return;

        var meiLinId = ModelDb.Character<MeiLinCharacter>().Id;
        if (owner.Character.Id == meiLinId)
            return;

        var meiLinUnlocked = owner.UnlockState.Characters.Any(c => c.Id == meiLinId);
        if (!meiLinUnlocked)
            return;

        var options = __result.ToList();
        var seaGlassOption = options.FirstOrDefault(o => o.Relic is SeaGlass);
        if (seaGlassOption?.Relic is not SeaGlass seaGlass)
            return;

        if (seaGlass.CharacterId == meiLinId)
            return;

        seaGlass.CharacterId = meiLinId;
        __result = options;
    }
}
