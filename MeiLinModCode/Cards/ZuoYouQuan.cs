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

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ZuoYouQuan() : MeiLinModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (var i = 0; i < 2; i++)
        {
            var strike = CombatState.CreateCard<StrikeMeilin>(Owner);
            if (strike == null)
                continue;

            if (IsUpgraded)
                CardCmd.Upgrade(strike);

            strike.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, addedByPlayer: true);
        }

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.EnterAttackStance();
    }

    protected override void OnUpgrade()
    {
    }
}


