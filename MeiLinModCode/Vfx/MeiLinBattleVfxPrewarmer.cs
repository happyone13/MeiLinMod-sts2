using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Config;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.MeiLinModCode.Vfx;

public static class MeiLinBattleVfxPrewarmer
{
    private const float WarmAlpha = 0.001f;
    private static int _generation;

    public static void Start(NCombatRoom room)
    {
        int generation = Interlocked.Increment(ref _generation);
        if (!MeiLinModConfig.UseCombatEffects || room == null || !GodotObject.IsInstanceValid(room))
            return;
        RunAsync(room, generation);
    }

    private static async void RunAsync(NCombatRoom room, int generation)
    {
        int warmed = 0;
        int failed = 0;
        try
        {
            await NextFrame(room);
            if (!IsCurrent(room, generation))
                return;
            await NextFrame(room);
            if (!IsCurrent(room, generation) ||
                !room.CreatureNodes.Any(node => node.Entity?.Player?.Character is MeiLinCharacter))
                return;

            var movementPaths = MeiLinAttackMovementController.GetPreloadScenePaths().ToHashSet(StringComparer.Ordinal);
            string[] scenePaths = MeiLinCommandVfxCoordinator.GetBattleWarmScenePaths()
                .Concat(movementPaths)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (string scenePath in scenePaths)
            {
                if (!IsCurrent(room, generation))
                    return;

                Node2D? instance = MeiLinVfxHelper.PlayComposite(
                    scenePath,
                    room.CombatVfxContainer,
                    Vector2.Zero);
                if (instance == null)
                {
                    failed++;
                    await NextFrame(room);
                    continue;
                }

                var lease = OneShotVfxPool.Find(instance);
                int instanceGeneration = lease?.Generation ?? 0;
                try
                {
                    instance.Modulate = new Color(1f, 1f, 1f, WarmAlpha);
                    warmed++;
                    await NextFrame(room);
                    if (!IsCurrent(room, generation))
                        return;
                    await NextFrame(room);
                    if (!IsCurrent(room, generation))
                        return;
                }
                finally
                {
                    // Command timelines use temporary parents; retain only movement instances here.
                    if (lease != null && movementPaths.Contains(scenePath))
                        lease.Return(instanceGeneration);
                    else if (GodotObject.IsInstanceValid(instance) &&
                             (lease == null || lease.Generation == instanceGeneration))
                        instance.QueueFree();
                }

                await NextFrame(room);
            }

            if (IsCurrent(room, generation))
            {
                MainFile.Logger.Info(
                    $"[MeiLinVfx] Battle deep prewarm complete. warmed={warmed}/{scenePaths.Length}, failed={failed}.");
            }
        }
        catch (Exception ex)
        {
            if (IsCurrent(room, generation))
                MainFile.Logger.Info($"[MeiLinVfx] Battle deep prewarm stopped. ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool IsCurrent(NCombatRoom room, int generation)
    {
        return generation == Volatile.Read(ref _generation) &&
               MeiLinModConfig.UseCombatEffects &&
               GodotObject.IsInstanceValid(room) &&
               ReferenceEquals(NCombatRoom.Instance, room) &&
               room.CombatVfxContainer != null &&
               GodotObject.IsInstanceValid(room.CombatVfxContainer);
    }

    private static async Task NextFrame(Node node)
    {
        SceneTree? tree = node.GetTree();
        if (tree != null)
            await node.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
