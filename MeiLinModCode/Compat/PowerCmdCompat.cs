using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Sts2PowerCmd = MegaCrit.Sts2.Core.Commands.PowerCmd;

namespace MeiLinMod.MeiLinModCode.Compat;

public static class PowerCmdCompat
{
    private static readonly MethodInfo? ApplyManyWithContext =
        FindApplyMethod(generic: true, withContext: true, targetKind: ApplyTargetKind.Many);
    private static readonly MethodInfo? ApplyOneWithContext =
        FindApplyMethod(generic: true, withContext: true, targetKind: ApplyTargetKind.One);
    private static readonly MethodInfo? ApplyPowerWithContext =
        FindApplyMethod(generic: false, withContext: true, targetKind: ApplyTargetKind.Power);

    private static readonly MethodInfo? ApplyManyWithoutContext =
        FindApplyMethod(generic: true, withContext: false, targetKind: ApplyTargetKind.Many);
    private static readonly MethodInfo? ApplyOneWithoutContext =
        FindApplyMethod(generic: true, withContext: false, targetKind: ApplyTargetKind.One);
    private static readonly MethodInfo? ApplyPowerWithoutContext =
        FindApplyMethod(generic: false, withContext: false, targetKind: ApplyTargetKind.Power);

    public static Task<IReadOnlyList<T>> Apply<T>(
        IEnumerable<Creature> targets,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel =>
        Apply<T>(CreateDefaultContext(), targets, amount, applier, cardSource, silent);

    public static Task<T> Apply<T>(
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel =>
        Apply<T>(CreateDefaultContext(), target, amount, applier, cardSource, silent);

    public static Task Apply(
        PowerModel power,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false) =>
        Apply(CreateDefaultContext(), power, target, amount, applier, cardSource, silent);

    public static Task<IReadOnlyList<T>> Apply<T>(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel
    {
        object result = InvokeApply(
            ApplyManyWithContext,
            ApplyManyWithoutContext,
            choiceContext,
            typeof(T),
            [targets, amount, applier, cardSource!, silent],
            [choiceContext, targets, amount, applier, cardSource!, silent]);
        return (Task<IReadOnlyList<T>>)result;
    }

    public static Task<T> Apply<T>(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
        where T : PowerModel
    {
        object result = InvokeApply(
            ApplyOneWithContext,
            ApplyOneWithoutContext,
            choiceContext,
            typeof(T),
            [target, amount, applier, cardSource!, silent],
            [choiceContext, target, amount, applier, cardSource!, silent]);
        return (Task<T>)result;
    }

    public static Task Apply(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        Creature target,
        decimal amount,
        Creature applier,
        CardModel? cardSource,
        bool silent = false)
    {
        object result = InvokeApply(
            ApplyPowerWithContext,
            ApplyPowerWithoutContext,
            choiceContext,
            genericArgument: null,
            [power, target, amount, applier, cardSource!, silent],
            [choiceContext, power, target, amount, applier, cardSource!, silent]);
        return (Task)result;
    }

    public static Task Remove<T>(Creature creature)
        where T : PowerModel =>
        Sts2PowerCmd.Remove<T>(creature);

    public static Task Remove(PowerModel power) => Sts2PowerCmd.Remove(power);

    public static Task Decrement(PowerModel power) => Sts2PowerCmd.Decrement(power);

    private static object InvokeApply(
        MethodInfo? withContextMethod,
        MethodInfo? withoutContextMethod,
        PlayerChoiceContext choiceContext,
        Type? genericArgument,
        object?[] withoutContextArgs,
        object?[] withContextArgs)
    {
        if (withContextMethod != null)
            return InvokeMethod(withContextMethod, genericArgument, withContextArgs);

        if (withoutContextMethod != null)
            return InvokeMethod(withoutContextMethod, genericArgument, withoutContextArgs);

        throw new MissingMethodException(
            $"No compatible PowerCmd.Apply overload was found for {typeof(Sts2PowerCmd).Assembly.FullName}.");
    }

    private static object InvokeMethod(MethodInfo method, Type? genericArgument, object?[] args)
    {
        MethodInfo resolved = method;
        if (genericArgument != null)
            resolved = resolved.MakeGenericMethod(genericArgument);

        return resolved.Invoke(null, args)
               ?? throw new InvalidOperationException($"PowerCmd method '{resolved}' returned null.");
    }

    private static MethodInfo? FindApplyMethod(bool generic, bool withContext, ApplyTargetKind targetKind)
    {
        Type[] parameters = BuildParameterList(withContext, targetKind);
        return typeof(Sts2PowerCmd)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == nameof(Sts2PowerCmd.Apply) &&
                method.IsGenericMethodDefinition == generic &&
                ParametersMatch(method, parameters));
    }

    private static Type[] BuildParameterList(bool withContext, ApplyTargetKind targetKind)
    {
        var parameters = new List<Type>();
        if (withContext)
            parameters.Add(typeof(PlayerChoiceContext));

        switch (targetKind)
        {
            case ApplyTargetKind.Many:
                parameters.Add(typeof(IEnumerable<Creature>));
                break;
            case ApplyTargetKind.One:
                parameters.Add(typeof(Creature));
                break;
            case ApplyTargetKind.Power:
                parameters.Add(typeof(PowerModel));
                parameters.Add(typeof(Creature));
                break;
        }

        parameters.Add(typeof(decimal));
        parameters.Add(typeof(Creature));
        parameters.Add(typeof(CardModel));
        parameters.Add(typeof(bool));
        return parameters.ToArray();
    }

    private static bool ParametersMatch(MethodInfo method, IReadOnlyList<Type> expectedParameters)
    {
        ParameterInfo[] actualParameters = method.GetParameters();
        if (actualParameters.Length != expectedParameters.Count)
            return false;

        for (int i = 0; i < actualParameters.Length; i++)
        {
            if (actualParameters[i].ParameterType != expectedParameters[i])
                return false;
        }

        return true;
    }

    private static ThrowingPlayerChoiceContext CreateDefaultContext() => new();

    private enum ApplyTargetKind
    {
        Many,
        One,
        Power
    }
}
