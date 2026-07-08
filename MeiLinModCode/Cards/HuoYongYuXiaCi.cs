using System;
using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class HuoYongYuXiaCi() : MeiLinModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        var target = cardPlay.Target;
        var shouldTriggerFatal = target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

        var attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCardCompat(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var killedByThisCard = shouldTriggerFatal && attackCommand.Results.SelectMany(r => r).Any(r => r.WasTargetKilled);
        if (killedByThisCard)
        {
            HuoYongYuXiaCiUpgradePower.QueueUpgrade(Owner, 1);
            MainFile.Logger.Info("[HuoYongYuXiaCi] kill confirmed by AttackCommand result, queued 1 post-combat upgrade.");
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
