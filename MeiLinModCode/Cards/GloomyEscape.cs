using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Encounters;
using MeiLinMod.MeiLinModCode.Migration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace MeiLinMod.MeiLinModCode.Cards;

/// <summary>
/// A combat-only, colorless escape option supplied by the Gloomy encounter.
/// Player creatures must remain attached to combat, so the effect safely ends
/// the encounter by marking it as escaped and applying vanilla enemy escape.
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class GloomyEscape() : MeiLinModCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    // This token can belong to any character. Do not play MeiLin-specific skill animations.
    public override Task BeforeCardPlayed(CardPlay cardPlay) => Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState?.Encounter is not GloomyPackEncounter encounter)
            return;

        encounter.MarkPlayerEscaped();

        // CreatureCmd.Escape mutates the enemy collection, so enumerate a snapshot.
        foreach (var enemy in combatState.Enemies.ToList())
            await CreatureCmd.Escape(enemy, removeCreatureNode: true);
    }

    protected override void OnUpgrade()
    {
    }
}
