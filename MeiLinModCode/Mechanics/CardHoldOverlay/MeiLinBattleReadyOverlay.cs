using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Config;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;

namespace MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

public static class MeiLinBattleReadyOverlay
{
    private const float OutDelaySeconds = 0.3f;
    private const float CancelOutDelaySeconds = 0.8f;
    private const float CardUseOutDelaySeconds = 0.2f;
    private const string AnimIn = "b_in";
    private const string AnimIdle = "b_idle";
    private const string AnimOut = "b_out";
    private const string AnimCardAttack = "card_attack";
    private static readonly string[] AnimCardNonAttackCandidates = ["card_casting"];

    private static PackedScene? _cachedScene;
    private static bool _sceneLoadAttempted;
    private static bool _sceneMissingWarned;

    private static Node? _node;
    private static MegaSprite? _sprite;
    private static bool _busy;

    private static bool _isHovered;
    private static bool _isUiFocused;
    private static ulong _focusToken;
    private static bool _outScheduled;

    private static bool _outPlaying;
    private static bool _cardUsePlaying;
    private static readonly Queue<string> CardAnimQueue = new();

    private static bool _baseCaptured;
    private static Vector2 _basePos;
    private static Vector2 _baseScale = Vector2.One;

    private static bool _hasAnimIn;
    private static bool _hasAnimIdle;
    private static bool _hasAnimOut;
    private static bool _hasCardAttack;
    private static string? _cardNonAttackAnim;

    private static string? _lastFirst;
    private static string? _lastNextLoop;

    private static readonly HashSet<string> MissingAnimsWarned = new(StringComparer.Ordinal);
    private static ulong _watchToken;
    private static long _createDisabledUntil;
    private static int _createErrorLogged;
    private const int CreateDisableMs = 30000;

    private static bool IsFocused => _isHovered || _isUiFocused;
    private static bool IsFocusedEffective => IsFocused || _outScheduled;

    public static void Preload()
    {
        if (!MeiLinModConfig.UseBattleReadyOverlay)
            return;

        _ = GetScene();
    }

    public static void NotifyCombatEnded()
    {
        _isHovered = false;
        _isUiFocused = false;
        _outScheduled = false;
        Cleanup();
    }

    public static void ApplyTransformFromSettings()
    {
        Node? node = _node;
        if (node != null && GodotObject.IsInstanceValid(node))
            ApplyTransform(node);
    }

    public static void NotifyHovered(CardModel card, bool hovered)
    {
        if (!MeiLinModConfig.UseBattleReadyOverlay)
            return;

        if (!MeiLinTarget.IsTarget(card.Owner?.Character))
            return;

        bool wasFocused = IsFocusedEffective;
        _isHovered = hovered;
        _focusToken++;

        if (hovered)
        {
            _outScheduled = false;
            if (!_busy)
            {
                EnsureCreated(playIntro: true);
                return;
            }

            if (!wasFocused && !_outPlaying && !_cardUsePlaying)
                PlayIntroOrIdle();
            return;
        }

        if (IsFocused)
            return;

        _outScheduled = true;
        ulong token = _focusToken;
        TaskHelper.RunSafely(DelayedOutIfStillUnfocused(token, OutDelaySeconds));
    }

    public static void NotifyUiFocused(CardModel card, bool focused)
    {
        if (!MeiLinModConfig.UseBattleReadyOverlay)
            return;

        if (!MeiLinTarget.IsTarget(card.Owner?.Character))
            return;

        bool wasFocused = IsFocusedEffective;
        _isUiFocused = focused;
        _focusToken++;

        if (focused)
        {
            _outScheduled = false;
            if (!_busy)
            {
                EnsureCreated(playIntro: true);
                return;
            }

            if (!wasFocused && !_outPlaying && !_cardUsePlaying)
                PlayIntroOrIdle();
            return;
        }

        if (IsFocused)
            return;

        _outScheduled = true;
        ulong token = _focusToken;
        TaskHelper.RunSafely(DelayedOutIfStillUnfocused(token, OutDelaySeconds));
    }

    public static void NotifyBeforeCardPlayed(CardPlay cardPlay)
    {
        if (!MeiLinModConfig.UseBattleReadyOverlay)
            return;

        CardModel? card = cardPlay.Card;
        if (card == null || !MeiLinTarget.IsTarget(card.Owner?.Character))
            return;

        _focusToken++;
        _isHovered = false;
        _outScheduled = false;
        EnsureCreated(playIntro: false);

        string? anim = GetCardUseAnim(card);
        if (anim == null)
            return;

        if (_outPlaying)
            _outPlaying = false;

        CardAnimQueue.Enqueue(anim);
        TryPlayNextQueuedCardAnim();
    }

    public static void NotifyCanceled(CardModel card)
    {
        if (!MeiLinModConfig.UseBattleReadyOverlay)
            return;

        if (!MeiLinTarget.IsTarget(card.Owner?.Character) || !_busy)
            return;

        _isHovered = false;
        _isUiFocused = false;
        _outScheduled = true;
        ulong token = ++_focusToken;

        if (!_cardUsePlaying && CardAnimQueue.Count == 0 && !_outPlaying)
            TaskHelper.RunSafely(DelayedOutIfStillUnfocused(token, CancelOutDelaySeconds));
    }

    private static PackedScene? GetScene()
    {
        if (_cachedScene != null)
            return _cachedScene;

        if (_sceneLoadAttempted)
            return null;

        _sceneLoadAttempted = true;
        try
        {
            _cachedScene = ResourceLoader.Load<PackedScene>(MeiLinBattleReadyProfile.BattleReadyScenePath);
            return _cachedScene;
        }
        catch
        {
            return null;
        }
    }

    private static void EnsureCreated(bool playIntro)
    {
        long now = System.Environment.TickCount64;
        if (now < _createDisabledUntil)
            return;

        if (_node != null && GodotObject.IsInstanceValid(_node) && _sprite != null)
            return;

        try
        {
            Cleanup();

            NCombatRoom? room = NCombatRoom.Instance;
            if (room == null)
                return;

            PackedScene? scene = GetScene();
            if (scene == null)
            {
                if (!_sceneMissingWarned)
                {
                    _sceneMissingWarned = true;
                    MainFile.Logger.Warn("[MeiLinBattleReadyOverlay] Missing scene " + MeiLinBattleReadyProfile.BattleReadyScenePath);
                }
                return;
            }

            Node instance = scene.Instantiate();
            _node = instance;
            _sprite = new MegaSprite(instance);
            InitAnimCache(_sprite);
            _busy = true;
            _outPlaying = false;
            _cardUsePlaying = false;
            CardAnimQueue.Clear();

            ulong watchToken = ++_watchToken;
            TaskHelper.RunSafely(IdleWatchLoop(instance, watchToken));

            _sprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>((_, _, _) =>
            {
                if (_node != instance)
                    return;

                    if (_cardUsePlaying)
                    {
                        if (TryPlayNextQueuedCardAnim(currentCompleted: true))
                            return;

                        _cardUsePlaying = false;
                        if (IsFocused)
                            PlaySequence(AnimIdle, AnimIdle);
                        else
                            ScheduleOutIfStillUnfocused(CardUseOutDelaySeconds);
                        return;
                    }

                if (_outPlaying)
                {
                    _outPlaying = false;
                    if (IsFocused)
                        PlayIntroOrIdle();
                    else
                        Cleanup();
                }

                if (string.Equals(_lastFirst, AnimIn, StringComparison.Ordinal) &&
                    string.Equals(_lastNextLoop, AnimIdle, StringComparison.Ordinal))
                {
                    _lastFirst = AnimIdle;
                    _lastNextLoop = null;
                }
            }));

            room.CombatVfxContainer.AddChildSafely(instance);
            if (instance is CanvasItem canvasItem)
                canvasItem.ZIndex = 0;

            CaptureBaseTransform(instance);
            ApplyTransform(instance);

            if (playIntro)
                PlayIntroOrIdle();
            else
                PlaySequence(AnimIdle, AnimIdle);
        }
        catch (Exception ex)
        {
            _createDisabledUntil = System.Environment.TickCount64 + CreateDisableMs;
            if (System.Threading.Interlocked.Exchange(ref _createErrorLogged, 1) == 0)
                MainFile.Logger.Warn("[MeiLinBattleReadyOverlay] Create failed: " + ex);
            Cleanup();
        }
    }

    private static void CaptureBaseTransform(Node instance)
    {
        if (_baseCaptured)
            return;

        if (instance is Node2D node2d)
        {
            _baseCaptured = true;
            _basePos = node2d.Position;
            _baseScale = node2d.Scale;
            return;
        }

        if (instance is Control control)
        {
            _baseCaptured = true;
            _basePos = control.Position;
            _baseScale = control.Scale;
        }
    }

    private static void ApplyTransform(Node instance)
    {
        Vector2 scale = GetTargetScale();
        Vector2 position = GetTargetPosition();

        if (instance is Node2D node2d)
        {
            node2d.Scale = scale;
            node2d.Position = position;
            return;
        }

        if (instance is Control control)
        {
            control.Scale = scale;
            control.Position = position;
        }
    }

    private static Vector2 GetTargetScale()
    {
        float sharedScale = MeiLinSharedSettings.BattleReadyScale;
        Vector2 scale = new(
            MeiLinBattleReadyProfile.BattleReadyScale * sharedScale,
            MeiLinBattleReadyProfile.BattleReadyScale * sharedScale);
        return _baseScale * scale;
    }

    private static Vector2 GetTargetPosition()
    {
        Vector2 offset = new(
            MeiLinBattleReadyProfile.BattleReadyOffsetX + MeiLinSharedSettings.BattleReadyOffsetX,
            -(MeiLinBattleReadyProfile.BattleReadyOffsetY + MeiLinSharedSettings.BattleReadyOffsetY));
        return _basePos + offset;
    }

    private static void Cleanup()
    {
        _busy = false;
        _outPlaying = false;
        _cardUsePlaying = false;
        CardAnimQueue.Clear();
        _lastFirst = null;
        _lastNextLoop = null;
        _baseCaptured = false;
        _basePos = Vector2.Zero;
        _baseScale = Vector2.One;

        Node? node = _node;
        _node = null;
        _sprite = null;

        if (node != null && GodotObject.IsInstanceValid(node))
            node.QueueFree();
    }

    private static async Task DelayedOutIfStillUnfocused(ulong token, float delaySeconds)
    {
        await WaitSeconds(delaySeconds);
        if (token != _focusToken || IsFocused || !_busy)
            return;

        _outScheduled = false;
        if (!_cardUsePlaying)
            StartOut();
    }

    private static async Task IdleWatchLoop(Node instance, ulong watchToken)
    {
        while (watchToken == _watchToken)
        {
            await WaitSeconds(1f);
            if (watchToken != _watchToken)
                return;

            if (!_busy || _node != instance || !GodotObject.IsInstanceValid(instance) || _sprite == null)
                return;

            if (!MeiLinModConfig.UseBattleReadyOverlay)
            {
                Cleanup();
                return;
            }

            ApplyTransform(instance);

            if (_cardUsePlaying || CardAnimQueue.Count > 0 || _outPlaying || _outScheduled || IsFocused)
                continue;

            if (!string.Equals(_lastFirst, AnimIdle, StringComparison.Ordinal) || _lastNextLoop != null)
                continue;

            StartOut();
        }
    }

    private static async Task WaitSeconds(float seconds)
    {
        if (seconds <= 0f)
            return;

        try
        {
            NCombatRoom? room = NCombatRoom.Instance;
            SceneTree? tree = room?.GetTree();
            if (room != null && tree != null)
            {
                SceneTreeTimer timer = tree.CreateTimer(seconds);
                await room.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
                return;
            }
        }
        catch
        {
        }

        await Cmd.CustomScaledWait(seconds, seconds);
    }

    private static void StartOut()
    {
        if (_cardUsePlaying || CardAnimQueue.Count > 0 || _outPlaying)
            return;

        if (!_hasAnimOut)
        {
            Cleanup();
            return;
        }

        _outScheduled = false;
        _outPlaying = true;
        if (!PlaySingle(AnimOut))
            Cleanup();
    }

    private static void PlayIntroOrIdle()
    {
        if (_hasAnimIn)
            PlaySequence(AnimIn, AnimIdle);
        else
            PlaySequence(AnimIdle, AnimIdle);
    }

    private static bool TryPlayNextQueuedCardAnim(bool currentCompleted = false)
    {
        if (_cardUsePlaying && !currentCompleted)
            return true;

        if (CardAnimQueue.Count == 0)
            return false;

        while (CardAnimQueue.Count > 0)
        {
            string anim = CardAnimQueue.Dequeue();
            _cardUsePlaying = true;
            _outScheduled = false;
            _outPlaying = false;

            if (PlaySingle(anim, restartIfSame: true))
                return true;

            _cardUsePlaying = false;
        }

        if (IsFocused)
            PlaySequence(AnimIdle, AnimIdle);
        else
            ScheduleOutIfStillUnfocused(CardUseOutDelaySeconds);

        return false;
    }

    private static void ScheduleOutIfStillUnfocused(float delaySeconds)
    {
        _outScheduled = true;
        ulong token = ++_focusToken;
        TaskHelper.RunSafely(DelayedOutIfStillUnfocused(token, delaySeconds));
    }

    private static void PlaySequence(string first, string nextLoop)
    {
        MegaSprite? sprite = _sprite;
        if (sprite == null)
            return;

        MegaAnimationState state = sprite.GetAnimationState();
        if (!HasAnim(sprite, first))
        {
            LogMissingAnimOnce(first);
            return;
        }

        if (string.Equals(first, nextLoop, StringComparison.Ordinal))
        {
            if (string.Equals(_lastFirst, first, StringComparison.Ordinal) && _lastNextLoop == null)
                return;

            state.SetAnimation(first, loop: true);
            _lastFirst = first;
            _lastNextLoop = null;
            return;
        }

        if (string.Equals(_lastFirst, first, StringComparison.Ordinal) &&
            string.Equals(_lastNextLoop, nextLoop, StringComparison.Ordinal))
            return;

        state.SetAnimation(first, loop: false);
        if (HasAnim(sprite, nextLoop))
        {
            state.AddAnimation(nextLoop, 0f, loop: true);
            _lastFirst = first;
            _lastNextLoop = nextLoop;
        }
        else
        {
            _lastFirst = first;
            _lastNextLoop = null;
        }
    }

    private static bool PlaySingle(string anim, bool restartIfSame = false)
    {
        MegaSprite? sprite = _sprite;
        if (sprite == null)
            return false;

        if (!HasAnim(sprite, anim))
        {
            LogMissingAnimOnce(anim);
            return false;
        }

        if (!restartIfSame && string.Equals(_lastFirst, anim, StringComparison.Ordinal) && _lastNextLoop == null)
            return true;

        sprite.GetAnimationState().SetAnimation(anim, loop: false);
        _lastFirst = anim;
        _lastNextLoop = null;
        return true;
    }

    private static string? GetCardUseAnim(CardModel card)
    {
        return card.Type == CardType.Attack
            ? _hasCardAttack ? AnimCardAttack : null
            : _cardNonAttackAnim;
    }

    private static void InitAnimCache(MegaSprite sprite)
    {
        _hasAnimIn = sprite.HasAnimation(AnimIn);
        _hasAnimIdle = sprite.HasAnimation(AnimIdle);
        _hasAnimOut = sprite.HasAnimation(AnimOut);
        _hasCardAttack = sprite.HasAnimation(AnimCardAttack);

        _cardNonAttackAnim = null;
        foreach (string candidate in AnimCardNonAttackCandidates)
        {
            if (!sprite.HasAnimation(candidate))
                continue;

            _cardNonAttackAnim = candidate;
            break;
        }

        _lastFirst = null;
        _lastNextLoop = null;
    }

    private static bool HasAnim(MegaSprite sprite, string anim)
    {
        if (string.Equals(anim, AnimIn, StringComparison.Ordinal))
            return _hasAnimIn;
        if (string.Equals(anim, AnimIdle, StringComparison.Ordinal))
            return _hasAnimIdle;
        if (string.Equals(anim, AnimOut, StringComparison.Ordinal))
            return _hasAnimOut;
        if (string.Equals(anim, AnimCardAttack, StringComparison.Ordinal))
            return _hasCardAttack;

        return sprite.HasAnimation(anim);
    }

    private static void LogMissingAnimOnce(string anim)
    {
        if (MissingAnimsWarned.Add(anim))
            MainFile.Logger.Warn("[MeiLinBattleReadyOverlay] Missing animation: " + anim);
    }
}
