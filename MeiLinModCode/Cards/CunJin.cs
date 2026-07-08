using System;
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
public class CunJin() : MeiLinModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string DelayedDamageKey = "DelayedDamage";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar(DelayedDamageKey, 15m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCardCompat(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<CunJinDelayedAoePower>(new BlockingPlayerChoiceContext(), Owner.Creature, DynamicVars[DelayedDamageKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DelayedDamageKey].UpgradeValueBy(5m);
    }
}



