using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeiLinMod.MeiLinModCode.Vfx;

public static class MeiLinVfxHelper
{
    private const string DelayMeta = "meilin_vfx_delay_sec";
    private const string PreviewAnimationProperty = "preview_animation";
    private static readonly object SceneCacheLock = new();
    private static readonly Dictionary<string, PackedScene?> SceneCache = new(StringComparer.Ordinal);

    public static void Prewarm(IEnumerable<string> scenePaths)
    {
        foreach (var scenePath in scenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                continue;

            _ = GetOrLoadScene(scenePath);
        }
    }

    public static Node2D? PlayAtCreature(
        string scenePath,
        Creature? creature,
        Vector2 offset = default,
        float uniformScale = 1f,
        string animationName = "animation",
        int? zIndex = null,
        bool followCreature = false)
    {
        if (creature == null)
            return null;

        var room = NCombatRoom.Instance;
        var creatureNode = room?.GetCreatureNode(creature);
        if (room == null || creatureNode == null || !GodotObject.IsInstanceValid(creatureNode))
            return null;

        var instance = PlayComposite(
            scenePath,
            room.CombatVfxContainer,
            creatureNode.VfxSpawnPosition + offset,
            animationName,
            uniformScale,
            zIndex);

        if (instance != null && followCreature)
            instance.AddChild(new MeiLinFollowCreatureVfx { Target = creatureNode, Offset = offset });

        return instance;
    }

    public static Node2D? PlayComposite(
        string scenePath,
        Node parent,
        Vector2 globalPosition,
        string animationName = "animation",
        float uniformScale = 1f,
        int? zIndex = null)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        var scene = GetOrLoadScene(scenePath);
        if (scene == null)
            return null;

        Node2D root;
        try
        {
            root = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
            parent.AddChild(root);
            root.GlobalPosition = globalPosition;
            root.Scale *= new Vector2(uniformScale, uniformScale);
            if (zIndex.HasValue)
                root.ZIndex = zIndex.Value;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Instantiate failed. scene={scenePath}, ex={ex.GetType().Name}: {ex.Message}");
            return null;
        }

        PlaySpinesAndAutoFree(root, animationName);
        return root;
    }

    private static PackedScene? GetOrLoadScene(string scenePath)
    {
        lock (SceneCacheLock)
        {
            if (SceneCache.TryGetValue(scenePath, out var cachedScene))
                return cachedScene;
        }

        PackedScene? scene;
        try
        {
            scene = ResourceLoader.Load<PackedScene>(scenePath, "", ResourceLoader.CacheMode.Reuse);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Load failed. scene={scenePath}, ex={ex.GetType().Name}: {ex.Message}");
            return null;
        }

        if (scene == null)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Scene missing: {scenePath}");
            return null;
        }

        lock (SceneCacheLock)
        {
            SceneCache[scenePath] = scene;
        }

        return scene;
    }

    public static void TriggerParticles(Node root)
    {
        foreach (var node in EnumerateNodes(root))
        {
            if (node is not GpuParticles2D particles)
                continue;

            try
            {
                particles.Restart();
                particles.Emitting = true;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Info($"[MeiLinVfx] Particle trigger failed. node={node.Name}, ex={ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    public static void PlaySpinesAndAutoFree(Node2D root, string animationName)
    {
        if (!GodotObject.IsInstanceValid(root))
            return;

        var nodes = EnumerateNodes(root).ToList();
        HideDelayedNodes(nodes);

        var sprites = new List<(Node Node, MegaSprite Sprite, string AnimationName, float Delay)>();
        foreach (var node in nodes)
        {
            try
            {
                var sprite = new MegaSprite(node);
                var resolvedAnimationName = ResolveAnimationName(node, sprite, animationName);
                if (resolvedAnimationName != null)
                    sprites.Add((node, sprite, resolvedAnimationName, GetEffectiveDelay(node)));
            }
            catch
            {
                // Node is not compatible with MegaSprite.
            }
        }

        foreach (var node in nodes)
        {
            if (node is not GpuParticles2D particles)
                continue;

            var delay = GetEffectiveDelay(node);
            StartAfter(root, delay, () =>
            {
                ShowDelayedAncestors(node);
                TriggerParticle(particles);
            });
        }

        var particleDuration = EstimateParticleDuration(nodes);
        var pendingConditions = 0;
        var freed = false;

        void CompleteCondition()
        {
            if (freed)
                return;

            pendingConditions--;
            if (pendingConditions > 0)
                return;

            freed = true;
            QueueFree(root);
        }

        if (sprites.Count > 0)
        {
            pendingConditions++;
            var remainingSprites = sprites.Count;

            foreach (var pair in sprites)
            {
                var sprite = pair.Sprite;
                var node = pair.Node;

                sprite.ConnectAnimationCompleted(Callable.From<GodotObject, GodotObject, GodotObject>((_, _, _) =>
                {
                    if (freed)
                        return;

                    remainingSprites--;
                    if (remainingSprites > 0)
                        return;

                    CompleteCondition();
                }));

                StartAfter(root, pair.Delay, () =>
                {
                    ShowDelayedAncestors(node);
                    try
                    {
                        sprite.GetAnimationState().SetAnimation(pair.AnimationName, loop: false);
                    }
                    catch (Exception ex)
                    {
                        MainFile.Logger.Info($"[MeiLinVfx] Spine play failed. anim={pair.AnimationName}, ex={ex.GetType().Name}: {ex.Message}");
                        remainingSprites--;
                        if (remainingSprites <= 0)
                            CompleteCondition();
                    }
                });
            }
        }

        if (particleDuration > 0f)
        {
            pendingConditions++;
            AutoCompleteAfter(root, MathF.Max(0.1f, particleDuration), CompleteCondition);
        }

        if (pendingConditions == 0)
        {
            freed = true;
            QueueFree(root);
        }
    }

    private static IEnumerable<Node> EnumerateNodes(Node root)
    {
        yield return root;

        foreach (Node child in root.GetChildren())
        {
            foreach (var nested in EnumerateNodes(child))
                yield return nested;
        }
    }

    private static float EstimateParticleDuration(List<Node> nodes)
    {
        var max = 0f;
        foreach (var node in nodes)
        {
            if (node is not GpuParticles2D particles)
                continue;

            var speed = MathF.Max(0.01f, (float)particles.SpeedScale);
            var delay = GetEffectiveDelay(node);
            var duration = delay + ((float)particles.Lifetime + (float)particles.Preprocess) / speed;
            max = MathF.Max(max, duration);
        }

        return max + 0.15f;
    }

    private static string? ResolveAnimationName(Node node, MegaSprite sprite, string fallbackAnimationName)
    {
        var previewAnimation = "";
        try
        {
            previewAnimation = node.Get(PreviewAnimationProperty).AsString();
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(previewAnimation) && sprite.HasAnimation(previewAnimation))
            return previewAnimation;

        if (!string.IsNullOrWhiteSpace(fallbackAnimationName) && sprite.HasAnimation(fallbackAnimationName))
            return fallbackAnimationName;

        return null;
    }

    private static void HideDelayedNodes(List<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (GetOwnDelay(node) <= 0f)
                continue;

            if (node is CanvasItem canvasItem)
                canvasItem.Visible = false;
        }
    }

    private static void ShowDelayedAncestors(Node node)
    {
        Node? current = node;
        while (current != null)
        {
            if (GetOwnDelay(current) > 0f && current is CanvasItem canvasItem)
                canvasItem.Visible = true;

            current = current.GetParent();
        }
    }

    private static float GetEffectiveDelay(Node node)
    {
        var delay = 0f;
        Node? current = node;
        while (current != null)
        {
            delay += GetOwnDelay(current);
            current = current.GetParent();
        }

        return delay;
    }

    private static float GetOwnDelay(Node node)
    {
        if (!GodotObject.IsInstanceValid(node) || !node.HasMeta(DelayMeta))
            return 0f;

        try
        {
            return MathF.Max(0f, node.GetMeta(DelayMeta).AsSingle());
        }
        catch
        {
            return 0f;
        }
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

        if (GodotObject.IsInstanceValid(node))
            action();
    }

    private static void TriggerParticle(GpuParticles2D particles)
    {
        try
        {
            particles.Restart();
            particles.Emitting = true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinVfx] Particle trigger failed. node={particles.Name}, ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async void AutoCompleteAfter(Node node, float seconds, Action action)
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

        if (GodotObject.IsInstanceValid(node))
            action();
    }

    private static void QueueFree(Node node)
    {
        if (GodotObject.IsInstanceValid(node))
            node.QueueFree();
    }
}

public partial class MeiLinFollowCreatureVfx : Node
{
    [Export] public NCreature? Target;
    [Export] public Vector2 AnchorOffset;
    [Export] public Vector2 Offset;

    public override void _Process(double delta)
    {
        if (Target == null || !GodotObject.IsInstanceValid(Target))
            return;

        if (GetParent() is Node2D parent)
            parent.GlobalPosition = Target.VfxSpawnPosition + AnchorOffset + Offset;
    }
}
