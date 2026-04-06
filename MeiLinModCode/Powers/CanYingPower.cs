using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class CanYingPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(cardPlay.Card))
            return;

        var extraTriggers = (int)decimal.Floor(Amount);
        if (extraTriggers <= 0)
            return;

        if (XiangzuLegacyPower.IsInAttackStance(Owner) && BasicStrikeDefendHelper.IsBasicStrike(cardPlay.Card) && cardPlay.Target != null)
        {
            for (var i = 0; i < extraTriggers; i++)
            {
                await DamageCmd.Attack(cardPlay.Card.DynamicVars.Damage.BaseValue)
                    .FromCard(cardPlay.Card)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(context);
            }
        }

        if (XiangzuLegacyPower.IsInGuardStance(Owner) && BasicStrikeDefendHelper.IsBasicDefend(cardPlay.Card))
        {
            for (var i = 0; i < extraTriggers; i++)
                await CreatureCmd.GainBlock(Owner, cardPlay.Card.DynamicVars.Block, cardPlay);
        }
    }
}
