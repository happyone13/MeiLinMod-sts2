using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class TanZhaoPower : MeiLinModPower
{
    private bool _createUpgradedStrike;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        _createUpgradedStrike = cardSource?.IsUpgraded == true;
        await base.AfterApplied(applier, cardSource);
    }

    public override Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier,
        CardModel? cardSource)
    {
        if (cardSource?.IsUpgraded == true)
            _createUpgradedStrike = true;

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        var strike = BasicStrikeDefendHelper.CreateBasicStrikeForPlayer(player, CombatState);
        if (strike == null)
            return;
        strike.SetToFreeThisCombat();
        if (_createUpgradedStrike)
            CardCmd.Upgrade(strike);
        CardCmd.ApplyKeyword(strike, CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, true, CardPilePosition.Random);
    }
}
