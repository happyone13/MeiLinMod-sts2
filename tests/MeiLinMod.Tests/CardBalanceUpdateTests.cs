using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class CardBalanceUpdateTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-card-balance-update");
    }

    [Fact]
    public async Task Updated_cards_have_requested_base_and_upgrade_values()
    {
        var yanBao = await AddToHand<YanBao>();
        Assert.Equal(8m, yanBao.DynamicVars.Damage.BaseValue);
        Assert.Equal(4m, yanBao.DynamicVars["BonusDamage"].BaseValue);
        CardCmd.Upgrade(yanBao);
        Assert.Equal(10m, yanBao.DynamicVars.Damage.BaseValue);
        Assert.Equal(6m, yanBao.DynamicVars["BonusDamage"].BaseValue);
        await Play(yanBao, EnemyAt(0));

        var qiPoBaFang = (QiPoBaFang)ModelDb.Card<QiPoBaFang>().ToMutable();
        Assert.Equal(8m, qiPoBaFang.DynamicVars.Damage.BaseValue);
        Assert.Equal(8m, qiPoBaFang.DynamicVars["Burst"].BaseValue);
        CardCmd.Upgrade(qiPoBaFang);
        Assert.Equal(10m, qiPoBaFang.DynamicVars.Damage.BaseValue);
        Assert.Equal(10m, qiPoBaFang.DynamicVars["Burst"].BaseValue);

        var canYing = (CanYing)ModelDb.Card<CanYing>().ToMutable();
        Assert.Equal(3m, canYing.EnergyCost.GetWithModifiers(CostModifiers.All));
        Assert.DoesNotContain(CardKeyword.Retain, canYing.Keywords);
        CardCmd.Upgrade(canYing);
        Assert.Equal(3m, canYing.EnergyCost.GetWithModifiers(CostModifiers.All));
        Assert.Contains(CardKeyword.Retain, canYing.Keywords);

        var tuNa = (TuNa)ModelDb.Card<TuNa>().ToMutable();
        Assert.Equal(1m, tuNa.DynamicVars["Progress"].BaseValue);

        var cunJin = (CunJin)ModelDb.Card<CunJin>().ToMutable();
        Assert.Equal(20m, cunJin.DynamicVars["DelayedDamage"].BaseValue);
        CardCmd.Upgrade(cunJin);
        Assert.Equal(25m, cunJin.DynamicVars["DelayedDamage"].BaseValue);

        var shanHe = (ShanHe)ModelDb.Card<ShanHe>().ToMutable();
        Assert.Equal(10m, shanHe.DynamicVars.Damage.BaseValue);
        CardCmd.Upgrade(shanHe);
        Assert.Equal(12m, shanHe.DynamicVars.Damage.BaseValue);

        var yinYang = (YinYang)ModelDb.Card<YinYang>().ToMutable();
        Assert.Equal(1m, yinYang.EnergyCost.GetWithModifiers(CostModifiers.All));
        CardCmd.Upgrade(yinYang);
        Assert.Equal(0m, yinYang.EnergyCost.GetWithModifiers(CostModifiers.All));

        var xinLiuPower = await ApplyPower<XinLiuPower>(Player.Creature, 1, Player.Creature);
        Assert.NotNull(xinLiuPower);
        Assert.Equal(4, xinLiuPower.DisplayAmount);
        var xinLiuUpgradedPower = await ApplyPower<XinLiuUpgradedPower>(Player.Creature, 1, Player.Creature);
        Assert.NotNull(xinLiuUpgradedPower);
        Assert.Equal(3, xinLiuUpgradedPower.DisplayAmount);

        var kaiTian = await AddToHand<KaiTian>();
        Assert.Equal(1m, kaiTian.DynamicVars.Damage.BaseValue);

        var tieBuShan = await AddToHand<TieBuShan>();
        Assert.Equal(8m, tieBuShan.DynamicVars.Block.BaseValue);
        CardCmd.Upgrade(tieBuShan);
        Assert.Equal(11m, tieBuShan.DynamicVars.Block.BaseValue);

        var shouJin = await AddToHand<ShouJin>();
        Assert.Equal(9m, shouJin.DynamicVars.Damage.BaseValue);
        CardCmd.Upgrade(shouJin);
        Assert.Equal(12m, shouJin.DynamicVars.Damage.BaseValue);
    }

    [Fact]
    public async Task ShouJin_clears_self_ember_and_applies_it_to_all_enemies()
    {
        var card = await AddToHand<ShouJin>();
        await PowerCmd.Apply<EmberPower>(
            new BlockingPlayerChoiceContext(),
            Player.Creature,
            3m,
            Player.Creature,
            card);

        var enemies = Combat.Enemies.ToArray();
        var hpBefore = enemies.Select(enemy => enemy.CurrentHp).ToArray();

        await Play(card);

        Assert.Equal(0m, Player.Creature.GetPower<EmberPower>()?.Amount ?? 0m);
        for (var i = 0; i < enemies.Length; i++)
        {
            Assert.Equal(hpBefore[i] - 9m, enemies[i].CurrentHp);
            Assert.Equal(3m, enemies[i].GetPower<EmberPower>()?.Amount ?? 0m);
        }
    }
}
