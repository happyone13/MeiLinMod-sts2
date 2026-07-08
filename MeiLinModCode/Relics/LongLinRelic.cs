using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace MeiLinMod.MeiLinModCode.Relics;

public class LongLinRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<BasicDefendBlockBonusPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 2m, Owner.Creature, null);
    }
}
