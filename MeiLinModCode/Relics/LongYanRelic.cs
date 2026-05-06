using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MeiLinMod.MeiLinModCode.Relics;

[Pool(typeof(MeiLinModRelicPool))]
public class LongYanRelic : MeiLinModRelic
{
    private const int TurnThreshold = 3;
    private int _turnsSeen;

    public override RelicRarity Rarity => RelicRarity.Shop;
    public override bool ShowCounter => true;
    public override int DisplayAmount => TurnsSeen;

    [SavedProperty]
    public int TurnsSeen
    {
        get => _turnsSeen;
        set
        {
            _turnsSeen = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
#if STS2_104
        ICombatState combatState
#else
        CombatState combatState
#endif
    )
    {
        if (side != Owner.Creature.Side)
            return;

        TurnsSeen = (TurnsSeen + 1) % TurnThreshold;
        Status = TurnsSeen == TurnThreshold - 1 ? RelicStatus.Active : RelicStatus.Normal;

        if (TurnsSeen != 0)
            return;

        Flash();
        await PowerCmd.Apply<NextPowerCardCostDownPower>(Owner.Creature, 1m, Owner.Creature, null);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
