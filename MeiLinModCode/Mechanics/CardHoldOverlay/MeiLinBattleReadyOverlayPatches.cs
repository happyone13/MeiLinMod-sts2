using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

public sealed class MeiLinBattleReadyBeforeCombatStartPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.BeforeCombatStart";

    public static bool IsCritical => false;

    public static string Description => "Preload MeiLin battle ready overlay on combat start";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.BeforeCombatStart), typeof(IRunState), typeof(ICombatState))
    ];

    public static void Postfix(IRunState runState, ICombatState? combatState)
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
}

public sealed class MeiLinBattleReadyAfterCombatVictoryPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.AfterCombatVictory";

    public static bool IsCritical => false;

    public static string Description => "Clear MeiLin battle ready overlay after combat victory";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.AfterCombatVictory), typeof(IRunState), typeof(ICombatState), typeof(CombatRoom))
    ];

    public static void Postfix(IRunState runState, ICombatState? combatState)
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
}

public sealed class MeiLinBattleReadyAfterDeathPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.AfterDeath";

    public static bool IsCritical => false;

    public static string Description => "Clear MeiLin battle ready overlay after player death";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.AfterDeath), typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(bool), typeof(float))
    ];

    public static void Postfix(IRunState runState, ICombatState? combatState, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
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
}

public sealed class MeiLinBattleReadyHandFocusPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.HandFocus";
    public static bool IsCritical => false;
    public static string Description => "Enter MeiLin battle ready overlay when a hand card gains focus";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NHandCardHolder>("OnFocus")
    ];

    public static void Postfix(NHandCardHolder __instance)
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
}

public sealed class MeiLinBattleReadyHandUnfocusPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.HandUnfocus";
    public static bool IsCritical => false;
    public static string Description => "Exit MeiLin battle ready overlay when a hand card loses focus";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NHandCardHolder>("OnUnfocus")
    ];

    public static void Postfix(NHandCardHolder __instance)
    {
        try
        {
            CardModel? card = __instance.CardModel;
            if (MeiLinTarget.IsMineTargetCard(card))
            {
                MeiLinBattleReadyOverlay.NotifyUiFocused(card!, focused: false);
                if (Input.IsMouseButtonPressed(MouseButton.Left))
                {
                    MeiLinCharacterHoverAnimation.NotifyClicked(card!);
                    return;
                }

                MeiLinCharacterHoverAnimation.NotifyFocused(card!, focused: false);
            }
        }
        catch
        {
        }
    }
}

public sealed class MeiLinBattleReadyHandMousePressedPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.HandMousePressed";
    public static bool IsCritical => false;
    public static string Description => "Keep MeiLin battle ready overlay active when a hand card is clicked";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NHandCardHolder>("OnMousePressed", typeof(InputEvent))
    ];

    public static void Postfix(NHandCardHolder __instance, InputEvent inputEvent)
    {
        try
        {
            if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                return;

            CardModel? card = __instance.CardModel;
            if (MeiLinTarget.IsMineTargetCard(card))
            {
                MeiLinBattleReadyOverlay.NotifyHovered(card!, hovered: true);
                MeiLinCharacterHoverAnimation.NotifyClicked(card!);
            }
        }
        catch
        {
        }
    }
}

public sealed class MeiLinBattleReadyMouseCardPlayStartPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.MouseCardPlayStart";
    public static bool IsCritical => false;
    public static string Description => "Keep MeiLin battle ready overlay active during mouse card play";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NMouseCardPlay>("Start")
    ];

    public static void Postfix(NMouseCardPlay __instance)
    {
        try
        {
            CardModel? card = __instance.Holder?.CardModel;
            if (!MeiLinTarget.IsMineTargetCard(card) || card!.CanPlay() != true)
                return;

            MeiLinBattleReadyOverlay.NotifyHovered(card, hovered: true);
            MeiLinCharacterHoverAnimation.NotifyPlayStarted(card);
        }
        catch
        {
        }
    }
}

public sealed class MeiLinBattleReadyControllerCardPlayStartPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.ControllerCardPlayStart";
    public static bool IsCritical => false;
    public static string Description => "Keep MeiLin battle ready overlay active during controller card play";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NControllerCardPlay>("Start")
    ];

    public static void Postfix(NControllerCardPlay __instance)
    {
        try
        {
            CardModel? card = __instance.Holder?.CardModel;
            if (!MeiLinTarget.IsMineTargetCard(card) || card!.CanPlay() != true)
                return;

            MeiLinBattleReadyOverlay.NotifyUiFocused(card, focused: true);
            MeiLinCharacterHoverAnimation.NotifyControllerStarted(card);
        }
        catch
        {
        }
    }
}

public sealed class MeiLinBattleReadyHandHoverEffectsPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.HandHoverEffects";
    public static bool IsCritical => false;
    public static string Description => "Synchronize MeiLin battle ready overlay with hand hover effects";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NHandCardHolder>("DoCardHoverEffects", typeof(bool))
    ];

    public static void Postfix(NHandCardHolder __instance, bool isHovered)
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
}

public sealed class MeiLinBattleReadyCancelPlayCardPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.CancelPlayCard";
    public static bool IsCritical => false;
    public static string Description => "Clear MeiLin battle ready overlay when card play is canceled";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCardPlay>("CancelPlayCard")
    ];

    public static void Postfix(NCardPlay __instance)
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
}

public sealed class MeiLinBattleReadyBeforeCardPlayedPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.BattleReadyOverlay.BeforeCardPlayed";
    public static bool IsCritical => false;
    public static string Description => "Clear MeiLin battle ready overlay when a card starts playing";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.BeforeCardPlayed), typeof(CombatState), typeof(CardPlay))
    ];

    public static void Prefix(CombatState combatState, CardPlay cardPlay)
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
