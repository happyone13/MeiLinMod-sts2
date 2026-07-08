using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class QiPoBaFang() : MeiLinModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private const string BurstKey = "Burst";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar(BurstKey, 6m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var qi = XiangzuCombatState.GetQi(Owner.Creature);
        var combatState = CombatState;
        if (combatState == null)
            return;
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + DynamicVars[BurstKey].BaseValue * qi)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .Execute(choiceContext);

        if (qi > 0)
        {
            await XiangzuCombatState.ConsumeAllQi(Owner.Creature, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[BurstKey].UpgradeValueBy(2m);
    }
}
