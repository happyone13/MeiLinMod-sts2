using System.Linq;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LongYinPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        for (var i = 0; i < Amount; i++)
        {
            var candidates = PileType.Draw.GetPile(player).Cards
                .Concat(PileType.Discard.GetPile(player).Cards)
                .Where(BasicStrikeDefendHelper.IsBasicDefend)
                .ToList();
            if (candidates.Count == 0)
                return;

            var selected = player.RunState.Rng.CombatTargets.NextItem(candidates);
            if (selected == null)
                return;

            await CardCmd.AutoPlay(choiceContext, selected, null);
        }
    }
}
