using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class QiPoBaFang() : MeiLinModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private const string BurstKey = "Burst";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(BurstKey, 10m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(5m)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        var qi = (int)(Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m);
        if (qi <= 0)
            return;

        await PowerCmd.Apply<QiPower>(Owner.Creature, -qi, Owner.Creature, this);
        await DamageCmd.Attack(DynamicVars[BurstKey].BaseValue)
            .FromCard(this)
            .WithHitCount(qi)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[BurstKey].UpgradeValueBy(5m);
    }
}
