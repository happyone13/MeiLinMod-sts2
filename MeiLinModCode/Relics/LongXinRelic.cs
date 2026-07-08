using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace MeiLinMod.MeiLinModCode.Relics;

public class LongXinRelic : MeiLinModRelic
{
    private bool _playedGuiYiThisCombat;
    private bool _shouldHealAfterCombatVictory;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card is GuiYi)
            _playedGuiYiThisCombat = true;

        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        var qualifiesForHeal = _shouldHealAfterCombatVictory;
        _shouldHealAfterCombatVictory = false;
        _playedGuiYiThisCombat = false;

        if (Owner.Creature.IsDead || !qualifiesForHeal)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, 5m);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _shouldHealAfterCombatVictory = XiangzuCombatState.IsInGuardStance(Owner.Creature) || _playedGuiYiThisCombat;
        _playedGuiYiThisCombat = false;
        return Task.CompletedTask;
    }
}
