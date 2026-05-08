using BaseLib.Utils;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class HuiGuiJiBenGong() : MeiLinModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();

        var generatedCards = new List<CardModel>(10);
        for (int i = 0; i < 5; i++)
        {
            var strike = BasicStrikeDefendHelper.CreateBasicStrikeForPlayer(Owner, CombatState);
            var defend = BasicStrikeDefendHelper.CreateBasicDefendForPlayer(Owner, CombatState);

            if (strike != null)
            {
                if (IsUpgraded)
                    CardCmd.Upgrade(strike);
                generatedCards.Add(strike);
            }

            if (defend != null)
            {
                if (IsUpgraded)
                    CardCmd.Upgrade(defend);
                generatedCards.Add(defend);
            }
        }

        if (generatedCards.Count > 0)
        {
            var results = await CardPileCmd.AddGeneratedCardsToCombat(generatedCards, PileType.Draw, true, CardPilePosition.Random);
            CardCmd.PreviewCardPileAdd(results, 1.8f, CardPreviewStyle.MessyLayout);
            PileType.Draw.GetPile(Owner).InvokeContentsChanged();
        }

        await PowerCmd.Apply<HuiGuiJiBenGongPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}
