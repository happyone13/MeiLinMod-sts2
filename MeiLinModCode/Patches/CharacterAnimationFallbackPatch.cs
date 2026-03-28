using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch]
public static class CharacterAnimationFallbackPatch
{
    private static readonly string[] MerchantFallbacks = ["relaxed_loop", "stop", "camping", "b_idle", "idle"];
    private static readonly string[] RestFallbacks = ["overgrowth_loop", "hive_loop", "glory_loop", "camping", "b_idle", "idle"];

    [HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
    [HarmonyPrefix]
    public static bool MerchantReadyPrefix(NMerchantCharacter __instance)
    {
        if (!IsMeiLinScene(__instance))
        {
            return true;
        }

        if (TryPlayFirstAvailableOnFirstChild(__instance, MerchantFallbacks, loop: true))
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
    [HarmonyPostfix]
    public static void RestSiteReadyPostfix(NRestSiteCharacter __instance)
    {
        if (!IsMeiLinScene(__instance))
        {
            return;
        }

        foreach (Node child in __instance.GetChildren())
        {
            if (child is not Node2D node2D)
            {
                continue;
            }

            TryPlayFirstAvailable(node2D, RestFallbacks, loop: true);
        }
    }

    private static bool IsMeiLinScene(Node node)
    {
        string path = node.SceneFilePath ?? string.Empty;
        return path.Contains("MeiLinMod/scenes/", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryPlayFirstAvailableOnFirstChild(Node parent, string[] candidates, bool loop)
    {
        Node? child = parent.GetChildCount() > 0 ? parent.GetChild(0) : null;
        return child is Node2D node2D && TryPlayFirstAvailable(node2D, candidates, loop);
    }

    private static bool TryPlayFirstAvailable(Node2D node, string[] candidates, bool loop)
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

            MegaTrackEntry? entry = sprite.GetAnimationState().SetAnimation(anim, loop);
            if (loop && entry != null)
            {
                entry.SetTrackTime(entry.GetAnimationEnd() * Rng.Chaotic.NextFloat());
            }

            return true;
        }

        return false;
    }
}
