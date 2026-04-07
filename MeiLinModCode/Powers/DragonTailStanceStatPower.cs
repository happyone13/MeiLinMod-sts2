using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Powers;

public class DragonTailStanceStatPower : MeiLinModPower
{
    private decimal _appliedStrength;
    private decimal _appliedDexterity;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await Refresh(cardSource);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;

        await Refresh(cardPlay.Card);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        await Refresh(null);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_appliedStrength != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -_appliedStrength, oldOwner, null, silent: true);

        if (_appliedDexterity != 0m)
            await PowerCmd.Apply<DexterityPower>(oldOwner, -_appliedDexterity, oldOwner, null, silent: true);

        _appliedStrength = 0m;
        _appliedDexterity = 0m;
    }

    private async Task Refresh(CardModel? cardSource)
    {
        var targetStrength = XiangzuLegacyPower.IsInGuardStance(Owner) ? Amount : 0m;
        var targetDexterity = XiangzuLegacyPower.IsInAttackStance(Owner) ? Amount : 0m;

        var deltaStrength = targetStrength - _appliedStrength;
        if (deltaStrength != 0m)
        {
            await PowerCmd.Apply<StrengthPower>(Owner, deltaStrength, Owner, cardSource, silent: true);
            _appliedStrength = targetStrength;
        }

        var deltaDexterity = targetDexterity - _appliedDexterity;
        if (deltaDexterity != 0m)
        {
            await PowerCmd.Apply<DexterityPower>(Owner, deltaDexterity, Owner, cardSource, silent: true);
            _appliedDexterity = targetDexterity;
        }
    }
}
