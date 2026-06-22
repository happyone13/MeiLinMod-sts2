using System.Linq;
using System.Threading.Tasks;
using Godot;
using MeiLinMod.MeiLinModCode.Config;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeiLinMod.MeiLinModCode.StanceVfx;

public sealed class MeiLinStanceVfxController
{
    public const string AttackAuraScenePath = "res://MeiLinMod/scenes/vfx/wrath_aura.tscn";
    public const string GuardAuraScenePath = "res://MeiLinMod/scenes/vfx/calm_aura.tscn";

    private const string ContainerName = "MeiLinStanceVfxContainer";

    private Node2D? _currentAura;
    private string? _currentAuraScenePath;

    public async Task SetAura(Creature owner, string? auraScenePath)
    {
        if (!MeiLinModConfig.UseCombatEffects)
        {
            await ClearAura();
            return;
        }

        if (string.IsNullOrWhiteSpace(auraScenePath))
        {
            await ClearAura();
            return;
        }

        var visuals = NCombatRoom.Instance?.GetCreatureNode(owner)?.Visuals;
        if (visuals == null)
            return;

        var container = visuals.GetNodeOrNull<Node2D>(ContainerName);
        if (container == null)
        {
            container = new Node2D
            {
                Name = ContainerName,
                Position = Vector2.Zero
            };
            visuals.AddChild(container);
        }

        if (_currentAura != null &&
            GodotObject.IsInstanceValid(_currentAura) &&
            _currentAura.GetParent() == container &&
            _currentAuraScenePath == auraScenePath)
        {
            return;
        }

        await ClearAura();

        var packed = PreloadManager.Cache.GetScene(auraScenePath);
        if (packed == null)
            return;

        Node2D aura;
        try
        {
            aura = packed.Instantiate<Node2D>();
            aura.Position = Vector2.Zero;
            aura.Scale = Vector2.One;
            container.AddChild(aura);
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Info($"[StanceVfx] Scene instantiate failed. scene={auraScenePath}, ex={ex.GetType().Name}: {ex.Message}");
            return;
        }

        foreach (var burst in aura.GetChildren()
                     .Where(c => c.Name.ToString().Contains("Burst"))
                     .OfType<Node2D>())
        {
            var pos = burst.GlobalPosition;
            burst.Reparent(visuals);
            burst.GlobalPosition = pos;
            visuals.MoveChild(burst, 0);
        }

        _currentAura = aura;
        _currentAuraScenePath = auraScenePath;
    }

    public Task ClearAura()
    {
        if (_currentAura == null || !GodotObject.IsInstanceValid(_currentAura))
        {
            _currentAura = null;
            _currentAuraScenePath = null;
            return Task.CompletedTask;
        }

        var aura = _currentAura;
        _currentAura = null;
        _currentAuraScenePath = null;

        foreach (var child in aura.GetChildren())
        {
            switch (child)
            {
                case MeiLinWrathGlowSparkSpawner sparks:
                    sparks.StopSpawning();
                    break;
                case MeiLinCalmFrostStreakSpawner streaks:
                    streaks.StopSpawning();
                    break;
                case MeiLinAuraBlobEmitter blob:
                    foreach (var cpu in blob.GetChildren().OfType<CpuParticles2D>())
                        cpu.Emitting = false;
                    var tree = blob.GetTree();
                    if (tree == null)
                    {
                        blob.QueueFree();
                    }
                    else
                    {
                        var timer = tree.CreateTimer(2.5f);
                        timer.Timeout += () =>
                        {
                            if (GodotObject.IsInstanceValid(blob))
                                blob.QueueFree();
                        };
                    }
                    break;
                case Node node when node.HasMethod("StopSpawning"):
                    node.Call("StopSpawning");
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
