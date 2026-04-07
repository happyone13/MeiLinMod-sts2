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
public class JiChuShi() : MeiLinModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var strike = BasicStrikeDefendHelper.CreateBasicStrikeForPlayer(Owner, CombatState);
        var defend = BasicStrikeDefendHelper.CreateBasicDefendForPlayer(Owner, CombatState);
        if (strike == null || defend == null)
            return;

        strike.SetToFreeThisCombat();
        defend.SetToFreeThisCombat();
        CardCmd.ApplyKeyword(strike, CardKeyword.Exhaust);
        CardCmd.ApplyKeyword(defend, CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, addedByPlayer: true);
        await CardPileCmd.AddGeneratedCardToCombat(defend, PileType.Hand, addedByPlayer: true);

        if (IsUpgraded)
        {
            CardCmd.Upgrade(strike);
            CardCmd.Upgrade(defend);
        }

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.EnterOtherStance();
    }

    protected override void OnUpgrade()
    {
    }
}

