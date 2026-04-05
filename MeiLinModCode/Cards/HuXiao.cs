using BaseLib.Utils;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class HuXiao : MeiLinModCard
{
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening];

    public HuXiao() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();
        await PowerCmd.Apply<AttackStanceStartStrikePower>(Owner.Creature, 1m, Owner.Creature, this);

        if (AwakeningHelper.IsAwakened(cardPlay))
            await PlayerCmd.GainEnergy(1, Owner);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
