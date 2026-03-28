using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class FaJin() : MeiLinModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string BonusKey = "Bonus";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar(BonusKey, 1m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var usedCount = MegaCrit.Sts2.Core.Combat.CombatManager.Instance.History.CardPlaysFinished.Count(e =>
            e.CardPlay.Card.Owner == Owner &&
            BasicStrikeDefendHelper.IsBasicStrikeOrDefend(e.CardPlay.Card));

        var damage = DynamicVars.Damage.BaseValue + (usedCount * DynamicVars[BonusKey].BaseValue);
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[BonusKey].UpgradeValueBy(1m);
    }
}



