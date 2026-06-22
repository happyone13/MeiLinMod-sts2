using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

[HarmonyPatch]
public static class MeiLinBattleReadyOverlayPatches
{
    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
    [HarmonyPostfix]
    public static void AfterBeforeCombatStart(IRunState runState, CombatState? combatState)
    {
        try
        {
            if (MeiLinTarget.IsTarget(LocalContext.GetMe(runState)))
            {
                MeiLinBattleReadyOverlay.Preload();
                MeiLinCharacterHoverAnimation.NotifyCombatEnded();
            }
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
    [HarmonyPostfix]
    public static void AfterCombatVictory(IRunState runState, CombatState? combatState)
    {
        try
        {
            if (MeiLinTarget.IsTarget(LocalContext.GetMe(runState)))
            {
                MeiLinBattleReadyOverlay.NotifyCombatEnded();
                MeiLinCharacterHoverAnimation.NotifyCombatEnded();
            }
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
    [HarmonyPostfix]
    public static void AfterDeathPostfix(IRunState runState, CombatState? combatState, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        try
        {
            if (creature is { IsPlayer: true } &&
                LocalContext.IsMe(creature) &&
                MeiLinTarget.IsTarget(creature.Player) &&
                !wasRemovalPrevented)
            {
                MeiLinBattleReadyOverlay.NotifyCombatEnded();
                MeiLinCharacterHoverAnimation.NotifyCombatEnded();
            }
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(NHandCardHolder), "OnFocus")]
    [HarmonyPostfix]
    public static void AfterHandFocus(NHandCardHolder __instance)
    {
        try
        {
            CardModel? card = __instance.CardModel;
            if (MeiLinTarget.IsMineTargetCard(card))
            {
                MeiLinBattleReadyOverlay.NotifyUiFocused(card!, focused: true);
                MeiLinCharacterHoverAnimation.NotifyFocused(card!, focused: true);
            }
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(NHandCardHolder), "OnUnfocus")]
    [HarmonyPostfix]
    public static void AfterHandUnfocus(NHandCardHolder __instance)
    {
        try
        {
            CardModel? card = __instance.CardModel;
            if (MeiLinTarget.IsMineTargetCard(card))
            {
                MeiLinBattleReadyOverlay.NotifyUiFocused(card!, focused: false);
                MeiLinCharacterHoverAnimation.NotifyFocused(card!, focused: false);
            }
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(NHandCardHolder), "OnMousePressed")]
    [HarmonyPostfix]
    public static void AfterHandMousePressed(NHandCardHolder __instance, InputEvent inputEvent)
    {
        try
        {
            if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                return;

            CardModel? card = __instance.CardModel;
            if (MeiLinTarget.IsMineTargetCard(card))
            {
                MeiLinBattleReadyOverlay.NotifyHovered(card!, hovered: true);
                MeiLinCharacterHoverAnimation.NotifySelected(card!);
            }
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(NMouseCardPlay), "Start")]
    [HarmonyPostfix]
    public static void AfterMouseCardPlayStart(NMouseCardPlay __instance)
    {
        try
        {
            CardModel? card = __instance.Holder?.CardModel;
            if (MeiLinTarget.IsMineTargetCard(card) && card!.CanPlay())
                MeiLinCharacterHoverAnimation.NotifySelected(card);
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(NControllerCardPlay), "Start")]
    [HarmonyPostfix]
    public static void AfterControllerCardPlayStart(NControllerCardPlay __instance)
    {
        try
        {
            CardModel? card = __instance.Holder?.CardModel;
            if (MeiLinTarget.IsMineTargetCard(card) && card!.CanPlay())
                MeiLinCharacterHoverAnimation.NotifySelected(card);
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(NHandCardHolder), "DoCardHoverEffects")]
    [HarmonyPostfix]
    public static void AfterHandHoverEffects(NHandCardHolder __instance, bool isHovered)
    {
        try
        {
            CardModel? card = __instance.CardModel;
            if (!MeiLinTarget.IsMineTargetCard(card))
                return;

            if (isHovered)
            {
                MeiLinBattleReadyOverlay.NotifyHovered(card!, hovered: true);
                MeiLinCharacterHoverAnimation.NotifyFocused(card!, focused: true);
                return;
            }

            if (!__instance.HasFocus() && !Input.IsMouseButtonPressed(MouseButton.Left))
            {
                MeiLinBattleReadyOverlay.NotifyHovered(card!, hovered: false);
                MeiLinCharacterHoverAnimation.NotifyFocused(card!, focused: false);
            }
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(NCardPlay), "CancelPlayCard")]
    [HarmonyPostfix]
    public static void AfterCancelPlayCard(NCardPlay __instance)
    {
        try
        {
            CardModel? card = __instance.Holder?.CardModel;
            if (!MeiLinTarget.IsMineTargetCard(card))
                return;

            MeiLinBattleReadyOverlay.NotifyCanceled(card!);
            MeiLinCharacterHoverAnimation.NotifyCanceled(card!);
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
    [HarmonyPrefix]
    public static void BeforeCardPlayedPrefix(CombatState combatState, CardPlay cardPlay)
    {
        try
        {
            if (MeiLinTarget.IsMineTargetCard(cardPlay.Card))
            {
                MeiLinBattleReadyOverlay.NotifyBeforeCardPlayed(cardPlay);
                MeiLinCharacterHoverAnimation.NotifyCardPlayed(cardPlay.Card!);
            }
        }
        catch
        {
        }
    }

}
