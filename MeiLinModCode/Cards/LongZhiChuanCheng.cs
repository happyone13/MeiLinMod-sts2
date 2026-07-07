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

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class LongZhiChuanCheng() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    private const string EmberKey = "Ember";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(EmberKey, 2m),
        new CardsVar(2),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        MeiLinHoverTipFactory.Ember,
        EnergyHoverTip
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null)
            return;

        await PowerCmd.Apply<EmberPower>(
            new BlockingPlayerChoiceContext(),
            cardPlay.Target,
            DynamicVars[EmberKey].BaseValue,
            Owner.Creature,
            this);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, targetPlayer);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, targetPlayer);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[EmberKey].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
