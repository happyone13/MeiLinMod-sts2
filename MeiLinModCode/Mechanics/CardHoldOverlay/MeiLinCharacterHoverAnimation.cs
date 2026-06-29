using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

public static class MeiLinCharacterHoverAnimation
{
    public static void NotifyFocused(CardModel card, bool focused)
    {
        if (focused)
            MeiLinAnimationSequenceManager.NotifyBattleIdleRequested(card, MeiLinBattleIdleRequest.Focus);
        else
            MeiLinAnimationSequenceManager.NotifyBattleIdleReleased(card);
    }

    public static void NotifyCanceled(CardModel card)
    {
        MeiLinAnimationSequenceManager.NotifyBattleIdleReleased(card);
    }

    public static void NotifyClicked(CardModel card)
    {
        MeiLinAnimationSequenceManager.NotifyBattleIdleRequested(card, MeiLinBattleIdleRequest.MouseClick);
    }

    public static void NotifyPlayStarted(CardModel card)
    {
        MeiLinAnimationSequenceManager.NotifyBattleIdleRequested(card, MeiLinBattleIdleRequest.PlayStart);
    }

    public static void NotifyControllerStarted(CardModel card)
    {
        MeiLinAnimationSequenceManager.NotifyBattleIdleRequested(card, MeiLinBattleIdleRequest.ControllerStart);
    }

    public static void NotifyCardPlayed(CardModel card)
    {
        MeiLinAnimationSequenceManager.NotifyCardPlayed(card);
    }

    public static void NotifyCombatEnded()
    {
        MeiLinAnimationSequenceManager.NotifyCombatEnded();
    }
}
