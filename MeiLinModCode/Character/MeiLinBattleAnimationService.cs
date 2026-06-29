using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MeiLinMod.MeiLinModCode.Character;

public static class MeiLinBattleAnimationService
{
    public sealed class AttackSegment
    {
        public required string Command { get; init; }
        public Creature? Target { get; init; }
        public int RemainingSegments { get; init; }
        public bool IsFirstSegment { get; init; }
    }

    private static readonly object Sync = new();
    private static readonly Queue<int> PendingAttackHits = new();
    private static readonly Queue<Creature?> PendingAttackCasters = new();
    private static readonly Queue<Creature?> PendingAttackTargets = new();
    private static readonly Queue<string> ActiveAttackCommands = new();
    private static Creature? ActiveAttackCaster;
    private static Creature? ActiveAttackTarget;
    private static int ActiveAttackTotalSegments;
    private static DateTime ActiveAttackExpiresUtc = DateTime.MinValue;
    private static Creature? LastAttackCaster;
    private static Creature? LastAttackTarget;

    public static void PrepareNextAttackHits(int hitCount)
    {
        lock (Sync)
        {
            PendingAttackHits.Enqueue(Math.Max(1, hitCount));
        }
    }

    public static int ConsumeNextAttackHits()
    {
        lock (Sync)
        {
            return PendingAttackHits.Count > 0 ? PendingAttackHits.Dequeue() : 1;
        }
    }

    public static void PrepareNextAttackContext(Creature? caster, Creature? target)
    {
        lock (Sync)
        {
            PendingAttackCasters.Enqueue(caster);
            PendingAttackTargets.Enqueue(target);
            LastAttackCaster = caster;
            LastAttackTarget = target;
        }
    }

    public static void PrepareNextAttackTarget(Creature? target)
    {
        lock (Sync)
        {
            PendingAttackTargets.Enqueue(target);
            LastAttackTarget = target;
        }
    }

    public static Creature? ConsumeNextAttackCaster()
    {
        lock (Sync)
        {
            return PendingAttackCasters.Count > 0 ? PendingAttackCasters.Dequeue() : LastAttackCaster;
        }
    }

    public static Creature? ConsumeNextAttackTarget()
    {
        lock (Sync)
        {
            return PendingAttackTargets.Count > 0 ? PendingAttackTargets.Dequeue() : LastAttackTarget;
        }
    }

    public static AttackSegment ConsumeNextAttackSegment(Creature caster)
    {
        lock (Sync)
        {
            var now = DateTime.UtcNow;
            if (ActiveAttackCommands.Count == 0 ||
                ActiveAttackCaster != caster ||
                now >= ActiveAttackExpiresUtc)
            {
                var totalHits = PendingAttackHits.Count > 0 ? PendingAttackHits.Dequeue() : 1;
                ActiveAttackCommands.Clear();
                foreach (var command in BuildAttackCommands(totalHits))
                    ActiveAttackCommands.Enqueue(command);

                ActiveAttackTotalSegments = ActiveAttackCommands.Count;
                ActiveAttackCaster = PendingAttackCasters.Count > 0 ? PendingAttackCasters.Dequeue() : caster;
                ActiveAttackTarget = PendingAttackTargets.Count > 0 ? PendingAttackTargets.Dequeue() : LastAttackTarget;
                LastAttackCaster = ActiveAttackCaster;
                LastAttackTarget = ActiveAttackTarget;
            }

            ActiveAttackExpiresUtc = now.AddSeconds(10);
            var remainingBeforeDequeue = ActiveAttackCommands.Count;
            var nextCommand = ActiveAttackCommands.Count > 0 ? ActiveAttackCommands.Dequeue() : "attack_play1";
            var remaining = ActiveAttackCommands.Count;
            if (remaining == 0)
                ActiveAttackExpiresUtc = now.AddSeconds(0.5);

            return new AttackSegment
            {
                Command = nextCommand,
                Target = ActiveAttackTarget,
                RemainingSegments = remaining,
                IsFirstSegment = remainingBeforeDequeue == ActiveAttackTotalSegments
            };
        }
    }

    public static IReadOnlyList<string> BuildAttackCommands(int hitCount)
    {
        int totalHits = Math.Max(1, hitCount);
        var commands = new List<string>(totalHits);

        for (int i = 0; i < totalHits; i++)
        {
            if (totalHits > 3 && i == totalHits - 1)
            {
                commands.Add("u2_attack_play");
                continue;
            }

            commands.Add(i % 2 == 0 ? "attack_play1" : "attack_play2");
        }

        return commands;
    }
}
