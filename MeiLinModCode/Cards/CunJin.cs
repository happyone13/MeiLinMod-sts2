using System;
using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
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
public class CunJin() : MeiLinModCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string EmberKey = "Ember";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar(EmberKey, 1m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening, MeiLinHoverTipFactory.Ember];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (AwakeningHelper.IsAwakened(cardPlay))
            await PowerCmd.Apply<EmberPower>(cardPlay.Target, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[EmberKey].UpgradeValueBy(1m);
    }
}



