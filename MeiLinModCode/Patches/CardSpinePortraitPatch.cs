using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Config;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardSpinePortraitPatch
{
    public const string SpineOverlayNodeName = "MeiLinSpinePortraitOverlay";
    internal const string SpineViewportContainerNodeName = "ViewportContainer";
    private const string OverlayTargetSlotMetaKey = "meilin_target_slot";
    private const string OverlayTargetSlotAncient = "ancient";
    private const string OverlayTargetSlotNormal = "normal";
    private const float AncientOverlayInsetLeft = 7.0f;
    private const float AncientOverlayInsetTop = 7.0f;
    private const float AncientOverlayInsetRight = 7.0f;
    private const float AncientOverlayInsetBottom = 10.0f;

    public static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Dictionary<string, PackedScene> SceneCache = new();
    private static readonly ConditionalWeakTable<NCard, PortraitVisibilityState> VisibilityStates = new();

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void ReloadPostfix(NCard __instance)
    {
        if (!TryGetSpineScenePath(__instance, out string? scenePath))
        {
            RemoveSpineOverlay(__instance);
            return;
        }

        if (scenePath != null && __instance.IsInsideTree())
            ApplySpinePortrait(__instance, scenePath);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void ReloadPrefix(NCard __instance)
    {
        PrepareForBaseVisuals(__instance);
    }

    public static void ApplySpinePortrait(NCard cardNode, string scenePath)
    {
        string? currentScenePath = null;
        if (!GodotObject.IsInstanceValid(cardNode) ||
            !TryGetSpineScenePath(cardNode, out currentScenePath) ||
            currentScenePath != scenePath)
        {
            MainFile.Logger.Info($"[CardSpinePortrait] Skip ApplySpinePortrait current={currentScenePath ?? "<null>"} requested={scenePath}");
            return;
        }

        RemoveSpineOverlay(cardNode);

        if (cardNode.Model is not MeiLinModCard cardModel)
            return;

        var portrait = PortraitField?.GetValue(cardNode) as TextureRect;
        var ancientPortrait = AncientPortraitField?.GetValue(cardNode) as TextureRect;

        bool applied = cardModel.CustomSpinePortraitSlot switch
        {
            SpinePortraitSlot.Ancient => ApplySpinePortraitToPortrait(cardNode, scenePath, ancientPortrait),
            _ => ApplySpinePortraitToPortrait(cardNode, scenePath, portrait)
        };

        if (applied)
        {
            MainFile.Logger.Info($"[CardSpinePortrait] Applied scene={scenePath} slot={cardModel.CustomSpinePortraitSlot}");
            ForcePortraitSlot(cardNode, portrait, ancientPortrait, cardModel.CustomSpinePortraitSlot);
        }

        if (!applied)
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] No portrait TextureRect accepted dynamic scene: {scenePath}");
        }
    }

    private static bool ApplySpinePortraitToPortrait(NCard cardNode, string scenePath, TextureRect? portrait)
    {
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] Invalid portrait target for scene: {scenePath}");
            return false;
        }

        RemoveAllSpineOverlays(cardNode);
        RemoveAllSpineOverlays(portrait);

        Node? spineInstance = GetOrCreateSpineInstance(scenePath);
        if (spineInstance == null)
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] Failed to load Spine scene: {scenePath}");
            return false;
        }

        var viewportContainer = spineInstance.GetNodeOrNull<SubViewportContainer>(SpineViewportContainerNodeName);
        var subViewport = viewportContainer?.GetNodeOrNull<SubViewport>("SubViewport")
                          ?? spineInstance.GetNodeOrNull<SubViewport>("SubViewport");
        if (subViewport == null)
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] SubViewport not found in Spine scene: {scenePath}");
            spineInstance.QueueFree();
            return false;
        }

        ConfigureDynamicSubViewport(subViewport);

        if (subViewport.GetNodeOrNull<Node>("SpineSprite") is Node spineSprite)
        {
            Variant skeletonData = spineSprite.Get("skeleton_data_res");
            MainFile.Logger.Info($"[CardSpinePortrait] SpineSprite scene={scenePath} skeleton_data_res_nil={skeletonData.VariantType == Variant.Type.Nil}");
        }

        var container = new Control
        {
            Name = SpineOverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 0,
            ClipContents = true,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            Modulate = Colors.White,
            SelfModulate = Colors.White
        };
        container.SetMeta(
            OverlayTargetSlotMetaKey,
            ReferenceEquals(AncientPortraitField?.GetValue(cardNode), portrait)
                ? OverlayTargetSlotAncient
                : OverlayTargetSlotNormal);

        portrait.ClipContents = true;
        var hostedViewportContainer = DetachOrCreateViewportContainer(viewportContainer, subViewport);
        hostedViewportContainer.ClipContents = true;
        container.AddChild(hostedViewportContainer);
        portrait.AddChild(container);
        spineInstance.QueueFree();

        portrait.Texture = null;
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        SyncOverlayLayout(cardNode, portrait, container, subViewport);
        MainFile.Logger.Info($"[CardSpinePortrait] Overlay attached scene={scenePath} portrait_visible={portrait.Visible} portrait_size={portrait.Size} container_size={container.Size}");

        var updater = new SpinePortraitUpdater();
        updater.Initialize(cardNode, container, subViewport);
        container.AddChild(updater);
        return true;
    }

    public static void RemoveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null)
            return;

        RemoveAllSpineOverlays(cardNode);

        if (PortraitField?.GetValue(cardNode) is TextureRect portraitRect)
            RemoveAllSpineOverlays(portraitRect);

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortraitRect)
            RemoveAllSpineOverlays(ancientPortraitRect);

        RestorePortraitTextures(cardNode);
        RestorePortraitVisibility(cardNode);
    }

    public static void PrepareForBaseVisuals(NCard? cardNode)
    {
        if (IsDynamicChaosCard(cardNode))
            return;

        RemoveSpineOverlay(cardNode);
    }

    public static void UpdateSpineAnimationState(
        NCard cardNode,
        Control container,
        SubViewport subViewport,
        int framesSinceCreated)
    {
        if (!GodotObject.IsInstanceValid(cardNode) ||
            !GodotObject.IsInstanceValid(container) ||
            !GodotObject.IsInstanceValid(subViewport))
        {
            return;
        }

        var parentPortrait = ResolveOverlayTargetPortrait(cardNode, container);
        if (parentPortrait != null)
            SyncOverlayLayout(cardNode, parentPortrait, container, subViewport);

        SetStaticPortraitFallback(cardNode, parentPortrait, container, enabled: false);
        SetSpinePlaybackPaused(subViewport, paused: false);
        if (subViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
            subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        if (framesSinceCreated == 8)
            LogOverlayDiagnostics(cardNode, container, subViewport, parentPortrait);
    }

    private static void SetStaticPortraitFallback(
        NCard cardNode,
        TextureRect? portrait,
        Control container,
        bool enabled)
    {
        if (portrait == null)
            return;

        container.Visible = !enabled;
        if (enabled)
        {
            portrait.Texture ??= cardNode.Model?.Portrait;
            return;
        }

        if (portrait.Texture == cardNode.Model?.Portrait)
            portrait.Texture = null;
    }

    private static void SyncOverlayLayout(NCard cardNode, TextureRect portrait, Control container, SubViewport subViewport)
    {
        if (!GodotObject.IsInstanceValid(portrait) ||
            !GodotObject.IsInstanceValid(container) ||
            !GodotObject.IsInstanceValid(subViewport))
        {
            return;
        }

        bool isAncientPortrait = ReferenceEquals(AncientPortraitField?.GetValue(cardNode), portrait);
        var insetPosition = isAncientPortrait
            ? new Vector2(AncientOverlayInsetLeft, AncientOverlayInsetTop)
            : Vector2.Zero;
        var insetSize = isAncientPortrait
            ? new Vector2(
                Mathf.Max(0.0f, portrait.Size.X - AncientOverlayInsetLeft - AncientOverlayInsetRight),
                Mathf.Max(0.0f, portrait.Size.Y - AncientOverlayInsetTop - AncientOverlayInsetBottom))
            : portrait.Size;

        bool overlayParentIsPortrait = ReferenceEquals(container.GetParent(), portrait);
        container.Position = overlayParentIsPortrait ? insetPosition : portrait.Position + insetPosition;
        container.Size = insetSize;
        container.Scale = overlayParentIsPortrait ? Vector2.One : portrait.Scale;
        container.Rotation = overlayParentIsPortrait ? 0.0f : portrait.Rotation;
        container.PivotOffset = Vector2.Zero;

        if (container.GetNodeOrNull<SubViewportContainer>(SpineViewportContainerNodeName) is { } viewportContainer)
        {
            viewportContainer.Position = Vector2.Zero;
            viewportContainer.Size = container.Size;
        }
    }

    private static void SetSpinePlaybackPaused(SubViewport subViewport, bool paused)
    {
        var spineSprite = subViewport.GetNodeOrNull<Node>("SpineSprite");
        if (spineSprite == null)
            return;

        var targetMode = paused ? Node.ProcessModeEnum.Disabled : Node.ProcessModeEnum.Inherit;
        if (spineSprite.ProcessMode != targetMode)
            spineSprite.ProcessMode = targetMode;
    }

    private static void ConfigureDynamicSubViewport(SubViewport subViewport)
    {
        subViewport.Set("transparent_bg", true);
        subViewport.TransparentBg = true;

        foreach (Node child in subViewport.GetChildren())
        {
            if (child is ColorRect colorRect)
                colorRect.Visible = false;
        }
    }

    private static SubViewportContainer DetachOrCreateViewportContainer(
        SubViewportContainer? embeddedViewportContainer,
        SubViewport subViewport)
    {
        if (embeddedViewportContainer != null)
        {
            embeddedViewportContainer.GetParent()?.RemoveChild(embeddedViewportContainer);
            PrepareViewportContainer(embeddedViewportContainer);
            return embeddedViewportContainer;
        }

        subViewport.GetParent()?.RemoveChild(subViewport);

        Vector2I vpSize = subViewport.Size;
        if (vpSize.X < 1 || vpSize.Y < 1)
        {
            vpSize = new Vector2I(598, 844);
            subViewport.Size = vpSize;
        }

        var fallbackViewportContainer = new SubViewportContainer();
        PrepareViewportContainer(fallbackViewportContainer);
        fallbackViewportContainer.AddChild(subViewport);
        return fallbackViewportContainer;
    }

    private static void PrepareViewportContainer(SubViewportContainer viewportContainer)
    {
        viewportContainer.Name = SpineViewportContainerNodeName;
        viewportContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        viewportContainer.AnchorLeft = 0.0f;
        viewportContainer.AnchorTop = 0.0f;
        viewportContainer.AnchorRight = 1.0f;
        viewportContainer.AnchorBottom = 1.0f;
        viewportContainer.OffsetLeft = 0.0f;
        viewportContainer.OffsetTop = 0.0f;
        viewportContainer.OffsetRight = 0.0f;
        viewportContainer.OffsetBottom = 0.0f;
        viewportContainer.Stretch = true;
        viewportContainer.ClipContents = true;
    }

    private static void LogOverlayDiagnostics(
        NCard cardNode,
        Control container,
        SubViewport subViewport,
        TextureRect? portrait)
    {
        try
        {
            Texture2D texture = subViewport.GetTexture();
            Color sampled = Colors.Transparent;
            Vector2I imageSize = Vector2I.Zero;

            if (texture?.GetImage() is { } image && !image.IsEmpty())
            {
                imageSize = new Vector2I(image.GetWidth(), image.GetHeight());
                int sampleX = Mathf.Clamp(image.GetWidth() / 2, 0, image.GetWidth() - 1);
                int sampleY = Mathf.Clamp(image.GetHeight() / 2, 0, image.GetHeight() - 1);
                sampled = image.GetPixel(sampleX, sampleY);
            }

            string slot = container.GetMeta(OverlayTargetSlotMetaKey, OverlayTargetSlotNormal).AsString();
            MainFile.Logger.Info(
                $"[CardSpinePortrait] Diagnostics card={cardNode.Model?.Id.Entry ?? "<null>"} slot={slot} " +
                $"portraitVisible={portrait?.Visible} portraitPos={portrait?.Position} portraitSize={portrait?.Size} " +
                $"containerPos={container.Position} containerSize={container.Size} viewportSize={subViewport.Size} " +
                $"imageSize={imageSize} sample={sampled}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] Diagnostics failed: {ex.Message}");
        }
    }

    private static void RestorePortraitTextures(NCard cardNode)
    {
        Texture2D? portraitTexture = cardNode.Model?.Portrait;
        if (portraitTexture == null)
            return;

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait && portrait.Texture == null)
            portrait.Texture = portraitTexture;

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait && ancientPortrait.Texture == null)
            ancientPortrait.Texture = portraitTexture;
    }

    private static bool TryGetSpineScenePath(NCard? cardNode, out string? scenePath)
    {
        scenePath = null;

        if (cardNode?.Model is not MeiLinModCard cardModel)
        {
            MainFile.Logger.Info("[CardSpinePortrait] Model is not MeiLinModCard");
            return false;
        }

        if (!cardModel.UsesDynamicChaosFrame)
            return false;

        if (!MeiLinModConfig.UseChaosCardDynamicPortraits)
        {
            MainFile.Logger.Info("[CardSpinePortrait] Dynamic portraits disabled by config");
            return false;
        }

        scenePath = cardModel.CustomSpinePortraitScenePath;
        bool exists = !string.IsNullOrWhiteSpace(scenePath) && ResourceLoader.Exists(scenePath);
        if (!exists)
            MainFile.Logger.Warn($"[CardSpinePortrait] Scene path missing or not found: {scenePath ?? "<null>"}");
        return exists;
    }

    private static bool IsDynamicChaosCard(NCard? cardNode)
    {
        return TryGetSpineScenePath(cardNode, out _);
    }

    private static Node? GetOrCreateSpineInstance(string scenePath)
    {
        if (!SceneCache.TryGetValue(scenePath, out PackedScene? scene))
        {
            scene = GD.Load<PackedScene>(scenePath);
            if (scene == null)
                return null;

            SceneCache[scenePath] = scene;
        }

        return scene.Instantiate<Node>();
    }

    private static void RemoveAllSpineOverlays(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child.Name == SpineOverlayNodeName && GodotObject.IsInstanceValid(child))
                child.Free();
        }
    }

    public static bool HasActiveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
            return false;

        if (cardNode.GetNodeOrNull<Control>(SpineOverlayNodeName) != null)
            return true;

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait &&
            portrait.GetNodeOrNull<Control>(SpineOverlayNodeName) != null)
        {
            return true;
        }

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait &&
            ancientPortrait.GetNodeOrNull<Control>(SpineOverlayNodeName) != null)
        {
            return true;
        }

        return false;
    }

    public static void ForcePortraitSlot(
        NCard cardNode,
        TextureRect? portrait,
        TextureRect? ancientPortrait,
        SpinePortraitSlot slot)
    {
        if (portrait == null || ancientPortrait == null)
            return;

        if (slot == SpinePortraitSlot.Ancient && !HasActiveSpineOverlay(cardNode))
        {
            portrait.Visible = true;
            ancientPortrait.Visible = false;
            return;
        }

        var state = VisibilityStates.GetOrCreateValue(cardNode);
        if (!state.HasSnapshot)
        {
            state.PortraitVisible = portrait.Visible;
            state.AncientPortraitVisible = ancientPortrait.Visible;
            state.HasSnapshot = true;
        }

        portrait.Visible = slot != SpinePortraitSlot.Ancient;
        ancientPortrait.Visible = slot == SpinePortraitSlot.Ancient;
    }

    private static void RestorePortraitVisibility(NCard cardNode)
    {
        if (!VisibilityStates.TryGetValue(cardNode, out PortraitVisibilityState? state) || !state.HasSnapshot)
            return;

        if (PortraitField?.GetValue(cardNode) is TextureRect portrait)
            portrait.Visible = state.PortraitVisible;

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortrait)
            ancientPortrait.Visible = state.AncientPortraitVisible;

        VisibilityStates.Remove(cardNode);
    }

    private static TextureRect? ResolveOverlayTargetPortrait(NCard cardNode, Control container)
    {
        string slot = container.GetMeta(OverlayTargetSlotMetaKey, OverlayTargetSlotNormal).AsString();
        return slot == OverlayTargetSlotAncient
            ? AncientPortraitField?.GetValue(cardNode) as TextureRect
            : PortraitField?.GetValue(cardNode) as TextureRect;
    }

    private sealed class PortraitVisibilityState
    {
        public bool HasSnapshot { get; set; }
        public bool PortraitVisible { get; set; }
        public bool AncientPortraitVisible { get; set; }
    }
}

[HarmonyPatch(typeof(NCard), "_EnterTree")]
public static class CardSpineEnterTreePatch
{
    [HarmonyPostfix]
    public static void EnterTreePostfix(NCard __instance)
    {
        if (__instance?.Model is not MeiLinModCard cardModel)
            return;

        if (!__instance.IsNodeReady())
            return;

        if (!MeiLinModConfig.UseChaosCardDynamicPortraits)
            return;

        string? scenePath = cardModel.CustomSpinePortraitScenePath;
        if (string.IsNullOrWhiteSpace(scenePath) || !ResourceLoader.Exists(scenePath))
            return;

        CardSpinePortraitPatch.ApplySpinePortrait(__instance!, scenePath);
    }
}

public partial class SpinePortraitUpdater : Node
{
    private NCard _card = null!;
    private Control _container = null!;
    private SubViewport _subViewport = null!;
    private int _framesSinceCreated;

    public void Initialize(NCard card, Control container, SubViewport subViewport)
    {
        _card = card;
        _container = container;
        _subViewport = subViewport;
        _framesSinceCreated = 0;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card) ||
            !GodotObject.IsInstanceValid(_container) ||
            !GodotObject.IsInstanceValid(_subViewport))
        {
            QueueFree();
            return;
        }

        CardSpinePortraitPatch.UpdateSpineAnimationState(_card, _container, _subViewport, _framesSinceCreated);
        _framesSinceCreated++;
    }
}

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class CardSpineUpdateVisualsPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void UpdateVisualsPrefix(NCard __instance)
    {
        CardSpinePortraitPatch.PrepareForBaseVisuals(__instance);
    }

    [HarmonyPostfix]
    public static void UpdateVisualsPostfix(NCard __instance)
    {
        if (__instance?.Model is not MeiLinModCard cardModel)
        {
            CardSpinePortraitPatch.RemoveSpineOverlay(__instance);
            return;
        }

        if (string.IsNullOrWhiteSpace(cardModel.CustomSpinePortraitScenePath) ||
            !MeiLinModConfig.UseChaosCardDynamicPortraits)
        {
            CardSpinePortraitPatch.RemoveSpineOverlay(__instance);
            return;
        }

        if (!CardSpinePortraitPatch.HasActiveSpineOverlay(__instance) &&
            ResourceLoader.Exists(cardModel.CustomSpinePortraitScenePath))
        {
            CardSpinePortraitPatch.ApplySpinePortrait(__instance, cardModel.CustomSpinePortraitScenePath);
        }

        var portrait = CardSpinePortraitPatch.PortraitField?.GetValue(__instance) as TextureRect;
        var ancientPortrait = CardSpinePortraitPatch.AncientPortraitField?.GetValue(__instance) as TextureRect;
        CardSpinePortraitPatch.ForcePortraitSlot(__instance!, portrait, ancientPortrait, cardModel.CustomSpinePortraitSlot);

        UpdateOverlay(
            __instance!,
            cardModel.CustomSpinePortraitSlot == SpinePortraitSlot.Ancient ? ancientPortrait : portrait);
    }

    private static void UpdateOverlay(NCard cardNode, TextureRect? portrait)
    {
        if (portrait == null)
            return;

        var container = cardNode.GetNodeOrNull<Control>(CardSpinePortraitPatch.SpineOverlayNodeName)
                        ?? portrait.GetNodeOrNull<Control>(CardSpinePortraitPatch.SpineOverlayNodeName);
        var subViewport = container?.GetNodeOrNull<SubViewport>($"{CardSpinePortraitPatch.SpineViewportContainerNodeName}/SubViewport");
        if (container != null && subViewport != null)
            CardSpinePortraitPatch.UpdateSpineAnimationState(cardNode, container, subViewport, int.MaxValue);
    }
}
