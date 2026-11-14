using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Models.Monsters;
using STS2RitsuLib.Scaffolding.Godot;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Vfx;
using TestTheSpire;
using Xunit;
using CharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class PresentationPerformanceTests : CombatTestSuite
{
    private const string Prefix = "MeiLinMod.MeiLinModCode";
    private const string VfxNamespace = Prefix + ".Vfx";
    private const string PortraitScene = "res://MeiLinMod/scenes/cards/attack_defense_unity_dynamic.tscn";
    private const string EffectScene = "res://MeiLinMod/scenes/vfx/generated/attack_play1/meirin_attack_play1_root.tscn";
    private const BindingFlags Internal = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<CharacterModel>().AddEnemy<BigDummy>().WithSeed("presentation-performance");

    [Fact]
    public async Task Attack_model_moves_and_returns_while_health_buffs_hitbox_and_talk_stay_fixed()
    {
        await Play(await AddToHand<DefendMeilin>());
        await MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
            Player.Creature, 2m, Player.Creature, null);
        var tree = (SceneTree)Engine.GetMainLoop();
        var creature = ResourceLoader.Load<PackedScene>("res://scenes/combat/creature.tscn").Instantiate<NCreature>();
        typeof(NCreature).GetProperty("Entity")!.SetValue(creature, Player.Creature);
        var visuals = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(((CharacterModel)Player.Character).CustomVisualsPath!);
        typeof(NCreature).GetProperty("Visuals")!.SetValue(creature, visuals);
        tree.Root.AddChild(creature);
        try
        {
            creature.Position = new Vector2(300, 650);
            // Vanilla health bars animate their own entrance before settling.
            await Wait(1.2);
            var controller = typeof(CharacterModel).Assembly.GetType(VfxNamespace + ".MeiLinAttackMovementController", true)!;
            var model = (Node2D)controller.GetMethod("GetModelRoot", Internal)!.Invoke(null, [creature])!;
            var origin = model.GlobalPosition;
            var rootPosition = creature.GlobalPosition;
            var controls = Descendants(creature).OfType<Control>().Where(control =>
                !visuals.IsAncestorOf(control)).ToDictionary(control => control, control => control.GlobalPosition);
            Assert.Contains(controls.Keys, control => control.Name == "HealthBar");
            var bounds = visuals.Bounds.GetRect();
            var talk = visuals.TalkPosition!;
            Assert.NotNull(talk);
            Assert.True(talk.Position.X >= bounds.End.X);
            Assert.True(talk.Position.Y <= bounds.Position.Y + 1);
            var talkPosition = talk.GlobalPosition;
            var hitPosition = creature.Hitbox.GlobalPosition;
            var centerPosition = creature.VfxSpawnPosition;
            var delta = new Vector2(440, -30);
            model.GlobalPosition += delta;
            creature.SpineAnimation.SetAnimation("attack_play1", loop: false);
            await Wait(0.1);
            Assert.Equal(rootPosition, creature.GlobalPosition);
            Assert.Equal(hitPosition, creature.Hitbox.GlobalPosition);
            Assert.Equal(talkPosition, talk.GlobalPosition);
            Assert.True(creature.VfxSpawnPosition.DistanceTo(centerPosition + delta) < 0.1f);
            foreach (var pair in controls)
                Assert.Equal(pair.Value, pair.Key.GlobalPosition);
            if (DisplayServer.GetName() != "headless")
            {
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = tree.Root.GetTexture().GetImage();
                Assert.Equal(Error.Ok, image.SavePng(Path.Combine(RepositoryRoot(), "tmp", "performance-model-ui.png")));
            }
            model.GlobalPosition = origin;
            creature.SpineAnimation.SetAnimation("idle", loop: true);
            Assert.True(creature.VfxSpawnPosition.DistanceTo(centerPosition) < 0.1f);
            Assert.Equal(rootPosition, creature.GlobalPosition);
        }
        finally
        {
            creature.Free();
        }
    }

    [Fact]
    public async Task Cinematics_prepare_without_advancing_the_clock_and_restore_borrowed_models()
    {
        await Play(await AddToHand<DefendMeilin>());
        var tree = (SceneTree)Engine.GetMainLoop();
        var actor = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(((CharacterModel)Player.Character).CustomVisualsPath!);
        var enemy = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(((CharacterModel)Player.Character).CustomVisualsPath!);
        tree.Root.AddChild(actor);
        tree.Root.AddChild(enemy);
        actor.Position = new Vector2(300, 650);
        enemy.Position = new Vector2(950, 650);
        var enemyTransform = enemy.Transform;
        var helper = typeof(CharacterModel).Assembly.GetType(VfxNamespace + ".CinematicFirstDraw", true)!;
        async Task<bool> Prepare(Node stage, Func<bool> current) =>
            await (Task<bool>)helper.GetMethod("PrepareAsync", Internal)!.Invoke(null, [stage, current])!;
        try
        {
            foreach (string kind in new[] { "ug", "ux" })
            {
                var stage = ResourceLoader.Load<PackedScene>($"res://MeiLinMod/scenes/vfx/{kind}/presentation.tscn").Instantiate();
                tree.Root.AddChild(stage);
                try
                {
                    Assert.True(await Prepare(stage, () => true));
                    Assert.Equal(0, stage.Get("elapsed").AsDouble());
                    Assert.False(stage.Get("running").AsBool());
                    Assert.True(actor.Visible);
                    Assert.True(enemy.Visible);
                    Assert.False(stage.GetNode<Node2D>("Stage").Visible);
                    stage.Call("begin", actor, new Godot.Collections.Array<GodotObject> { enemy });
                    Assert.True(stage.Get("running").AsBool());
                    Assert.False(actor.Visible);
                    await Wait(0.06);
                    Assert.True(stage.Get("presentation_elapsed").AsDouble() > 0);
                    var config = stage.Get("config").AsGodotDictionary();
                    double seconds = config["hit"].AsDouble() + stage.Get("cutin_lead_in").AsDouble() - 0.1;
                    while (stage.Get("presentation_elapsed").AsDouble() < seconds)
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    if (DisplayServer.GetName() != "headless")
                    {
                        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                        using var image = tree.Root.GetTexture().GetImage();
                        var colors = new HashSet<Color>();
                        for (int y = 0; y < image.GetHeight(); y += 40)
                            for (int x = 0; x < image.GetWidth(); x += 40)
                                colors.Add(image.GetPixel(x, y));
                        Assert.True(colors.Count > 8, $"{kind} rendered a uniform frame.");
                        Assert.Equal(Error.Ok, image.SavePng(Path.Combine(RepositoryRoot(), "tmp", $"performance-{kind}.png")));
                    }
                    stage.Call("finish");
                    Assert.True(actor.Visible);
                    Assert.Same(tree.Root, enemy.GetParent());
                    Assert.Equal(enemyTransform, enemy.Transform);
                    Assert.True(enemy.Visible);
                    int checks = 0;
                    Assert.False(await Prepare(stage, () => ++checks == 1));
                    Assert.False(stage.GetNode<Node2D>("Stage").Visible);
                    Assert.True(actor.Visible);
                }
                finally
                {
                    stage.Free();
                }
            }
        }
        finally
        {
            actor.Free();
            enemy.Free();
        }
    }

    [Fact]
    public async Task Portrait_pauses_outside_view_and_resumes_without_losing_its_image()
    {
        await Play(await AddToHand<DefendMeilin>());
        var tree = (SceneTree)Engine.GetMainLoop();
        var clip = new Control { Position = new Vector2(40, 40), Size = new Vector2(610, 860), ClipContents = true };
        tree.Root.AddChild(clip);
        var scene = ResourceLoader.Load<PackedScene>(PortraitScene).Instantiate<Node2D>();
        clip.AddChild(scene);
        var container = scene.GetNode<SubViewportContainer>("ViewportContainer");
        var viewport = container.GetNode<SubViewport>("SubViewport");
        var spine = viewport.GetNode("SpineSprite");
        var activity = typeof(CharacterModel).Assembly.GetType(Prefix + ".Patches.DynamicPortraitActivity", true)!;
        bool Visible() => (bool)activity.GetMethod("ShouldRender")!.Invoke(null, [container])!;
        void Active(bool active) => activity.GetMethod("SetActive")!.Invoke(null, [viewport, active]);
        double TrackTime()
        {
            var state = spine.Call("get_animation_state").AsGodotObject();
            var entry = state.Call("get_current", 0).AsGodotObject();
            return entry.Call("get_track_time").AsDouble();
        }
        try
        {
            await Wait(0.2);
            Assert.True(Visible());
            scene.Position = new Vector2(-1000, 0);
            Assert.False(Visible());
            scene.Position = new Vector2(650, 0);
            Assert.False(Visible());
            scene.Position = Vector2.Zero;
            clip.Modulate = new Color(1, 1, 1, 0);
            Assert.False(Visible());
            clip.Modulate = Colors.White;
            clip.Hide();
            Assert.False(Visible());
            Active(false);
            Assert.Equal(Node.ProcessModeEnum.Disabled, viewport.ProcessMode);
            Assert.Equal(SubViewport.UpdateMode.Disabled, viewport.RenderTargetUpdateMode);
            double paused = TrackTime();
            await Wait(0.2);
            Assert.Equal(paused, TrackTime());
            clip.Show();
            Active(Visible());
            await Wait(0.25);
            Assert.True(TrackTime() > paused);
            Assert.Equal(SubViewport.UpdateMode.Always, viewport.RenderTargetUpdateMode);
            if (DisplayServer.GetName() != "headless")
            {
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = viewport.GetTexture().GetImage();
                Assert.Contains(image.GetData().Where((_, index) => index % 4 == 3), alpha => alpha > 0);
                Assert.Equal(Error.Ok, image.SavePng(Path.Combine(RepositoryRoot(), "tmp", "performance-portrait.png")));
            }
        }
        finally
        {
            clip.Free();
        }
    }

    [Fact]
    public async Task One_shot_pool_is_bounded_resets_state_and_rejects_old_returns()
    {
        await Play(await AddToHand<DefendMeilin>());
        var parent = new Node2D();
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(parent);
        using var packed = new PackedScene();
        using var template = new Node2D { Scale = new Vector2(1.5f, 2) };
        Assert.Equal(Error.Ok, packed.Pack(template));
        var pool = typeof(CharacterModel).Assembly.GetType(VfxNamespace + ".OneShotVfxPool", true)!;
        object Rent() => pool.GetMethod("Rent", Internal)!.Invoke(null, [parent, "test", packed])!;
        Node2D Root(object lease) => (Node2D)lease.GetType().GetField("Root", Internal)!.GetValue(lease)!;
        int Generation(object lease) => (int)lease.GetType().GetProperty("Generation", Internal)!.GetValue(lease)!;
        void Return(object lease, int generation) => lease.GetType().GetMethod("Return", Internal)!.Invoke(lease, [generation]);
        try
        {
            object first = Rent(), second = Rent(), third = Rent();
            int old = Generation(first);
            Root(first).Scale = Vector2.One * 10;
            Root(first).Rotation = 1.5f;
            Root(first).Modulate = new Color(1, 1, 1, 0.001f);
            Return(second, Generation(second));
            Return(first, old);
            Return(third, Generation(third));
            await Wait(0.05);
            Assert.Equal(2, parent.GetChildCount());
            Assert.All(parent.GetChildren().Cast<Node2D>(), root =>
            {
                Assert.False(root.Visible);
                Assert.Equal(Node.ProcessModeEnum.Disabled, root.ProcessMode);
            });
            object reused = Rent();
            Assert.Same(first, reused);
            Assert.Equal(new Vector2(1.5f, 2), Root(reused).Scale);
            Assert.Equal(0f, Root(reused).Rotation);
            Assert.Equal(Colors.White, Root(reused).Modulate);
            Return(first, old);
            Assert.True(Root(reused).Visible);
            Return(reused, Generation(reused));
        }
        finally
        {
            parent.Free();
        }
    }

    [Fact]
    public async Task Real_spine_effect_completes_and_replays_on_the_same_instance()
    {
        await Play(await AddToHand<DefendMeilin>());
        var parent = new Node2D();
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(parent);
        try
        {
            var first = Spawn(parent, new Vector2(500, 500), 2);
            Assert.NotNull(first);
            Assert.True(first.Visible);
            Assert.Contains(Descendants(first), node => node.IsClass("SpineSprite"));
            await WaitUntilIdle(first);
            var second = Spawn(parent, new Vector2(600, 400), 1);
            Assert.Same(first, second);
            Assert.Equal(Vector2.One, second.Scale);
            Assert.Equal(new Vector2(600, 400), second.GlobalPosition);
            await Wait(0.12);
            Assert.True(second.Visible);
            if (DisplayServer.GetName() != "headless")
            {
                var tree = (SceneTree)Engine.GetMainLoop();
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = tree.Root.GetTexture().GetImage();
                Assert.Equal(Error.Ok, image.SavePng(Path.Combine(RepositoryRoot(), "tmp", "performance-vfx.png")));
            }
            await WaitUntilIdle(second);
            Assert.Equal(1, parent.GetChildCount());
        }
        finally
        {
            parent.Free();
        }
    }

    private static Node2D Spawn(Node parent, Vector2 position, float scale) =>
        MeiLinVfxHelper.PlayComposite(EffectScene, parent, position, uniformScale: scale)!;

    private static IEnumerable<Node> Descendants(Node node)
    {
        yield return node;
        foreach (Node child in node.GetChildren())
            foreach (Node nested in Descendants(child))
                yield return nested;
    }

    private static async Task WaitUntilIdle(Node2D node)
    {
        for (int i = 0; i < 80 && node.Visible; i++)
            await Wait(0.1);
        Assert.True(GodotObject.IsInstanceValid(node));
        Assert.False(node.Visible);
        Assert.Equal(Node.ProcessModeEnum.Disabled, node.ProcessMode);
    }

    private static async Task Wait(double seconds)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
