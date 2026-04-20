using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch]
public static class MeiLinBattleAnimationSequencePatch
{
    private sealed class LastAnimationHolder
    {
        public string Name = string.Empty;
    }

    private sealed class RegistrationMarker;

    private static readonly ConditionalWeakTable<MegaAnimationState, LastAnimationHolder> LastAnimations = new();
    private static readonly ConditionalWeakTable<MegaAnimationState, RegistrationMarker> RegisteredStates = new();
    private static bool _sequenceInProgress;

    [HarmonyPatch(typeof(MeiLinMod.MeiLinModCode.Character.MeiLinMod), nameof(MeiLinMod.MeiLinModCode.Character.MeiLinMod.GenerateAnimator))]
    [HarmonyPostfix]
    public static void GenerateAnimatorPostfix(MegaSprite controller)
    {
        MegaAnimationState? animationState = controller.GetAnimationState();
        if (animationState != null)
        {
            RegisteredStates.GetOrCreateValue(animationState);
        }
    }

    [HarmonyPatch(typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), [typeof(string), typeof(bool), typeof(int)])]
    [HarmonyPrefix]
    public static bool SetAnimationWithTrackPrefix(MegaAnimationState __instance, string __0, bool __1, int __2, ref MegaTrackEntry? __result)
    {
        return HandleSetAnimation(__instance, __0, __1, __2, ref __result);
    }

    [HarmonyPatch(typeof(MegaAnimationState), nameof(MegaAnimationState.SetAnimation), [typeof(string), typeof(bool), typeof(int)])]
    [HarmonyPostfix]
    public static void SetAnimationWithTrackPostfix(MegaAnimationState __instance, string __0, ref MegaTrackEntry? __result)
    {
        if (__result != null)
        {
            RememberAnimation(__instance, __0);
        }
    }

    private static bool HandleSetAnimation(MegaAnimationState animationState, string animation, bool loop, int track, ref MegaTrackEntry? result)
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

        if (requested == "attack_play1")
        {
            int hitCount = MeiLinBattleAnimationService.ConsumeNextAttackHits();
            if (TryPlayAttackSequence(animationState, track, hitCount, out result))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        if (requested == "buff_play")
        {
            if (TryPlayTwoStepSequence(animationState, track, "buff_ready", "buff_play", loopSecond: false, out result))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        if (requested == "death")
        {
            if (TryPlayTwoStepSequence(animationState, track, "death_ready", "death", loopSecond: false, out result))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        if (requested == "b_idle" && previous == "idle")
        {
            if (TryPlayTwoStepSequence(animationState, track, "idle_to_b_idle", "b_idle", loopSecond: true, out result))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        if (requested == "idle" && previous == "b_idle")
        {
            if (TryPlayTwoStepSequence(animationState, track, "b_idle_to_idle", "idle", loopSecond: true, out result))
            {
                RememberAnimation(animationState, requested);
                return false;
            }

            return true;
        }

        return true;
    }

    private static bool TryPlayAttackSequence(MegaAnimationState animationState, int track, int hitCount, out MegaTrackEntry? result)
    {
        result = null;

        try
        {
            _sequenceInProgress = true;
            result = animationState.SetAnimation("attack_play1", false, track);
            if (result == null)
            {
                return false;
            }

            int totalHits = Math.Max(1, hitCount);
            for (int i = 1; i < totalHits; i++)
            {
                string clip = i < totalHits - 1
                    ? "attack_play1"
                    : "u2_attack_play";
                if (!TryAddAnimation(animationState, clip, false, track, 0f))
                {
                    MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Failed to queue attack clip '{clip}' on track {track}.");
                }
            }

            if (!TryAddAnimation(animationState, "attack_end", false, track, 0f))
            {
                MainFile.Logger.Info($"[MeiLinBattleAnimationSequencePatch] Failed to queue attack clip 'attack_end' on track {track}.");
            }
            return true;
        }
        finally
        {
            _sequenceInProgress = false;
        }
    }

    private static bool TryPlayTwoStepSequence(
        MegaAnimationState animationState,
        int track,
        string first,
        string second,
        bool loopSecond,
        out MegaTrackEntry? result)
    {
        result = null;

        try
        {
            _sequenceInProgress = true;
            result = animationState.SetAnimation(first, false, track);
            if (result == null)
            {
                return false;
            }

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
}
