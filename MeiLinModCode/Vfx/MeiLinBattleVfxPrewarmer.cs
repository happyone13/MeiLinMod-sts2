using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Config;

namespace MeiLinMod.MeiLinModCode.Vfx;

public static class MeiLinBattleVfxPrewarmer
{
    private const float WarmAlpha = 0.001f;
    private static int _generation;

    public static void Start(NCombatRoom room)
    {
        if (!MeiLinModConfig.UseCombatEffects || room == null || !GodotObject.IsInstanceValid(room))
            return;

        int generation = Interlocked.Increment(ref _generation);
        string[] scenePaths = MeiLinCommandVfxCoordinator.GetBattleWarmScenePaths()
            .Concat(MeiLinAttackMovementController.GetPreloadScenePaths())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        RunAsync(room, generation, scenePaths);
    }

    private static async void RunAsync(NCombatRoom room, int generation, IReadOnlyList<string> scenePaths)
    {
        int warmed = 0;
        int failed = 0;
        try
        {
            await NextFrame(room);
            await NextFrame(room);

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

                instance.Modulate = new Color(1f, 1f, 1f, WarmAlpha);
                warmed++;
                await NextFrame(room);
                await NextFrame(room);

                if (GodotObject.IsInstanceValid(instance))
                    instance.QueueFree();

                await NextFrame(room);
            }

            if (IsCurrent(room, generation))
            {
                MainFile.Logger.Info(
                    $"[MeiLinVfx] Battle deep prewarm complete. warmed={warmed}/{scenePaths.Count}, failed={failed}.");
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
