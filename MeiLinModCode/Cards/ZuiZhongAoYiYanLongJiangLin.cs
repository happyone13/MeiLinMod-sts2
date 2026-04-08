using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Services;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ZuiZhongAoYiYanLongJiangLin() : MeiLinModCard(2, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MeiLinAudioService.SuppressNextDefaultCastSfx(Owner);
        MeiLinAudioService.TryPlayCustomCardClip("zui_zhong_ao_yi_yan_long_jiang_lin", Owner);

        for (var i = 0; i < 4; i++)
        {
            var strike = BasicStrikeDefendHelper.CreateBasicStrikeForPlayer(Owner, CombatState);
            if (strike == null)
                continue;

            if (IsUpgraded)
                CardCmd.Upgrade(strike);

            strike.SetToFreeThisCombat();
            CardCmd.ApplyKeyword(strike, CardKeyword.Exhaust);
            strike.BaseReplayCount += 1;
            await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, addedByPlayer: true);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
