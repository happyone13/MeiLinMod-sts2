using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Vfx;
using TestTheSpire;
using Xunit;
using CharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class CommandEffectRoomPoolTests : CombatTestSuite
{
    private const string Smoke = "res://MeiLinMod/scenes/vfx/generated/attack_play1/meirin_attack_play1_root.tscn";
    private const string Hit = "res://MeiLinMod/scenes/vfx/generated/common/meirin_attack_play_target.tscn";
    private const BindingFlags Internal = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly Type Pool = typeof(MeiLinVfxHelper).Assembly.GetType(
        "MeiLinMod.MeiLinModCode.Vfx.OneShotVfxPool", true)!;

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<CharacterModel>().AddEnemy<BigDummy>().WithSeed("command-effect-room-pool");

    [Fact]
    public async Task Prewarmed_attack_is_reused_across_commands_without_inheriting_placement_or_old_returns()
    {
        await Play(await AddToHand<DefendMeilin>());
        var room = AddContainer();
        try
        {
            Assert.Contains(Smoke, MeiLinCommandVfxCoordinator.GetBattleWarmScenePaths());
            room.Position = new Vector2(70, 15);
            var warm = MeiLinVfxHelper.PlayComposite(Smoke, room, Vector2.Zero)!;
            warm.Modulate = new Color(1, 1, 1, 0.001f);
            await Wait(0.02);
            int warmGeneration = Generation(warm);
            Return(warm, warmGeneration);

            var oldCommand = AddNode(room);
            oldCommand.Position = new Vector2(-30, 45);
            var previousLayer = AddNode(oldCommand);
            var world = new Vector2(430, 320);
            var first = TryPlay(Smoke, oldCommand, world, 2.5f, 4)!;
            Assert.Same(warm, first);
            Assert.Same(oldCommand, first.GetParent());
            Assert.True(first.GetIndex() > previousLayer.GetIndex());
            Assert.True(first.GlobalPosition.IsEqualApprox(world));
            Assert.Equal(Vector2.One * 2.5f, first.Scale);
            Assert.Equal(4, first.ZIndex);
            Assert.Equal(Colors.White, first.Modulate);
            int firstGeneration = Generation(first);
            first.Rotation = 0.7f;
            first.AddChild(new MeiLinFollowCreatureVfx());
            await Until(() => first.GetParent() == room && !first.Visible);
            Assert.Contains(oldCommand.GetChildren(), node => node == previousLayer);
            Assert.DoesNotContain(first.GetChildren(), node => node is MeiLinFollowCreatureVfx);

            var newCommand = AddNode(room);
            newCommand.Position = new Vector2(140, -30);
            var nextWorld = new Vector2(250, 550);
            var second = TryPlay(Smoke, newCommand, nextWorld)!;
            Assert.Same(first, second);
            Assert.True(second.GlobalPosition.IsEqualApprox(nextWorld));
            Assert.Equal(Vector2.One, second.Scale);
            Assert.Equal(0f, second.Rotation);
            Assert.Equal(0, second.ZIndex);
            Return(warm, warmGeneration);
            Return(first, firstGeneration);
            oldCommand.QueueFree();
            Assert.Null(TryPlay(Smoke, oldCommand, world));
            Assert.True(second.Visible);
            Assert.Same(newCommand, second.GetParent());
            await Until(() => !GodotObject.IsInstanceValid(oldCommand));
            await Until(() => second.GetParent() == room && !second.Visible);
            Assert.Equal(Node.ProcessModeEnum.Disabled, second.ProcessMode);
            Assert.True(Generation(second) > firstGeneration);
        }
        finally
        {
            room.Free();
        }
    }

    [Fact]
    public async Task Overlapping_commands_keep_separate_effects_and_deadlines_and_room_exit_discard_live_work()
    {
        await Play(await AddToHand<DefendMeilin>());
        var room = AddContainer();
        var nextRoom = AddContainer();
        try
        {
            Assert.Contains(Hit, MeiLinCommandVfxCoordinator.GetBattleWarmScenePaths());
            var early = AddNode(room);
            var late = AddNode(room);
            var first = TryPlay(Hit, early, Vector2.One)!;
            var overlap = TryPlay(Hit, late, Vector2.Zero)!;
            Assert.NotSame(first, overlap);
            Assert.Same(early, first.GetParent());
            Assert.Same(late, overlap.GetParent());
            Assert.True(early.GetIndex() < late.GetIndex());

            // Hold both Spine completions outstanding: only the command deadline may remove the first.
            first.ProcessMode = overlap.ProcessMode = Node.ProcessModeEnum.Disabled;
            typeof(MeiLinCommandVfxCoordinator).GetMethod("AutoFreeAfter", Internal)!.Invoke(null, [early, 0.1f]);
            await Until(() => !GodotObject.IsInstanceValid(early));
            Assert.False(GodotObject.IsInstanceValid(first));
            Assert.True(overlap.Visible);
            Assert.Same(late, overlap.GetParent());

            var parked = TryPlay(Smoke, late, Vector2.Zero)!;
            Return(parked, Generation(parked));
            await Until(() => parked.GetParent() == room);
            var pending = TryPlay(Hit, late, Vector2.Zero)!;
            Return(pending, Generation(pending));
            room.QueueFree();
            Assert.Null(TryPlay(Hit, late, Vector2.Zero));
            var nextCommand = AddNode(nextRoom);
            var fresh = TryPlay(Hit, nextCommand, Vector2.Zero)!;
            fresh.ProcessMode = Node.ProcessModeEnum.Disabled;
            Assert.NotSame(overlap, fresh);
            Assert.NotSame(pending, fresh);
            await Until(() => !GodotObject.IsInstanceValid(room));
            Assert.False(GodotObject.IsInstanceValid(parked));
            Assert.False(GodotObject.IsInstanceValid(pending));
            Assert.False(GodotObject.IsInstanceValid(overlap));
            Assert.True(fresh.Visible);
            Assert.Same(nextCommand, fresh.GetParent());
        }
        finally
        {
            if (GodotObject.IsInstanceValid(room))
                room.Free();
            nextRoom.Free();
        }
    }

    private static Control AddContainer()
    {
        var container = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(container);
        return container;
    }

    private static Node2D AddNode(Node parent)
    {
        var child = new Node2D();
        parent.AddChild(child);
        return child;
    }

    private static Node2D? TryPlay(string path, Node2D command, Vector2 world, float scale = 1f, int? z = null) =>
        (Node2D?)typeof(MeiLinVfxHelper).GetMethod("PlayCommandComposite", Internal)!
            .Invoke(null, [path, command, world, scale, z]);

    private static object Lease(Node2D root) => Pool.GetMethod("Find", Internal)!.Invoke(null, [root])!;

    private static int Generation(Node2D root)
    {
        var lease = Lease(root);
        return (int)lease.GetType().GetProperty("Generation", Internal)!.GetValue(lease)!;
    }

    private static void Return(Node2D root, int generation)
    {
        var lease = Lease(root);
        lease.GetType().GetMethod("Return", Internal)!.Invoke(lease, [generation]);
    }

    private static async Task Until(Func<bool> done)
    {
        for (int i = 0; i < 80 && !done(); i++)
            await Wait(0.1);
        Assert.True(done());
    }

    private static async Task Wait(double seconds)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}
