using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Services;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ShengLongJiao() : MeiLinModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;
    public override string? CustomSpinePortraitScenePath => "res://MeiLinMod/scenes/cards/sheng_long_jiao_dynamic.tscn";
    public override SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Ancient;
    public override bool UseCustomAncientFrame => true;
    public override bool UsesDynamicChaosFrame => true;
    public override string? CustomAncientFrameMaterialPath => ChaosAncientFrameMaterialPath;
    public override string? CustomAncientBannerMaterialPath => ChaosAncientBannerMaterialPath;

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("CurrentHitCount", GetHitCountThisTurn() + 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MeiLinAudioService.SuppressNextDefaultAttackSfx(Owner);
        MeiLinAudioService.TryPlayCustomCardClip("sheng_long_jiao", Owner);

        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        var hitCount = GetHitCountThisTurn() + 1;

        if (hitCount <= 0)
            return;

        PrepareAttackAnimation(hitCount);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .WithHitCount(hitCount)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }

    private int GetHitCountThisTurn()
    {
        if (!IsMutable)
            return 0;

        var history = MegaCrit.Sts2.Core.Combat.CombatManager.Instance?.History?.CardPlaysFinished;
        var combatState = CombatState ?? Owner?.Creature?.CombatState;
        if (history == null || combatState == null)
            return 0;

        return history.Count(e =>
            e.HappenedThisTurn(combatState) &&
            e.CardPlay.Card.Owner == Owner);
    }
}
