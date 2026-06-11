using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class XinRuZhiShuiPower : MeiLinModPower
{
    private bool _createUpgradedDefend;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        _createUpgradedDefend = cardSource?.IsUpgraded == true;
        await base.AfterApplied(applier, cardSource);
    }

    public override Task AfterPowerAmountChanged(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier,
        CardModel? cardSource)
    {
        if (cardSource?.IsUpgraded == true)
            _createUpgradedDefend = true;

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        var defend = BasicStrikeDefendHelper.CreateBasicDefendForPlayer(player, CombatState);
        if (defend == null)
            return;
        defend.SetToFreeThisCombat();
        if (_createUpgradedDefend)
            CardCmd.Upgrade(defend);
        CardCmd.ApplyKeyword(defend, CardKeyword.Ethereal);
        await CardPileCmd.AddGeneratedCardToCombat(defend, PileType.Hand, player, CardPilePosition.Top);
    }
}
