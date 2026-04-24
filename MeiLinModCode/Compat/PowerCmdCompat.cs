using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Sts2PowerCmd = MegaCrit.Sts2.Core.Commands.PowerCmd;

namespace MeiLinMod.MeiLinModCode.Compat;

public static class PowerCmdCompat
{
    public static Task<IReadOnlyList<T>> Apply<T>(
        IEnumerable<Creature> targets,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel =>
        Sts2PowerCmd.Apply<T>(CreateDefaultContext(), targets, amount, applier, cardSource, silent);

    public static Task<T> Apply<T>(
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel =>
        Sts2PowerCmd.Apply<T>(CreateDefaultContext(), target, amount, applier, cardSource, silent);

    public static Task Apply(
        PowerModel power,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false) =>
        Sts2PowerCmd.Apply(CreateDefaultContext(), power, target, amount, applier, cardSource, silent);

    public static Task<IReadOnlyList<T>> Apply<T>(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel =>
        Sts2PowerCmd.Apply<T>(choiceContext, targets, amount, applier, cardSource, silent);

    public static Task<T> Apply<T>(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel =>
        Sts2PowerCmd.Apply<T>(choiceContext, target, amount, applier, cardSource, silent);

    public static Task Apply(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false) =>
        Sts2PowerCmd.Apply(choiceContext, power, target, amount, applier, cardSource, silent);

    public static Task Remove<T>(Creature creature)
        where T : PowerModel =>
        Sts2PowerCmd.Remove<T>(creature);

    public static Task Remove(PowerModel power) => Sts2PowerCmd.Remove(power);

    public static Task Decrement(PowerModel power) => Sts2PowerCmd.Decrement(power);

    private static BlockingPlayerChoiceContext CreateDefaultContext() => new();
}
