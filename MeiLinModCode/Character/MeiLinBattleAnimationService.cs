using System;
using System.Collections.Generic;

namespace MeiLinMod.MeiLinModCode.Character;

public static class MeiLinBattleAnimationService
{
    private static readonly object Sync = new();
    private static readonly Queue<int> PendingAttackHits = new();

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
}
