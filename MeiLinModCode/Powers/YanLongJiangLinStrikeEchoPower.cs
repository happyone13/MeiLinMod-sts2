using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class YanLongJiangLinStrikeEchoPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Preserve the existing icon assets while using a fresh internal model id.
    public override string CustomPackedIconPath => "zui_zhong_ao_yi_yan_long_jiang_lin_power.png".PowerImagePathOrDefault();
    public override string CustomBigIconPath => "zui_zhong_ao_yi_yan_long_jiang_lin_power.png".BigPowerImagePathOrDefault();

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsStrikeCard(card))
            return playCount;

        var extraPlays = (int)decimal.Floor(Amount);
        if (extraPlays <= 0)
            return playCount;

        return playCount + extraPlays;
    }
}
