using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MegaCrit.Sts2.Core.Entities.Cards;
using TestTheSpire;
using Xunit;

namespace MeiLinMod.Tests;

public sealed class BasicCombatTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-basic-smoke");
    }

    [Fact]
    public async Task StrikeMeilin_deals_six_damage()
    {
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;
        var strike = await AddToHand<StrikeMeilin>();

        await Play(strike, enemy);

        Assert.Equal(hpBefore - 6, enemy.CurrentHp);
    }

    [Fact]
    public async Task DefendMeilin_is_basic_defend_and_can_be_played()
    {
        var defend = await AddToHand<DefendMeilin>();

        Assert.True(MeiLinTarget.EntryEquals(defend.Id.Entry, "MEILINMOD_DEFEND_MEILIN"));
        Assert.True(BasicStrikeDefendHelper.IsBasicDefend(defend));

        await Play(defend);
    }

    [Fact]
    public async Task HuiGuiJiBenGong_generates_four_strikes_and_four_defends()
    {
        var drawPile = PileType.Draw.GetPile(Player);
        var strikeCountBefore = drawPile.Cards.Count(BasicStrikeDefendHelper.IsBasicStrike);
        var defendCountBefore = drawPile.Cards.Count(BasicStrikeDefendHelper.IsBasicDefend);
        var card = await AddToHand<HuiGuiJiBenGong>();

        await Play(card);

        Assert.Equal(strikeCountBefore + 4, drawPile.Cards.Count(BasicStrikeDefendHelper.IsBasicStrike));
        Assert.Equal(defendCountBefore + 4, drawPile.Cards.Count(BasicStrikeDefendHelper.IsBasicDefend));
    }
}
