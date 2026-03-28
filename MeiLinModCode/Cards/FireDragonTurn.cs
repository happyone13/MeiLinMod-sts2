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

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class FireDragonTurn() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string EmberKey = "Ember";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(EmberKey, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [MeiLinHoverTipFactory.Ember];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await PowerCmd.Apply<EmberPower>(
            cardPlay.Target,
            DynamicVars[EmberKey].BaseValue,
            Owner.Creature,
            this);

        XiangzuLegacyPower? legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy == null)
            return;

        // Default stance(Heng) first switch goes to Attack(Gong), then Gong<->Yu.
        if (Owner.Creature.HasPower<StanceGongPower>())
        {
            await legacy.EnterGuardStance();
        }
        else
        {
            await legacy.EnterAttackStance();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[EmberKey].UpgradeValueBy(1m);
    }
}
