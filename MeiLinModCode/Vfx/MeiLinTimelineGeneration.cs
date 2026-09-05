using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeiLinMod.MeiLinModCode.Vfx;

/// <summary>
/// Identifies one logical command timeline for a creature. Starting a newer
/// timeline invalidates delayed gameplay callbacks from the older one without
/// forcing already spawned visual nodes to disappear.
/// </summary>
internal readonly record struct MeiLinTimelineLease(
    Creature? Caster,
    NCombatRoom? Room,
    int Generation)
{
    public bool IsCurrent => MeiLinTimelineGeneration.IsCurrent(this);
}

internal static class MeiLinTimelineGeneration
{
    private sealed class State
    {
        public int Generation;
    }

    private static readonly ConditionalWeakTable<Creature, State> States = new();
    private static readonly object Gate = new();

    public static MeiLinTimelineLease Begin(Creature? caster)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (caster == null)
            return new MeiLinTimelineLease(null, room, 0);

        lock (Gate)
        {
            State state = States.GetOrCreateValue(caster);
            state.Generation = Next(state.Generation);
            return new MeiLinTimelineLease(caster, room, state.Generation);
        }
    }

    public static bool IsCurrent(MeiLinTimelineLease lease)
    {
        if (!ReferenceEquals(NCombatRoom.Instance, lease.Room))
            return false;

        if (lease.Caster == null)
            return true;

        lock (Gate)
        {
            return States.TryGetValue(lease.Caster, out State? state) &&
                   state.Generation == lease.Generation;
        }
    }

    public static void Invalidate(Creature? caster)
    {
        if (caster == null)
            return;

        lock (Gate)
        {
            State state = States.GetOrCreateValue(caster);
            state.Generation = Next(state.Generation);
        }
    }

    private static int Next(int generation) =>
        generation == int.MaxValue ? 1 : generation + 1;
}
