using System;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
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
    protected override bool IsPlayable => XiangzuCombatState.HasQi(Owner.Creature);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar("AttackBonus", 12m),
        new DynamicVar("GuardBonus", 9m)
    ];

    protected override bool ShouldGlowGoldInternal => XiangzuCombatState.HasQi(Owner.Creature);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge,
        MeiLinHoverTipFactory.QiConsume,
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!XiangzuCombatState.HasQi(Owner.Creature))
            return;

        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        await XiangzuCombatState.TryConsumeQi(Owner.Creature, 1, Owner.Creature, this);

        if (XiangzuCombatState.IsInAttackStance(Owner.Creature))
        {
            await DamageCmd.Attack(DynamicVars["AttackBonus"].BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        if (XiangzuCombatState.IsInGuardStance(Owner.Creature))
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["GuardBonus"].BaseValue, ValueProp.Move | ValueProp.Unpowered, null, fast: true);
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Id != Id)
            return;

        await CardPileCmd.Add(cardPlay.Card, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["AttackBonus"].UpgradeValueBy(3m);
        DynamicVars["GuardBonus"].UpgradeValueBy(3m);
    }
}
