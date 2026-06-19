using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinCharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch]
public static class AncientRelicMeiLinPatch
{
    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.SetupForPlayer))]
    [HarmonyPrefix]
    public static bool ArchaicToothSetupForPlayerPrefix(ArchaicTooth __instance, Player player, ref bool __result)
    {
        var starter = GetStarterAttackDefenseUnity(player);
        if (starter == null)
        {
            return true;
        }

        var transformed = player.RunState.CreateCard<ShenGongFangYiTi>(player);
        CopyStarterUpgradesAndEnchantments(starter, transformed);

        __instance.SetupForTests(starter.ToSerializable(), transformed.ToSerializable());
        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained))]
    [HarmonyPrefix]
    public static bool ArchaicToothAfterObtainedPrefix(ArchaicTooth __instance, ref Task __result)
    {
        var owner = __instance.Owner;
        if (owner == null)
        {
            return true;
        }

        var starter = GetStarterAttackDefenseUnity(owner);
        if (starter == null)
        {
            return true;
        }

        __result = HandleArchaicToothTransform(owner, starter);
        return false;
    }

    [HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
    [HarmonyPrefix]
    public static bool DustyTomeSetupForPlayerPrefix(DustyTome __instance, Player player)
    {
        if (!IsMeiLinPlayer(player))
        {
            return true;
        }

        var cardPool = player.Character?.CardPool;
        if (cardPool == null)
        {
            return true;
        }

        var candidates = GetCardPoolCards(player)
            .Where(IsDustyTomeCandidate)
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

    [HarmonyPatch(typeof(DustyTome), nameof(DustyTome.AfterObtained))]
    [HarmonyPrefix]
    public static void DustyTomeAfterObtainedPrefix(DustyTome __instance)
    {
        var owner = __instance.Owner;
        if (__instance.AncientCard != null || owner?.Character == null)
        {
            return;
        }

        var setupMethod = AccessTools.Method(typeof(DustyTome), nameof(DustyTome.SetupForPlayer));
        setupMethod?.Invoke(__instance, [owner]);
    }

    private static async Task HandleArchaicToothTransform(Player owner, CardModel starter)
    {
        var transformed = owner.RunState.CreateCard<ShenGongFangYiTi>(owner);
        CopyStarterUpgradesAndEnchantments(starter, transformed);
        await CardCmd.Transform(starter, transformed);
    }

    private static void CopyStarterUpgradesAndEnchantments(CardModel starter, CardModel transformed)
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

    private static CardModel? GetStarterAttackDefenseUnity(Player player)
    {
        return player.Deck.Cards.FirstOrDefault(card =>
            card is AttackDefenseUnity || card.Id.Entry == "MEILINMOD-ATTACK_DEFENSE_UNITY");
    }

    private static bool IsMeiLinPlayer(Player? player)
    {
        if (player == null)
        {
            return false;
        }

        if (player.Character is MeiLinCharacterModel || player.Character?.Id.Entry == "MEILINMOD-MEI_LIN_MOD")
        {
            return true;
        }

        return GetStarterAttackDefenseUnity(player) != null;
    }

    private static IEnumerable<CardModel> GetCardPoolCards(Player player)
    {
        var allCardsProp = AccessTools.Property(player.Character.CardPool.GetType(), "AllCards");
        if (allCardsProp?.GetValue(player.Character.CardPool) is IEnumerable<CardModel> allCards)
        {
            return allCards;
        }

        return player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
    }

    private static bool IsDustyTomeCandidate(CardModel card)
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
        if (card is ShenGongFangYiTi || card.Id.Entry == "MEILINMOD-SHEN_GONG_FANG_YI_TI")
        {
            return false;
        }

        return true;
    }
}
