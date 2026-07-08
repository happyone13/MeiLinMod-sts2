using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Relics;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class CharacterRegistrationTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-character-registration");
    }

    [Fact]
    public async Task Character_template_preserves_starting_stats_deck_and_relic()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var character = ModelDb.Character<MeiLinCharacter>();
        Assert.Equal(75, character.StartingHp);
        Assert.Equal(99, character.StartingGold);

        var startingDeck = Player.Character.StartingDeck.ToArray();
        Assert.Equal(10, startingDeck.Length);
        AssertCardCount<AttackDefenseUnity>(startingDeck, 1);
        AssertCardCount<FireDragonGem>(startingDeck, 1);
        AssertCardCount<StrikeMeilin>(startingDeck, 4);
        AssertCardCount<DefendMeilin>(startingDeck, 4);

        Assert.Contains(GetStartingRelicTypes(character), type => type == typeof(XiangzuLegacyRelic));
    }

    [Fact]
    public async Task Character_template_preserves_multiplayer_arm_textures()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var character = ModelDb.Character<MeiLinCharacter>();
        Assert.Equal("MeiLinMod/images/charui/multiplayer_hand_meilin_point.png", character.CustomArmPointingTexturePath);
        Assert.Equal("MeiLinMod/images/charui/multiplayer_hand_meilin_rock.png", character.CustomArmRockTexturePath);
        Assert.Equal("MeiLinMod/images/charui/multiplayer_hand_meilin_paper.png", character.CustomArmPaperTexturePath);
        Assert.Equal("MeiLinMod/images/charui/multiplayer_hand_meilin_scissors.png", character.CustomArmScissorsTexturePath);
    }

    private static void AssertCardCount<TCard>(IEnumerable<CardModel> cards, int expected)
        where TCard : CardModel
    {
        var cardId = ModelDb.Card<TCard>().Id;
        Assert.Equal(expected, cards.Count(card => card.Id == cardId));
    }

    private static Type[] GetStartingRelicTypes(object character)
    {
        var property = character
            .GetType()
            .GetProperty("StartingRelicTypes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (property?.GetValue(character) is IEnumerable<Type> relicTypes)
            return relicTypes.ToArray();

        throw new InvalidOperationException($"{character.GetType().FullName} does not expose StartingRelicTypes.");
    }
}
