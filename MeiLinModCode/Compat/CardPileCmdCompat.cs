using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Sts2CardPileCmd = MegaCrit.Sts2.Core.Commands.CardPileCmd;

namespace MeiLinMod.MeiLinModCode.Compat;

public static class CardPileCmdCompat
{
    private static readonly MethodInfo? AddGeneratedCardWithCreatorMethod =
        FindMethod(
            nameof(Sts2CardPileCmd.AddGeneratedCardToCombat),
            [typeof(CardModel), typeof(PileType), typeof(Player), typeof(CardPilePosition)]);

    private static readonly MethodInfo? AddGeneratedCardWithBoolMethod =
        FindMethod(
            nameof(Sts2CardPileCmd.AddGeneratedCardToCombat),
            [typeof(CardModel), typeof(PileType), typeof(bool), typeof(CardPilePosition)]);

    private static readonly MethodInfo? AddGeneratedCardsWithCreatorMethod =
        FindMethod(
            nameof(Sts2CardPileCmd.AddGeneratedCardsToCombat),
            [typeof(IEnumerable<CardModel>), typeof(PileType), typeof(Player), typeof(CardPilePosition)]);

    private static readonly MethodInfo? AddGeneratedCardsWithBoolMethod =
        FindMethod(
            nameof(Sts2CardPileCmd.AddGeneratedCardsToCombat),
            [typeof(IEnumerable<CardModel>), typeof(PileType), typeof(bool), typeof(CardPilePosition)]);

    public static Task<CardPileAddResult> Add(
        CardModel card,
        PileType newPileType,
        CardPilePosition position = CardPilePosition.Bottom,
        AbstractModel? source = null,
        bool skipVisuals = false) =>
        Sts2CardPileCmd.Add(card, newPileType, position, source, skipVisuals);

    public static Task<IEnumerable<CardModel>> Draw(
        PlayerChoiceContext choiceContext,
        decimal count,
        Player player,
        bool fromHandDraw = false) =>
        Sts2CardPileCmd.Draw(choiceContext, count, player, fromHandDraw);

    public static Task<CardPileAddResult> AddGeneratedCardToCombat(
        CardModel card,
        PileType newPileType,
        Player? creator,
        CardPilePosition position = CardPilePosition.Bottom)
    {
        if (AddGeneratedCardWithCreatorMethod != null)
            return Invoke<CardPileAddResult>(AddGeneratedCardWithCreatorMethod, [card, newPileType, creator!, position]);

        if (AddGeneratedCardWithBoolMethod != null)
            return Invoke<CardPileAddResult>(AddGeneratedCardWithBoolMethod, [card, newPileType, creator != null, position]);

        throw new MissingMethodException(
            $"No compatible CardPileCmd.AddGeneratedCardToCombat overload was found for {typeof(Sts2CardPileCmd).Assembly.FullName}.");
    }

    public static Task<IReadOnlyList<CardPileAddResult>> AddGeneratedCardsToCombat(
        IEnumerable<CardModel> cards,
        PileType newPileType,
        Player? creator,
        CardPilePosition position = CardPilePosition.Bottom)
    {
        if (AddGeneratedCardsWithCreatorMethod != null)
            return Invoke<IReadOnlyList<CardPileAddResult>>(AddGeneratedCardsWithCreatorMethod, [cards, newPileType, creator!, position]);

        if (AddGeneratedCardsWithBoolMethod != null)
            return Invoke<IReadOnlyList<CardPileAddResult>>(AddGeneratedCardsWithBoolMethod, [cards, newPileType, creator != null, position]);

        throw new MissingMethodException(
            $"No compatible CardPileCmd.AddGeneratedCardsToCombat overload was found for {typeof(Sts2CardPileCmd).Assembly.FullName}.");
    }

    private static Task<T> Invoke<T>(MethodInfo method, object?[] args)
    {
        return (Task<T>)(method.Invoke(null, args)
            ?? throw new InvalidOperationException($"CardPileCmd method '{method}' returned null."));
    }

    private static MethodInfo? FindMethod(string name, IReadOnlyList<Type> parameterTypes)
    {
        return typeof(Sts2CardPileCmd)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == name && ParametersMatch(method, parameterTypes));
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
}
