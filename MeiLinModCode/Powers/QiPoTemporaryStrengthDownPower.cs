using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Extensions;

namespace MeiLinMod.MeiLinModCode.Powers;

public class QiPoTemporaryStrengthDownPower : MeiLinModPower, ICustomModel
{
    private decimal _appliedAmount;

    public AbstractModel OriginModel => ModelDb.Card<QiPo>();
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse QiPo icon so this debuff is always visible and themed.
    public override string CustomPackedIconPath => "qi_po_power.png".PowerImagePathOrDefault();
    public override string CustomBigIconPath => "qi_po_power.png".BigPowerImagePathOrDefault();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (_appliedAmount != 0m || Amount == 0m)
            return;

        _appliedAmount = Amount;
        await PowerCmd.Apply<StrengthPower>(Owner, -_appliedAmount, applier, cardSource, silent: true);
    }

    public override async Task AfterPowerAmountChanged(
#if STS2_104
        PlayerChoiceContext choiceContext,
#endif
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount == 0m)
            return;

        // Skip the initial amount-changed callback if the opening stack was already
        // applied in AfterApplied; later stack changes still pass through here.
        if (amount > 0m && _appliedAmount == Amount)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, -amount, applier, cardSource, silent: true);
        _appliedAmount += amount;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_appliedAmount == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(oldOwner, _appliedAmount, oldOwner, null, silent: true);
        _appliedAmount = 0m;
    }
}
