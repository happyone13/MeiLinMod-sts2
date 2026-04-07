using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace MeiLinMod.MeiLinModCode.Relics;

[Pool(typeof(MeiLinModRelicPool))]
public class LongXinRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead || !XiangzuLegacyPower.IsInGuardStance(Owner.Creature))
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, 5m);
    }
}
