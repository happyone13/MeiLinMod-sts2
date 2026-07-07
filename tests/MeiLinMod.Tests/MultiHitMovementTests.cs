using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Character;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class MultiHitMovementTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-multihit-movement");
    }

    [Fact]
    public async Task Aborted_multi_hit_attack_discards_remaining_animation_segments()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        MeiLinBattleAnimationService.PrepareNextAttackContext(Player.Creature, EnemyAt(0));
        MeiLinBattleAnimationService.PrepareNextAttackHits(5);

        var firstSegment = MeiLinBattleAnimationService.ConsumeNextAttackSegment(Player.Creature);
        Assert.Equal("attack_play1", firstSegment.Command);
        Assert.True(firstSegment.RemainingSegments > 0);

        MeiLinBattleAnimationService.AbortActiveAttack(Player.Creature);
        MeiLinBattleAnimationService.PrepareNextAttackContext(Player.Creature, EnemyAt(1));

        var nextAttack = MeiLinBattleAnimationService.ConsumeNextAttackSegment(Player.Creature);
        Assert.Equal("attack_play1", nextAttack.Command);
        Assert.Equal(0, nextAttack.RemainingSegments);
    }

    [Fact]
    public async Task Multi_hit_attack_commands_follow_repeating_1212_then_u2_finisher_rule()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        Assert.Equal(["attack_play1"], MeiLinBattleAnimationService.BuildAttackCommands(0));
        Assert.Equal(["attack_play1"], MeiLinBattleAnimationService.BuildAttackCommands(1));
        Assert.Equal(["attack_play1", "attack_play2"], MeiLinBattleAnimationService.BuildAttackCommands(2));
        Assert.Equal(["attack_play1", "attack_play2", "attack_play1"], MeiLinBattleAnimationService.BuildAttackCommands(3));
        Assert.Equal(["attack_play1", "attack_play2", "attack_play1", "u2_attack_play"], MeiLinBattleAnimationService.BuildAttackCommands(4));
        Assert.Equal(["attack_play1", "attack_play2", "attack_play1", "attack_play2", "u2_attack_play"], MeiLinBattleAnimationService.BuildAttackCommands(5));
    }
}
