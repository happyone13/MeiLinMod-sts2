using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Compat;

public static class CombatStateCompat
{
    private static readonly PropertyInfo? CardCombatStateProperty =
        typeof(CardModel).GetProperty("CombatState", BindingFlags.Public | BindingFlags.Instance);

    private static readonly PropertyInfo? CreatureCombatStateProperty =
        typeof(Creature).GetProperty("CombatState", BindingFlags.Public | BindingFlags.Instance);

    private static readonly MethodInfo? CardCreateMethod =
        FindCreateCardMethod(generic: false);

    private static readonly MethodInfo? GenericCardCreateMethod =
        FindCreateCardMethod(generic: true);

    public static object? TryGetCombatState(CardModel? card) =>
        TryGetPropertyValue(card, CardCombatStateProperty);

    public static object? TryGetCombatState(Creature? creature) =>
        TryGetPropertyValue(creature, CreatureCombatStateProperty);

    public static CardModel? CreateCard(
        object? combatState,
        CardModel canonical,
        Player player)
    {
        if (combatState == null || CardCreateMethod == null)
            return null;

        return TryInvoke(() => CardCreateMethod.Invoke(combatState, [canonical, player]) as CardModel);
    }

    public static T? CreateCard<T>(object? combatState, Player player)
        where T : CardModel
    {
        if (combatState == null || GenericCardCreateMethod == null)
            return null;

        return TryInvoke(() => GenericCardCreateMethod.MakeGenericMethod(typeof(T)).Invoke(combatState, [player]) as T);
    }

    public static bool HappenedThisTurn(object entry, object? combatState)
    {
        if (entry == null || combatState == null)
            return false;

        var happenedThisTurnMethod = entry.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(method =>
                method.Name == "HappenedThisTurn" &&
                method.ReturnType == typeof(bool) &&
                method.GetParameters().Length == 1);
        if (happenedThisTurnMethod == null)
            return false;

        return TryInvoke(() => (bool?)happenedThisTurnMethod.Invoke(entry, [combatState])) ?? false;
    }

    private static MethodInfo? FindCreateCardMethod(bool generic)
    {
        return typeof(CardModel).Assembly
            .GetTypes()
            .Where(type => type.Name is "CombatState" or "ICombatState")
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .FirstOrDefault(method =>
                method.Name == "CreateCard" &&
                method.IsGenericMethodDefinition == generic &&
                ParametersMatch(
                    method,
                    generic
                        ? [typeof(Player)]
                        : [typeof(CardModel), typeof(Player)]));
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

    private static object? TryGetPropertyValue(object? target, PropertyInfo? property)
    {
        if (target == null || property == null)
            return null;

        return TryInvoke(() => property.GetValue(target));
    }

    private static T? TryInvoke<T>(Func<T?> action)
    {
        try
        {
            return action();
        }
        catch
        {
            return default;
        }
    }
}
