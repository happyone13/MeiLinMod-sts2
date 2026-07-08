using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace MeiLinMod.MeiLinModCode.Relics;

public class LongYiRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await XiangzuCombatState.GainQi(Owner.Creature, 1m, Owner.Creature, null);
    }
}
