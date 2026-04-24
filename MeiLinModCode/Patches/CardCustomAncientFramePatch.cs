using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Config;

namespace MeiLinMod.MeiLinModCode.Patches;

public static class CardCustomAncientFramePatch
{
    private const string ChaosFrameBasePath = "res://MeiLinMod/images/cards/chaos_frame/";
    private const string AncientBorderPath =
        ChaosFrameBasePath + "ancient_card_border.tres";
    private const string AncientHighlightPath =
        ChaosFrameBasePath + "card_highlight_ancient.tres";
    private const string AncientBannerPath =
        ChaosFrameBasePath + "ancient_banner.tres";

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
    private static readonly Dictionary<string, Resource?> ResourceCache = new();
    private static readonly HashSet<string> MissingResourceWarnings = new();

    public static void Apply(NCard? cardNode)
    {
        if (!TryGetCustomFrameCard(cardNode, out MeiLinModCard? cardModel))
            return;

        var frame = Get<TextureRect>(FrameField, cardNode!);
        var portrait = Get<TextureRect>(CardSpinePortraitPatch.PortraitField, cardNode!);
        var ancientPortrait = Get<TextureRect>(CardSpinePortraitPatch.AncientPortraitField, cardNode!);
        var portraitBorder = Get<TextureRect>(PortraitBorderField, cardNode!);
        var banner = Get<TextureRect>(BannerField, cardNode!);
        var ancientBorder = Get<TextureRect>(AncientBorderField, cardNode!);
        var ancientTextBg = Get<TextureRect>(AncientTextBgField, cardNode!);
        var ancientBanner = Get<Control>(AncientBannerField, cardNode!);
        var ancientHighlight = Get<TextureRect>(AncientHighlightField, cardNode!);

        frame?.Hide();
        portrait?.Hide();
        portraitBorder?.Hide();
        banner?.Hide();

        if (ancientPortrait != null)
            ancientPortrait.Show();

        Material? frameMaterial = LoadResource<Material>(cardModel!.CustomAncientFrameMaterialPath);
        Material? bannerMaterial = LoadResource<Material>(cardModel.CustomAncientBannerMaterialPath);

        ApplyTextureRect(ancientBorder, AncientBorderPath, frameMaterial, show: true);
        ApplyTextureRect(ancientTextBg, GetAncientTextBgPath(cardModel.Type), frameMaterial, show: true);
        ApplyTextureRect(ancientHighlight, AncientHighlightPath, material: null, show: true);

        if (ancientBanner != null)
        {
            ancientBanner.Show();
            if (bannerMaterial != null)
                ancientBanner.Material = bannerMaterial;

            if (ancientBanner is TextureRect ancientBannerTexture)
                ApplyTextureRect(ancientBannerTexture, AncientBannerPath, bannerMaterial, show: true);
        }
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
