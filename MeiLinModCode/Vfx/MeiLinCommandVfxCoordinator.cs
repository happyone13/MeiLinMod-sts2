using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Config;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using FileAccess = Godot.FileAccess;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MeiLinMod.MeiLinModCode.Vfx;

public sealed class MeiLinCommandVfxPlaybackOptions
{
    public Vector2 ScreenPosition { get; init; } = Vector2.Zero;
    public Vector2 CenterPosition { get; init; } = Vector2.Zero;
    public bool FollowAttachEffects { get; init; } = true;
    public bool LogTimelineEvents { get; init; }
}

public static class MeiLinCommandVfxCoordinator
{
    private const string CommandConfigPath = "res://MeiLinMod/vfx_configs/1027/generated/meilin_vfx_commands.json";
    private const float ModelVfxScale = 1.3f;
    private const float FootAnchorYOffset = 160f;
    private const float AttackSelfAnchorYOffset = 120f;
    private const string AttackEffectMarkerName = "MeiLinAttackEff";
    private const string FootEffectMarkerName = "MeiLinFootEff";
    private const float TargetGlowLeadSeconds = 0.12f;
    private const float CameraShakeDelaySeconds = 0.04f;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static MeiLinCommandVfxConfig? _cachedConfig;
    private static bool _hitFxMethodResolved;
    private static System.Reflection.MethodInfo? _hitFxMethod;

    public static Node2D? PlayCommandEffects(
        string commandName,
        Creature? caster,
        Creature? target = null,
        MeiLinCommandVfxPlaybackOptions? options = null)
    {
        if (!MeiLinModConfig.UseCombatEffects)
            return null;

        if (string.IsNullOrWhiteSpace(commandName))
            return null;

        var config = LoadConfig();
        if (config == null || !config.Commands.TryGetValue(commandName, out var command))
        {
            MainFile.Logger.Info($"[MeiLinVfx] Command missing: {commandName}");
            return null;
        }

        var room = NCombatRoom.Instance;
        if (room == null || room.CombatVfxContainer == null || !GodotObject.IsInstanceValid(room.CombatVfxContainer))
            return null;

        options ??= new MeiLinCommandVfxPlaybackOptions();

        var root = new Node2D { Name = $"MeiLinCommandVfx_{SafeNodeName(commandName)}" };
        room.CombatVfxContainer.AddChild(root);

        foreach (var effect in command.Effects)
        {
            if (string.IsNullOrWhiteSpace(effect.ScenePath))
                continue;

            var delay = MathF.Max(0f, effect.DelayMs / 1000f);
            StartAfter(root, delay, () => PlayEffect(root, room, effect, caster, target, options));
        }

        if (options.LogTimelineEvents)
            LogTimeline(commandName, command);

        AutoFreeAfter(root, EstimateCommandDurationSeconds(command) + 2f);
        return root;
    }

    public static bool TryGetCommand(string commandName, out MeiLinCommandVfxCommand command)
    {
        command = default!;
        var config = LoadConfig();
        return config != null && config.Commands.TryGetValue(commandName, out command!);
    }

    public static void PreloadConfiguredScenes()
    {
        try
        {
            var config = LoadConfig();
            if (config == null)
                return;

            var scenePaths = config.Commands.Values
                .SelectMany(command => command.Effects)
                .Select(effect => effect.ScenePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal);

            MeiLinVfxHelper.Prewarm(scenePaths!);
            MainFile.Logger.Info($"[MeiLinVfx] Configured VFX scenes preloaded. commands={config.Commands.Count}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Preload failed. ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    public static async Task PlayCommandSetTimelineAsync(
        string commandSetName,
        Creature? caster,
        Creature? target = null,
        Func<Task>? onHit = null,
        MeiLinCommandVfxPlaybackOptions? options = null)
    {
        using var actionScope = MeiLinAnimationSequenceManager.BeginAction($"commandSet:{commandSetName}");
        var config = LoadConfig();
        if (config == null || !config.CommandSets.TryGetValue(commandSetName, out var commandSet))
        {
            MainFile.Logger.Info($"[MeiLinVfx] Command set missing: {commandSetName}");
            if (onHit != null)
                await onHit();
            return;
        }

        var hitFired = false;

        async Task HitOnce()
        {
            if (hitFired || onHit == null)
                return;

            hitFired = true;
            await onHit();
        }

        await PlayCommandTimelineAsync(commandSet.Ready, caster, target, HitOnce, options);

        if (!string.IsNullOrWhiteSpace(commandSet.PlayReady))
        await PlayCommandTimelineAsync(commandSet.PlayReady, caster, target, HitOnce, options);

        if (commandSet.PlayDelay > 0f)
            await Cmd.CustomScaledWait(commandSet.PlayDelay / 1000f, commandSet.PlayDelay / 1000f);

        await PlayCommandTimelineAsync(commandSet.Play, caster, target, HitOnce, options);
        if (!string.IsNullOrWhiteSpace(commandSet.End) && config.Commands.ContainsKey(commandSet.End))
            await PlayCommandTimelineAsync(commandSet.End, caster, target, HitOnce, options);

        if (!hitFired && onHit != null)
            await onHit();

        ReturnCharacterToIdle(caster);
    }

    public static async Task PlayCommandSetUntilFirstHitAsync(
        string commandSetName,
        Creature? caster,
        Creature? target = null,
        MeiLinCommandVfxPlaybackOptions? options = null)
    {
        var hitReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var playbackTask = PlayCommandSetTimelineAsync(
            commandSetName,
            caster,
            target,
            () =>
            {
                hitReached.TrySetResult();
                return Task.CompletedTask;
            },
            options);
        _ = CompleteHitWhenPlaybackStopsAsync(playbackTask, hitReached, $"commandSet={commandSetName}");

        await hitReached.Task;
    }

    public static async Task PlayCommandSequenceTimelineAsync(
        IEnumerable<string?> commandNames,
        Creature? caster,
        Creature? target = null,
        Func<Task>? onHit = null,
        MeiLinCommandVfxPlaybackOptions? options = null)
    {
        using var actionScope = MeiLinAnimationSequenceManager.BeginAction("commandSequence");
        var hitFired = false;

        async Task HitOnce()
        {
            if (hitFired || onHit == null)
                return;

            hitFired = true;
            await onHit();
        }

        foreach (var commandName in commandNames)
            await PlayCommandTimelineAsync(commandName, caster, target, HitOnce, options);

        if (!hitFired && onHit != null)
            await onHit();

        ReturnCharacterToIdle(caster);
    }

    public static async Task PlayCommandSequenceUntilFirstHitAsync(
        IEnumerable<string?> commandNames,
        Creature? caster,
        Creature? target = null,
        MeiLinCommandVfxPlaybackOptions? options = null)
    {
        var hitReached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandList = commandNames.ToArray();

        var playbackTask = PlayCommandSequenceTimelineAsync(
            commandList,
            caster,
            target,
            () =>
            {
                hitReached.TrySetResult();
                return Task.CompletedTask;
            },
            options);
        _ = CompleteHitWhenPlaybackStopsAsync(playbackTask, hitReached, $"commands={string.Join(",", commandList.Where(command => !string.IsNullOrWhiteSpace(command)))}");

        await hitReached.Task;
    }

    public static float GetCommandDurationSeconds(string commandName)
    {
        if (!TryGetCommand(commandName, out var command))
            return 0f;

        return EstimateCommandDurationSeconds(command);
    }

    public static async Task PlayCommandSegmentAsync(
        string? commandName,
        Creature? caster,
        Creature? target = null,
        float waitTime = 0f,
        bool queueEndAnimation = true,
        MeiLinCommandVfxPlaybackOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return;

        var config = LoadConfig();
        if (config == null || !config.Commands.TryGetValue(commandName, out var command))
        {
            MainFile.Logger.Info($"[MeiLinVfx] Command missing: {commandName}");
            return;
        }

        MeiLinAnimationSequenceManager.MarkActionBusy(
            $"segment:{commandName}",
            EstimateCommandDurationSeconds(command) + 0.25f);

        var room = NCombatRoom.Instance;
        if (room == null)
            return;

        NCreature? casterNode = null;
        if (caster != null)
        {
            try
            {
                casterNode = room.GetCreatureNode(caster);
            }
            catch
            {
                casterNode = null;
            }
        }

        var animation = command.Animation.FirstOrDefault();
        if (casterNode != null && GodotObject.IsInstanceValid(casterNode) && !string.IsNullOrWhiteSpace(animation?.AnimationName))
            PlayCharacterAction(casterNode, animation.AnimationName, queueEndAnimation);

        var fallbackHitDelay = GetFirstHitDelaySeconds(command);
        var hitWait = fallbackHitDelay >= 0f
            ? fallbackHitDelay
            : waitTime > 0f
                ? MathF.Min(waitTime * 0.25f, 0.12f)
                : 0.12f;

        PlayCommandEffects(commandName, caster, target, options);
        ScheduleCombatFeedback(room, command, target, hitWait);

        MainFile.Logger.Info($"[MeiLinVfx] Segment wait. command={commandName}, hitWait={hitWait:0.###}, originalWait={waitTime:0.###}, queueEnd={queueEndAnimation}");

        if (hitWait > 0f)
            await Cmd.CustomScaledWait(hitWait, hitWait);
    }

    public static async Task PlayCommandTimelineAsync(
        string? commandName,
        Creature? caster,
        Creature? target = null,
        Func<Task>? onHit = null,
        MeiLinCommandVfxPlaybackOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return;

        var config = LoadConfig();
        if (config == null || !config.Commands.TryGetValue(commandName, out var command))
        {
            MainFile.Logger.Info($"[MeiLinVfx] Command missing: {commandName}");
            return;
        }

        MeiLinAnimationSequenceManager.MarkActionBusy(
            $"timeline:{commandName}",
            EstimateCommandDurationSeconds(command) + 0.25f);

        var room = NCombatRoom.Instance;
        if (room == null)
            return;

        NCreature? casterNode = null;
        if (caster != null)
        {
            try
            {
                casterNode = room.GetCreatureNode(caster);
            }
            catch
            {
                casterNode = null;
            }
        }

        var animation = command.Animation.FirstOrDefault();
        var animationDuration = MathF.Max(0f, animation?.DurationMs ?? 0f) / 1000f;
        var animationDelay = MathF.Max(0f, animation?.DelayMs ?? 0f) / 1000f;

        if (animationDelay > 0f)
            await Cmd.CustomScaledWait(animationDelay, animationDelay);

        if (casterNode != null && GodotObject.IsInstanceValid(casterNode) && !string.IsNullOrWhiteSpace(animation?.AnimationName))
        {
            try
            {
                casterNode.SpineAnimation.SetAnimation(animation.AnimationName, animation.Loop);
                animationDuration = MathF.Max(animationDuration, casterNode.GetCurrentAnimationLength());
            }
            catch (Exception ex)
            {
                MainFile.Logger.Info($"[MeiLinVfx] Character animation failed. command={commandName}, anim={animation.AnimationName}, ex={ex.GetType().Name}: {ex.Message}");
            }
        }

        PlayCommandEffects(commandName, caster, target, options);

        var hitDelay = GetFirstHitDelaySeconds(command);
        if (onHit != null && hitDelay >= 0f)
        {
            var hitTask = RunHitAfter(hitDelay, onHit);
            await Cmd.CustomScaledWait(MathF.Max(animationDuration, hitDelay), MathF.Max(animationDuration, hitDelay));
            await hitTask;
        }
        else if (animationDuration > 0f)
        {
            await Cmd.CustomScaledWait(animationDuration, animationDuration);
        }
    }

    private static void PlayEffect(
        Node2D root,
        NCombatRoom room,
        MeiLinCommandVfxEffect effect,
        Creature? caster,
        Creature? target,
        MeiLinCommandVfxPlaybackOptions options)
    {
        if (!GodotObject.IsInstanceValid(root))
            return;

        var anchor = ResolveAnchor(room, effect, caster, target, options, out var followTarget);
        if (anchor == null)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Skip effect without anchor. file={effect.FileName}, type={effect.Type}");
            return;
        }

        var offset = ParseOffset(effect.OffsetXY) * ResolveModelScale(effect);
        var uniformScale = ResolveEffectScale(effect);
        var instance = MeiLinVfxHelper.PlayComposite(
            effect.ScenePath,
            root,
            anchor.Value + offset,
            uniformScale: uniformScale,
            zIndex: ResolveZIndex(effect));

        if (instance == null)
            return;

        if (Math.Abs(effect.Rotation) > 0.001f)
            instance.RotationDegrees += effect.Rotation;

        if (options.FollowAttachEffects && IsAttachEffect(effect) && followTarget != null)
        {
            var anchorOffset = anchor.Value - followTarget.VfxSpawnPosition;
            instance.AddChild(new MeiLinFollowCreatureVfx
            {
                Target = followTarget,
                AnchorOffset = anchorOffset,
                Offset = offset,
            });
        }
    }

    private static Vector2? ResolveAnchor(
        NCombatRoom room,
        MeiLinCommandVfxEffect effect,
        Creature? caster,
        Creature? target,
        MeiLinCommandVfxPlaybackOptions options,
        out NCreature? followTarget)
    {
        followTarget = null;
        var type = effect.Type?.Trim().ToUpperInvariant();

        if (type == "SELF")
            return TryGetCreaturePosition(room, caster, effect, out followTarget);

        if (type == "TARGET")
            return TryGetCreaturePosition(room, target ?? caster, effect, out followTarget);

        if (type == "SCREEN")
            return options.ScreenPosition == Vector2.Zero ? GetContainerOrigin(room) : options.ScreenPosition;

        if (type == "FOR_CENTER")
            return options.CenterPosition == Vector2.Zero ? GetContainerOrigin(room) : options.CenterPosition;

        return TryGetCreaturePosition(room, caster, effect, out followTarget);
    }

    private static Vector2? TryGetCreaturePosition(NCombatRoom room, Creature? creature, MeiLinCommandVfxEffect effect, out NCreature? creatureNode)
    {
        creatureNode = null;
        if (creature == null)
            return null;

        try
        {
            creatureNode = room.GetCreatureNode(creature);
        }
        catch
        {
            creatureNode = null;
        }

        if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode))
            return null;

        if (IsAttackSelfEffect(effect))
        {
            var markerPosition = TryGetEffectMarkerPosition(creatureNode, AttackEffectMarkerName);
            if (markerPosition != null)
                return markerPosition.Value;

            return creatureNode.VfxSpawnPosition + new Vector2(0f, AttackSelfAnchorYOffset * ResolveModelScale(effect));
        }

        if (IsGroundAnchoredEffect(effect))
        {
            var markerPosition = TryGetEffectMarkerPosition(creatureNode, FootEffectMarkerName);
            if (markerPosition != null)
                return markerPosition.Value;

            return creatureNode.VfxSpawnPosition + new Vector2(0f, FootAnchorYOffset * ResolveModelScale(effect));
        }

        return creatureNode.VfxSpawnPosition;
    }

    private static Vector2? TryGetEffectMarkerPosition(NCreature creatureNode, string markerName)
    {
        try
        {
            Node2D? visualsRoot = creatureNode.Visuals;
            if (visualsRoot != null && GodotObject.IsInstanceValid(visualsRoot))
            {
                Marker2D? marker = visualsRoot.GetNodeOrNull<Marker2D>($"Visuals/{markerName}");
                if (marker != null && GodotObject.IsInstanceValid(marker))
                    return marker.GlobalPosition;

                marker = visualsRoot.GetNodeOrNull<Marker2D>($"%{markerName}");
                if (marker != null && GodotObject.IsInstanceValid(marker))
                    return marker.GlobalPosition;
            }
        }
        catch
        {
        }

        try
        {
            Marker2D? marker = creatureNode.GetNodeOrNull<Marker2D>($"%{markerName}");
            if (marker != null && GodotObject.IsInstanceValid(marker))
                return marker.GlobalPosition;
        }
        catch
        {
        }

        return null;
    }

    private static void PlayCharacterAction(NCreature creatureNode, string animationName, bool queueEndAnimation = true)
    {
        try
        {
            var state = creatureNode.SpineAnimation.GetAnimationState();
            if (state == null)
                return;

            state.SetAnimation(animationName, loop: false);

            if (queueEndAnimation)
            {
                string? endAnimation = ResolveActionEndAnimation(animationName);
                if (!string.IsNullOrWhiteSpace(endAnimation))
                    state.AddAnimation(endAnimation, 0f, loop: false);

                if (IsAttackAnimation(animationName))
                    QueueBattleIdleToIdle(state);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Character segment animation failed. anim={animationName}, ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void QueueAttackEndToIdle(Creature? caster, string? commandName)
    {
        if (caster == null)
            return;

        try
        {
            var room = NCombatRoom.Instance;
            var creatureNode = room?.GetCreatureNode(caster);
            if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode))
                return;

            MeiLinAnimationSequenceManager.PlayAttackEndToIdle(creatureNode, commandName);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Queue attack end idle failed. command={commandName ?? "<null>"}, ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void ReturnCharacterToIdle(Creature? caster)
    {
        if (caster == null)
            return;

        try
        {
            var room = NCombatRoom.Instance;
            var creatureNode = room?.GetCreatureNode(caster);
            if (creatureNode == null || !GodotObject.IsInstanceValid(creatureNode))
                return;

            var state = creatureNode.SpineAnimation.GetAnimationState();
            if (state == null)
                return;

            state.SetAnimation("b_idle_to_idle", loop: false);
            state.AddAnimation("idle", 0f, loop: true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Return idle failed. ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void QueueBattleIdleToIdle(MegaAnimationState state)
    {
        state.AddAnimation("b_idle_to_idle", 0f, loop: false);
        state.AddAnimation("idle", 0f, loop: true);
    }

    private static bool IsGroundAnchoredEffect(MeiLinCommandVfxEffect effect)
    {
        if (string.Equals(effect.Bone, "root", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(effect.Type, "SELF", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(effect.Type, "TARGET", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var name = $"{effect.FileName} {effect.SceneGroup}".ToLowerInvariant();
        return name.Contains("_root", StringComparison.Ordinal) ||
               name.Contains("_bot", StringComparison.Ordinal) ||
               name.Contains("botglow", StringComparison.Ordinal) ||
               name.Contains("ground", StringComparison.Ordinal) ||
               name.Contains("floor", StringComparison.Ordinal) ||
               name.Contains("smoke", StringComparison.Ordinal);
    }

    private static bool IsAttackSelfEffect(MeiLinCommandVfxEffect effect)
    {
        if (!string.Equals(effect.Type, "SELF", StringComparison.OrdinalIgnoreCase))
            return false;

        if (IsGroundAnchoredEffect(effect))
            return false;

        var name = $"{effect.FileName} {effect.SceneGroup}".ToLowerInvariant();
        return name.Contains("attack_play", StringComparison.Ordinal) ||
               name.Contains("strong_attack", StringComparison.Ordinal) ||
               name.Contains("technical_attack", StringComparison.Ordinal);
    }

    private static string? ResolveActionEndAnimation(string animationName)
    {
        if (string.Equals(animationName, "u2_attack_play", StringComparison.OrdinalIgnoreCase))
            return "u2_attack_end";

        if (animationName.StartsWith("attack_play", StringComparison.OrdinalIgnoreCase))
            return "attack_end";

        return null;
    }

    private static bool IsAttackAnimation(string animationName)
    {
        return string.Equals(animationName, "u2_attack_play", StringComparison.OrdinalIgnoreCase) ||
               animationName.StartsWith("attack_play", StringComparison.OrdinalIgnoreCase);
    }

    private static float ResolveModelScale(MeiLinCommandVfxEffect effect)
    {
        return ModelVfxScale;
    }

    private static float ResolveEffectScale(MeiLinCommandVfxEffect effect)
    {
        var scale = effect.Scale <= 0f ? 1f : effect.Scale;
        return scale * ResolveModelScale(effect);
    }

    private static void ScheduleCombatFeedback(
        NCombatRoom room,
        MeiLinCommandVfxCommand command,
        Creature? target,
        float hitDelaySeconds)
    {
        if (target == null || !IsAttackCommand(command))
            return;

        var anticipationDelay = MathF.Max(0f, hitDelaySeconds - TargetGlowLeadSeconds);
        var hitDelay = MathF.Max(0f, hitDelaySeconds);
        StartAfter(room, anticipationDelay, () => PlayTargetAnticipationGlow(command, target));
        StartAfter(room, hitDelay, () => TriggerTargetHitFeedback(room, target));
    }

    private static bool IsAttackCommand(MeiLinCommandVfxCommand command)
    {
        var animationName = command.Animation.FirstOrDefault()?.AnimationName ?? "";
        return IsAttackAnimation(animationName);
    }

    private static void PlayTargetAnticipationGlow(MeiLinCommandVfxCommand command, Creature target)
    {
        var glow = command.Effects.FirstOrDefault(effect =>
            !string.IsNullOrWhiteSpace(effect.ScenePath) &&
            effect.FileName.Contains("botglow", StringComparison.OrdinalIgnoreCase));

        if (glow == null)
            return;

        var room = NCombatRoom.Instance;
        if (room == null)
            return;

        var glowCopy = glow.CloneForTargetRoot();
        var root = new Node2D { Name = "MeiLinTargetAnticipationGlow" };
        room.CombatVfxContainer.AddChild(root);
        PlayEffect(root, room, glowCopy, target, target, new MeiLinCommandVfxPlaybackOptions());
        AutoFreeAfter(root, 1.2f);
    }

    private static void TriggerTargetHitFeedback(NCombatRoom room, Creature target)
    {
        TryTriggerTargetHitFx(target);

        var tree = room.GetTree();
        if (tree != null)
            _ = MeiLinTimelineHitstop.PlayAsync(tree);

        StartAfter(room, CameraShakeDelaySeconds, () =>
        {
            if (GodotObject.IsInstanceValid(room))
                _ = MeiLinTimelineCameraShake.PlayAsync(room);
        });
    }

    private static void TryTriggerTargetHitFx(Creature target)
    {
        try
        {
            if (!_hitFxMethodResolved)
            {
                _hitFxMethodResolved = true;
                _hitFxMethod =
                    AccessTools.Method(typeof(CreatureCmd), "TriggerHitFx", [typeof(Creature)]) ??
                    AccessTools.Method(typeof(CreatureCmd), "TriggerHitVfx", [typeof(Creature)]) ??
                    AccessTools.Method(typeof(CreatureCmd), "TriggerHurtFx", [typeof(Creature)]) ??
                    AccessTools.Method(typeof(CreatureCmd), "TriggerHurtVfx", [typeof(Creature)]);
            }

            _hitFxMethod?.Invoke(null, [target]);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Trigger target hit fx failed. ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Vector2 GetContainerOrigin(NCombatRoom room)
    {
        return Vector2.Zero;
    }

    private static bool IsAttachEffect(MeiLinCommandVfxEffect effect)
    {
        return string.Equals(effect.BoneType, "ATTACH", StringComparison.OrdinalIgnoreCase);
    }

    private static int? ResolveZIndex(MeiLinCommandVfxEffect effect)
    {
        if (effect.GlobalZ != 0)
            return effect.GlobalZ;

        return effect.ZOrder;
    }

    private static Vector2 ParseOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Vector2.Zero;

        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return Vector2.Zero;

        return float.TryParse(parts[0], out var x) && float.TryParse(parts[1], out var y)
            ? new Vector2(x, y)
            : Vector2.Zero;
    }

    private static MeiLinCommandVfxConfig? LoadConfig()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        if (!FileAccess.FileExists(CommandConfigPath))
        {
            MainFile.Logger.Info($"[MeiLinVfx] Command config missing: {CommandConfigPath}");
            return null;
        }

        using var file = FileAccess.Open(CommandConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Command config open failed: {CommandConfigPath}");
            return null;
        }

        try
        {
            _cachedConfig = JsonSerializer.Deserialize<MeiLinCommandVfxConfig>(file.GetAsText(), JsonOptions);
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Command config parse failed. ex={ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static float EstimateCommandDurationSeconds(MeiLinCommandVfxCommand command)
    {
        var maxMs = 0f;

        foreach (var animation in command.Animation)
            maxMs = MathF.Max(maxMs, animation.DelayMs + MathF.Max(0f, animation.DurationMs));

        foreach (var effect in command.Effects)
            maxMs = MathF.Max(maxMs, effect.DelayMs + MathF.Max(0f, effect.DurationMs));

        foreach (var timelineEvent in command.Hits.Concat(command.Shakes).Concat(command.Stops))
            maxMs = MathF.Max(maxMs, timelineEvent.DelayMs + MathF.Max(0f, timelineEvent.Duration));

        return MathF.Max(0.1f, maxMs / 1000f);
    }

    private static float GetFirstHitDelaySeconds(MeiLinCommandVfxCommand command)
    {
        if (command.Hits.Count == 0)
            return -1f;

        var hitMs = command.Hits
            .Select(hit => MathF.Max(hit.DelayMs, hit.MotionDelay))
            .Where(delay => delay >= 0f)
            .DefaultIfEmpty(0f)
            .Min();

        return MathF.Max(0f, hitMs / 1000f);
    }

    private static async Task RunHitAfter(float seconds, Func<Task> onHit)
    {
        if (seconds > 0f)
            await Cmd.CustomScaledWait(seconds, seconds);

        await onHit();
    }

    private static async Task CompleteHitWhenPlaybackStopsAsync(
        Task playbackTask,
        TaskCompletionSource hitReached,
        string context)
    {
        try
        {
            await playbackTask;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Timeline playback failed before hit. {context}, ex={ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            hitReached.TrySetResult();
        }
    }

    private static void LogTimeline(string commandName, MeiLinCommandVfxCommand command)
    {
        foreach (var hit in command.Hits)
            MainFile.Logger.Info($"[MeiLinVfx] Command hit. command={commandName}, name={hit.Name}, delayMs={hit.DelayMs}");

        foreach (var shake in command.Shakes)
            MainFile.Logger.Info($"[MeiLinVfx] Command shake. command={commandName}, name={shake.FileName}, delayMs={shake.DelayMs}");

        foreach (var stop in command.Stops)
            MainFile.Logger.Info($"[MeiLinVfx] Command stop. command={commandName}, type={stop.Type}, strong={stop.StrongType}, delayMs={stop.DelayMs}, durationMs={stop.Duration}");
    }

    private static async void StartAfter(Node node, float seconds, Action action)
    {
        if (!GodotObject.IsInstanceValid(node))
            return;

        try
        {
            var tree = node.GetTree();
            if (tree != null && seconds > 0f)
            {
                var timer = tree.CreateTimer(seconds);
                await node.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
            }
        }
        catch
        {
        }

        if (!GodotObject.IsInstanceValid(node))
            return;

        try
        {
            action();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Delayed action failed. ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async void AutoFreeAfter(Node node, float seconds)
    {
        if (!GodotObject.IsInstanceValid(node))
            return;

        try
        {
            var tree = node.GetTree();
            if (tree != null)
            {
                var timer = tree.CreateTimer(MathF.Max(0.1f, seconds));
                await node.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
            }
        }
        catch
        {
        }

        if (GodotObject.IsInstanceValid(node))
            node.QueueFree();
    }

    private static string SafeNodeName(string value)
    {
        return string.Concat(value.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_'));
    }
}

public sealed class MeiLinCommandVfxConfig
{
    [JsonPropertyName("commandSets")]
    public Dictionary<string, MeiLinCommandVfxCommandSet> CommandSets { get; set; } = new();

    [JsonPropertyName("commands")]
    public Dictionary<string, MeiLinCommandVfxCommand> Commands { get; set; } = new();
}

public sealed class MeiLinCommandVfxCommandSet
{
    [JsonPropertyName("ready")]
    public string? Ready { get; set; }

    [JsonPropertyName("play_ready")]
    public string? PlayReady { get; set; }

    [JsonPropertyName("play")]
    public string? Play { get; set; }

    [JsonPropertyName("play_delay")]
    public float PlayDelay { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }
}

public sealed class MeiLinCommandVfxCommand
{
    [JsonPropertyName("animation")]
    public List<MeiLinCommandVfxAnimation> Animation { get; set; } = new();

    [JsonPropertyName("effects")]
    public List<MeiLinCommandVfxEffect> Effects { get; set; } = new();

    [JsonPropertyName("hits")]
    public List<MeiLinCommandVfxTimelineEvent> Hits { get; set; } = new();

    [JsonPropertyName("shakes")]
    public List<MeiLinCommandVfxTimelineEvent> Shakes { get; set; } = new();

    [JsonPropertyName("stops")]
    public List<MeiLinCommandVfxTimelineEvent> Stops { get; set; } = new();

    [JsonPropertyName("closeCombat")]
    public bool CloseCombat { get; set; }

    [JsonPropertyName("closeCombatOffset")]
    public string? CloseCombatOffset { get; set; }
}

public sealed class MeiLinCommandVfxAnimation
{
    [JsonPropertyName("animationName")]
    public string? AnimationName { get; set; }

    [JsonPropertyName("delayMs")]
    public float DelayMs { get; set; }

    [JsonPropertyName("durationMs")]
    public float DurationMs { get; set; }

    [JsonPropertyName("loop")]
    public bool Loop { get; set; }
}

public sealed class MeiLinCommandVfxEffect
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = "";

    [JsonPropertyName("sceneGroup")]
    public string SceneGroup { get; set; } = "";

    [JsonPropertyName("scenePath")]
    public string ScenePath { get; set; } = "";

    [JsonPropertyName("delayMs")]
    public float DelayMs { get; set; }

    [JsonPropertyName("durationMs")]
    public float DurationMs { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("boneType")]
    public string? BoneType { get; set; }

    [JsonPropertyName("bone")]
    public string? Bone { get; set; }

    [JsonPropertyName("offsetXY")]
    public string? OffsetXY { get; set; }

    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 1f;

    [JsonPropertyName("inheritScale")]
    public bool InheritScale { get; set; }

    [JsonPropertyName("formationScale")]
    public bool FormationScale { get; set; }

    [JsonPropertyName("ignoreSlotScale")]
    public bool IgnoreSlotScale { get; set; }

    [JsonPropertyName("rotation")]
    public float Rotation { get; set; }

    [JsonPropertyName("zOrder")]
    public int ZOrder { get; set; }

    [JsonPropertyName("globalZ")]
    public int GlobalZ { get; set; }

    public MeiLinCommandVfxEffect CloneForTargetRoot()
    {
        return new MeiLinCommandVfxEffect
        {
            FileName = FileName,
            SceneGroup = SceneGroup,
            ScenePath = ScenePath,
            DelayMs = 0f,
            DurationMs = DurationMs,
            Type = "TARGET",
            BoneType = "POSITION",
            Bone = "root",
            OffsetXY = OffsetXY,
            Scale = Scale,
            InheritScale = InheritScale,
            FormationScale = FormationScale,
            IgnoreSlotScale = IgnoreSlotScale,
            Rotation = Rotation,
            ZOrder = ZOrder,
            GlobalZ = GlobalZ
        };
    }
}

public sealed class MeiLinCommandVfxTimelineEvent
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("strong_type")]
    public string? StrongType { get; set; }

    [JsonPropertyName("delayMs")]
    public float DelayMs { get; set; }

    [JsonPropertyName("motion_delay")]
    public float MotionDelay { get; set; }

    [JsonPropertyName("duration")]
    public float Duration { get; set; }
}
