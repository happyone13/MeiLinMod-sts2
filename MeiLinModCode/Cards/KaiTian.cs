using System;
using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class KaiTian() : MeiLinModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override bool IsPlayable => (Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) >= 1m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar("AttackBonus", 15m),
        new DynamicVar("GuardBonus", 10m)
    ];

    protected override bool ShouldGlowGoldInternal => (Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) >= 1m;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge,
        MeiLinHoverTipFactory.QiConsume,
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if ((Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) < 1m)
            return;

        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var bonus = 0m;
        if (XiangzuLegacyPower.IsInAttackStance(Owner.Creature))
        {
            bonus = DynamicVars["AttackBonus"].BaseValue;
        }
        else if (XiangzuLegacyPower.IsInGuardStance(Owner.Creature))
        {
            bonus = DynamicVars["GuardBonus"].BaseValue;
        }

        await DamageCmd.Attack(1m + bonus)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        await PowerCmd.Apply<QiPower>(Owner.Creature, -1m, Owner.Creature, this);
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Id != Id)
            return;

        var current = EnergyCost.GetWithModifiers(CostModifiers.All);
        if (current != 1m)
        {
            EnergyCost.AddThisCombat((int)(1m - current));
        }
        await CardPileCmd.Add(cardPlay.Card, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["AttackBonus"].UpgradeValueBy(5m);
        DynamicVars["GuardBonus"].UpgradeValueBy(5m);
    }
}
