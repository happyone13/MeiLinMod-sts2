using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Config;

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
    private const string FrameSparkNodeName = "MeiLinChaosFrameSpark";
    private const string CategoryIconNodeName = "MeiLinChaosCategoryIcon";
    private const string CategoryTextNodeName = "MeiLinChaosCategoryText";
    private const string CostTextNodeName = "MeiLinChaosCostText";
    private const string UpgradeIconNodeName = "MeiLinChaosUpgradeIcon";
    private const string DescriptionMaskNodeName = "MeiLinChaosDescriptionMask";
    private static readonly NodeLayout TitleRibbonLayout = new(-146.0f, -214.0f, 292.0f, 82.0f);
    private static readonly NodeLayout CardTitleLayout = new(-151.0f, -209.0f, 201.0f, 58.0f);
    private static readonly NodeLayout CostLineLayout = new(-145.0f, -200.0f, 68.0f, 115.0f);
    private static readonly NodeLayout CostTextLayout = new(-138.0f, -235.0f, 55.0f, 90.0f);
    private static readonly NodeLayout CategoryIconLayout = new(-87.0f, -177.0f, 28.0f, 44.0f);
    private static readonly NodeLayout CategoryTextLayout = new(-57.0f, -178.0f, 198.0f, 42.0f);
    private static readonly NodeLayout DescriptionTextLayout = new(-142.0f, 40.0f, 278.0f, 161.0f);
    private static readonly NodeLayout DescriptionMaskLayout = new(-153.0f, -63.0f, 298.0f, 271.0f);
    private static readonly NodeLayout EgoBadgeLayout = new(-202.0f, -216.0f, 96.0f, 427.0f);
    private static readonly NodeLayout RarityBaseLayout = new(-174.0f, -194.0f, 35.0f, 78.0f);
    private static readonly NodeLayout RaritySubLayout = new(120.0f, -199.0f, 56.0f, 90.0f);
    private static readonly NodeLayout FrameSparkLayout = new(-91.0f, -83.0f, 157.0f, 218.0f);
    private static readonly NodeLayout UpgradeIconLayout = new(-131.0f, -138.0f, 32.0f, 32.0f, Visible: false);

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
    private static readonly ConditionalWeakTable<NCard, OriginalCardVisualState> OriginalStates = new();
    private static Control? _templateRoot;
    private static Control? _templateCardContainer;
    private static Texture2D? _descriptionMaskTexture;

    public static void Apply(NCard? cardNode)
    {
        if (!TryGetCustomFrameCard(cardNode, out MeiLinModCard? cardModel))
        {
            RemoveChaosEffects(cardNode);
            NormalizeNonChaosCard(cardNode);
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

        if (!CardSpinePortraitPatch.HasActiveSpineOverlay(cardNode))
        {
            RemoveChaosEffects(cardNode);
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

        CaptureOriginalState(cardNode!);
        frame?.Hide();
        portrait?.Hide();
        portraitBorder?.Hide();
        banner?.Hide();

        if (ancientPortrait != null)
            ancientPortrait.Show();

        Material? frameMaterial = LoadResource<Material>(cardModel!.CustomAncientFrameMaterialPath);
        Material? bannerMaterial = LoadResource<Material>(cardModel.CustomAncientBannerMaterialPath);

        ApplyTextureRect(ancientBorder, AncientBorderPath, frameMaterial, show: true);
        if (ancientTextBg != null)
            ancientTextBg.Hide();
        ApplyTextureRect(ancientHighlight, AncientHighlightPath, material: null, show: true);

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

    private static bool TryGetCustomFrameCard(NCard? cardNode, out MeiLinModCard? cardModel)
    {
        cardModel = null;

        if (cardNode?.Model is not MeiLinModCard model)
            return false;

        if (!MeiLinModConfig.UseChaosCardDynamicPortraits || !model.UseCustomAncientFrame)
            return false;

        string? scenePath = model.CustomSpinePortraitScenePath;
        if (!string.IsNullOrWhiteSpace(scenePath) && !ResourceLoader.Exists(scenePath))
            return false;

        cardModel = model;
        return true;
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
        ApplyTemplateLayout(Get<TextureRect>(BannerField, cardNode), "TitleRibbon", TitleRibbonLayout);
        ApplyTemplateLayout(Get<Control>(TitleLabelField, cardNode), "CardTitle", CardTitleLayout);
        ApplyTemplateLayout(Get<TextureRect>(EnergyIconField, cardNode), "CostLine", CostLineLayout);
        ApplyTemplateLayout(Get<Control>(DescriptionLabelField, cardNode), "DescriptionText", DescriptionTextLayout);

        ApplyTextureRect(Get<TextureRect>(BannerField, cardNode), GetRarityTitlePath(cardModel.Rarity), material: null, show: true);
        ApplyTextureRect(Get<TextureRect>(EnergyIconField, cardNode), $"{ChaosEffectsBasePath}energy_line_default.png", material: null, show: true);

        var energyLabel = Get<Control>(EnergyLabelField, cardNode);
        bool energyLabelVisible = energyLabel?.Visible ?? false;
        if (energyLabel != null)
            energyLabel.Hide();

        EnsureTemplateOverlay(cardNode, CostTextNodeName, "CostText", () => CreateLabelOverlay(CostTextLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "CostText", CostTextLayout);
            SetOverlayText(control, GetControlText(energyLabel), true, energyLabel);
        });

        if (Get<Control>(AncientBannerField, cardNode) is { } ancientBanner)
            ancientBanner.Hide();

        var typePlaque = Get<Control>(TypePlaqueField, cardNode);
        if (typePlaque != null)
            typePlaque.Visible = false;

        var typeLabel = Get<Control>(TypeLabelField, cardNode);
        bool typeLabelVisible = typeLabel?.Visible ?? false;
        if (typeLabel != null)
            typeLabel.Hide();

        EnsureTemplateOverlay(cardNode, CategoryTextNodeName, "CategoryText", () => CreateLabelOverlay(CategoryTextLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "CategoryText", CategoryTextLayout);
            SetOverlayText(control, GetControlText(typeLabel), true, typeLabel);
        });

        RemoveNode(cardNode, DescriptionMaskNodeName);

        EnsureTemplateOverlay(cardNode, EgoBadgeNodeName, "EgoBadge", () => CreateTextureOverlay(EgoBadgeLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "EgoBadge", EgoBadgeLayout);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, $"{ChaosEffectsBasePath}card_ego_love.png", material: null, show: true);
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
            SetOverlayVisibility(control, true, typeLabel);
            EnsureDrawBefore(control, typeLabel);
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, GetCategoryIconPath(cardModel.Type), material: null, show: true);
        });

        EnsureTemplateOverlay(cardNode, UpgradeIconNodeName, "UpgradeIcon", () => CreateTextureOverlay(UpgradeIconLayout), configure: control =>
        {
            ApplyTemplateLayout(control, "UpgradeIcon", UpgradeIconLayout with { Visible = cardModel.IsUpgraded });
            if (control is TextureRect textureRect)
                ApplyTextureRect(textureRect, $"{ChaosEffectsBasePath}icon_card_battle_expand_default.png", material: null, show: cardModel.IsUpgraded);
        });
    }

    private static void RemoveChaosEffects(NCard? cardNode)
    {
        if (cardNode == null)
            return;

        RemoveNode(cardNode, RarityBaseNodeName);
        RemoveNode(cardNode, RaritySubNodeName);
        RemoveNode(cardNode, EgoBadgeNodeName);
        RemoveNode(cardNode, FrameSparkNodeName);
        RemoveNode(cardNode, CategoryIconNodeName);
        RemoveNode(cardNode, CategoryTextNodeName);
        RemoveNode(cardNode, CostTextNodeName);
        RemoveNode(cardNode, UpgradeIconNodeName);
        RemoveNode(cardNode, DescriptionMaskNodeName);
        RestoreOriginalState(cardNode);
    }

    private static void EnsureTemplateOverlay(
        NCard cardNode,
        string runtimeNodeName,
        string templateNodeName,
        Func<Control?> fallbackCreate,
        Action<Control>? configure = null)
    {
        Control? control = cardNode.GetNodeOrNull<Control>(runtimeNodeName);
        if (control == null)
        {
            control = DuplicateTemplateNode(templateNodeName) ?? fallbackCreate();
            if (control == null)
                return;

            control.Name = runtimeNodeName;
            cardNode.AddChild(control);
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
        parent.GetNodeOrNull<Node>(nodeName)?.QueueFree();
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

        if (!ReferenceEquals(state.CapturedModel, cardNode.Model))
        {
            OriginalStates.Remove(cardNode);
            return;
        }

        RestoreControlSnapshot(Get<Control>(BannerField, cardNode), state.Banner);
        RestoreControlSnapshot(Get<Control>(FrameField, cardNode), state.Frame);
        RestoreControlSnapshot(Get<Control>(CardSpinePortraitPatch.PortraitField, cardNode), state.Portrait);
        RestoreControlSnapshot(Get<Control>(CardSpinePortraitPatch.AncientPortraitField, cardNode), state.AncientPortrait);
        RestoreControlSnapshot(Get<Control>(PortraitBorderField, cardNode), state.PortraitBorder);
        RestoreControlSnapshot(Get<Control>(AncientBorderField, cardNode), state.AncientBorder);
        RestoreControlSnapshot(Get<Control>(AncientBannerField, cardNode), state.AncientBanner);
        RestoreControlSnapshot(Get<Control>(AncientTextBgField, cardNode), state.AncientTextBg);
        RestoreControlSnapshot(Get<Control>(AncientHighlightField, cardNode), state.AncientHighlight);
        RestoreControlSnapshot(Get<Control>(TitleLabelField, cardNode), state.TitleLabel);
        RestoreControlSnapshot(Get<Control>(EnergyIconField, cardNode), state.EnergyIcon);
        RestoreControlSnapshot(Get<Control>(DescriptionLabelField, cardNode), state.DescriptionLabel);
        RestoreControlSnapshot(Get<Control>(EnergyLabelField, cardNode), state.EnergyLabel);
        RestoreControlSnapshot(Get<Control>(TypeLabelField, cardNode), state.TypeLabel);
        RestoreControlSnapshot(Get<Control>(TypePlaqueField, cardNode), state.TypePlaque);
        OriginalStates.Remove(cardNode);
    }

    private static void NormalizeNonChaosCard(NCard? cardNode)
    {
        if (cardNode == null || cardNode.Model == null)
            return;

        if (cardNode.Model.Rarity == CardRarity.Ancient)
            return;

        Get<Control>(FrameField, cardNode)?.Show();
        Get<Control>(CardSpinePortraitPatch.PortraitField, cardNode)?.Show();
        Get<Control>(PortraitBorderField, cardNode)?.Show();
        Get<Control>(BannerField, cardNode)?.Show();
        Get<Control>(CardSpinePortraitPatch.AncientPortraitField, cardNode)?.Hide();
        Get<Control>(AncientBorderField, cardNode)?.Hide();
        Get<Control>(AncientTextBgField, cardNode)?.Hide();
        Get<Control>(AncientBannerField, cardNode)?.Hide();
        Get<Control>(AncientHighlightField, cardNode)?.Hide();
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

    private static void RestoreControlSnapshot(Control? control, ControlSnapshot? snapshot)
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

    private static void BringToFront(Node child)
    {
        if (child.GetParent() == null)
            return;

        child.GetParent().MoveChild(child, child.GetParent().GetChildCount() - 1);
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

    private static string GetControlText(Control? control)
    {
        return control switch
        {
            Label label => label.Text,
            RichTextLabel richTextLabel => richTextLabel.Text,
            _ => string.Empty
        };
    }

    private static void SetOverlayText(Control control, string text, bool sourceVisible, Control? source = null)
    {
        SetOverlayVisibility(control, sourceVisible, source);
        bool visible = sourceVisible && !string.IsNullOrWhiteSpace(text);
        switch (control)
        {
            case Label label:
                label.Text = text;
                label.Visible = visible;
                break;
            case RichTextLabel richTextLabel:
                richTextLabel.Text = text;
                richTextLabel.Visible = visible;
                break;
            default:
                control.Visible = visible;
                break;
        }
    }

    private static void SetOverlayVisibility(Control control, bool sourceVisible, Control? source = null)
    {
        control.Visible = sourceVisible;
        if (source == null)
            return;

        control.ZIndex = source.ZIndex;
        control.Modulate = source.Modulate;
        control.SelfModulate = source.SelfModulate;
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

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class CardCustomAncientFrameUpdateVisualsPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void UpdateVisualsPostfix(NCard __instance)
    {
        CardCustomAncientFramePatch.Apply(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardCustomAncientFrameReloadPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void ReloadPostfix(NCard __instance)
    {
        CardCustomAncientFramePatch.Apply(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "_EnterTree")]
public static class CardCustomAncientFrameEnterTreePatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void EnterTreePostfix(NCard __instance)
    {
        CardCustomAncientFramePatch.Apply(__instance);
    }
}
