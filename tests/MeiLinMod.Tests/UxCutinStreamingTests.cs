using Godot;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;
using CharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using Defend = MeiLinMod.MeiLinModCode.Cards.DefendMeilin;
using UxPresentation = MeiLinMod.MeiLinModCode.Vfx.MeiLinUxPresentation;

namespace MeiLinMod.Tests;

public sealed class UxCutinStreamingTests : CombatTestSuite
{
    private const string UxDir = "res://MeiLinMod/scenes/vfx/ux/";

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<CharacterModel>().AddEnemy<BigDummy>().WithSeed("ux-cutin-window");

    [Fact]
    public async Task Room_prefix_first_draw_and_playback_window_use_real_textures()
    {
        await Play(await AddToHand<Defend>());
        var tree = (SceneTree)Engine.GetMainLoop();
        var room = new Node();
        tree.Root.AddChild(room);
        try
        {
            bool enabled = true;
            var opening = new Node();
            opening.SetScript(ResourceLoader.Load<GDScript>(UxDir + "cutin_window.gd"));
            room.AddChild(opening);
            opening.Call("warm_opening", UxDir + "timeline.json", Callable.From(() => enabled));
            Assert.InRange(Count(opening), 0, 1);
            await Until(tree, () => Count(opening) == 3);
            Assert.False(opening.Get("textures").AsGodotDictionary().ContainsKey(3));

            var stage = ResourceLoader.Load<PackedScene>(UxDir + "presentation.tscn").Instantiate();
            stage.Set("opening", opening);
            room.AddChild(stage);
            var sprite = stage.GetNode<Sprite2D>("Stage/UxVideo");
            var window = (Node)stage.Get("window").AsGodotObject();
            Assert.True(FirstFrame(stage, () => true).IsCompletedSuccessfully);
            using var textures = window.Get("textures").AsGodotDictionary();
            textures.Remove(0);
            window.SetProcess(false);
            var warming = FirstFrame(stage, () => true);
            Assert.False(warming.IsCompleted);
            textures[0] = opening.Call("get_frame", 0);
            await Until(tree, () => warming.IsCompleted);
            Assert.True(warming.IsCompletedSuccessfully);
            window.SetProcess(true);
            stage.Call("prepare_first_draw");
            Assert.True(sprite.Visible);
            Assert.Same(opening.Call("get_frame", 0).AsGodotObject(), sprite.Texture);
            Assert.Equal(0, stage.Get("elapsed").AsDouble());
            Assert.False(stage.Get("running").AsBool());
            stage.Call("reset_first_draw");
            Assert.False(sprite.Visible);

            var config = stage.Get("config").AsGodotDictionary();
            var frames = config["cutin"].AsGodotDictionary()["frames"].AsGodotArray();
            double Duration(Variant frame) => frame.AsGodotDictionary()["duration"].AsDouble();
            Assert.Equal(frames.Sum(Duration), stage.Get("cutin_lead_in").AsDouble(), 6);
            stage.Call("begin_preview");
            stage.SetProcess(false);
            stage.Call("_process", frames.Take(8).Sum(Duration) + 0.0001);
            Assert.Equal(8, window.Get("first").AsInt32());
            Assert.False(window.Get("textures").AsGodotDictionary().ContainsKey(0));
            await Until(tree, () => Count(window) == 4);
            stage.Call("_process", 0.0);
            Assert.Same(window.Call("get_frame", 8).AsGodotObject(), sprite.Texture);
            Assert.Equal(3, Count(opening));

            stage.Call("finish");
            Assert.Null(sprite.Texture);
            Assert.Equal(0, Count(window));
            enabled = false;
            await Until(tree, () => !GodotObject.IsInstanceValid(opening));
        }
        finally
        {
            room.Free();
        }
    }

    [Fact]
    public async Task Cancelling_a_cold_window_preserves_the_authored_hit_clock()
    {
        await Play(await AddToHand<Defend>());
        var tree = (SceneTree)Engine.GetMainLoop();
        var stage = ResourceLoader.Load<PackedScene>(UxDir + "presentation.tscn").Instantiate();
        tree.Root.AddChild(stage);
        try
        {
            var window = (Node)stage.Get("window").AsGodotObject();
            Assert.InRange(Count(window), 0, 1);
            // Force the cold branch even when a previous test left Godot's first texture cached.
            window.Get("textures").AsGodotDictionary().Remove(0);
            stage.Call("prepare_first_draw");
            var sprite = stage.GetNode<Sprite2D>("Stage/UxVideo");
            Assert.False(sprite.Visible);
            Assert.Null(sprite.Texture);
            Assert.Equal(0, stage.Get("presentation_elapsed").AsDouble());
            stage.Call("reset_first_draw");
            stage.Set("keep_cutin", Callable.From(() => false));
            stage.Call("begin_preview");
            stage.SetProcess(false);
            Assert.Null(stage.GetNode<Sprite2D>("Stage/UxVideo").Texture);
            Assert.Null(stage.Get("window").AsGodotObject());
            Assert.Equal(0, Count(window));
            var config = stage.Get("config").AsGodotDictionary();
            double hit = config["hit"].AsDouble();
            double lead = stage.Get("cutin_lead_in").AsDouble();
            Assert.True(lead > 1);
            stage.Call("_process", lead + hit - 0.01);
            Assert.Equal(hit - 0.01, stage.Get("elapsed").AsDouble(), 6);
            stage.Call("_process", 0.02);
            Assert.Equal(hit + 0.01, stage.Get("elapsed").AsDouble(), 6);
        }
        finally
        {
            stage.Free();
        }
    }

    [Fact]
    public async Task First_frame_wait_skips_failures_and_cancellation_and_expires_before_playback()
    {
        await Play(await AddToHand<Defend>());
        var tree = (SceneTree)Engine.GetMainLoop();
        var stage = ResourceLoader.Load<PackedScene>(UxDir + "presentation.tscn").Instantiate();
        tree.Root.AddChild(stage);
        try
        {
            var window = (Node)stage.Get("window").AsGodotObject();
            window.SetProcess(false);
            window.Get("textures").AsGodotDictionary().Remove(0);
            using var failed = window.Get("failed").AsGodotDictionary();
            failed[0] = true;
            Assert.True(FirstFrame(stage, () => true).IsCompletedSuccessfully);
            failed.Remove(0);
            Assert.True(FirstFrame(stage, () => false).IsCompletedSuccessfully);

            ulong start = Time.GetTicksMsec(), previous = start, longestFrame = 0;
            var waiting = FirstFrame(stage, () => true);
            Assert.False(waiting.IsCompleted);
            while (!waiting.IsCompleted && previous - start < 5000)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                ulong now = Time.GetTicksMsec();
                longestFrame = Math.Max(longestFrame, now - previous);
                previous = now;
            }
            Assert.True(waiting.IsCompletedSuccessfully);
            Assert.InRange(previous - start, 100UL, 100UL + 2 * longestFrame + 10UL);
            Assert.Equal(0, stage.Get("presentation_elapsed").AsDouble());
            Assert.False(stage.Get("running").AsBool());
        }
        finally
        {
            stage.Free();
        }
    }

    private static Task FirstFrame(Node stage, Func<bool> isCurrent) =>
        (Task)typeof(UxPresentation).GetMethod("WaitForOpeningAsync",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(null, [stage, isCurrent])!;

    private static int Count(Node window) => window.Get("textures").AsGodotDictionary().Count;

    private static async Task Until(SceneTree tree, Func<bool> ready)
    {
        ulong deadline = Time.GetTicksMsec() + 5000;
        while (!ready() && Time.GetTicksMsec() < deadline)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Assert.True(ready(), "The UX frame window did not reach the expected state.");
    }
}
