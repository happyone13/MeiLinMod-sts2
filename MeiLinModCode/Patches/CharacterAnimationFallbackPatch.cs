using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class MerchantCharacterAnimationFallbackPatch : IPatchMethod
{
    public static string PatchId => "meilin_merchant_character_animation_fallback";

    public static bool IsCritical => false;

    public static string Description => "Play the first available MeiLin merchant scene fallback animation";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NMerchantCharacter>(nameof(NMerchantCharacter._Ready))
    ];

    public static bool Prefix(NMerchantCharacter __instance)
    {
        if (!CharacterAnimationFallbackPatch.IsMeiLinScene(__instance))
        {
            return true;
        }

        if (CharacterAnimationFallbackPatch.TryPlayFirstAvailableOnFirstChild(
                __instance,
                CharacterAnimationFallbackPatch.MerchantFallbacks,
                loop: true))
        {
            return false;
        }

        return true;
    }
}

public sealed class MerchantCharacterPlayAnimationFallbackPatch : IPatchMethod
{
    public static string PatchId => "meilin_merchant_character_play_animation_fallback";

    public static bool IsCritical => false;

    public static string Description => "Remap missing MeiLin merchant scene animation cues";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NMerchantCharacter>(nameof(NMerchantCharacter.PlayAnimation))
    ];

    public static void Prefix(NMerchantCharacter __instance, ref string anim)
    {
        if (!CharacterAnimationFallbackPatch.IsMeiLinScene(__instance))
        {
            return;
        }

        if (string.Equals(anim, "relaxed_loop", System.StringComparison.OrdinalIgnoreCase))
        {
            anim = "idle";
        }
    }
}

public sealed class RestSiteCharacterAnimationFallbackPatch : IPatchMethod
{
    public static string PatchId => "meilin_rest_site_character_animation_fallback";

    public static bool IsCritical => false;

    public static string Description => "Play the first available MeiLin rest site scene fallback animation";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NRestSiteCharacter>(nameof(NRestSiteCharacter._Ready))
    ];

    public static void Postfix(NRestSiteCharacter __instance)
    {
        if (!CharacterAnimationFallbackPatch.IsMeiLinScene(__instance))
        {
            return;
        }

        foreach (Node child in __instance.GetChildren())
        {
            if (child is not Node2D node2D)
            {
                continue;
            }

            CharacterAnimationFallbackPatch.TryPlayFirstAvailable(
                node2D,
                CharacterAnimationFallbackPatch.RestFallbacks,
                loop: true);
        }
    }
}

internal static class CharacterAnimationFallbackPatch
{
    public static readonly string[] MerchantFallbacks = ["relaxed_loop", "stop", "camping", "b_idle", "idle"];
    public static readonly string[] RestFallbacks = ["overgrowth_loop", "hive_loop", "glory_loop", "camping", "b_idle", "idle"];

    public static bool IsMeiLinScene(Node node)
    {
        string path = node.SceneFilePath ?? string.Empty;
        if (path.Contains("MeiLinMod/scenes/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string name = node.Name.ToString();
        return name.Contains("MeiLin", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryPlayFirstAvailableOnFirstChild(Node parent, string[] candidates, bool loop)
    {
        Node? child = parent.GetChildCount() > 0 ? parent.GetChild(0) : null;
        return child is Node2D node2D && TryPlayFirstAvailable(node2D, candidates, loop);
    }

    public static bool TryPlayFirstAvailable(Node2D node, string[] candidates, bool loop)
    {
        MegaSprite sprite;
        try
        {
            sprite = new MegaSprite(node);
        }
        catch
        {
            return false;
        }

        foreach (string anim in candidates)
        {
            bool hasAnimation;
            try
            {
                hasAnimation = sprite.HasAnimation(anim);
            }
            catch
            {
                return false;
            }

            if (!hasAnimation)
            {
                continue;
            }

            sprite.GetAnimationState().SetAnimation(anim, loop);
            return true;
        }

        return false;
    }
}
