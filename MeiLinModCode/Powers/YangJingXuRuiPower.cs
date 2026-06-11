using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class YangJingXuRuiPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => GetRemainingTurns();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player != Owner.Player)
            return;

        var remainingTurns = GetRemainingTurns();
        var extraDrawTurns = GetExtraDrawTurns();
        if (remainingTurns <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        var drawPerTurn = extraDrawTurns > 0 ? 2m : 1m;
        await CardPileCmd.Draw(choiceContext, drawPerTurn, player);
        await PlayerCmd.GainEnergy(1m, player);

        var nextTurns = remainingTurns - 1;
        var nextExtraDrawTurns = Math.Max(0, extraDrawTurns - 1);
        if (nextTurns <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        var nextEncodedAmount = Encode(nextTurns, nextExtraDrawTurns);
        await PowerCmd.Apply<YangJingXuRuiPower>(new BlockingPlayerChoiceContext(), Owner, nextEncodedAmount - Amount, Owner, null, silent: true);
    }

    private int GetRemainingTurns() => Math.Abs((int)Amount) % 1000;

    private int GetExtraDrawTurns()
    {
        return Math.Abs((int)Amount) / 1000;
    }

    private static decimal Encode(int turns, int extraDrawTurns)
    {
        return extraDrawTurns * 1000 + turns;
    }
}
