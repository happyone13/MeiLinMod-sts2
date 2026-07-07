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

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class QiPo() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private const string EmberKey = "Ember";
    private const string StrengthLossKey = "StrengthLoss";
    protected override bool IsPlayable => XiangzuCombatState.HasQi(Owner.Creature);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(StrengthLossKey, 4m),
        new DynamicVar(EmberKey, 2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiConsume,
        MeiLinHoverTipFactory.Ember
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!XiangzuCombatState.HasQi(Owner.Creature))
            return;
        var combatState = CombatState;
        if (combatState == null)
            return;

        await XiangzuCombatState.TryConsumeQi(Owner.Creature, 1, Owner.Creature, this);
        foreach (var enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<QiPoTemporaryStrengthDownPower>(new BlockingPlayerChoiceContext(), enemy, DynamicVars[StrengthLossKey].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), enemy, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[StrengthLossKey].UpgradeValueBy(2m);
        DynamicVars[EmberKey].UpgradeValueBy(1m);
    }
}
