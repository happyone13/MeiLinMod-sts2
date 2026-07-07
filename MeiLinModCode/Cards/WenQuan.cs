using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class WenQuan() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [MeiLinHoverTipFactory.Awakening];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var toExhaust = PileType.Hand.GetPile(Owner).Cards.Where(c => c != this).ToList();
        if (toExhaust.Count > 0)
        {
            foreach (var card in toExhaust)
                await CardPileCmd.Add(card, PileType.Exhaust);

            for (var i = 0; i < toExhaust.Count; i++)
            {
                CardModel? generated = BasicStrikeDefendHelper.CreateBasicStrikeForPlayer(Owner, CombatState);
                if (generated == null)
                    continue;
                if (IsUpgraded)
                    CardCmd.Upgrade(generated);
                CardCmd.ApplyKeyword(generated, CardKeyword.Exhaust);
                await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner, CardPilePosition.Top);
            }
        }

        if (AwakeningHelper.IsAwakened(cardPlay))
        {
            await PlayerCmd.LoseEnergy(3, Owner);
            await PowerCmd.Apply<BasicStrikeDefendFreeThisTurnPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
