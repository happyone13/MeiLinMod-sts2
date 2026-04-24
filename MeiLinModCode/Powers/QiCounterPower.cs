using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class QiCounterPower : MeiLinModPower
{
    private bool _resolving;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => (int)Amount;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await ResolveQiGain(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount <= 0m)
            return;

        await ResolveQiGain(applier, cardSource);
    }

    public static async Task AddProgress(Creature owner, int progress, Creature? applier, CardModel? cardSource)
    {
        if (progress <= 0)
            return;

        if (owner.HasPower<QiProgressDoubleThisTurnPower>())
            progress *= 2;

        var actor = applier ?? owner;
        await PowerCmd.Apply<QiCounterPower>(owner, progress, actor, cardSource, silent: true);
    }

    public static async Task ResolvePending(Creature owner, Creature? applier, CardModel? cardSource)
    {
        var counter = owner.GetPower<QiCounterPower>();
        if (counter == null)
            return;

        await counter.ResolveQiGain(applier, cardSource);
    }

    public static int GetRequiredSlotsForNextQi(Creature owner)
    {
        var qiAmount = owner.GetPower<QiPower>()?.Amount ?? 0m;
        var qi = (int)decimal.Floor(qiAmount);

        // TongQiao: gaining Qi no longer increases required Qi slots.
        if (owner.HasPower<TongQiaoPower>())
            qi = 0;

        var reduction = GetRequirementReduction(owner);
        var increase = (int)decimal.Floor(owner.GetPower<QiRequirementIncreasePower>()?.Amount ?? 0m);
        return Math.Max(1, 3 + qi - reduction + Math.Max(0, increase));
    }

    private static int GetRequirementReduction(Creature owner)
    {
        var spiritAmount = owner.GetPower<XiangzuSpiritPower>()?.Amount ?? 0m;
        var spiritStacks = (int)decimal.Floor(spiritAmount);
        return Math.Max(0, spiritStacks);
    }

    private async Task ResolveQiGain(Creature? applier, CardModel? cardSource)
    {
        if (_resolving || Owner == null)
            return;

        _resolving = true;
        try
        {
            var actor = applier ?? Owner;
            while (true)
            {
                var required = GetRequiredSlotsForNextQi(Owner);
                if (Amount < required)
                    break;

                await PowerCmd.Apply<QiCounterPower>(Owner, -required, actor, cardSource, silent: true);
                await EnsureAttackStanceWhenNoStance(actor, cardSource);
                await PowerCmd.Apply<QiPower>(Owner, 1m, actor, cardSource, silent: true);
                Flash();
            }
        }
        finally
        {
            _resolving = false;
        }
    }

    private async Task EnsureAttackStanceWhenNoStance(Creature actor, CardModel? cardSource)
    {
        if (Owner.HasPower<StanceGongPower>() || Owner.HasPower<StanceYuPower>() || Owner.HasPower<GuiYiDualStancePower>())
            return;

        var legacy = Owner.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
        {
            await legacy.EnterAttackStance();
            return;
        }

        await PowerCmd.Apply<StanceGongPower>(Owner, 1m, actor, cardSource, silent: true);
    }
}
