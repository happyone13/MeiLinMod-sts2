using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ZuoYouQuan() : MeiLinModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await XiangzuLegacyApi.SetStance(Owner, XiangzuStance.Attack);

        for (var i = 0; i < 2; i++)
        {
            var strike = BasicStrikeDefendHelper.CreateBasicStrikeForPlayer(Owner, CombatState);
            if (strike == null)
                continue;

            if (IsUpgraded)
                CardCmd.Upgrade(strike);

            strike.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, true);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
