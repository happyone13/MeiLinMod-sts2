using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

public static class MeiLinTarget
{
    public const string CharacterId = "MEILINMOD_MEI_LIN_MOD";
    private const string LegacyCharacterId = "MeiLinMod";

    public static bool IsTarget(Player? player)
    {
        return IsTarget(player?.Character);
    }

    public static bool IsTarget(CharacterModel? character)
    {
        return character != null &&
               (EntryEquals(character.Id.Entry, CharacterId) ||
                string.Equals(character.Id.Entry, LegacyCharacterId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsMineTargetCard(CardModel? card)
    {
        return card != null && IsTarget(card.Owner?.Character);
    }

    public static bool EntryEquals(string? actual, string publicEntry)
    {
        if (string.IsNullOrEmpty(actual))
            return false;

        return string.Equals(NormalizeEntry(actual), NormalizeEntry(publicEntry), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeEntry(string entry)
    {
        return entry.Replace('-', '_');
    }
}
