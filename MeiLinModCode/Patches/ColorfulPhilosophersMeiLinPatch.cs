using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class ColorfulPhilosophersMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_colorful_philosophers_card_pool";

    public static bool IsCritical => false;

    public static string Description => "Allow Colorful Philosophers to offer MeiLin card rewards";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<ColorfulPhilosophers>("GenerateInitialOptions")
    ];

    private static readonly MethodInfo? OfferRewardsMethod =
        AccessTools.Method(typeof(ColorfulPhilosophers), "OfferRewards");

    public static bool Prefix(ColorfulPhilosophers __instance, ref IReadOnlyList<EventOption> __result)
    {
        var owner = __instance.Owner;
        if (owner == null || OfferRewardsMethod == null)
            return true;

        var meiLinPool = ModelDb.CardPool<MeiLinModCardPool>();
        var character = owner.Character;
        var unlockedPools = owner.UnlockState.CharacterCardPools.ToList();

        var optionPools = new List<CardPoolModel>
        {
            ModelDb.CardPool<NecrobinderCardPool>(),
            ModelDb.CardPool<IroncladCardPool>(),
            ModelDb.CardPool<RegentCardPool>(),
            ModelDb.CardPool<SilentCardPool>(),
            ModelDb.CardPool<DefectCardPool>(),
            meiLinPool
        };

        var options = new List<EventOption>();
        foreach (var pool in optionPools)
        {
            if (character.CardPool.Id == pool.Id || unlockedPools.All(p => p.Id != pool.Id))
                continue;

            if (pool.Id == meiLinPool.Id)
            {
                const string meiLinKeyBase = "COLORFUL_PHILOSOPHERS.pages.INITIAL.options.MEILIN";
                options.Add(new EventOption(
                    __instance,
                    () => InvokeOfferRewards(__instance, pool),
                    new LocString("events", $"{meiLinKeyBase}.title"),
                    new LocString("events", $"{meiLinKeyBase}.description"),
                    meiLinKeyBase,
                    []));
            }
            else
            {
                options.Add(new EventOption(
                    __instance,
                    () => InvokeOfferRewards(__instance, pool),
                    $"COLORFUL_PHILOSOPHERS.pages.INITIAL.options.{pool.EnergyColorName.ToUpperInvariant()}"));
            }
        }

        var maxOptions = System.Math.Min(3, options.Count);
        while (options.Count > maxOptions)
            options.RemoveAt(__instance.Rng.NextInt(options.Count));

        __result = options;
        return false;
    }

    private static Task InvokeOfferRewards(ColorfulPhilosophers eventModel, CardPoolModel pool)
    {
        var task = OfferRewardsMethod?.Invoke(eventModel, [pool]) as Task;
        return task ?? Task.CompletedTask;
    }
}
