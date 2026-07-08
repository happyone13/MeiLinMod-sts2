using System;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ShanHe() : MeiLinModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string BurstKey = "Burst";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new EnergyVar(1),
        new DynamicVar(BurstKey, 10m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [MeiLinHoverTipFactory.Awakening];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCardCompat(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;
        var combatState = CombatState;
        if (combatState == null)
            return;

        await PlayerCmd.LoseEnergy(DynamicVars.Energy.IntValue, Owner);
        await DamageCmd.Attack(DynamicVars[BurstKey].BaseValue)
            .FromCardCompat(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[BurstKey].UpgradeValueBy(5m);
    }
}
