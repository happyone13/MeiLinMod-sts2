using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Patches;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class OptionalCardVisualPatchTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-optional-card-visual-patches");
    }

    [Fact]
    public async Task Optional_card_visual_patches_stay_non_critical_with_stable_descriptions()
    {
        await InitializeBattle();

        Type[] cardVisualPatchTypes =
        [
            typeof(CardCustomAncientFrameEnterTreePatch),
            typeof(CardCustomAncientFrameFreedToPoolPatch),
            typeof(CardCustomAncientFrameReadyPatch),
            typeof(CardCustomAncientFrameReloadPatch),
            typeof(CardCustomAncientFrameUpdateVisualsPatch),
            typeof(CardSpinePortraitEnterTreePatch),
            typeof(CardSpinePortraitReloadPatch),
            typeof(CardSpinePortraitUpdateVisualsPatch)
        ];

        foreach (var patchType in cardVisualPatchTypes)
        {
            Assert.False(ReadStatic<bool>(patchType, nameof(IPatchMethod.IsCritical)));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.PatchId))));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.Description))));
            Assert.NotEmpty(InvokeStatic<ModPatchTarget[]>(patchType, nameof(IPatchMethod.GetTargets)));
        }
    }

    [Fact]
    public async Task Dynamic_card_portrait_models_keep_existing_scenes_and_custom_frame_flags()
    {
        await InitializeBattle();

        var allCards = GetConcreteCardModels()
            .OrderBy(card => card.GetType().Name, StringComparer.Ordinal)
            .ToArray();
        var dynamicCards = allCards
            .Where(card => card.UsesDynamicChaosFrame || !string.IsNullOrWhiteSpace(card.CustomSpinePortraitScenePath))
            .OrderBy(card => card.GetType().Name, StringComparer.Ordinal)
            .ToArray();

        Assert.All(allCards, card =>
        {
            Assert.True(card.UseCustomAncientFrame, $"{card.GetType().Name} must use the unified full-frame presentation.");
            Assert.Equal(SpinePortraitSlot.Ancient, card.CustomSpinePortraitSlot);
            AssertResourcePathExists(card.CustomAncientBorderMaterialPath);
            AssertResourcePathExists(card.CustomAncientBannerMaterialPath);
        });

        Assert.Equal(
            [
                "AttackDefenseUnity",
                "FireDragonGem",
                "HuoLongJingTian",
                "ShengLongJiao",
                "XiangzuSpiritCard"
            ],
            dynamicCards.Select(card => card.GetType().Name).ToArray());

        foreach (var card in dynamicCards)
        {
            Assert.True(card.UsesDynamicChaosFrame, $"{card.GetType().Name} must opt into the dynamic chaos frame.");
            Assert.True(card.UseCustomAncientFrame, $"{card.GetType().Name} must opt into the custom ancient frame.");
            Assert.Equal(SpinePortraitSlot.Ancient, card.CustomSpinePortraitSlot);
            Assert.False(string.IsNullOrWhiteSpace(card.CustomAncientBorderMaterialPath));
            Assert.False(string.IsNullOrWhiteSpace(card.CustomAncientBannerMaterialPath));
            AssertResourcePathExists(card.CustomSpinePortraitScenePath);
            AssertResourcePathExists(card.CustomAncientBorderMaterialPath);
            AssertResourcePathExists(card.CustomAncientBannerMaterialPath);
        }
    }

    [Fact]
    public async Task Custom_frame_source_keeps_cost_and_type_overlays_visible_on_enlarged_cards()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Patches", "CardCustomAncientFramePatch.cs"));

        Assert.Contains("EnsureTemplateOverlay(cardNode, CostLineNodeName", source);
        Assert.Contains("EnsureTemplateOverlay(cardNode, CostTextNodeName", source);
        Assert.Contains("EnsureTemplateOverlay(cardNode, CostTextFallbackNodeName", source);
        Assert.Contains("\"CostTextAtlasPreview\"", source);
        Assert.Contains("GetCostAtlasVariant(energyLabelControl)", source);
        Assert.Contains("card_green_0.png", source);
        Assert.Contains("card_red_0.png", source);
        Assert.Contains("energy_line_up.png", source);
        Assert.Contains("energy_line_down.png", source);
        Assert.Contains("BringCostOverlayToFront(cardNode);", source);
        Assert.Contains("EnsureCostOverlayRefresh(cardNode);", source);
        Assert.Contains("return ((Control)cardNode).GetGlobalTransform().Scale.Y > 1.1f;", source);

        Assert.Contains("EnsureTemplateOverlay(cardNode, CategoryTextNodeName", source);
        Assert.Contains("SetOverlayText(control, typeText", source);
        Assert.Contains("typeLabel.Hide();", source);
        Assert.Contains("ancientPortrait.Texture = cardNode!.Model?.Portrait;", source);
        Assert.Contains("ancientPortrait.StretchMode = TextureRect.StretchModeEnum.Scale;", source);
        Assert.Contains("Node overlayParent = GetOverlayParent(cardNode);", source);
        Assert.Contains("Control? body = cardNode.Body;", source);
        Assert.Contains("DestroyNodeImmediately(parent.GetNodeOrNull<Node>(nodeName));", source);
        Assert.Contains("banner.Material = null;", source);
        Assert.Contains("ApplyTextureRect(ancientTextBg, GetAncientTextBgPath(cardModel.Type), frameMaterial, show: true);", source);
        Assert.Contains("RemoveNode(cardNode, DescriptionMaskNodeName);", source);
        Assert.Contains("GetEgoBadgePath(cardModel.Rarity)", source);
        Assert.Contains("CardRarity.Uncommon => \"card_ego_narcissism.png\"", source);
        Assert.Contains("CardRarity.Rare => \"card_ego_instinct.png\"", source);
        Assert.Contains("CardRarity.Ancient => \"card_ego_all.png\"", source);
        Assert.Contains("EgoBadge2NodeName", source);
        Assert.Contains("deco_card_copy.png", source);
        Assert.Contains("control.Visible = cardModel.Rarity == CardRarity.Ancient;", source);
        Assert.Contains("CardTitleLayout = new(-77.0f, -213.0f, 209.0f, 58.0f);", source);
        Assert.Contains("CategoryIconLayout = new(-69.0f, -160.0f, 28.0f, 28.0f);", source);
        Assert.Contains("CategoryTextLayout = new(-45.0f, -163.0f, 76.0f, 32.0f);", source);
        Assert.Contains("ancientBorder?.Hide();", source);
        Assert.Contains("ancientHighlight?.Hide();", source);

        var template = File.ReadAllText(RepoFile("MeiLinMod", "scenes", "cards", "chaos_card_effects_frame_template.tscn"));
        Assert.Contains("[node name=\"CategoryIcon\" type=\"TextureRect\" parent=\"CardContainer\"]", template);
        Assert.Contains("offset_left = -69.0", template);
        Assert.Contains("offset_right = -41.0", template);
        Assert.Contains("offset_bottom = -132.0", template);
        Assert.Contains("expand_mode = 1", template);

        int titleStart = template.IndexOf("[node name=\"CardTitle\"", StringComparison.Ordinal);
        int categoryIconStart = template.IndexOf("[node name=\"CategoryIcon\"", titleStart, StringComparison.Ordinal);
        Assert.True(titleStart >= 0 && categoryIconStart > titleStart);
        string titleSection = template[titleStart..categoryIconStart];
        Assert.Contains("offset_left = -77.0", titleSection);
        Assert.Contains("offset_top = -213.0", titleSection);
        Assert.DoesNotContain("horizontal_alignment = 1", titleSection);

        var costFont = File.ReadAllText(RepoFile("MeiLinMod", "images", "cards", "card_effects", "card_normal.fnt"));
        Assert.Contains("char id=49   x=158   y=4     width=78    height=87", costFont);
        Assert.Contains("yoffset=4", costFont);
    }

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }

    private static IEnumerable<MeiLinModCard> GetConcreteCardModels()
    {
        foreach (var type in typeof(MeiLinModCard).Assembly.GetTypes()
                     .Where(type => typeof(MeiLinModCard).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false })
                     .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            yield return (MeiLinModCard)GetModelDbModel("Card", type);
        }
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

    private static void AssertResourcePathExists(string? resourcePath)
    {
        Assert.False(string.IsNullOrWhiteSpace(resourcePath));
        Assert.StartsWith("res://", resourcePath, StringComparison.Ordinal);
        Assert.True(
            File.Exists(RepoFile(resourcePath!["res://".Length..].Split('/'))),
            $"Resource path points to a missing file: {resourcePath}");
    }

    private static T ReadStatic<T>(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(null));
    }

    private static T InvokeStatic<T>(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<T>(method.Invoke(null, null));
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
