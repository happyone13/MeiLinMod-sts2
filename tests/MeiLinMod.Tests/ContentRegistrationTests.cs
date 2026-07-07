using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Potions;
using MeiLinMod.MeiLinModCode.Powers;
using MeiLinMod.MeiLinModCode.Relics;
using STS2RitsuLib.Content;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class ContentRegistrationTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-content-registration");
    }

    [Fact]
    public async Task All_ritsu_registered_models_preserve_legacy_public_entries()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        AssertEntry("MEILINMOD_MEI_LIN_MOD", ModelDb.Character<MeiLinCharacter>().Id.Entry);

        foreach (var type in GetConcreteModTypes<MeiLinModCard>())
            AssertLegacyEntry(type, GetModelDbModel("Card", type));

        foreach (var type in GetConcreteModTypes<MeiLinModRelic>())
            AssertLegacyEntry(type, GetModelDbModel("Relic", type));

        foreach (var type in GetConcreteModTypes<MeiLinModPotion>())
            AssertLegacyEntry(type, GetModelDbModel("Potion", type));

        foreach (var type in GetConcreteModTypes<MeiLinModPower>())
            AssertLegacyEntry(type, GetModelDbModel("Power", type));
    }

    [Fact]
    public async Task Cards_are_registered_only_in_their_declared_meilin_pool()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var mainPoolCards = GetAllCards(ModelDb.CardPool<MeiLinModCardPool>());
        var nonePoolCards = GetAllCards(ModelDb.CardPool<NoneCardPool>());
        var meilinPools = new Dictionary<Type, CardModel[]>
        {
            [typeof(MeiLinModCardPool)] = mainPoolCards,
            [typeof(NoneCardPool)] = nonePoolCards
        };

        foreach (var type in GetConcreteModTypes<MeiLinModCard>())
        {
            var expectedPool = GetDeclaredPoolType(type);
            var expectedCard = (CardModel)GetModelDbModel("Card", type);

            Assert.Contains(meilinPools[expectedPool], card => card.Id == expectedCard.Id);

            foreach (var (poolType, cards) in meilinPools)
            {
                if (poolType == expectedPool)
                    continue;

                Assert.DoesNotContain(cards, card => card.Id == expectedCard.Id);
            }
        }

        AssertContains<YinSheChuDong>(nonePoolCards);
        AssertContains<JianJiXingShi>(nonePoolCards);
        AssertContains<YanQiChanShen>(nonePoolCards);
        AssertContains<YanDunFanJi>(nonePoolCards);
        AssertContains<YiLiYuJianTaZhiShang>(nonePoolCards);
        AssertContains<BuMie>(nonePoolCards);
        AssertContains<LongXi>(nonePoolCards);
        AssertContains<TongQiao>(nonePoolCards);
        AssertContains<RuTao>(nonePoolCards);

        AssertDoesNotContain<YinSheChuDong>(mainPoolCards);
        AssertDoesNotContain<JianJiXingShi>(mainPoolCards);
        AssertDoesNotContain<YanQiChanShen>(mainPoolCards);
        AssertDoesNotContain<YanDunFanJi>(mainPoolCards);
        AssertDoesNotContain<YiLiYuJianTaZhiShang>(mainPoolCards);
        AssertDoesNotContain<BuMie>(mainPoolCards);
        AssertDoesNotContain<LongXi>(mainPoolCards);
        AssertDoesNotContain<TongQiao>(mainPoolCards);
        AssertDoesNotContain<RuTao>(mainPoolCards);

        AssertContains<TiaoXi>(mainPoolCards);
    }

    [Fact]
    public async Task Ritsu_content_registration_keeps_centralized_legacy_entry_and_pool_shape()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var registrationSource = File.ReadAllText(RepoFile("MeiLinModCode", "Migration", "MeiLinRitsuContentRegistration.cs"));
        var poolAttributeSource = File.ReadAllText(RepoFile("MeiLinModCode", "Migration", "PoolAttribute.cs"));
        var entrySource = File.ReadAllText(RepoFile("MeiLinModCode", "Migration", "MeiLinRitsuMigration.cs"));

        Assert.Contains("MeiLinRitsuContentRegistration.Register(assembly);", entrySource);
        Assert.Contains("ModContentRegistry.For(MainFile.ModId)", registrationSource);
        Assert.Contains("registry.RegisterCharacter<Character.MeiLinMod>();", registrationSource);
        Assert.Contains("ApplyLegacyPublicEntry(registry, typeof(Character.MeiLinMod));", registrationSource);

        Assert.Contains("typeof(MeiLinModCard).IsAssignableFrom(type)", registrationSource);
        Assert.Contains("registry.RegisterCard(poolType, type, LegacyPublicEntry(type));", registrationSource);
        Assert.Contains("typeof(MeiLinModRelic).IsAssignableFrom(type)", registrationSource);
        Assert.Contains("registry.RegisterRelic(poolType, type, LegacyPublicEntry(type));", registrationSource);
        Assert.Contains("typeof(MeiLinModPotion).IsAssignableFrom(type)", registrationSource);
        Assert.Contains("registry.RegisterPotion(poolType, type, LegacyPublicEntry(type));", registrationSource);
        Assert.Contains("typeof(MeiLinModPower).IsAssignableFrom(type)", registrationSource);
        Assert.Contains("ApplyLegacyPublicEntry(registry, type);", registrationSource);
        Assert.Contains("registry.RegisterPower(type);", registrationSource);

        Assert.Contains("type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(MeiLinModCardPool)", registrationSource);
        Assert.Contains("type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(MeiLinModRelicPool)", registrationSource);
        Assert.Contains("type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(MeiLinModPotionPool)", registrationSource);
        Assert.Contains("ModelPublicEntryOptions.FromFullPublicEntry($\"MEILINMOD_{stem}\")", registrationSource);
        Assert.Contains("\"ApplyFixedPublicEntryForModel\"", registrationSource);

        Assert.Contains("internal sealed class PoolAttribute(Type poolType) : Attribute", poolAttributeSource);
        Assert.Contains("public Type PoolType { get; } = poolType;", poolAttributeSource);
        Assert.DoesNotContain("Alchyr.Sts2.BaseLib", registrationSource + poolAttributeSource);
        Assert.DoesNotContain("BaseLib", registrationSource + poolAttributeSource);
    }

    private static void AssertEntry(string expected, string actual)
    {
        Assert.True(MeiLinTarget.EntryEquals(actual, expected), $"Expected entry {expected}, got {actual}.");
    }

    private static void AssertLegacyEntry(Type type, AbstractModel model)
    {
        var expected = $"MEILINMOD_{ModContentRegistry.NormalizePublicStem(type.Name)}";
        AssertEntry(expected, model.Id.Entry);
    }

    private static void AssertContains<TCard>(IEnumerable<CardModel> cards)
        where TCard : CardModel
    {
        var expected = ModelDb.Card<TCard>().Id;
        Assert.Contains(cards, card => card.Id == expected);
    }

    private static void AssertDoesNotContain<TCard>(IEnumerable<CardModel> cards)
        where TCard : CardModel
    {
        var expected = ModelDb.Card<TCard>().Id;
        Assert.DoesNotContain(cards, card => card.Id == expected);
    }

    private static CardModel[] GetAllCards(CardPoolModel pool)
    {
        var allCardsProp = pool.GetType().GetProperty("AllCards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (allCardsProp?.GetValue(pool) is IEnumerable<CardModel> allCards)
            return allCards.ToArray();

        throw new InvalidOperationException($"Card pool {pool.GetType().FullName} does not expose AllCards.");
    }

    private static IEnumerable<Type> GetConcreteModTypes<TBase>()
    {
        return typeof(TBase)
            .Assembly
            .GetTypes()
            .Where(type => typeof(TBase).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false })
            .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    private static Type GetDeclaredPoolType(Type type)
    {
        var poolAttribute = type
            .GetCustomAttributes(inherit: true)
            .FirstOrDefault(attribute => attribute.GetType().Name == "PoolAttribute");
        var poolType = poolAttribute?.GetType().GetProperty("PoolType")?.GetValue(poolAttribute) as Type;

        return poolType ?? typeof(MeiLinModCardPool);
    }

    private static AbstractModel GetModelDbModel(string methodName, Type modelType)
    {
        var method = typeof(ModelDb)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == methodName &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 0);

        return (AbstractModel)(method.MakeGenericMethod(modelType).Invoke(null, null)
                               ?? throw new InvalidOperationException($"ModelDb.{methodName} returned null for {modelType.FullName}."));
    }

    private static string RepoFile(params string[] segments)
    {
        return Path.Combine(RepositoryRoot(), Path.Combine(segments));
    }

    private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        var testDirectory = Path.GetDirectoryName(sourcePath)
                            ?? throw new InvalidOperationException("CallerFilePath did not provide a source directory.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }
}
