using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MeiLinMod.MeiLinModCode.Services;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class FireDragonGem() : MeiLinModCard(1, CardType.Power, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];
    public override string? CustomSpinePortraitScenePath =>
        "res://MeiLinMod/scenes/cards/fire_dragon_gem_dynamic.tscn";
    public override SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Ancient;
    public override bool UseCustomAncientFrame => true;
    public override bool UsesDynamicChaosFrame => true;
    public override string? CustomAncientFrameMaterialPath => ChaosAncientFrameMaterialPath;
    public override string? CustomAncientBannerMaterialPath => ChaosAncientBannerMaterialPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MeiLinAudioService.SuppressNextDefaultCastSfx(Owner);
        MeiLinAudioService.TryPlayCustomCardClip("fire_dragon_gam", Owner);

        await CreatureCmd.TriggerAnim(
            Owner.Creature,
            "Cast",
            Owner.Character.CastAnimDelay
        );

        await PowerCmd.Apply<FireDragonGemPower>(
            Owner.Creature,
            1m,
            Owner.Creature,
            this
        );
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
