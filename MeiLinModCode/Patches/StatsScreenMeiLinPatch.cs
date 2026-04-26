using Godot;
using HarmonyLib;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
public static class StatsScreenMeiLinPatch
{
    private const string MeiLinStatsNodeName = "MeiLinStats";

    private static readonly AccessTools.FieldRef<NGeneralStatsGrid, Control?> CharacterStatContainerRef =
        AccessTools.FieldRefAccess<NGeneralStatsGrid, Control?>("_characterStatContainer");

    [HarmonyPostfix]
    public static void LoadStatsPostfix(NGeneralStatsGrid __instance)
    {
        var characterStatContainer = CharacterStatContainerRef(__instance);
        if (characterStatContainer == null)
        {
            MainFile.Logger.Info("[StatsScreenMeiLinPatch] Character stats container not found.");
            return;
        }

        if (characterStatContainer.HasNode(MeiLinStatsNodeName))
        {
            return;
        }

        var meiLinStats = GetMeiLinStats();
        if (meiLinStats == null)
        {
            return;
        }

        var statsNode = NCharacterStats.Create(meiLinStats);
        statsNode.Name = MeiLinStatsNodeName;
        characterStatContainer.AddChild(statsNode);
    }

    private static CharacterStats? GetMeiLinStats()
    {
        var meiLinId = ModelDb.Character<MeiLinCharacter>().Id;
        return SaveManager.Instance.Progress.GetStatsForCharacter(meiLinId);
    }
}
