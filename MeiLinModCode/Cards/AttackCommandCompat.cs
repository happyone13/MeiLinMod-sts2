using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

internal static class AttackCommandCompat
{
    public static AttackCommand FromCardCompat(this AttackCommand command, CardModel card, CardPlay cardPlay)
    {
#if STS2_108
        return command.FromCard(card, cardPlay);
#else
        return command.FromCard(card);
#endif
    }
}
