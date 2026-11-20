using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Services;
using MeiLinCharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.MeiLinModCode.Vfx;

/// <summary>One card play owns one cinematic; the card retains all damage rules.</summary>
public static class MeiLinUxPresentation
{
    private const string SceneDir = "res://MeiLinMod/scenes/vfx/ux/";
    private const string OpeningNode = "MeiLinUxOpening";
    private const ulong FirstFrameBudgetMs = 100;
    public const string ScenePath = SceneDir + "presentation.tscn";
    private static PackedScene? _scene;

    private static bool CanPresent(NCombatRoom room) =>
        GodotObject.IsInstanceValid(room) && !room.IsQueuedForDeletion() && NCombatRoom.Instance == room &&
        !CombatManager.Instance.IsOverOrEnding && MeiLinMod.MeiLinModCode.Config.MeiLinModConfig.UseCombatEffects &&
        MeiLinSharedSettings.UltimateCinematicsEnabled;

    internal static Node? PrefetchOpening(NCombatRoom room)
    {
        if (!CanPresent(room)) return null;
        if (room.GetNodeOrNull<Node>(OpeningNode) is { } existing)
            return existing.IsQueuedForDeletion() ? null : existing;
        var script = ResourceLoader.Load<GDScript>(SceneDir + "cutin_window.gd");
        if (script == null || !script.CanInstantiate()) return null;
        var opening = new Node { Name = OpeningNode };
        opening.SetScript(script);
        room.AddChild(opening);
        opening.Call("warm_opening", SceneDir + "timeline.json", Callable.From(() => CanPresent(room)));
        return opening;
    }

    public static void Preload()
    {
        if (!MeiLinMod.MeiLinModCode.Config.MeiLinModConfig.UseCombatEffects || !MeiLinSharedSettings.UltimateCinematicsEnabled) return;
        _scene = ResourceLoader.Load<PackedScene>(ScenePath);
    }

    public static async Task PlayAsync(Creature caster, IEnumerable<Creature> targets, Func<bool, Task> onHit)
    {
        if (MeiLinReplayVisuals.ShouldSkip(caster))
        {
            await onHit(false);
            return;
        }

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
        bool prepared = false;
        try
        {
            try
            {
                _scene ??= ResourceLoader.Load<PackedScene>(ScenePath);
                stage = _scene?.Instantiate();
                if (stage != null)
                {
                    if (PrefetchOpening(room) is { } opening)
                        stage.Set("opening", opening);
                    stage.Set("keep_cutin", Callable.From(() => CanPresent(room)));
                    room.AddChild(stage);
                    bool Current() => CanPresent(room);
                    await WaitForOpeningAsync(stage, Current);
                    prepared = await CinematicFirstDraw.PrepareAsync(stage, Current);
                    if (prepared)
                    {
                        var visuals = new Godot.Collections.Array<GodotObject>();
                        foreach (var target in targets.Where(t => t.IsAlive).OrderBy(t => room.GetCreatureNode(t)?.GlobalPosition.X))
                        {
                            var node = room.GetCreatureNode(target)?.Visuals;
                            if (node != null) visuals.Add(node);
                        }
                        stage.Call("begin", actor, visuals);
                        // Start voice and effects from the same frame and real-time
                        // epoch used by the visual timeline.
                        MeiLinAudioService.SuppressNextDefaultCastSfx(caster.Player);
                        MeiLinAudioService.TryPlayUxVoice(caster.Player);
                        MeiLinAudioService.TryPlayUxSound(caster.Player);
                    }
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"[UX] Presentation unavailable: {ex.Message}");
                Cleanup(stage);
                stage = null;
            }

            if (stage == null || !prepared)
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

    internal static async Task WaitForOpeningAsync(Node stage, Func<bool> isCurrent)
    {
        static bool Live(Node node) =>
            GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion() && node.IsInsideTree();

        if (!Live(stage) || !isCurrent() || stage.Get("window").AsGodotObject() is not Node window || !Live(window))
            return;
        var tree = stage.GetTree();
        // The stage and its clock stay stopped. Already-warm or failed frame 0 takes no extra frame.
        ulong deadline = Time.GetTicksMsec() + FirstFrameBudgetMs;
        using var textures = window.Get("textures").AsGodotDictionary();
        using var failed = window.Get("failed").AsGodotDictionary();
        while (window.Get("limit").AsInt32() > 0 && !textures.ContainsKey(0) && !failed.ContainsKey(0)
               && Time.GetTicksMsec() < deadline)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (!Live(stage) || !Live(window) || !isCurrent()) return;
        }
    }

    private static void Cleanup(Node? stage)
    {
        if (stage == null || !GodotObject.IsInstanceValid(stage)) return;
        if (stage.HasMethod("finish")) stage.Call("finish");
        stage.QueueFree();
    }
}
