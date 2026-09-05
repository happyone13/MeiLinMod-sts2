using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinCharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.MeiLinModCode.Vfx;

/// <summary>One card play owns one cinematic; the card retains all damage rules.</summary>
public static class MeiLinUgPresentation
{
    public const string ScenePath = "res://MeiLinMod/scenes/vfx/ug/presentation.tscn";
    private static PackedScene? _scene;

    public static void Preload()
    {
        if (!MeiLinMod.MeiLinModCode.Config.MeiLinModConfig.UseCombatEffects || !MeiLinSharedSettings.UltimateCinematicsEnabled) return;
        _scene = ResourceLoader.Load<PackedScene>(ScenePath);
    }

    public static async Task PlayAsync(Creature caster, IEnumerable<Creature> targets, Func<bool, Task> onHit)
    {
        var room = NCombatRoom.Instance;
        var actor = room?.GetCreatureNode(caster)?.Visuals;
        if (!MeiLinMod.MeiLinModCode.Config.MeiLinModConfig.UseCombatEffects
            || !MeiLinSharedSettings.UltimateCinematicsEnabled
            || caster.Player?.Character is not MeiLinCharacterModel
            || room == null || actor == null)
        {
            await onHit(false);
            return;
        }

        Node? stage = null;
        try
        {
            try
            {
                _scene ??= ResourceLoader.Load<PackedScene>(ScenePath);
                stage = _scene?.Instantiate();
                if (stage != null)
                {
                    room.AddChild(stage);
                    var visuals = new Godot.Collections.Array<GodotObject>();
                    foreach (var target in targets.Where(t => t.IsAlive).OrderBy(t => room.GetCreatureNode(t)?.GlobalPosition.X))
                    {
                        var node = room.GetCreatureNode(target)?.Visuals;
                        if (node != null) visuals.Add(node);
                    }
                    stage.Call("begin", actor, visuals);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"[UG] Presentation unavailable: {ex.Message}");
                Cleanup(stage);
                stage = null;
            }

            if (stage == null)
            {
                await onHit(false);
                return;
            }

            var tree = room.GetTree();
            var config = stage.Get("config").AsGodotDictionary();
            double hit = config["hit"].AsDouble();
            double total = config["total"].AsDouble();
            bool hitFired = false;
            // The presentation and waits use the same real-time clock. Faster game
            // settings must not move damage ahead of the authored HIT beat.
            while (GodotObject.IsInstanceValid(stage) && GodotObject.IsInstanceValid(room)
                   && NCombatRoom.Instance == room && !CombatManager.Instance.IsOverOrEnding)
            {
                double elapsed = stage.Get("elapsed").AsDouble();
                if (!hitFired && elapsed >= hit)
                {
                    hitFired = true;
                    await onHit(true);
                }
                if (elapsed >= total) break;
                await stage.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        finally
        {
            Cleanup(stage);
        }
    }

    private static void Cleanup(Node? stage)
    {
        if (stage == null || !GodotObject.IsInstanceValid(stage)) return;
        if (stage.HasMethod("finish")) stage.Call("finish");
        stage.QueueFree();
    }
}
