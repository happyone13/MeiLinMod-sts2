using Godot;
using HarmonyLib;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class StatsScreenMeiLinPatch : IPatchMethod
{
    private const string MeiLinStatsNodeName = "MeiLinStats";

    private static readonly AccessTools.FieldRef<NGeneralStatsGrid, Control?> CharacterStatContainerRef =
        AccessTools.FieldRefAccess<NGeneralStatsGrid, Control?>("_characterStatContainer");

    public static string PatchId => "meilin_stats_screen_stats_grid";
    public static bool IsCritical => false;
    public static string Description => "Add MeiLin character stats to the vanilla stats screen";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NGeneralStatsGrid>(nameof(NGeneralStatsGrid.LoadStats))
    ];

    public static void Postfix(NGeneralStatsGrid __instance)
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
