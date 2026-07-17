using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Encounters;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class GloomyEscapeTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .Encounter<GloomyPackEncounter>()
            .WithSeed("meilinmod-gloomy-escape");
    }

    [Fact]
    public async Task Encounter_deals_one_colorless_escape_token_before_opening_hand()
    {
        var cards = PileType.Hand.GetPile(Player).Cards.OfType<GloomyEscape>().ToArray();
        var card = Assert.Single(cards);
        var encounter = Assert.IsType<GloomyPackEncounter>(Combat.Encounter);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Token, card.Rarity);
        Assert.Equal(TargetType.Self, card.TargetType);
        Assert.IsType<ColorlessCardPool>(card.Pool);
        Assert.Contains(CardKeyword.Retain, card.Keywords);
        Assert.Contains(CardKeyword.Exhaust, card.Keywords);
        Assert.True(encounter.EscapeCardsDealt);

        await Play(await AddToHand<DefendMeilin>());
    }

    [Fact]
    public async Task Playing_escape_marks_no_rewards_and_escapes_every_enemy()
    {
        var card = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<GloomyEscape>());
        var encounter = Assert.IsType<GloomyPackEncounter>(Combat.Encounter);
        var enemyCount = Combat.Enemies.Count;

        await Play(card);

        Assert.True(encounter.WasPlayerEscape);
        Assert.False(encounter.ShouldGiveRewards);
        Assert.Equal(0f, encounter.CalculateGoldProportion(Combat));
        Assert.Empty(Combat.Enemies);
        Assert.Equal(enemyCount, Combat.EscapedCreatures.Count);
        Assert.DoesNotContain(Player.Creature, Combat.EscapedCreatures);
    }

    [Fact]
    public async Task Escape_state_round_trips_through_encounter_custom_state()
    {
        var encounter = Assert.IsType<GloomyPackEncounter>(Combat.Encounter);
        encounter.MarkPlayerEscaped();

        var restored = (GloomyPackEncounter)ModelDb.Encounter<GloomyPackEncounter>().ToMutable();
        restored.LoadCustomState(encounter.SaveCustomState());

        Assert.True(restored.WasPlayerEscape);
        Assert.False(restored.ShouldGiveRewards);

        await Play(await AddToHand<DefendMeilin>());
    }
}

public sealed class GloomyEscapeMultiplayerTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddRemotePlayer<MeiLinCharacter>(2)
            .Encounter<GloomyPackEncounter>()
            .WithSeed("meilinmod-gloomy-escape-multiplayer");
    }

    [Fact]
    public async Task Encounter_deals_exactly_one_escape_token_to_each_player()
    {
        Assert.Equal(2, Players.Count);

        foreach (var player in Players)
        {
            Assert.Single(PileType.Hand.GetPile(player).Cards.OfType<GloomyEscape>());
        }

        await Play(await AddToHand<DefendMeilin>());
    }
}
