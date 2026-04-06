using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class QuanXinQuanLingPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(card))
        {
            modifiedCost = originalCost;
            return false;
        }

        modifiedCost = 0m;
        return true;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(card))
            return (pileType, position);

        return (PileType.Exhaust, position);
    }
}
