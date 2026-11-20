using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace MeiLinMod.MeiLinModCode.Vfx;

// Command effects play under the command for draw order, but park under their combat room.
internal static class OneShotVfxPool
{
    private const int MaxIdlePerEffect = 2;
    private static readonly ConditionalWeakTable<Node, Dictionary<string, Stack<Lease>>> Pools = new();
    private static readonly ConditionalWeakTable<Node2D, Lease> Leases = new();

    internal sealed class Lease
    {
        internal readonly Node2D Root;
        internal readonly List<Node> Nodes = new();
        internal readonly List<(Node Node, MegaSprite Sprite)> Spines = new();
        private readonly Node _home;
        private readonly Stack<Lease> _idle;
        private readonly Transform2D _transform;
        private readonly Color _modulate;
        private readonly Color _selfModulate;
        private readonly int _zIndex;
        private readonly bool _zAsRelative;
        private readonly Node.ProcessModeEnum _processMode;
        private readonly List<(CanvasItem Item, bool Visible)> _visibility = new();
        private readonly List<(MegaSprite Sprite, Callable Callback)> _callbacks = new();
        private bool _active;
        internal int Generation { get; private set; }

        internal Lease(Node2D root, Node home, Stack<Lease> idle)
        {
            Root = root;
            _home = home;
            _idle = idle;
            _transform = root.Transform;
            _modulate = root.Modulate;
            _selfModulate = root.SelfModulate;
            _zIndex = root.ZIndex;
            _zAsRelative = root.ZAsRelative;
            _processMode = root.ProcessMode;
            Collect(root);
        }

        private void Collect(Node node)
        {
            Nodes.Add(node);
            if (node is CanvasItem canvas)
                _visibility.Add((canvas, canvas.Visible));
            if (node.IsClass("SpineSprite"))
                Spines.Add((node, new MegaSprite(node)));
            foreach (Node child in node.GetChildren())
                Collect(child);
        }

        internal void Begin()
        {
            Generation++;
            _active = true;
            Root.Transform = _transform;
            Root.Modulate = _modulate;
            Root.SelfModulate = _selfModulate;
            Root.ZIndex = _zIndex;
            Root.ZAsRelative = _zAsRelative;
            foreach (var pair in _visibility)
                if (GodotObject.IsInstanceValid(pair.Item))
                    pair.Item.Visible = pair.Visible;
            Root.ProcessMode = _processMode;
        }

        internal bool IsCurrent(int generation) =>
            _active && generation == Generation &&
            GodotObject.IsInstanceValid(Root) && !Root.IsQueuedForDeletion();

        internal void Connect(MegaSprite sprite, Callable callback)
        {
            sprite.ConnectAnimationCompleted(callback);
            _callbacks.Add((sprite, callback));
        }

        internal void Return(int generation)
        {
            if (!IsCurrent(generation))
                return;
            _active = false;
            foreach (var pair in _callbacks)
                pair.Sprite.DisconnectAnimationCompleted(pair.Callback);
            _callbacks.Clear();
            foreach (Node node in Nodes)
            {
                if (node is GpuParticles2D particles)
                    particles.Emitting = false;
            }
            // Follow nodes are added by callers after the scene is instantiated.
            foreach (Node child in Root.GetChildren())
            {
                if (child is MeiLinFollowCreatureVfx)
                {
                    Root.RemoveChild(child);
                    child.QueueFree();
                }
            }
            Root.Hide();
            Root.ProcessMode = Node.ProcessModeEnum.Disabled;
            if (Root.GetParent() == _home)
                Park(generation);
            else
                // Completion can run inside a native Spine update. Move the subtree after it returns.
                Callable.From(() => Park(generation)).CallDeferred();
        }

        private void Park(int generation)
        {
            if (generation != Generation || _active || !GodotObject.IsInstanceValid(Root) ||
                Root.IsQueuedForDeletion())
                return;
            var parent = Root.GetParent();
            if (_idle.Count >= MaxIdlePerEffect || !GodotObject.IsInstanceValid(_home) ||
                _home.IsQueuedForDeletion() || !GodotObject.IsInstanceValid(parent) || parent.IsQueuedForDeletion())
            {
                Root.QueueFree();
                return;
            }
            if (parent != _home)
            {
                if (!_home.IsInsideTree() || parent.GetParent() != _home)
                {
                    Root.QueueFree();
                    return;
                }
                Root.Reparent(_home, false);
                if (Root.GetParent() != _home)
                {
                    Root.QueueFree();
                    return;
                }
            }
            _idle.Push(this);
        }
    }

    internal static Lease Rent(Node parent, string key, PackedScene scene) =>
        RentForPlayback(parent, parent, key, scene);

    internal static Lease RentForPlayback(Node home, Node parent, string key, PackedScene scene)
    {
        var pool = Pools.GetOrCreateValue(home);
        if (!pool.TryGetValue(key, out var idle))
            pool[key] = idle = new Stack<Lease>();
        Lease? lease = null;
        while (idle.Count > 0)
        {
            var candidate = idle.Pop();
            var root = candidate.Root;
            if (!GodotObject.IsInstanceValid(root) || root.IsQueuedForDeletion() || root.GetParent() != home)
                continue;
            if (home != parent)
            {
                root.Reparent(parent, false);
                if (root.GetParent() != parent)
                {
                    root.QueueFree();
                    continue;
                }
            }
            lease = candidate;
            break;
        }
        if (lease == null)
        {
            var root = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
            try
            {
                parent.AddChild(root);
                if (root.GetParent() != parent)
                    throw new InvalidOperationException("Could not attach VFX to its playback parent.");
                lease = new Lease(root, home, idle);
                Leases.Add(root, lease);
            }
            catch
            {
                root.QueueFree();
                throw;
            }
        }
        lease.Begin();
        return lease;
    }

    internal static Lease? Find(Node2D root) => Leases.TryGetValue(root, out var lease) ? lease : null;
}
