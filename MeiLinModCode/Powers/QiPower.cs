using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MeiLinMod.MeiLinModCode.Extensions;

namespace MeiLinMod.MeiLinModCode.Powers;

public class QiPower : MeiLinModPower
{
    private decimal _appliedStrength;
    private decimal _appliedDexterity;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await RefreshFromState(cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power.Owner != Owner)
            return;

        if (power is not QiPower &&
            power is not StanceGongPower &&
            power is not StanceYuPower &&
            power is not GuiYiDualStancePower &&
            power != this)
        {
            return;
        }

        await RefreshFromState(cardSource);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_appliedStrength != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -_appliedStrength, oldOwner, null, silent: true);

        if (_appliedDexterity != 0m)
            await PowerCmd.Apply<DexterityPower>(oldOwner, -_appliedDexterity, oldOwner, null, silent: true);

        _appliedStrength = 0m;
        _appliedDexterity = 0m;
    }

    public async Task RefreshFromState(CardModel? cardSource)
    {
        var qiAmount = Amount;
        var targetStrength = XiangzuCombatState.IsInAttackStance(Owner) ? qiAmount : 0m;
        var targetDexterity = XiangzuCombatState.IsInGuardStance(Owner) ? qiAmount : 0m;

        var deltaStrength = targetStrength - _appliedStrength;
        if (deltaStrength != 0m)
        {
            await PowerCmd.Apply<StrengthPower>(Owner, deltaStrength, Owner, cardSource, silent: true);
            _appliedStrength = targetStrength;
        }

        var deltaDexterity = targetDexterity - _appliedDexterity;
        if (deltaDexterity != 0m)
        {
            await PowerCmd.Apply<DexterityPower>(Owner, deltaDexterity, Owner, cardSource, silent: true);
            _appliedDexterity = targetDexterity;
        }
    }
}
