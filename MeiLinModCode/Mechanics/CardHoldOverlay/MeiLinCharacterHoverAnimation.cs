using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

public static class MeiLinCharacterHoverAnimation
{
    private const double CardPlayFocusSuppressSeconds = 2.5;
    private static bool _isFocused;
    private static CardModel? _focusedCard;
    private static DateTime _suppressFocusUntilUtc = DateTime.MinValue;

    public static void NotifyFocused(CardModel card, bool focused)
    {
        if (!IsValidCard(card))
            return;

        if (IsFocusSuppressed())
            return;

        if (focused)
        {
            if (_isFocused && _focusedCard == card)
                return;

            var creatureNode = GetCreatureNode(card);
            if (creatureNode == null || IsPlayingActionAnimation(creatureNode))
                return;

            _isFocused = true;
            _focusedCard = card;
            EnterBattleIdle(creatureNode);
            return;
        }

        if (!_isFocused || _focusedCard != card)
            return;

        _isFocused = false;
        _focusedCard = null;

        ExitBattleIdle(card);
    }

    public static void NotifyCanceled(CardModel card)
    {
        if (!IsValidCard(card))
            return;

        if (!_isFocused || _focusedCard != card)
            return;

        _isFocused = false;
        _focusedCard = null;

        ExitBattleIdle(card);
    }

    public static void NotifyClicked(CardModel card)
    {
        if (!IsValidCard(card))
            return;

        if (IsFocusSuppressed())
            return;

        var creatureNode = GetCreatureNode(card);
        if (creatureNode == null || IsPlayingActionAnimation(creatureNode))
            return;

        _isFocused = true;
        _focusedCard = card;
        PlayLoop(creatureNode, "b_idle");
    }

    public static void NotifyCardPlayed(CardModel card)
    {
        if (!IsValidCard(card))
            return;

        _isFocused = false;
        _focusedCard = null;
        _suppressFocusUntilUtc = DateTime.UtcNow.AddSeconds(CardPlayFocusSuppressSeconds);
        ExitBattleIdle(card);
    }

    public static void NotifyCombatEnded()
    {
        _isFocused = false;
        _focusedCard = null;
        _suppressFocusUntilUtc = DateTime.MinValue;
    }

    private static bool IsFocusSuppressed()
    {
        return DateTime.UtcNow < _suppressFocusUntilUtc;
    }

    private static bool IsValidCard(CardModel? card)
    {
        return card != null &&
               card.IsMutable &&
               LocalContext.IsMine(card) &&
               MeiLinTarget.IsTarget(card.Owner?.Character);
    }

    private static NCreature? GetCreatureNode(CardModel card)
    {
        var creature = card.Owner?.Creature;
        return creature == null ? null : NCombatRoom.Instance?.GetCreatureNode(creature);
    }

    private static bool IsPlayingActionAnimation(NCreature creatureNode)
    {
        try
        {
            var name = GetCurrentAnimationName(creatureNode);
            return name.StartsWith("attack_", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("u", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("buff_", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("hit", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("death", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("victory", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("enter_", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCurrentAnimation(NCreature creatureNode, string animationName)
    {
        return string.Equals(GetCurrentAnimationName(creatureNode), animationName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCurrentAnimationName(NCreature creatureNode)
    {
        try
        {
            var current = creatureNode.Visuals?.SpineBody?.GetAnimationState()?.GetCurrent(0);
            return current?.GetAnimationName() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static void PlaySequence(NCreature creatureNode, string transition, string loop)
    {
        var spine = creatureNode.Visuals?.SpineBody;
        if (spine == null)
            return;

        try
        {
            MegaAnimationState state = spine.GetAnimationState();
            if (HasAnimation(spine, transition))
            {
                state.SetAnimation(transition, loop: false);
                if (HasAnimation(spine, loop))
                    state.AddAnimation(loop, 0f, loop: true);
                return;
            }

            if (HasAnimation(spine, loop))
                state.SetAnimation(loop, loop: true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinCharacterHover] Animation failed. transition={transition}, loop={loop}, ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void PlayLoop(NCreature creatureNode, string loop)
    {
        var spine = creatureNode.Visuals?.SpineBody;
        if (spine == null)
            return;

        try
        {
            if (HasAnimation(spine, loop))
                spine.GetAnimationState().SetAnimation(loop, loop: true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinCharacterHover] Loop animation failed. loop={loop}, ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void EnterBattleIdle(NCreature creatureNode)
    {
        if (IsCurrentAnimation(creatureNode, "b_idle") ||
            IsCurrentAnimation(creatureNode, "idle_to_b_idle"))
        {
            return;
        }

        PlaySequence(creatureNode, "idle_to_b_idle", "b_idle");
    }

    private static bool HasAnimation(MegaSprite sprite, string animationName)
    {
        try
        {
            return sprite.HasAnimation(animationName);
        }
        catch
        {
            return false;
        }
    }

    private static void ExitBattleIdle(CardModel card)
    {
        var node = GetCreatureNode(card);
        if (node == null || IsPlayingActionAnimation(node))
            return;

        if (IsCurrentAnimation(node, "idle") ||
            IsCurrentAnimation(node, "b_idle_to_idle"))
        {
            return;
        }

        PlaySequence(node, "b_idle_to_idle", "idle");
    }
}
