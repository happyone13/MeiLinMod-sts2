using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class MeiLinBattleAnimationGenerateAnimatorPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.Animation.GenerateAnimatorRegistration";

    public static bool IsCritical => false;

    public static string Description => "Register MeiLin animation states for custom animation sequencing";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<MeiLinMod.MeiLinModCode.Character.MeiLinMod>(nameof(MeiLinMod.MeiLinModCode.Character.MeiLinMod.GenerateAnimator))
    ];

    public static void Postfix(MegaSprite controller)
    {
        MeiLinBattleAnimationSequencePatch.RegisterAnimator(controller);
    }
}

public sealed class MeiLinBattleAnimationSetAnimationPrefixPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.Animation.SetAnimationSequencePrefix";

    public static bool IsCritical => false;

    public static string Description => "Convert MeiLin requested animations into queued battle animation sequences";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<MegaAnimationState>(
            nameof(MegaAnimationState.SetAnimation),
            [typeof(string), typeof(bool), typeof(int)])
    ];

    public static bool Prefix(MegaAnimationState __instance, string __0, bool __1, int __2)
    {
        return MeiLinBattleAnimationSequencePatch.SetAnimationWithTrackPrefix(__instance, __0, __1, __2);
    }
}

public sealed class MeiLinBattleAnimationSetAnimationPostfixPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.Animation.SetAnimationSequencePostfix";

    public static bool IsCritical => false;

    public static string Description => "Remember the last MeiLin animation requested for transition sequencing";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<MegaAnimationState>(
            nameof(MegaAnimationState.SetAnimation),
            [typeof(string), typeof(bool), typeof(int)])
    ];

    public static void Postfix(MegaAnimationState __instance, string __0)
    {
        MeiLinBattleAnimationSequencePatch.SetAnimationWithTrackPostfix(__instance, __0);
    }
}

public static class MeiLinBattleAnimationSequencePatch
{
    private sealed class LastAnimationHolder
    {
        public string Name = string.Empty;
    }

    private sealed class RegistrationMarker;

    private sealed class CreatureHolder
    {
        public Creature? Creature;
    }

    private sealed class ActiveAttackSequenceHolder
    {
        public DateTime UntilUtc;
    }

    private static readonly ConditionalWeakTable<MegaAnimationState, LastAnimationHolder> LastAnimations = new();
    private static readonly ConditionalWeakTable<MegaAnimationState, RegistrationMarker> RegisteredStates = new();
    private static readonly ConditionalWeakTable<MegaAnimationState, CreatureHolder> RegisteredCreatures = new();
    private static readonly ConditionalWeakTable<MegaAnimationState, ActiveAttackSequenceHolder> ActiveAttackSequences = new();
    private static bool _sequenceInProgress;

    public static void RegisterAnimator(MegaSprite controller)
    {
        MegaAnimationState? animationState = controller.GetAnimationState();
        if (animationState != null)
        {
            RegisteredStates.GetOrCreateValue(animationState);
            RegisteredCreatures.GetOrCreateValue(animationState).Creature = ResolveCreature(controller);
        }
    }

    public static bool SetAnimationWithTrackPrefix(MegaAnimationState __instance, string __0, bool __1, int __2)
    {
        return HandleSetAnimation(__instance, __0, __1, __2);
    }

    public static void SetAnimationWithTrackPostfix(MegaAnimationState __instance, string __0)
    {
        RememberAnimation(__instance, __0);
    }

    private static bool HandleSetAnimation(MegaAnimationState animationState, string animation, bool loop, int track)
    {
        if (_sequenceInProgress)
        {
            return true;
        }

        if (!RegisteredStates.TryGetValue(animationState, out _))
        {
            return true;
        }

        string requested = Normalize(animation);
        string previous = GetLastAnimation(animationState);

        if (requested == "buff_play")
        {
            if (TryPlayTwoStepSequence(animationState, track, "buff_ready", "buff_play", loopSecond: false))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        if (requested == "death")
        {
            if (TryPlayTwoStepSequence(animationState, track, "death_ready", "death", loopSecond: false))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        if (requested == "b_idle" && previous == "idle")
        {
            if (TryPlayTwoStepSequence(animationState, track, "idle_to_b_idle", "b_idle", loopSecond: true))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        if (requested == "idle" && previous == "b_idle")
        {
            if (TryPlayTwoStepSequence(animationState, track, "b_idle_to_idle", "idle", loopSecond: true))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        return true;
    }

    private static bool TryPlayAttackSequence(MegaAnimationState animationState, int track, int hitCount)
    {
        try
        {
            _sequenceInProgress = true;
            animationState.SetAnimation("attack_play1", false, track);

            IReadOnlyList<string> commands = MeiLinBattleAnimationService.BuildAttackCommands(hitCount);
            MarkAttackSequenceActive(animationState, commands);
            MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Starting attack sequence. hits={Math.Max(1, hitCount)}, commands={string.Join(",", commands)}");

            for (int i = 1; i < commands.Count; i++)
            {
                string clip = commands[i];
                if (!TryAddAnimation(animationState, clip, false, track, 0f))
                {
                    MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Failed to queue attack clip '{clip}' on track {track}.");
                }
            }

            string endClip = commands[^1] == "u2_attack_play" ? "u2_attack_end" : "attack_end";
            if (!TryAddAnimation(animationState, endClip, false, track, 0f))
            {
                MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Failed to queue attack clip '{endClip}' on track {track}.");
            }

            StartAttackVfxSequence(animationState, commands);
            return true;
        }
        finally
        {
            _sequenceInProgress = false;
        }
    }

    private static bool IsAttackSequenceActive(MegaAnimationState animationState)
    {
        if (!ActiveAttackSequences.TryGetValue(animationState, out ActiveAttackSequenceHolder? holder))
            return false;

        return DateTime.UtcNow < holder.UntilUtc;
    }

    private static void MarkAttackSequenceActive(MegaAnimationState animationState, IReadOnlyList<string> commands)
    {
        float seconds = 0f;
        foreach (string command in commands)
            seconds += MathF.Max(0.2f, MeiLinCommandVfxCoordinator.GetCommandDurationSeconds(command));

        string endClip = commands[^1] == "u2_attack_play" ? "u2_attack_end" : "attack_end";
        seconds += MathF.Max(0.2f, MeiLinCommandVfxCoordinator.GetCommandDurationSeconds(endClip));
        seconds += 0.25f;

        ActiveAttackSequences.GetOrCreateValue(animationState).UntilUtc = DateTime.UtcNow.AddSeconds(seconds);
    }

    private static bool TryPlayTwoStepSequence(
        MegaAnimationState animationState,
        int track,
        string first,
        string second,
        bool loopSecond)
    {
        try
        {
            _sequenceInProgress = true;
            animationState.SetAnimation(first, false, track);

            if (!TryAddAnimation(animationState, second, loopSecond, track, 0f))
            {
                MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Failed to queue follow-up clip '{second}' on track {track}.");
            }
            return true;
        }
        finally
        {
            _sequenceInProgress = false;
        }
    }

    private static bool TryAddAnimation(MegaAnimationState animationState, string animation, bool loop, int track, float delay)
    {
        var animationStateType = animationState.GetType();
        if (TryInvokeAddAnimation(animationState, animationStateType, new[] { typeof(string), typeof(bool), typeof(float), typeof(int) }, [animation, loop, delay, track]))
        {
            return true;
        }

        if (TryInvokeAddAnimation(animationState, animationStateType, new[] { typeof(int), typeof(string), typeof(bool), typeof(float) }, [track, animation, loop, delay]))
        {
            return true;
        }

        if (TryInvokeAddAnimation(animationState, animationStateType, new[] { typeof(int), typeof(string), typeof(float), typeof(bool) }, [track, animation, delay, loop]))
        {
            return true;
        }

        if (TryInvokeAddAnimation(animationState, animationStateType, new[] { typeof(string), typeof(float), typeof(bool), typeof(int) }, [animation, delay, loop, track]))
        {
            return true;
        }

        if (TryInvokeAddAnimation(animationState, animationStateType, new[] { typeof(string), typeof(bool), typeof(float) }, [animation, loop, delay]))
        {
            return true;
        }

        IEnumerable<MethodInfo> methods = animationState.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => string.Equals(method.Name, "AddAnimation", StringComparison.Ordinal));

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (!TryBuildArguments(parameters, animation, loop, track, delay, out object?[] args))
            {
                continue;
            }

            try
            {
                method.Invoke(animationState, args);
                return true;
            }
            catch
            {
                // Try the next matching overload.
            }
        }

        return false;
    }

    private static bool TryInvokeAddAnimation(MegaAnimationState animationState, Type animationStateType, Type[] parameterTypes, object?[] args)
    {
        MethodInfo? method = animationStateType.GetMethod("AddAnimation", BindingFlags.Instance | BindingFlags.Public, null, parameterTypes, null);
        if (method == null)
        {
            return false;
        }

        try
        {
            method.Invoke(animationState, args);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryBuildArguments(
        IReadOnlyList<ParameterInfo> parameters,
        string animation,
        bool loop,
        int track,
        float delay,
        out object?[] args)
    {
        args = new object?[parameters.Count];

        for (int i = 0; i < parameters.Count; i++)
        {
            Type parameterType = parameters[i].ParameterType;
            if (parameterType == typeof(string))
            {
                args[i] = animation;
                continue;
            }

            if (parameterType == typeof(bool))
            {
                args[i] = loop;
                continue;
            }

            if (parameterType == typeof(int))
            {
                args[i] = track;
                continue;
            }

            if (parameterType == typeof(float))
            {
                args[i] = delay;
                continue;
            }

            if (parameterType == typeof(double))
            {
                args[i] = (double)delay;
                continue;
            }

            return false;
        }

        return true;
    }

    private static string GetLastAnimation(MegaAnimationState animationState)
    {
        return LastAnimations.TryGetValue(animationState, out LastAnimationHolder? holder)
            ? holder.Name
            : string.Empty;
    }

    private static void RememberAnimation(MegaAnimationState animationState, string animation)
    {
        LastAnimations.GetOrCreateValue(animationState).Name = Normalize(animation);
    }

    private static string Normalize(string animation)
    {
        return animation.Trim().ToLowerInvariant();
    }

    private static Creature? ResolveCreature(MegaSprite controller)
    {
        if (controller.BoundObject is not Node node)
            return null;

        Node? current = node;
        while (current != null)
        {
            if (current is NCreature creatureNode)
                return creatureNode.Entity;

            current = current.GetParent();
        }

        return null;
    }

    private static void StartAttackVfxSequence(MegaAnimationState animationState, IReadOnlyList<string> commands)
    {
        Creature? caster = RegisteredCreatures.TryGetValue(animationState, out CreatureHolder? holder)
            ? holder.Creature
            : null;
        caster ??= MeiLinBattleAnimationService.ConsumeNextAttackCaster();
        Creature? target = MeiLinBattleAnimationService.ConsumeNextAttackTarget();

        if (caster == null)
        {
            MainFile.Logger.Info("[MeiLinBattleAnimationSequencePatch] Skip attack VFX: caster is null.");
            return;
        }

        MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Start attack VFX. commands={string.Join(",", commands)}, hasTarget={target != null}");
        _ = PlayAttackVfxSequenceAsync(caster, target, commands);
    }

    private static async Task PlayAttackVfxSequenceAsync(Creature caster, Creature? target, IReadOnlyList<string> commands)
    {
        foreach (string command in commands)
        {
            MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Play attack VFX command={command}");
            MeiLinCommandVfxCoordinator.PlayCommandEffects(command, caster, target);

            float duration = MeiLinCommandVfxCoordinator.GetCommandDurationSeconds(command);
            if (duration <= 0f)
                duration = 0.2f;

            await Cmd.CustomScaledWait(duration, duration);
        }
    }
}
