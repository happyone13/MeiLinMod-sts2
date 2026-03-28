using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class CanYingPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(cardPlay.Card))
            return;

        if (Owner.HasPower<StanceGongPower>() && BasicStrikeDefendHelper.IsBasicStrike(cardPlay.Card) && cardPlay.Target != null)
        {
            await DamageCmd.Attack(cardPlay.Card.DynamicVars.Damage.BaseValue)
                .FromCard(cardPlay.Card)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(context);
        }

        if (Owner.HasPower<StanceYuPower>() && BasicStrikeDefendHelper.IsBasicDefend(cardPlay.Card))
            await CreatureCmd.GainBlock(Owner, cardPlay.Card.DynamicVars.Block, cardPlay);
    }
}
