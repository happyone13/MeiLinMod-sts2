using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class CunJinDelayedAoePower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player != Owner.Player)
            return;

        var dealer = Applier ?? Owner;
        if (dealer != null)
        {
            var enemies = CombatState.HittableEnemies.ToList();
            if (enemies.Count > 0)
            {
                await CreatureCmd.Damage(choiceContext, enemies, Amount, ValueProp.Move, dealer);
            }
        }

        await PowerCmd.Remove(this);
    }
}
