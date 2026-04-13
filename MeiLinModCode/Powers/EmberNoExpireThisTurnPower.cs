using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class EmberNoExpireThisTurnPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var shouldRemoveThisTurnEnd = Owner.IsPlayer
            ? side != Owner.Side
            : side == Owner.Side;
        if (!shouldRemoveThisTurnEnd)
            return;

        await PowerCmd.Remove(this);
    }
}
