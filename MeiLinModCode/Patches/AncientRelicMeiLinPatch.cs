using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinCharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class ArchaicToothSetupForPlayerMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_archaic_tooth_setup_for_player";

    public static bool IsCritical => false;

    public static string Description => "Allow Archaic Tooth setup to transform MeiLin starter card";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<ArchaicTooth>(nameof(ArchaicTooth.SetupForPlayer))
    ];

    public static bool Prefix(ArchaicTooth __instance, Player player, ref bool __result)
    {
        var starter = AncientRelicMeiLinPatch.GetStarterAttackDefenseUnity(player);
        if (starter == null)
        {
            return true;
        }

        var transformed = player.RunState.CreateCard<ShenGongFangYiTi>(player);
        AncientRelicMeiLinPatch.CopyStarterUpgradesAndEnchantments(starter, transformed);

        __instance.SetupForTests(starter.ToSerializable(), transformed.ToSerializable());
        __result = true;
        return false;
    }
}

public sealed class ArchaicToothAfterObtainedMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_archaic_tooth_after_obtained";

    public static bool IsCritical => false;

    public static string Description => "Allow Archaic Tooth to transform MeiLin starter card after obtained";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<ArchaicTooth>(nameof(ArchaicTooth.AfterObtained))
    ];

    public static bool Prefix(ArchaicTooth __instance, ref Task __result)
    {
        var owner = __instance.Owner;
        if (owner == null)
        {
            return true;
        }

        var starter = AncientRelicMeiLinPatch.GetStarterAttackDefenseUnity(owner);
        if (starter == null)
        {
            return true;
        }

        __result = AncientRelicMeiLinPatch.HandleArchaicToothTransform(owner, starter);
        return false;
    }
}

public sealed class DustyTomeSetupForPlayerMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_dusty_tome_setup_for_player";

    public static bool IsCritical => false;

    public static string Description => "Allow Dusty Tome to select MeiLin ancient cards";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<DustyTome>(nameof(DustyTome.SetupForPlayer))
    ];

    public static bool Prefix(DustyTome __instance, Player player)
    {
        if (!AncientRelicMeiLinPatch.IsMeiLinPlayer(player))
        {
            return true;
        }

        var cardPool = player.Character?.CardPool;
        if (cardPool == null)
        {
            return true;
        }

        var candidates = AncientRelicMeiLinPatch.GetCardPoolCards(player)
            .Where(AncientRelicMeiLinPatch.IsDustyTomeCandidate)
            .ToList();

        if (candidates.Count == 0)
        {
            return true;
        }

        var selected = player.PlayerRng.Rewards.NextItem(candidates);
        if (selected == null)
        {
            return true;
        }

        __instance.AncientCard = selected.Id;
        return false;
    }
}

public sealed class DustyTomeAfterObtainedMeiLinPatch : IPatchMethod
{
    public static string PatchId => "meilin_dusty_tome_after_obtained";

    public static bool IsCritical => false;

    public static string Description => "Ensure Dusty Tome initializes a MeiLin ancient card when needed";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<DustyTome>(nameof(DustyTome.AfterObtained))
    ];

    public static void Prefix(DustyTome __instance)
    {
        var owner = __instance.Owner;
        if (__instance.AncientCard != null || owner?.Character == null)
        {
            return;
        }

        var setupMethod = AccessTools.Method(typeof(DustyTome), nameof(DustyTome.SetupForPlayer));
        setupMethod?.Invoke(__instance, [owner]);
    }
}

internal static class AncientRelicMeiLinPatch
{
    public static async Task HandleArchaicToothTransform(Player owner, CardModel starter)
    {
        var transformed = owner.RunState.CreateCard<ShenGongFangYiTi>(owner);
        CopyStarterUpgradesAndEnchantments(starter, transformed);
        await CardCmd.Transform(starter, transformed);
    }

    public static void CopyStarterUpgradesAndEnchantments(CardModel starter, CardModel transformed)
    {
        if (starter.IsUpgraded)
        {
            CardCmd.Upgrade(transformed);
        }

        if (starter.Enchantment != null)
        {
            var enchantment = (EnchantmentModel)starter.Enchantment.MutableClone();
            CardCmd.Enchant(enchantment, transformed, enchantment.Amount);
        }
    }

    public static CardModel? GetStarterAttackDefenseUnity(Player player)
    {
        return player.Deck.Cards.FirstOrDefault(card =>
            card is AttackDefenseUnity || MeiLinTarget.EntryEquals(card.Id.Entry, "MEILINMOD_ATTACK_DEFENSE_UNITY"));
    }

    public static bool IsMeiLinPlayer(Player? player)
    {
        if (player == null)
        {
            return false;
        }

        if (player.Character is MeiLinCharacterModel || MeiLinTarget.IsTarget(player))
        {
            return true;
        }

        return GetStarterAttackDefenseUnity(player) != null;
    }

    public static IEnumerable<CardModel> GetCardPoolCards(Player player)
    {
        var allCardsProp = AccessTools.Property(player.Character.CardPool.GetType(), "AllCards");
        if (allCardsProp?.GetValue(player.Character.CardPool) is IEnumerable<CardModel> allCards)
        {
            return allCards;
        }

        return player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
    }

    public static bool IsDustyTomeCandidate(CardModel card)
    {
        if (card.Rarity != CardRarity.Ancient)
        {
            return false;
        }

        if (ArchaicTooth.TranscendenceCards.Contains(card))
        {
            return false;
        }

        // Custom ArchaicTooth transform target for MeiLin should not be offered by Dusty Tome.
        if (card is ShenGongFangYiTi || MeiLinTarget.EntryEquals(card.Id.Entry, "MEILINMOD_SHEN_GONG_FANG_YI_TI"))
        {
            return false;
        }

        return true;
    }
}
