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
    private const string SpineViewportTextureNodeName = "ViewportTexture";

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

    public static void ApplySpinePortrait(NCard cardNode, string scenePath)
    {
        if (!GodotObject.IsInstanceValid(cardNode) ||
            !TryGetSpineScenePath(cardNode, out string? currentScenePath) ||
            currentScenePath != scenePath)
        {
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
            ForcePortraitSlot(cardNode, portrait, ancientPortrait, cardModel.CustomSpinePortraitSlot);

        if (!applied)
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] No portrait TextureRect accepted dynamic scene: {scenePath}");
        }
    }

    private static bool ApplySpinePortraitToPortrait(NCard cardNode, string scenePath, TextureRect? portrait)
    {
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return false;

        RemoveAllSpineOverlays(portrait);

        Node? spineInstance = GetOrCreateSpineInstance(scenePath);
        if (spineInstance == null)
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] Failed to load Spine scene: {scenePath}");
            return false;
        }

        var subViewport = spineInstance.GetNodeOrNull<SubViewport>("SubViewport");
        if (subViewport == null)
        {
            MainFile.Logger.Warn($"[CardSpinePortrait] SubViewport not found in Spine scene: {scenePath}");
            spineInstance.QueueFree();
            return false;
        }

        subViewport.GetParent()?.RemoveChild(subViewport);

        Vector2I vpSize = subViewport.Size;
        if (vpSize.X < 1 || vpSize.Y < 1)
        {
            vpSize = new Vector2I(598, 844);
            subViewport.Size = vpSize;
        }

        var container = new Control
        {
            Name = SpineOverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 0,
            ClipContents = true,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f
        };

        var viewportTexture = new TextureRect
        {
            Name = SpineViewportTextureNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered
        };

        container.AddChild(subViewport);
        container.AddChild(viewportTexture);
        portrait.AddChild(container);
        spineInstance.QueueFree();

        portrait.Texture = null;
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        SyncOverlayLayout(portrait, container, subViewport);

        var updater = new SpinePortraitUpdater();
        updater.Initialize(cardNode, container, subViewport);
        container.AddChild(updater);
        return true;
    }

    public static void RemoveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null)
            return;

        if (PortraitField?.GetValue(cardNode) is TextureRect portraitRect)
            RemoveAllSpineOverlays(portraitRect);

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect ancientPortraitRect)
            RemoveAllSpineOverlays(ancientPortraitRect);

        RestorePortraitTextures(cardNode);
        RestorePortraitVisibility(cardNode);
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

        var parentPortrait = container.GetParent() as TextureRect;
        if (parentPortrait != null)
            SyncOverlayLayout(parentPortrait, container, subViewport);

        SetStaticPortraitFallback(cardNode, parentPortrait, container, enabled: false);
        SetSpinePlaybackPaused(subViewport, paused: false);
        if (subViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
            subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
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

    private static void SyncOverlayLayout(TextureRect portrait, Control container, SubViewport subViewport)
    {
        if (!GodotObject.IsInstanceValid(portrait) ||
            !GodotObject.IsInstanceValid(container) ||
            !GodotObject.IsInstanceValid(subViewport))
        {
            return;
        }

        container.Position = Vector2.Zero;
        container.Size = portrait.Size;

        if (container.GetNodeOrNull<TextureRect>(SpineViewportTextureNodeName) is { } viewportTexture)
        {
            viewportTexture.Position = Vector2.Zero;
            viewportTexture.Size = container.Size;
            viewportTexture.Texture = subViewport.GetTexture();
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
            return false;

        if (!MeiLinModConfig.UseChaosCardDynamicPortraits)
            return false;

        scenePath = cardModel.CustomSpinePortraitScenePath;
        return !string.IsNullOrWhiteSpace(scenePath) && ResourceLoader.Exists(scenePath);
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

    private static void RemoveAllSpineOverlays(TextureRect ancientPortrait)
    {
        foreach (Node child in ancientPortrait.GetChildren())
        {
            if (child.Name == SpineOverlayNodeName && GodotObject.IsInstanceValid(child))
                child.Free();
        }
    }

    public static bool HasActiveSpineOverlay(TextureRect? portrait)
    {
        return portrait != null &&
               GodotObject.IsInstanceValid(portrait) &&
               portrait.GetNodeOrNull<Control>(SpineOverlayNodeName) != null;
    }

    public static void ForcePortraitSlot(
        NCard cardNode,
        TextureRect? portrait,
        TextureRect? ancientPortrait,
        SpinePortraitSlot slot)
    {
        if (portrait == null || ancientPortrait == null)
            return;

        if (slot == SpinePortraitSlot.Ancient && !HasActiveSpineOverlay(ancientPortrait))
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

        var container = portrait.GetNodeOrNull<Control>(CardSpinePortraitPatch.SpineOverlayNodeName);
        var subViewport = container?.GetNodeOrNull<SubViewport>("SubViewport");
        if (container != null && subViewport != null)
            CardSpinePortraitPatch.UpdateSpineAnimationState(cardNode, container, subViewport, int.MaxValue);
    }
}
