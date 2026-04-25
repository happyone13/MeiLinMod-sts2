using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class BorrowForceShieldPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return;

        if (!props.HasFlag(ValueProp.Move))
            return;

        if (!BasicStrikeDefendHelper.IsStrikeCard(cardSource))
            return;

        var gainedBlock = result.TotalDamage;
        if (gainedBlock > 0m)
            await CreatureCmd.GainBlock(Owner, gainedBlock, ValueProp.Unpowered | ValueProp.Move, null, fast: true);

        await PowerCmd.Remove(this);
    }
}
