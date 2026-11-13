using Godot;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Godot;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class DeathAnimationCueTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<MeiLinCharacter>().AddEnemy<BigDummy>().WithSeed("meilinmod-game-over-death");
    }

    [Fact]
    public async Task Game_over_die_cue_plays_death_on_fresh_visuals_and_keeps_its_final_pose()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var visuals = RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            ((MeiLinCharacter)Player.Character).CustomVisualsPath!);
        Assert.NotNull(visuals);
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(visuals);
        try
        {
            visuals.Position = tree.Root.GetVisibleRect().Size * new Vector2(0.5f, 0.8f);
            Assert.NotNull(visuals.SpineBody);
            Assert.True(visuals.SpineBody.HasAnimation("death"));
            Assert.False(visuals.SpineBody.HasAnimation("die"));

            Assert.True(ModCreatureVisualPlayback.TryPlayCue(visuals, Player.Character, "die"));
            Assert.Equal("death", visuals.SpineBody.GetAnimationState().GetCurrent(0)?.GetAnimationName());

            await tree.ToSignal(tree.CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
            Assert.Equal("death", visuals.SpineBody.GetAnimationState().GetCurrent(0)?.GetAnimationName());

            if (DisplayServer.GetName() != "headless")
            {
                await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = tree.Root.GetTexture().GetImage();
                Assert.Equal(Error.Ok, image.SavePng(
                    Path.Combine(RepositoryRoot(), "tmp", "migration113-fixed", "death-animation.png")));
            }
        }
        finally
        {
            visuals.Free();
        }
    }

    private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "..", ".."));
    }
}
