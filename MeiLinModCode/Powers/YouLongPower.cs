using System.Linq;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class YouLongPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
            return;

        if (BasicStrikeDefendHelper.IsBasicStrike(cardPlay.Card))
        {
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered | ValueProp.Move, null, fast: true);
            return;
        }

        if (!BasicStrikeDefendHelper.IsBasicDefend(cardPlay.Card))
            return;

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return;

        var player = Owner.Player;
        if (player == null)
            return;

        var target = player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target == null)
            return;

        await CreatureCmd.Damage(context, target, Amount, ValueProp.Unpowered | ValueProp.Move, Owner, null);
    }
}
