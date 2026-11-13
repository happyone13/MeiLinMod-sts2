using System.Reflection;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Encounters;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Monsters.Gloomy;
using MeiLinMod.MeiLinModCode.Monsters.Gloomy.Powers;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class Migration113GloomyTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<MeiLinCharacter>()
            .Encounter<GloomyPackEncounter>()
            .WithSeed("meilinmod-113-gloomy-death");
    }

    [Fact]
    public async Task Beast_death_queues_one_replacement_on_the_next_enemy_turn()
    {
        var prime = Enemy<GloomyPrimeMonster>();
        var beast = Combat.Enemies.First(enemy => enemy.Monster is GloomyBeastMonster);
        var summon = Assert.IsType<GloomySummonPower>(
            await ApplyPower<GloomySummonPower>(prime, 2, prime));
        var source = await AddToHand<StrikeMeilin>();

        await CreatureCmd.Kill(beast, source);
        await WaitForIdle();

        Assert.DoesNotContain(beast, Combat.Enemies);
        Assert.Equal(2, Combat.Enemies.Count(enemy => enemy.Monster is GloomyBeastMonster));
        Assert.Equal(2, summon.Amount);

        await EndTurn();

        Assert.Equal(3, Combat.Enemies.Count(enemy => enemy.Monster is GloomyBeastMonster));
        Assert.Equal(1, summon.Amount);
    }
}

public sealed class Migration113CardLocationTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-113-card-location");
    }

    [Fact]
    public async Task HuoLongXinZang_returns_to_hand_after_consuming_qi()
    {
        await ApplyPower<QiPower>(Player.Creature, 1, Player.Creature);
        var card = await AddToHand<HuoLongXinZang>();

        var energyBeforePlay = Player.PlayerCombatState!.Energy;
        await EnqueueNetworkAction(new PlayCardAction(card, null));

        Assert.Contains(card, PileType.Hand.GetPile(Player).Cards);
        Assert.DoesNotContain(card, PileType.Discard.GetPile(Player).Cards);
        Assert.False(Player.Creature.HasPower<QiPower>());
        Assert.Equal(energyBeforePlay + 1, Player.PlayerCombatState.Energy);
    }

    [Fact]
    public async Task QuanXinQuanLing_sends_basic_strike_and_defend_to_exhaust()
    {
        await ApplyPower<QuanXinQuanLingPower>(Player.Creature, 1, Player.Creature);
        var strike = await AddToHand<StrikeMeilin>();
        var defend = await AddToHand<DefendMeilin>();

        await Play(strike, EnemyAt(0));
        await Play(defend);

        var exhausted = PileType.Exhaust.GetPile(Player).Cards;
        Assert.Contains(strike, exhausted);
        Assert.Contains(defend, exhausted);
    }
}

public sealed class Migration113OverlayTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle.Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-113-overlay-death");
    }

    [Fact]
    public async Task AfterDeath_hook_clears_local_overlay_only_when_death_is_not_prevented()
    {
        var card = await AddToHand<DefendMeilin>();
        await Play(card);
        var overlayType = typeof(MeiLinBattleReadyOverlay);
        var hovered = overlayType.GetField("_isHovered", BindingFlags.NonPublic | BindingFlags.Static)!;
        var focused = overlayType.GetField("_isUiFocused", BindingFlags.NonPublic | BindingFlags.Static)!;

        try
        {
            hovered.SetValue(null, true);
            focused.SetValue(null, true);

            await Hook.AfterDeath(Context.RunState, Combat, Player.Creature, card, true, 0f);
            Assert.Equal(true, hovered.GetValue(null));
            Assert.Equal(true, focused.GetValue(null));

            await Hook.AfterDeath(Context.RunState, Combat, EnemyAt(0), card, false, 0f);
            Assert.Equal(true, hovered.GetValue(null));
            Assert.Equal(true, focused.GetValue(null));

            await Hook.AfterDeath(Context.RunState, Combat, Player.Creature, null, false, 0f);
            Assert.Equal(false, hovered.GetValue(null));
            Assert.Equal(false, focused.GetValue(null));
        }
        finally
        {
            MeiLinBattleReadyOverlay.NotifyCombatEnded();
        }
    }
}
