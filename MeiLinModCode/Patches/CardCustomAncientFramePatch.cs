using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MeiLinMod.MeiLinModCode.Cards;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public static class CardCustomAncientFramePatch
{
    private const string ChaosFrameBasePath = "res://MeiLinMod/images/cards/chaos_frame/";
    private const string ChaosEffectsBasePath = "res://MeiLinMod/images/cards/card_effects/";
    private const string ChaosEffectsTemplatePath =
        "res://MeiLinMod/scenes/cards/chaos_card_effects_frame_template.tscn";
    private const string TemplateCardContainerPath = "CardContainer";
    private const string AncientBorderPath =
        ChaosFrameBasePath + "ancient_card_border.tres";
    private const string AncientHighlightPath =
        ChaosFrameBasePath + "card_highlight_ancient.tres";
    private const string AncientBannerPath =
        ChaosFrameBasePath + "ancient_banner.tres";
    private const string RarityBaseNodeName = "MeiLinChaosRarityBase";
    private const string RaritySubNodeName = "MeiLinChaosRaritySub";
    private const string EgoBadgeNodeName = "MeiLinChaosEgoBadge";
    private const string EgoBadge2NodeName = "MeiLinChaosEgoBadge2";
    private const string FrameSparkNodeName = "MeiLinChaosFrameSpark";
    private const string CategoryIconNodeName = "MeiLinChaosCategoryIcon";
    private const string CategoryTextNodeName = "MeiLinChaosCategoryText";
    private const string CostLineNodeName = "MeiLinChaosCostLine";
    private const string CostTextNodeName = "MeiLinChaosCostText";
    private const string CostTextFallbackNodeName = "MeiLinChaosCostTextFallback";
    private const string CostOverlayRefreshNodeName = "MeiLinChaosCostOverlayRefresh";
    private const string UpgradeIconNodeName = "MeiLinChaosUpgradeIcon";
    private const string DescriptionMaskNodeName = "MeiLinChaosDescriptionMask";
    private static readonly NodeLayout TitleRibbonLayout = new(-146.0f, -214.0f, 292.0f, 82.0f);
    private static readonly NodeLayout CardTitleLayout = new(-77.0f, -213.0f, 209.0f, 58.0f);
    private static readonly NodeLayout CostLineLayout = new(-145.0f, -200.0f, 68.0f, 115.0f);
    private static readonly NodeLayout CostTextLayout = new(-138.0f, -235.0f, 55.0f, 90.0f);
    private static readonly NodeLayout CategoryIconLayout = new(-69.0f, -160.0f, 28.0f, 28.0f);
    private static readonly NodeLayout CategoryTextLayout = new(-45.0f, -163.0f, 76.0f, 32.0f);
    private static readonly NodeLayout DescriptionTextLayout = new(-142.0f, 40.0f, 278.0f, 161.0f);
    private static readonly NodeLayout DescriptionMaskLayout = new(-153.0f, -63.0f, 298.0f, 271.0f);
    private static readonly NodeLayout EgoBadgeLayout = new(-202.0f, -216.0f, 96.0f, 427.0f);
    private static readonly NodeLayout EgoBadge2Layout = new(96.0f, -215.0f, 96.0f, 427.0f, Visible: false);
    private static readonly NodeLayout RarityBaseLayout = new(-174.0f, -194.0f, 35.0f, 78.0f);
    private static readonly NodeLayout RaritySubLayout = new(120.0f, -199.0f, 56.0f, 90.0f);
    private static readonly NodeLayout FrameSparkLayout = new(-91.0f, -83.0f, 157.0f, 218.0f);
    private static readonly NodeLayout UpgradeIconLayout = new(-131.0f, -138.0f, 32.0f, 32.0f, Visible: false);
    private static readonly Dictionary<char, Rect2> NormalDigitRegions = new()
    {
        ['0'] = new Rect2(79.0f, 4.0f, 78.0f, 87.0f),
        ['1'] = new Rect2(158.0f, 4.0f, 78.0f, 87.0f),
        ['2'] = new Rect2(237.0f, 4.0f, 78.0f, 87.0f),
        ['3'] = new Rect2(316.0f, 4.0f, 78.0f, 87.0f),
        ['4'] = new Rect2(395.0f, 4.0f, 78.0f, 87.0f),
        ['5'] = new Rect2(0.0f, 96.0f, 78.0f, 87.0f),
        ['6'] = new Rect2(79.0f, 96.0f, 78.0f, 87.0f),
        ['7'] = new Rect2(158.0f, 96.0f, 78.0f, 87.0f),
        ['8'] = new Rect2(237.0f, 96.0f, 78.0f, 87.0f),
        ['9'] = new Rect2(316.0f, 96.0f, 78.0f, 87.0f),
        ['X'] = new Rect2(395.0f, 96.0f, 74.0f, 83.0f)
    };
    private static readonly Dictionary<char, Rect2> GreenDigitRegions = new()
    {
        ['0'] = new Rect2(0.0f, 4.0f, 78.0f, 87.0f),
        ['1'] = new Rect2(79.0f, 4.0f, 78.0f, 87.0f),
        ['2'] = new Rect2(158.0f, 4.0f, 78.0f, 87.0f),
        ['3'] = new Rect2(237.0f, 4.0f, 78.0f, 87.0f),
        ['4'] = new Rect2(316.0f, 4.0f, 78.0f, 87.0f),
        ['5'] = new Rect2(395.0f, 4.0f, 78.0f, 87.0f),
        ['6'] = new Rect2(0.0f, 96.0f, 78.0f, 87.0f),
        ['7'] = new Rect2(79.0f, 96.0f, 78.0f, 87.0f),
        ['8'] = new Rect2(158.0f, 96.0f, 78.0f, 87.0f),
        ['9'] = new Rect2(237.0f, 96.0f, 78.0f, 87.0f)
    };
    private static readonly Dictionary<char, Rect2> RedDigitRegions = new()
    {
        ['0'] = new Rect2(0.0f, 4.0f, 78.0f, 87.0f),
        ['1'] = new Rect2(79.0f, 4.0f, 78.0f, 87.0f),
        ['2'] = new Rect2(158.0f, 4.0f, 78.0f, 87.0f),
        ['3'] = new Rect2(237.0f, 4.0f, 78.0f, 87.0f),
        ['4'] = new Rect2(316.0f, 4.0f, 78.0f, 87.0f),
        ['5'] = new Rect2(395.0f, 4.0f, 78.0f, 87.0f),
        ['6'] = new Rect2(0.0f, 96.0f, 78.0f, 87.0f),
        ['7'] = new Rect2(79.0f, 96.0f, 78.0f, 87.0f),
        ['8'] = new Rect2(158.0f, 96.0f, 78.0f, 87.0f),
        ['9'] = new Rect2(237.0f, 96.0f, 78.0f, 87.0f)
    };

    private static readonly FieldInfo? FrameField =
        typeof(NCard).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientBorderField =
        typeof(NCard).GetField("_ancientBorder", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientBannerField =
        typeof(NCard).GetField("_ancientBanner", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientTextBgField =
        typeof(NCard).GetField("_ancientTextBg", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? AncientHighlightField =
        typeof(NCard).GetField("_ancientHighlight", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? PortraitBorderField =
        typeof(NCard).GetField("_portraitBorder", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? BannerField =
        typeof(NCard).GetField("_banner", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? EnergyIconField =
        typeof(NCard).GetField("_energyIcon", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? EnergyLabelField =
        typeof(NCard).GetField("_energyLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TitleLabelField =
        typeof(NCard).GetField("_titleLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TypeLabelField =
        typeof(NCard).GetField("_typeLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TypePlaqueField =
        typeof(NCard).GetField("_typePlaque", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? DescriptionLabelField =
        typeof(NCard).GetField("_descriptionLabel", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly Dictionary<string, Resource?> ResourceCache = new();
    private static readonly HashSet<string> MissingResourceWarnings = new();
    private static readonly Dictionary<CostAtlasVariant, Texture2D?> CostAtlasTextures = new();
    private static readonly ConditionalWeakTable<NCard, OriginalCardVisualState> OriginalStates = new();
    private static Control? _templateRoot;
    private static Control? _templateCardContainer;
    private static Texture2D? _descriptionMaskTexture;

    public static void Apply(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsNodeReady())
            return;

        if (!TryGetCustomFrameCard(cardNode, out MeiLinModCard? cardModel))
        {
            if (HasMeiLinVisualState(cardNode))
                RemoveChaosEffects(cardNode, restoreOriginalState: true);

            return;
        }

        var frame = Get<TextureRect>(FrameField, cardNode!);
        var portrait = Get<TextureRect>(CardSpinePortraitPatch.PortraitField, cardNode!);
        var ancientPortrait = Get<TextureRect>(CardSpinePortraitPatch.AncientPortraitField, cardNode!);
        var portraitBorder = Get<TextureRect>(PortraitBorderField, cardNode!);
        var banner = Get<TextureRect>(BannerField, cardNode!);
        var ancientBorder = Get<TextureRect>(AncientBorderField, cardNode!);
        var ancientTextBg = Get<TextureRect>(AncientTextBgField, cardNode!);
        var ancientBanner = Get<Control>(AncientBannerField, cardNode!);
        var ancientHighlight = Get<TextureRect>(AncientHighlightField, cardNode!);
        bool hasDynamicSpineScene = CardSpinePortraitPatch.TryGetSpineScenePath(cardNode, out _);
        bool shouldDisplayDynamicOverlays = !hasDynamicSpineScene ||
                                            CardSpinePortraitPatch.ShouldDisplayDynamicOverlays(cardNode);
        Material? frameMaterial = LoadResource<Material>(cardModel!.CustomAncientBorderMaterialPath);
        Material? bannerMaterial = LoadResource<Material>(cardModel.CustomAncientBannerMaterialPath);

        if (hasDynamicSpineScene && !CardSpinePortraitPatch.HasActiveSpineOverlay(cardNode))
            CardSpinePortraitPatch.Apply(cardNode);

        if (hasDynamicSpineScene && !CardSpinePortraitPatch.HasActiveSpineOverlay(cardNode))
        {
            RemoveChaosEffects(cardNode, restoreOriginalState: true);
            frame?.Show();
            portrait?.Show();
            portraitBorder?.Show();
            banner?.Show();
            ancientPortrait?.Hide();
            ancientBorder?.Hide();
            ancientTextBg?.Hide();
            ancientBanner?.Hide();
            ancientHighlight?.Hide();
            return;
        }

        if (hasDynamicSpineScene && !shouldDisplayDynamicOverlays)
        {
            ApplyTransitionDynamicPortraitState(cardNode!, cardModel!, frame, portrait, ancientPortrait, portraitBorder, banner,
                ancientBorder, ancientTextBg, ancientBanner, ancientHighlight, frameMaterial, bannerMaterial);
            return;
        }

        CaptureOriginalState(cardNode!);
        frame?.Hide();
        portrait?.Hide();
        portraitBorder?.Hide();
        banner?.Hide();

        if (ancientPortrait != null)
        {
            ancientPortrait.Texture = cardNode!.Model?.Portrait;
            ancientPortrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            ancientPortrait.StretchMode = TextureRect.StretchModeEnum.Scale;
            ancientPortrait.Show();
        }

        ApplyTextureRect(ancientBorder, AncientBorderPath, frameMaterial, show: true);
        ancientBorder?.Hide();
        ApplyTextureRect(ancientTextBg, GetAncientTextBgPath(cardModel.Type), frameMaterial, show: true);
        ApplyTextureRect(ancientHighlight, AncientHighlightPath, material: null, show: true);
        ancientHighlight?.Hide();

        if (ancientBanner != null)
        {
            ancientBanner.Show();
            if (bannerMaterial != null)
                ancientBanner.Material = bannerMaterial;

            if (ancientBanner is TextureRect ancientBannerTexture)
                ApplyTextureRect(ancientBannerTexture, AncientBannerPath, bannerMaterial, show: true);
        }
        ApplyChaosEffects(cardNode!, cardModel);
    }

    private static void ApplyTransitionDynamicPortraitState(
        NCard cardNode,
        MeiLinModCard cardModel,
        TextureRect? frame,
        TextureRect? portrait,
        TextureRect? ancientPortrait,
        TextureRect? portraitBorder,
        TextureRect? banner,
        TextureRect? ancientBorder,
        TextureRect? ancientTextBg,
        Control? ancientBanner,
        TextureRect? ancientHighlight,
        Material? frameMaterial,
        Material? bannerMaterial)
    {
        CaptureOriginalState(cardNode);
        RemoveChaosEffects(cardNode, restoreOriginalState: true);

        frame?.Hide();
        portraitBorder?.Hide();
        ApplyTextureRect(banner, GetRarityTitlePath(cardModel.Rarity), bannerMaterial, show: true);
        ApplyTextureRect(ancientBorder, AncientBorderPath, frameMaterial, show: true);
        ancientBorder?.Hide();
        ancientTextBg?.Hide();
        ancientBanner?.Hide();
        ancientHighlight?.Hide();

        if (Get<TextureRect>(EnergyIconField, cardNode) is { } energyIcon)
            ApplyTextureRect(energyIcon, $"{ChaosEffectsBasePath}energy_line_default.png", material: null, show: true);

        Get<Control>(TitleLabelField, cardNode)?.Show();
        Get<Control>(EnergyLabelField, cardNode)?.Show();
        Get<Control>(DescriptionLabelField, cardNode)?.Show();
        Get<Control>(TypeLabelField, cardNode)?.Show();
        Get<Control>(TypePlaqueField, cardNode)?.Show();

        CardSpinePortraitPatch.ForcePortraitSlot(cardNode, portrait, ancientPortrait, cardModel.CustomSpinePortraitSlot);
        if (portrait != null && cardModel.CustomSpinePortraitSlot == SpinePortraitSlot.Ancient)
            portrait.Hide();
        if (ancientPortrait != null && cardModel.CustomSpinePortraitSlot == SpinePortraitSlot.Ancient)
            ancientPortrait.Show();

        BringCostOverlayToFront(cardNode);
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsNodeReady())
            return;

        if (!TryGetCustomFrameCard(cardNode, out _) && !HasMeiLinVisualState(cardNode))
            return;

        RemoveChaosEffects(cardNode, restoreOriginalState: true);
    }

    public static void CleanupPooledCard(NCard? cardNode)
    {
        if (cardNode == null)
            return;

        if (HasMeiLinVisualState(cardNode))
            RemoveChaosEffects(cardNode, restoreOriginalState: true);

        if (CardSpinePortraitPatch.HasActiveSpineOverlay(cardNode))
            CardSpinePortraitPatch.RemoveSpineOverlay(cardNode);

        OriginalStates.Remove(cardNode);
    }

    private static bool TryGetCustomFrameCard(NCard? cardNode, out MeiLinModCard? cardModel)
    {
        cardModel = null;

        if (cardNode?.Model is not MeiLinModCard model)
            return false;

        if (!model.UseCustomAncientFrame)
            return false;

        cardModel = model;
        return true;
    }

    private static bool HasMeiLinVisualState(NCard cardNode)
    {
        if (OriginalStates.TryGetValue(cardNode, out OriginalCardVisualState? state) && state.HasSnapshot)
            return true;

        return GetOverlayNode(cardNode, RarityBaseNodeName) != null ||
               GetOverlayNode(cardNode, RaritySubNodeName) != null ||
               GetOverlayNode(cardNode, EgoBadgeNodeName) != null ||
               GetOverlayNode(cardNode, EgoBadge2NodeName) != null ||
               GetOverlayNode(cardNode, FrameSparkNodeName) != null ||
               GetOverlayNode(cardNode, CategoryIconNodeName) != null ||
               GetOverlayNode(cardNode, CategoryTextNodeName) != null ||
               GetOverlayNode(cardNode, CostLineNodeName) != null ||
               GetOverlayNode(cardNode, CostTextNodeName) != null ||
               GetOverlayNode(cardNode, CostTextFallbackNodeName) != null ||
               GetOverlayNode(cardNode, UpgradeIconNodeName) != null ||
               GetOverlayNode(cardNode, DescriptionMaskNodeName) != null;
    }

    private static void ApplyTextureRect(TextureRect? textureRect, string texturePath, Material? material, bool show)
    {
        if (textureRect == null)
            return;

        Texture2D? texture = LoadResource<Texture2D>(texturePath);
        if (texture != null)
            textureRect.Texture = texture;

        if (material != null)
            textureRect.Material = material;

        if (show)
            textureRect.Show();
    }

    private static string GetAncientTextBgPath(CardType type)
    {
        string cardType = type switch
        {
            CardType.Skill => "skill",
            CardType.Power => "power",
            _ => "attack"
        };

        return $"{ChaosFrameBasePath}ancient_card_text_bg_{cardType}.tres";
    }

    private static void ApplyChaosEffects(NCard cardNode, CardModel cardModel)
    {
        var banner = Get<TextureRect>(BannerField, cardNode);
        var titleLabel = Get<Control>(TitleLabelField, cardNode);
        var energyIcon = Get<TextureRect>(EnergyIconField, cardNode);
        var descriptionLabel = Get<Control>(DescriptionLabelField, cardNode);
        var energyLabel = Get<Control>(EnergyLabelField, cardNode);
        var typeLabel = Get<Control>(TypeLabelField, cardNode);
        var typePlaque = Get<Control>(TypePlaqueField, cardNode);

        ApplyTemplateLayout(banner, "TitleRibbon", TitleRibbonLayout);
        ApplyTemplateLayout(titleLabel, "CardTitle", CardTitleLayout);
        ApplyTemplateLayout(descriptionLabel, "DescriptionText", DescriptionTextLayout);
        ApplyTemplateLayout(typeLabel, "CategoryText", CategoryTextLayout);
        EnsureControlVisible(banner);
        EnsureControlVisible(titleLabel);
        EnsureControlVisible(descriptionLabel);

        if (banner != null)
            banner.Material = null;
        ApplyTextureRect(banner, GetRarityTitlePath(cardModel.Rarity), material: null, show: true);
        ConfigureCostOverlay(cardNode, cardModel, energyIcon, energyLabel);

        if (Get<Control>(AncientBannerField, cardNode) is { } ancientBanner)
            ancientBanner.Hide();

        if (typePlaque != null)
            typePlaque.Visible = false;
        string typeText = ResolveTypeText(cardModel, typeLabel);
        if (typeLabel != null)
            typeLabel.Hide();

        EnsureTemplateOverlay(cardNode, CategoryTextNodeName, "CategoryText", () => CreateLabelOverlay(CategoryTextLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "CategoryText", CategoryTextLayout);
            SetOverlayText(control, typeText, !string.IsNullOrWhiteSpace(typeText));
            BringToFront(control);
        });

        BringToFront(banner);
        BringToFront(titleLabel);
        BringToFront(descriptionLabel);

        RemoveNode(cardNode, DescriptionMaskNodeName);

        EnsureTemplateOverlay(cardNode, EgoBadgeNodeName, "EgoBadge", () => CreateTextureOverlay(EgoBadgeLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "EgoBadge", EgoBadgeLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetEgoBadgePath(cardModel.Rarity), material: null, show: true);
        });

        EnsureTemplateOverlay(cardNode, EgoBadge2NodeName, "EgoBadge2", () => CreateTextureOverlay(EgoBadge2Layout), configure: control =>
        {
            ApplyTemplateLayout(control, "EgoBadge2", EgoBadge2Layout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, $"{ChaosEffectsBasePath}deco_card_copy.png", material: null, show: false);
            control.Visible = cardModel.Rarity == CardRarity.Ancient;
        });

        EnsureTemplateOverlay(cardNode, RarityBaseNodeName, "RarityBase", () => CreateTextureOverlay(RarityBaseLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "RarityBase", RarityBaseLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetRarityBasePath(cardModel.Rarity), material: null, show: true);
        });

        EnsureTemplateOverlay(cardNode, RaritySubNodeName, "RaritySub", () => CreateTextureOverlay(RaritySubLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "RaritySub", RaritySubLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetRaritySubPath(cardModel.Rarity), material: null, show: true);
        });

        EnsureTemplateOverlay(cardNode, FrameSparkNodeName, "FrameSpark", () => CreateTextureOverlay(FrameSparkLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "FrameSpark", FrameSparkLayout);
            BringToFront(control);
        });

        EnsureTemplateOverlay(cardNode, CategoryIconNodeName, "CategoryIcon", () => CreateTextureOverlay(CategoryIconLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "CategoryIcon", CategoryIconLayout);
            SetOverlayVisibility(control, !string.IsNullOrWhiteSpace(typeText));
            EnsureDrawBefore(control, typeLabel);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetCategoryIconPath(cardModel.Type), material: null, show: true);
            BringToFront(control);
        });

        EnsureTemplateOverlay(cardNode, UpgradeIconNodeName, "UpgradeIcon", () => CreateTextureOverlay(UpgradeIconLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "UpgradeIcon", UpgradeIconLayout with { Visible = cardModel.IsUpgraded });
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, $"{ChaosEffectsBasePath}icon_card_battle_expand_default.png", material: null, show: cardModel.IsUpgraded);
        });

        BringCostOverlayToFront(cardNode);
        EnsureCostOverlayRefresh(cardNode);
    }

    private static void ConfigureCostOverlay(
        NCard cardNode,
        CardModel cardModel,
        TextureRect? energyIcon,
        Control? energyLabelControl)
    {
        CostAtlasVariant costVariant = GetCostAtlasVariant(energyLabelControl);

        EnsureTemplateOverlay(cardNode, CostLineNodeName, "CostLine", () => CreateTextureOverlay(CostLineLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "CostLine", CostLineLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetEnergyLinePath(costVariant), material: null, show: true);
            BringToFront(control);
        });

        if (GetOverlayNode(cardNode, CostTextNodeName) is Label)
            RemoveNode(cardNode, CostTextNodeName);

        EnsureTemplateOverlay(cardNode, CostTextNodeName, "CostTextAtlasPreview", () => new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        }, configure: control =>
        {
            ApplyTemplateLayout(control, "CostTextAtlasPreview", CostTextLayout);
            BringToFront(control);
        });

        if (GetOverlayNode(cardNode, CostTextFallbackNodeName) is { } existingFallback && existingFallback is not Label)
            RemoveNode(cardNode, CostTextFallbackNodeName);

        EnsureTemplateOverlay(cardNode, CostTextFallbackNodeName, "CostText", () => CreateLabelOverlay(CostTextLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "CostText", CostTextLayout);
            BringToFront(control);
        });

        if (energyIcon != null)
            energyIcon.Hide();

        if (energyLabelControl != null)
            energyLabelControl.Hide();

        string displayText = ResolveCostText(cardModel, energyLabelControl);
        var preview = GetOverlayNode(cardNode, CostTextNodeName);
        var fallbackLabel = GetOverlayNode(cardNode, CostTextFallbackNodeName) as Label;
        if (preview == null || fallbackLabel == null)
            return;

        if (!string.IsNullOrWhiteSpace(displayText) && IsAtlasCostText(displayText))
        {
            // X exists only in the normal BMFont and intentionally does not change color.
            CostAtlasVariant renderVariant = displayText.Contains('X') ? CostAtlasVariant.Normal : costVariant;
            if (renderVariant != CostAtlasVariant.Normal && RenderCostDigits(preview, displayText, renderVariant))
            {
                preview.Show();
                fallbackLabel.Hide();
                return;
            }
        }

        ClearCostDigits(preview);
        preview.Hide();

        if (string.IsNullOrWhiteSpace(displayText))
        {
            fallbackLabel.Hide();
            return;
        }

        bool isXCost = displayText.Contains('X');
        fallbackLabel.Text = GetFallbackCostText(displayText);
        SyncFallbackCostTheme(
            fallbackLabel,
            isXCost ? GetTemplateNode<Label>("CostText") : energyLabelControl as Label);
        fallbackLabel.Show();
    }

    private static void RemoveChaosEffects(NCard? cardNode, bool restoreOriginalState)
    {
        if (cardNode == null)
            return;

        RemoveNode(cardNode, RarityBaseNodeName);
        RemoveNode(cardNode, RaritySubNodeName);
        RemoveNode(cardNode, EgoBadgeNodeName);
        RemoveNode(cardNode, EgoBadge2NodeName);
        RemoveNode(cardNode, FrameSparkNodeName);
        RemoveNode(cardNode, CategoryIconNodeName);
        RemoveNode(cardNode, CategoryTextNodeName);
        RemoveNode(cardNode, CostLineNodeName);
        RemoveNode(cardNode, CostTextNodeName);
        RemoveNode(cardNode, CostTextFallbackNodeName);
        RemoveNode(cardNode, CostOverlayRefreshNodeName);
        RemoveNode(cardNode, UpgradeIconNodeName);
        RemoveNode(cardNode, DescriptionMaskNodeName);
        if (restoreOriginalState)
            RestoreOriginalState(cardNode);
    }

    private static void EnsureTemplateOverlay(
        NCard cardNode,
        string runtimeNodeName,
        string templateNodeName,
        Func<Control?> fallbackCreate,
        Action<Control>? configure = null)
    {
        Node overlayParent = GetOverlayParent(cardNode);
        Control? control = GetOverlayNode(cardNode, runtimeNodeName);
        if (control == null)
        {
            control = DuplicateTemplateNode(templateNodeName) ?? fallbackCreate();
            if (control == null)
                return;

            control.Name = runtimeNodeName;
            overlayParent.AddChild(control);
        }
        else if (control.GetParent() != overlayParent)
        {
            control.GetParent()?.RemoveChild(control);
            overlayParent.AddChild(control);
        }

        configure?.Invoke(control);
    }

    private static void ApplyTemplateLayout(Control? target, string templateNodeName, NodeLayout fallbackLayout)
    {
        if (target == null)
            return;

        if (GetTemplateNode<Control>(templateNodeName) is { } template)
        {
            ApplyLayout(target, template);
            return;
        }

        ApplyLayout(target, fallbackLayout);
    }

    private static void ApplyLayout(Control? target, Control template)
    {
        if (target == null)
            return;

        target.Position = template.Position;
        target.Size = template.Size;
        target.AnchorLeft = template.AnchorLeft;
        target.AnchorTop = template.AnchorTop;
        target.AnchorRight = template.AnchorRight;
        target.AnchorBottom = template.AnchorBottom;
        target.OffsetLeft = template.OffsetLeft;
        target.OffsetTop = template.OffsetTop;
        target.OffsetRight = template.OffsetRight;
        target.OffsetBottom = template.OffsetBottom;
        target.PivotOffset = template.PivotOffset;
        target.Rotation = template.Rotation;
        target.Scale = template.Scale;
        target.CustomMinimumSize = template.CustomMinimumSize;
        target.Visible = template.Visible;

        if (target is TextureRect targetTextureRect && template is TextureRect templateTextureRect)
        {
            targetTextureRect.StretchMode = templateTextureRect.StretchMode;
            targetTextureRect.ExpandMode = templateTextureRect.ExpandMode;
            targetTextureRect.Modulate = templateTextureRect.Modulate;
        }

        if (target is Label targetLabel && template is Label templateLabel)
        {
            targetLabel.HorizontalAlignment = templateLabel.HorizontalAlignment;
            targetLabel.VerticalAlignment = templateLabel.VerticalAlignment;
            targetLabel.AutowrapMode = templateLabel.AutowrapMode;
            targetLabel.ClipText = templateLabel.ClipText;
            targetLabel.Uppercase = templateLabel.Uppercase;
        }

        if (target is RichTextLabel targetRichText && template is RichTextLabel templateRichText)
        {
            targetRichText.ScrollActive = templateRichText.ScrollActive;
            targetRichText.FitContent = templateRichText.FitContent;
            targetRichText.AutowrapMode = templateRichText.AutowrapMode;
        }
    }

    private static void ApplyLayout(Control? target, NodeLayout layout)
    {
        if (target == null)
            return;

        target.Position = layout.Position;
        target.Size = layout.Size;
        target.Visible = layout.Visible;
    }

    private static Control? DuplicateTemplateNode(string templateNodeName)
    {
        return GetTemplateNode<Control>(templateNodeName)?.Duplicate() as Control;
    }

    private static T? GetTemplateNode<T>(string nodePath) where T : Node
    {
        return GetTemplateCardContainer()?.GetNodeOrNull<T>(nodePath);
    }

    private static Control? GetTemplateCardContainer()
    {
        if (_templateRoot != null &&
            GodotObject.IsInstanceValid(_templateRoot) &&
            _templateCardContainer != null &&
            GodotObject.IsInstanceValid(_templateCardContainer))
        {
            return _templateCardContainer;
        }

        PackedScene? scene = LoadResource<PackedScene>(ChaosEffectsTemplatePath);
        if (scene == null)
            return null;

        if (scene.Instantiate<Control>() is not { } root)
            return null;

        _templateRoot = root;
        _templateCardContainer = root.GetNodeOrNull<Control>(TemplateCardContainerPath);
        if (_templateCardContainer == null)
        {
            root.QueueFree();
            _templateRoot = null;
        }

        return _templateCardContainer;
    }

    private static void EnsureDrawBefore(Control node, Control? reference)
    {
        if (reference?.GetParent() != node.GetParent() || node.GetParent() == null)
            return;

        int referenceIndex = reference.GetIndex();
        if (node.GetIndex() > referenceIndex)
            node.GetParent().MoveChild(node, referenceIndex);
    }

    private static void RemoveNode(Node parent, string nodeName)
    {
        DestroyNodeImmediately(parent.GetNodeOrNull<Node>(nodeName));

        if (parent is NCard cardNode)
        {
            Node overlayParent = GetOverlayParent(cardNode);
            if (overlayParent != parent)
                DestroyNodeImmediately(overlayParent.GetNodeOrNull<Node>(nodeName));
        }
    }

    private static Control? GetOverlayNode(Node parent, string nodeName)
    {
        Control? control;
        if (parent is NCard cardNode)
        {
            Node overlayParent = GetOverlayParent(cardNode);
            control = overlayParent.GetNodeOrNull<Control>(nodeName);
            if (control == null && overlayParent != parent)
                control = parent.GetNodeOrNull<Control>(nodeName);
        }
        else
        {
            control = parent.GetNodeOrNull<Control>(nodeName);
        }

        if (control == null)
            return null;

        if (!GodotObject.IsInstanceValid(control) || control.IsQueuedForDeletion())
        {
            DestroyNodeImmediately(control);
            return null;
        }

        return control;
    }

    private static Node GetOverlayParent(NCard cardNode)
    {
        Control? body = cardNode.Body;
        if (body != null && GodotObject.IsInstanceValid(body) && !body.IsQueuedForDeletion())
            return body;

        return cardNode;
    }

    private static void DestroyNodeImmediately(Node? node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return;

        node.GetParent()?.RemoveChild(node);
        node.Free();
    }

    private static void CaptureOriginalState(NCard cardNode)
    {
        var state = OriginalStates.GetOrCreateValue(cardNode);
        if (state.HasSnapshot && ReferenceEquals(state.CapturedModel, cardNode.Model))
            return;

        state.CapturedModel = cardNode.Model;
        state.Banner = CaptureControlSnapshot(Get<Control>(BannerField, cardNode));
        state.Frame = CaptureControlSnapshot(Get<Control>(FrameField, cardNode));
        state.Portrait = CaptureControlSnapshot(Get<Control>(CardSpinePortraitPatch.PortraitField, cardNode));
        state.AncientPortrait = CaptureControlSnapshot(Get<Control>(CardSpinePortraitPatch.AncientPortraitField, cardNode));
        state.PortraitBorder = CaptureControlSnapshot(Get<Control>(PortraitBorderField, cardNode));
        state.AncientBorder = CaptureControlSnapshot(Get<Control>(AncientBorderField, cardNode));
        state.AncientBanner = CaptureControlSnapshot(Get<Control>(AncientBannerField, cardNode));
        state.AncientTextBg = CaptureControlSnapshot(Get<Control>(AncientTextBgField, cardNode));
        state.AncientHighlight = CaptureControlSnapshot(Get<Control>(AncientHighlightField, cardNode));
        state.TitleLabel = CaptureControlSnapshot(Get<Control>(TitleLabelField, cardNode));
        state.EnergyIcon = CaptureControlSnapshot(Get<Control>(EnergyIconField, cardNode));
        state.DescriptionLabel = CaptureControlSnapshot(Get<Control>(DescriptionLabelField, cardNode));
        state.EnergyLabel = CaptureControlSnapshot(Get<Control>(EnergyLabelField, cardNode));
        state.TypeLabel = CaptureControlSnapshot(Get<Control>(TypeLabelField, cardNode));
        state.TypePlaque = CaptureControlSnapshot(Get<Control>(TypePlaqueField, cardNode));
        state.HasSnapshot = true;
    }

    private static void RestoreOriginalState(NCard cardNode)
    {
        if (!OriginalStates.TryGetValue(cardNode, out OriginalCardVisualState? state) || !state.HasSnapshot)
            return;

        bool restoreTextures = ReferenceEquals(state.CapturedModel, cardNode.Model);
        RestoreControlSnapshot(Get<Control>(BannerField, cardNode), state.Banner, restoreTextures);
        RestoreControlSnapshot(Get<Control>(FrameField, cardNode), state.Frame, restoreTextures);
        RestoreControlSnapshot(Get<Control>(CardSpinePortraitPatch.PortraitField, cardNode), state.Portrait, restoreTextures);
        RestoreControlSnapshot(Get<Control>(CardSpinePortraitPatch.AncientPortraitField, cardNode), state.AncientPortrait, restoreTextures);
        RestoreControlSnapshot(Get<Control>(PortraitBorderField, cardNode), state.PortraitBorder, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientBorderField, cardNode), state.AncientBorder, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientBannerField, cardNode), state.AncientBanner, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientTextBgField, cardNode), state.AncientTextBg, restoreTextures);
        RestoreControlSnapshot(Get<Control>(AncientHighlightField, cardNode), state.AncientHighlight, restoreTextures);
        RestoreControlSnapshot(Get<Control>(TitleLabelField, cardNode), state.TitleLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(EnergyIconField, cardNode), state.EnergyIcon, restoreTextures);
        RestoreControlSnapshot(Get<Control>(DescriptionLabelField, cardNode), state.DescriptionLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(EnergyLabelField, cardNode), state.EnergyLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(TypeLabelField, cardNode), state.TypeLabel, restoreTextures);
        RestoreControlSnapshot(Get<Control>(TypePlaqueField, cardNode), state.TypePlaque, restoreTextures);
        OriginalStates.Remove(cardNode);
    }

    private static ControlSnapshot? CaptureControlSnapshot(Control? control)
    {
        if (control == null)
            return null;

        return new ControlSnapshot
        {
            Position = control.Position,
            Size = control.Size,
            AnchorLeft = control.AnchorLeft,
            AnchorTop = control.AnchorTop,
            AnchorRight = control.AnchorRight,
            AnchorBottom = control.AnchorBottom,
            OffsetLeft = control.OffsetLeft,
            OffsetTop = control.OffsetTop,
            OffsetRight = control.OffsetRight,
            OffsetBottom = control.OffsetBottom,
            PivotOffset = control.PivotOffset,
            Rotation = control.Rotation,
            Scale = control.Scale,
            CustomMinimumSize = control.CustomMinimumSize,
            Visible = control.Visible,
            ZIndex = control.ZIndex,
            Modulate = control.Modulate,
            SelfModulate = control.SelfModulate,
            Texture = (control as TextureRect)?.Texture,
            Material = control.Material,
            TextureExpandMode = (control as TextureRect)?.ExpandMode,
            TextureStretchMode = (control as TextureRect)?.StretchMode,
            LabelHorizontalAlignment = (control as Label)?.HorizontalAlignment,
            LabelVerticalAlignment = (control as Label)?.VerticalAlignment,
            LabelAutowrapMode = (control as Label)?.AutowrapMode,
            LabelClipText = (control as Label)?.ClipText,
            LabelUppercase = (control as Label)?.Uppercase,
            RichTextScrollActive = (control as RichTextLabel)?.ScrollActive,
            RichTextFitContent = (control as RichTextLabel)?.FitContent,
            RichTextAutowrapMode = (control as RichTextLabel)?.AutowrapMode
        };
    }

    private static void RestoreControlSnapshot(Control? control, ControlSnapshot? snapshot, bool restoreTexture)
    {
        if (control == null || snapshot == null)
            return;

        control.Position = snapshot.Position;
        control.Size = snapshot.Size;
        control.AnchorLeft = snapshot.AnchorLeft;
        control.AnchorTop = snapshot.AnchorTop;
        control.AnchorRight = snapshot.AnchorRight;
        control.AnchorBottom = snapshot.AnchorBottom;
        control.OffsetLeft = snapshot.OffsetLeft;
        control.OffsetTop = snapshot.OffsetTop;
        control.OffsetRight = snapshot.OffsetRight;
        control.OffsetBottom = snapshot.OffsetBottom;
        control.PivotOffset = snapshot.PivotOffset;
        control.Rotation = snapshot.Rotation;
        control.Scale = snapshot.Scale;
        control.CustomMinimumSize = snapshot.CustomMinimumSize;
        control.Visible = snapshot.Visible;
        control.ZIndex = snapshot.ZIndex;
        control.Modulate = snapshot.Modulate;
        control.SelfModulate = snapshot.SelfModulate;
        control.Material = snapshot.Material;

        if (control is TextureRect textureRect)
        {
            if (restoreTexture)
                textureRect.Texture = snapshot.Texture;
            if (snapshot.TextureExpandMode.HasValue)
                textureRect.ExpandMode = snapshot.TextureExpandMode.Value;
            if (snapshot.TextureStretchMode.HasValue)
                textureRect.StretchMode = snapshot.TextureStretchMode.Value;
        }

        if (control is Label label)
        {
            if (snapshot.LabelHorizontalAlignment.HasValue)
                label.HorizontalAlignment = snapshot.LabelHorizontalAlignment.Value;
            if (snapshot.LabelVerticalAlignment.HasValue)
                label.VerticalAlignment = snapshot.LabelVerticalAlignment.Value;
            if (snapshot.LabelAutowrapMode.HasValue)
                label.AutowrapMode = snapshot.LabelAutowrapMode.Value;
            if (snapshot.LabelClipText.HasValue)
                label.ClipText = snapshot.LabelClipText.Value;
            if (snapshot.LabelUppercase.HasValue)
                label.Uppercase = snapshot.LabelUppercase.Value;
        }

        if (control is RichTextLabel richTextLabel)
        {
            if (snapshot.RichTextScrollActive.HasValue)
                richTextLabel.ScrollActive = snapshot.RichTextScrollActive.Value;
            if (snapshot.RichTextFitContent.HasValue)
                richTextLabel.FitContent = snapshot.RichTextFitContent.Value;
            if (snapshot.RichTextAutowrapMode.HasValue)
                richTextLabel.AutowrapMode = snapshot.RichTextAutowrapMode.Value;
        }
    }

    private static void BringToFront(Node? child)
    {
        if (child?.GetParent() == null)
            return;

        child.GetParent().MoveChild(child, child.GetParent().GetChildCount() - 1);
    }

    private static void BringCostOverlayToFront(NCard cardNode)
    {
        BringToFront(GetOverlayNode(cardNode, CostLineNodeName));
        BringToFront(GetOverlayNode(cardNode, CostTextNodeName));
        BringToFront(GetOverlayNode(cardNode, CostTextFallbackNodeName));
    }

    private static Control? CreateTextureOverlay(NodeLayout layout)
    {
        var textureRect = new TextureRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            StretchMode = TextureRect.StretchModeEnum.Scale
        };
        ApplyLayout(textureRect, layout);
        return textureRect;
    }

    private static Control CreateLabelOverlay(NodeLayout layout)
    {
        var label = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        ApplyLayout(label, layout);
        return label;
    }

    private static string GetControlText(Control? control)
    {
        return control switch
        {
            Label label => label.Text,
            RichTextLabel richTextLabel => richTextLabel.Text,
            _ => string.Empty
        };
    }

    private static string ResolveCostText(CardModel cardModel, Control? energyLabel)
    {
        string labelText = GetControlText(energyLabel);
        if (!string.IsNullOrWhiteSpace(labelText))
            return labelText;

        try
        {
            decimal cost = cardModel.EnergyCost.GetWithModifiers(CostModifiers.All);
            return cost < 0 ? "X" : ((int)cost).ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[MeiLinCardFrame] Failed to resolve cost text for {cardModel.Id}: {ex}");
            return string.Empty;
        }
    }

    private static string GetFallbackCostText(string displayText)
    {
        // card_normal.fnt stores the uppercase X artwork in its legacy lowercase-x slot.
        return displayText.Replace('X', 'x');
    }

    private static bool IsAtlasCostText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (char c in text)
        {
            if (!char.IsDigit(c) && c != 'X')
                return false;
        }

        return true;
    }

    private static void SyncFallbackCostTheme(Label target, Label? source)
    {
        if (source == null)
            return;

        target.AddThemeColorOverride("font_color", source.GetThemeColor("font_color"));
        target.AddThemeColorOverride("font_outline_color", source.GetThemeColor("font_outline_color"));
        target.AddThemeConstantOverride("outline_size", source.GetThemeConstant("outline_size"));
    }

    private static CostAtlasVariant GetCostAtlasVariant(Control? energyLabelControl)
    {
        if (energyLabelControl is not Label label)
            return CostAtlasVariant.Normal;

        Color fontColor = label.GetThemeColor("font_color");
        Color outlineColor = label.GetThemeColor("font_outline_color");
        if (LooksLikeGreen(fontColor) || LooksLikeGreen(outlineColor))
            return CostAtlasVariant.Green;
        if (LooksLikeRed(fontColor) || LooksLikeRed(outlineColor))
            return CostAtlasVariant.Red;

        return CostAtlasVariant.Normal;
    }

    private static bool LooksLikeGreen(Color color)
    {
        return color.G >= 0.6f && color.G >= color.R + 0.08f && color.G >= color.B + 0.08f;
    }

    private static bool LooksLikeRed(Color color)
    {
        return color.R >= 0.6f && color.R >= color.G + 0.15f && color.R >= color.B + 0.15f;
    }

    private static bool RenderCostDigits(Control preview, string text, CostAtlasVariant variant)
    {
        ClearCostDigits(preview);

        Dictionary<char, Rect2> digitRegions = GetDigitRegions(variant);
        Texture2D? texture = LoadCostAtlasTexture(variant);
        if (texture == null)
        {
            preview.Hide();
            return false;
        }

        var visibleDigits = new List<char>(text.Length);
        float totalSourceWidth = 0.0f;
        float maxSourceHeight = 0.0f;

        foreach (char c in text)
        {
            if (!digitRegions.TryGetValue(c, out Rect2 region))
                continue;

            visibleDigits.Add(c);
            totalSourceWidth += region.Size.X;
            maxSourceHeight = MathF.Max(maxSourceHeight, region.Size.Y);
        }

        if (visibleDigits.Count == 0 || totalSourceWidth <= 0.0f || maxSourceHeight <= 0.0f)
        {
            preview.Hide();
            return false;
        }

        float scale = MathF.Min(preview.Size.Y / maxSourceHeight, preview.Size.X / totalSourceWidth);
        if (scale <= 0.0f || float.IsNaN(scale) || float.IsInfinity(scale))
        {
            preview.Hide();
            return false;
        }

        float startX = (preview.Size.X - totalSourceWidth * scale) * 0.5f;
        float startY = (preview.Size.Y - maxSourceHeight * scale) * 0.5f;
        float cursorX = startX;

        for (int i = 0; i < visibleDigits.Count; i++)
        {
            Rect2 region = digitRegions[visibleDigits[i]];
            var rect = new TextureRect
            {
                Name = $"CostDigit{i}",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Texture = new AtlasTexture
                {
                    Atlas = texture,
                    Region = region
                },
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Position = new Vector2(cursorX, startY),
                Size = region.Size * scale
            };
            preview.AddChild(rect);
            cursorX += region.Size.X * scale;
        }

        preview.Show();
        return true;
    }

    private static void ClearCostDigits(Control preview)
    {
        foreach (Node child in preview.GetChildren())
        {
            if (child.Name.ToString().StartsWith("CostDigit", StringComparison.Ordinal))
                DestroyNodeImmediately(child);
        }
    }

    private static Texture2D? LoadCostAtlasTexture(CostAtlasVariant variant)
    {
        if (CostAtlasTextures.TryGetValue(variant, out Texture2D? cached) &&
            cached != null && GodotObject.IsInstanceValid(cached))
        {
            return cached;
        }

        string path = variant switch
        {
            CostAtlasVariant.Green => $"{ChaosEffectsBasePath}card_green_0.png",
            CostAtlasVariant.Red => $"{ChaosEffectsBasePath}card_red_0.png",
            _ => $"{ChaosEffectsBasePath}card_normal_0.png"
        };

        Texture2D? texture = LoadResource<Texture2D>(path);
        CostAtlasTextures[variant] = texture;
        return texture;
    }

    private static Dictionary<char, Rect2> GetDigitRegions(CostAtlasVariant variant)
    {
        return variant switch
        {
            CostAtlasVariant.Green => GreenDigitRegions,
            CostAtlasVariant.Red => RedDigitRegions,
            _ => NormalDigitRegions
        };
    }

    private static string ResolveTypeText(CardModel cardModel, Control? typeLabel)
    {
        string labelText = GetControlText(typeLabel);
        if (!string.IsNullOrWhiteSpace(labelText))
            return labelText;

        return cardModel.Type switch
        {
            CardType.Attack => "攻击",
            CardType.Skill => "技能",
            CardType.Power => "能力",
            CardType.Status => "状态",
            CardType.Curse => "诅咒",
            CardType.Quest => "任务",
            _ => cardModel.Type.ToString()
        };
    }

    private static void SetOverlayText(Control control, string text, bool sourceVisible, Control? source = null)
    {
        SetOverlayVisibility(control, sourceVisible, source);
        bool visible = sourceVisible && !string.IsNullOrWhiteSpace(text);
        if (control is Label label)
            label.Text = text;

        control.Visible = visible;
        if (visible)
            EnsureReadableModulate(control);
    }

    private static Control CreateDescriptionMask(NodeLayout layout)
    {
        var mask = new TextureRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Texture = GetDescriptionMaskTexture()
        };
        ApplyLayout(mask, layout);
        return mask;
    }

    private static Texture2D? GetDescriptionMaskTexture()
    {
        if (_descriptionMaskTexture != null && GodotObject.IsInstanceValid(_descriptionMaskTexture))
            return _descriptionMaskTexture;

        if (GetTemplateNode<TextureRect>("DescriptionMask") is { Texture: Texture2D templateTexture } &&
            GodotObject.IsInstanceValid(templateTexture))
        {
            _descriptionMaskTexture = templateTexture;
            return _descriptionMaskTexture;
        }

        var image = Image.CreateEmpty(4, 256, false, Image.Format.Rgba8);
        for (int y = 0; y < image.GetHeight(); y++)
        {
            float t = y / (float)(image.GetHeight() - 1);
            float alpha = t < 0.62f
                ? Mathf.Lerp(0.0f, 0.32f, t / 0.62f)
                : Mathf.Lerp(0.32f, 0.82f, (t - 0.62f) / 0.38f);

            var color = new Color(0.0f, 0.0f, 0.0f, alpha);
            for (int x = 0; x < image.GetWidth(); x++)
                image.SetPixel(x, y, color);
        }

        _descriptionMaskTexture = ImageTexture.CreateFromImage(image);
        return _descriptionMaskTexture;
    }

    private static void SetOverlayVisibility(Control control, bool sourceVisible, Control? source = null)
    {
        control.Visible = sourceVisible;
        if (source == null)
        {
            if (sourceVisible)
                EnsureReadableModulate(control);
            return;
        }

        control.ZIndex = source.ZIndex;
        control.Modulate = source.Modulate;
        control.SelfModulate = source.SelfModulate;
        if (sourceVisible)
            EnsureReadableModulate(control);
    }

    private static void EnsureControlVisible(Control? control)
    {
        if (control != null)
            control.Visible = true;
    }

    private static void EnsureReadableModulate(Control control)
    {
        if (control.Modulate.A <= 0.01f)
            control.Modulate = new Color(control.Modulate.R, control.Modulate.G, control.Modulate.B, 1.0f);

        if (control.SelfModulate.A <= 0.01f)
            control.SelfModulate = new Color(
                control.SelfModulate.R,
                control.SelfModulate.G,
                control.SelfModulate.B,
                1.0f);
    }

    private static string GetCategoryIconPath(CardType type)
    {
        string file = type switch
        {
            CardType.Attack => "icon_category_card_atk.png",
            CardType.Skill => "icon_category_card_skill.png",
            CardType.Power => "icon_category_card_power.png",
            CardType.Status => "icon_category_card_abnorm.png",
            CardType.Curse => "icon_category_card_curse.png",
            _ => "icon_category_card_potion.png"
        };

        return $"{ChaosEffectsBasePath}{file}";
    }

    private static string GetEgoBadgePath(CardRarity rarity)
    {
        string file = rarity switch
        {
            CardRarity.Basic => "card_ego_basic.png",
            CardRarity.Common => "card_ego_basic.png",
            CardRarity.Uncommon => "card_ego_narcissism.png",
            CardRarity.Rare => "card_ego_instinct.png",
            CardRarity.Ancient => "card_ego_all.png",
            CardRarity.Token => "card_ego_creed.png",
            _ => "card_ego_basic.png"
        };

        return $"{ChaosEffectsBasePath}{file}";
    }

    private static string GetEnergyLinePath(CostAtlasVariant variant)
    {
        string file = variant switch
        {
            CostAtlasVariant.Red => "energy_line_up.png",
            CostAtlasVariant.Green => "energy_line_down.png",
            _ => "energy_line_default.png"
        };

        return $"{ChaosEffectsBasePath}{file}";
    }

    private static string GetRarityBasePath(CardRarity rarity)
    {
        string suffix = rarity switch
        {
            CardRarity.Uncommon => "rare",
            CardRarity.Rare => "legend",
            CardRarity.Ancient => "unique",
            _ => "common"
        };

        return $"{ChaosEffectsBasePath}card_rarity_{suffix}.png";
    }

    private static string GetRaritySubPath(CardRarity rarity)
    {
        string suffix = rarity switch
        {
            CardRarity.Uncommon => "rare",
            CardRarity.Rare => "legend",
            CardRarity.Ancient => "unique",
            _ => "common"
        };

        return $"{ChaosEffectsBasePath}card_rarity_{suffix}_sub.png";
    }

    private static string GetRarityTitlePath(CardRarity rarity)
    {
        string suffix = rarity switch
        {
            CardRarity.Uncommon => "rare",
            CardRarity.Rare => "legend",
            CardRarity.Ancient => "unique",
            _ => "common"
        };

        return $"{ChaosEffectsBasePath}card_title_rarity_{suffix}.png";
    }

    private static T? Get<T>(FieldInfo? field, NCard cardNode) where T : GodotObject
    {
        return field?.GetValue(cardNode) as T;
    }

    internal static void ApplyDeferredIfValid(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode) || !cardNode.IsInsideTree() || !cardNode.IsNodeReady())
            return;

        Apply(cardNode);
    }

    private static void EnsureCostOverlayRefresh(NCard cardNode)
    {
        if (!IsEnlargedCard(cardNode) ||
            cardNode.GetNodeOrNull<Node>(CostOverlayRefreshNodeName) != null)
        {
            return;
        }

        var refresh = new MeiLinCardCostOverlayRefresh
        {
            Name = CostOverlayRefreshNodeName,
            CardNode = cardNode
        };
        cardNode.AddChild(refresh);
    }

    private static bool IsEnlargedCard(NCard cardNode)
    {
        try
        {
            return ((Control)cardNode).GetGlobalTransform().Scale.Y > 1.1f;
        }
        catch
        {
            return false;
        }
    }

    private static T? LoadResource<T>(string? path) where T : Resource
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (ResourceCache.TryGetValue(path, out Resource? cached))
            return cached as T;

        if (!ResourceLoader.Exists(path))
        {
            if (MissingResourceWarnings.Add(path))
                MainFile.Logger.Warn($"[CardCustomAncientFrame] Missing resource: {path}");

            return null;
        }

        T? resource = ResourceLoader.Load<T>(path, "", ResourceLoader.CacheMode.Reuse);
        ResourceCache[path] = resource;
        return resource;
    }

    private readonly record struct NodeLayout(float Left, float Top, float Width, float Height, bool Visible = true)
    {
        public Vector2 Position => new(Left, Top);
        public Vector2 Size => new(Width, Height);
    }

    private enum CostAtlasVariant
    {
        Normal,
        Green,
        Red
    }

    private sealed class OriginalCardVisualState
    {
        public bool HasSnapshot { get; set; }
        public CardModel? CapturedModel { get; set; }
        public ControlSnapshot? Banner { get; set; }
        public ControlSnapshot? Frame { get; set; }
        public ControlSnapshot? Portrait { get; set; }
        public ControlSnapshot? AncientPortrait { get; set; }
        public ControlSnapshot? PortraitBorder { get; set; }
        public ControlSnapshot? AncientBorder { get; set; }
        public ControlSnapshot? AncientBanner { get; set; }
        public ControlSnapshot? AncientTextBg { get; set; }
        public ControlSnapshot? AncientHighlight { get; set; }
        public ControlSnapshot? TitleLabel { get; set; }
        public ControlSnapshot? EnergyIcon { get; set; }
        public ControlSnapshot? DescriptionLabel { get; set; }
        public ControlSnapshot? EnergyLabel { get; set; }
        public ControlSnapshot? TypeLabel { get; set; }
        public ControlSnapshot? TypePlaque { get; set; }
    }

    private sealed class ControlSnapshot
    {
        public Vector2 Position { get; init; }
        public Vector2 Size { get; init; }
        public float AnchorLeft { get; init; }
        public float AnchorTop { get; init; }
        public float AnchorRight { get; init; }
        public float AnchorBottom { get; init; }
        public float OffsetLeft { get; init; }
        public float OffsetTop { get; init; }
        public float OffsetRight { get; init; }
        public float OffsetBottom { get; init; }
        public Vector2 PivotOffset { get; init; }
        public float Rotation { get; init; }
        public Vector2 Scale { get; init; }
        public Vector2 CustomMinimumSize { get; init; }
        public bool Visible { get; init; }
        public int ZIndex { get; init; }
        public Color Modulate { get; init; }
        public Color SelfModulate { get; init; }
        public Texture2D? Texture { get; init; }
        public Material? Material { get; init; }
        public TextureRect.ExpandModeEnum? TextureExpandMode { get; init; }
        public TextureRect.StretchModeEnum? TextureStretchMode { get; init; }
        public HorizontalAlignment? LabelHorizontalAlignment { get; init; }
        public VerticalAlignment? LabelVerticalAlignment { get; init; }
        public TextServer.AutowrapMode? LabelAutowrapMode { get; init; }
        public bool? LabelClipText { get; init; }
        public bool? LabelUppercase { get; init; }
        public bool? RichTextScrollActive { get; init; }
        public bool? RichTextFitContent { get; init; }
        public TextServer.AutowrapMode? RichTextAutowrapMode { get; init; }
    }
}

public partial class MeiLinCardCostOverlayRefresh : Node
{
    public NCard? CardNode { get; init; }
    private int _remainingFrames = 8;

    public override void _Process(double delta)
    {
        if (CardNode == null ||
            !GodotObject.IsInstanceValid(CardNode) ||
            !CardNode.IsInsideTree() ||
            _remainingFrames-- <= 0)
        {
            QueueFree();
            return;
        }

        CardCustomAncientFramePatch.ApplyDeferredIfValid(CardNode);
    }
}

public sealed class CardCustomAncientFrameUpdateVisualsPatch : IPatchMethod
{
    public static string PatchId => "meilin_card_custom_ancient_frame_update_visuals";
    public static bool IsCritical => false;
    public static string Description => "Synchronize MeiLin custom ancient card frame during NCard.UpdateVisuals";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCard>(nameof(NCard.UpdateVisuals))
    ];

    [HarmonyPriority(Priority.First)]
    public static void Prefix(NCard __instance)
    {
        CardCustomAncientFramePatch.PrepareForBaseVisuals(__instance);
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        CardCustomAncientFramePatch.Apply(__instance);
    }
}

public sealed class CardCustomAncientFrameReloadPatch : IPatchMethod
{
    public static string PatchId => "meilin_card_custom_ancient_frame_reload";
    public static bool IsCritical => false;
    public static string Description => "Refresh MeiLin custom ancient card frame after NCard.Reload";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCard>("Reload")
    ];

    [HarmonyPriority(Priority.First)]
    public static void Prefix(NCard __instance)
    {
        CardCustomAncientFramePatch.PrepareForBaseVisuals(__instance);
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        CardCustomAncientFramePatch.Apply(__instance);
    }
}

public sealed class CardCustomAncientFrameEnterTreePatch : IPatchMethod
{
    public static string PatchId => "meilin_card_custom_ancient_frame_enter_tree";
    public static bool IsCritical => false;
    public static string Description => "Apply MeiLin custom ancient card frame when cards enter the tree";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCard>("_EnterTree")
    ];

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        CardCustomAncientFramePatch.Apply(__instance);
    }
}

public sealed class CardCustomAncientFrameReadyPatch : IPatchMethod
{
    public static string PatchId => "meilin_card_custom_ancient_frame_ready";
    public static bool IsCritical => false;
    public static string Description => "Deferred MeiLin custom ancient card frame apply when NCard is ready";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCard>("_Ready")
    ];

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        Callable.From(() => CardCustomAncientFramePatch.ApplyDeferredIfValid(__instance)).CallDeferred();
    }
}

public sealed class CardCustomAncientFrameFreedToPoolPatch : IPatchMethod
{
    public static string PatchId => "meilin_card_custom_ancient_frame_freed_to_pool";
    public static bool IsCritical => false;
    public static string Description => "Clean up MeiLin custom ancient card frame state when cards return to pool";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCard>(nameof(NCard.OnFreedToPool))
    ];

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(NCard __instance)
    {
        CardCustomAncientFramePatch.CleanupPooledCard(__instance);
    }
}
