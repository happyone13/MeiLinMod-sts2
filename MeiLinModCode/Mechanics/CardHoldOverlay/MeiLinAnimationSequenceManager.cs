using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

public enum MeiLinBattleIdleRequest
{
    Focus,
    Hover,
    MouseClick,
    PlayStart,
    ControllerStart
}

public static class MeiLinAnimationSequenceManager
{
    private const double CardPlayFocusSuppressSeconds = 2.5;

    private static readonly Dictionary<string, MeiLinAnimationSequence> Sequences = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enter_b_idle"] = new("idle_to_b_idle", false, "b_idle", true),
        ["exit_b_idle"] = new("b_idle_to_idle", false, "idle", true),
        ["attack_end"] = new("attack_end", false, "b_idle_to_idle", false, "idle", true),
        ["u2_attack_end"] = new("u2_attack_end", false, "b_idle_to_idle", false, "idle", true),
    };

    private static bool _isFocused;
    private static CardModel? _focusedCard;
    private static DateTime _suppressFocusUntilUtc = DateTime.MinValue;
    private static DateTime _actionBusyUntilUtc = DateTime.MinValue;
    private static int _actionDepth;

    public static bool IsActionBusy => _actionDepth > 0 || DateTime.UtcNow < _actionBusyUntilUtc;

    public static IDisposable BeginAction(string reason, float minimumSeconds = 0.1f)
    {
        MarkActionBusy(reason, minimumSeconds);
        _actionDepth++;
        return new ActionScope();
    }

    public static void MarkActionBusy(string reason, float seconds)
    {
        if (seconds <= 0f)
            return;

        var until = DateTime.UtcNow.AddSeconds(seconds);
        if (until > _actionBusyUntilUtc)
            _actionBusyUntilUtc = until;
    }

    public static void NotifyBattleIdleRequested(CardModel card, MeiLinBattleIdleRequest request)
    {
        if (!IsValidCard(card) || IsFocusSuppressed() || IsActionBusy)
            return;

        var creatureNode = GetCreatureNode(card);
        if (creatureNode == null || IsPlayingActionAnimation(creatureNode))
            return;

        if (_isFocused && _focusedCard == card)
        {
            EnterBattleIdle(creatureNode);
            return;
        }

        _isFocused = true;
        _focusedCard = card;
        EnterBattleIdle(creatureNode);
    }

    public static void NotifyBattleIdleReleased(CardModel card, bool playAnimation = true)
    {
        if (!IsValidCard(card))
            return;

        if (!_isFocused || _focusedCard != card)
            return;

        _isFocused = false;
        _focusedCard = null;

        if (!playAnimation || IsActionBusy)
            return;

        ExitBattleIdle(card);
    }

    public static void NotifyCardPlayed(CardModel card)
    {
        if (!IsValidCard(card))
            return;

        _isFocused = false;
        _focusedCard = null;
        _suppressFocusUntilUtc = DateTime.UtcNow.AddSeconds(CardPlayFocusSuppressSeconds);
    }

    public static void NotifyCombatEnded()
    {
        _isFocused = false;
        _focusedCard = null;
        _suppressFocusUntilUtc = DateTime.MinValue;
        _actionBusyUntilUtc = DateTime.MinValue;
        _actionDepth = 0;
    }

    public static void PlayAttackEndToIdle(NCreature creatureNode, string? commandName)
    {
        var sequenceKey = string.Equals(commandName, "u2_attack_play", StringComparison.OrdinalIgnoreCase)
            ? "u2_attack_end"
            : "attack_end";

        if (Sequences.TryGetValue(sequenceKey, out var sequence))
            PlaySequence(creatureNode, sequence);
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
        if (creature == null)
            return null;

        try
        {
            return NCombatRoom.Instance?.GetCreatureNode(creature);
        }
        catch
        {
            return null;
        }
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

    private static void EnterBattleIdle(NCreature creatureNode)
    {
        if (IsCurrentAnimation(creatureNode, "b_idle") ||
            IsCurrentAnimation(creatureNode, "idle_to_b_idle"))
        {
            return;
        }

        if (Sequences.TryGetValue("enter_b_idle", out var sequence))
            PlaySequence(creatureNode, sequence);
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

        if (Sequences.TryGetValue("exit_b_idle", out var sequence))
            PlaySequence(node, sequence);
    }

    private static void PlaySequence(NCreature creatureNode, MeiLinAnimationSequence sequence)
    {
        var spine = creatureNode.Visuals?.SpineBody;
        if (spine == null)
            return;

        try
        {
            MegaAnimationState state = spine.GetAnimationState();
            if (HasAnimation(spine, sequence.First))
            {
                state.SetAnimation(sequence.First, sequence.FirstLoop);
                if (!string.IsNullOrWhiteSpace(sequence.Second) && HasAnimation(spine, sequence.Second))
                    state.AddAnimation(sequence.Second, 0f, sequence.SecondLoop);
                if (!string.IsNullOrWhiteSpace(sequence.Third) && HasAnimation(spine, sequence.Third))
                    state.AddAnimation(sequence.Third, 0f, sequence.ThirdLoop);
                return;
            }

            if (!string.IsNullOrWhiteSpace(sequence.Second) && HasAnimation(spine, sequence.Second))
            {
                state.SetAnimation(sequence.Second, sequence.SecondLoop);
                if (!string.IsNullOrWhiteSpace(sequence.Third) && HasAnimation(spine, sequence.Third))
                    state.AddAnimation(sequence.Third, 0f, sequence.ThirdLoop);
                return;
            }

            if (!string.IsNullOrWhiteSpace(sequence.Third) && HasAnimation(spine, sequence.Third))
                state.SetAnimation(sequence.Third, sequence.ThirdLoop);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinAnimationSequence] Animation failed. first={sequence.First}, ex={ex.GetType().Name}: {ex.Message}");
        }
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

    private sealed class ActionScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _actionDepth = Math.Max(0, _actionDepth - 1);
        }
    }

    private sealed record MeiLinAnimationSequence(
        string First,
        bool FirstLoop,
        string? Second = null,
        bool SecondLoop = false,
        string? Third = null,
        bool ThirdLoop = false);
}
