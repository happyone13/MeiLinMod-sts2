using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Powers;

public class XiangzuSpiritCardPower : MeiLinModPower
{
    private class Data
    {
        public readonly Dictionary<CardModel, int> AmountsForPlayedCards = [];
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType =>
        DynamicVars["StrengthApplied"].IntValue != 0 ? PowerStackType.Counter : PowerStackType.None;

    public override int DisplayAmount => DynamicVars["StrengthApplied"].IntValue;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new DynamicVar("StrengthApplied", 0m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return Task.CompletedTask;

        GetInternalData<Data>().AmountsForPlayedCards.Add(cardPlay.Card, DynamicVars.Strength.IntValue);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player ||
            !GetInternalData<Data>().AmountsForPlayedCards.Remove(cardPlay.Card, out var value))
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Owner, value, Owner, null, silent: true);
        DynamicVars["StrengthApplied"].BaseValue += DynamicVars.Strength.IntValue;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants)
    {
        if (side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Owner, -DynamicVars["StrengthApplied"].BaseValue, Owner, null, silent: true);
    }
}
