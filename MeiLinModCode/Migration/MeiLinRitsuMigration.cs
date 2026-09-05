using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Encounters;
using MeiLinMod.MeiLinModCode.Patches;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Settings;

namespace MeiLinMod.MeiLinModCode.Migration;

internal static class MeiLinRitsuMigration
{
    private const string SettingsPageId = "meilin-settings";

    public static void Initialize()
    {
        var assembly = typeof(MainFile).Assembly;
        MainFile.Logger.Info("[MeiLinRitsuMigration] Initializing RitsuLib integration.");
        ModTypeDiscoveryHub.RegisterModAssembly(MainFile.ModId, assembly);
        MeiLinRitsuContentRegistration.Register(assembly);
        RegisterSettingsPage();
        RegisterOptionalPatchers();
        MainFile.Logger.Info("[MeiLinRitsuMigration] RitsuLib integration initialized.");
    }

    private static void RegisterOptionalPatchers()
    {
        var uiPatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-ui", "optional UI");
        uiPatcher.RegisterPatch<StatsScreenMeiLinPatch>();
        uiPatcher.RegisterPatch<YukiSettingsPanelEmptyReadyCompatPatch>();
        uiPatcher.PatchAll();

        var audioPatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-audio", "optional audio");
        audioPatcher.RegisterPatch<SfxCmdMeiLinAudioPatch>();
        audioPatcher.PatchAll();

        var overlayPatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-overlay", "optional battle ready overlay");
        overlayPatcher.RegisterPatch<MeiLinBattleReadyAfterCombatVictoryPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyAfterDeathPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyBeforeCombatStartPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyBeforeCardPlayedPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyCancelPlayCardPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyControllerCardPlayStartPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyHandFocusPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyHandHoverEffectsPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyHandMousePressedPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyHandUnfocusPatch>();
        overlayPatcher.RegisterPatch<MeiLinBattleReadyMouseCardPlayStartPatch>();
        overlayPatcher.PatchAll();

        var combatAnimationPatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-combat-animation", "optional combat animation");
        combatAnimationPatcher.RegisterPatch<MeiLinTriggerAnimPatch>();
        combatAnimationPatcher.RegisterPatch<MeiLinBattleVfxWarmPatch>();
        combatAnimationPatcher.PatchAll();

        var scenePatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-scene", "optional scene");
        scenePatcher.RegisterPatch<MerchantCharacterAnimationFallbackPatch>();
        scenePatcher.RegisterPatch<MerchantCharacterPlayAnimationFallbackPatch>();
        scenePatcher.RegisterPatch<RestSiteCharacterAnimationFallbackPatch>();
        scenePatcher.PatchAll();

        var cardVisualPatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-card-visual", "optional card visuals");
        cardVisualPatcher.RegisterPatch<CardCustomAncientFrameEnterTreePatch>();
        cardVisualPatcher.RegisterPatch<CardCustomAncientFrameFreedToPoolPatch>();
        cardVisualPatcher.RegisterPatch<CardCustomAncientFrameReadyPatch>();
        cardVisualPatcher.RegisterPatch<CardCustomAncientFrameReloadPatch>();
        cardVisualPatcher.RegisterPatch<CardCustomAncientFrameUpdateVisualsPatch>();
        cardVisualPatcher.RegisterPatch<CardSpinePortraitEnterTreePatch>();
        cardVisualPatcher.RegisterPatch<CardSpinePortraitReloadPatch>();
        cardVisualPatcher.RegisterPatch<CardSpinePortraitUpdateVisualsPatch>();
        cardVisualPatcher.PatchAll();

        var contentPatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-content", "optional content");
        contentPatcher.RegisterPatch<GloomyEscapeCardBeforeCombatStartPatch>();
        contentPatcher.RegisterPatch<ArchaicToothAfterObtainedMeiLinPatch>();
        contentPatcher.RegisterPatch<ArchaicToothSetupForPlayerMeiLinPatch>();
        contentPatcher.RegisterPatch<ColorfulPhilosophersMeiLinPatch>();
        contentPatcher.RegisterPatch<DustyTomeAfterObtainedMeiLinPatch>();
        contentPatcher.RegisterPatch<DustyTomeSetupForPlayerMeiLinPatch>();
        contentPatcher.RegisterPatch<HuoYongYuXiaCiAfterCombatPatch>();
        contentPatcher.RegisterPatch<OrobasSeaGlassMeiLinPatch>();
        contentPatcher.RegisterPatch<PrismaticGemMeiLinPatch>();
        contentPatcher.RegisterPatch<TouchOfOrobasMeiLinPatch>();
        contentPatcher.PatchAll();

        MainFile.Logger.Info("[MeiLinRitsuMigration] Optional patchers registered: ui, audio, overlay, combat-animation, scene, card-visual, content.");
    }

    private static void RegisterSettingsPage()
    {
        RitsuLibFramework.RegisterModSettings(
            MainFile.ModId,
            page =>
            {
                page
                    .WithTitle(SettingsText("MEILINMOD_RITSU_PAGE.title", "Mei Lin Settings"))
                    .WithModDisplayName(ModSettingsText.Literal("MeiLinMod"))
                    .WithDescription(SettingsText(
                        "MEILINMOD_RITSU_PAGE.description",
                        "MeiLinMod settings registered through RitsuLib. These options share their configuration with Yuki/Chaos."));

                page.AddSection("visuals", section =>
                {
                    section.WithTitle(SettingsText("MEILINMOD_RITSU_SECTION_VISUALS.title", "Visuals"));
                    section.AddToggle(
                        "battle_ready_overlay",
                        SettingsText("MEILINMOD_RITSU_BATTLE_READY_OVERLAY.title", "Back-Facing Portrait"),
                        BoolBinding(
                            "battle_ready_overlay",
                            () => MeiLinSharedSettings.BattleReadyOverlayEnabled,
                            value =>
                            {
                                MeiLinSharedSettings.SetBattleReadyOverlayEnabled(value, persist: true);
                                if (value)
                                    MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
                                else
                                    MeiLinBattleReadyOverlay.NotifyCombatEnded();
                            }))
                        .AddToggle(
                            "combat_effects",
                            SettingsText("MEILINMOD_RITSU_COMBAT_EFFECTS.title", "Combat Effects"),
                            BoolBinding(
                                "combat_effects",
                                () => MeiLinSharedSettings.CombatEffectsEnabled,
                                value => MeiLinSharedSettings.SetCombatEffectsEnabled(value, persist: true)))
                        .AddToggle(
                            "ultimate_cinematics",
                            SettingsText("MEILINMOD_RITSU_ULTIMATE_CINEMATICS.title", "UG / UX Cinematics"),
                            BoolBinding(
                                "ultimate_cinematics",
                                () => MeiLinSharedSettings.UltimateCinematicsEnabled,
                                value => MeiLinSharedSettings.SetUltimateCinematicsEnabled(value, persist: true)))
                        .AddToggle(
                            "dynamic_card_portraits",
                            SettingsText("MEILINMOD_RITSU_DYNAMIC_CARD_PORTRAITS.title", "Dynamic Card Art"),
                            BoolBinding(
                                "dynamic_card_portraits",
                                () => MeiLinSharedSettings.DynamicCardPortraitsEnabled,
                                value => MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(value, persist: true)));
                });

                page.AddSection("portrait_transform", section =>
                {
                    section.WithTitle(SettingsText(
                        "MEILINMOD_RITSU_SECTION_PORTRAIT_TRANSFORM.title",
                        "Back-Facing Portrait Position"));
                    section.AddSlider(
                        "battle_ready_scale",
                        SettingsText("MEILINMOD_RITSU_BATTLE_READY_SCALE.title", "Portrait Scale"),
                        DoubleBinding(
                            "battle_ready_scale",
                            () => MeiLinSharedSettings.BattleReadyScale,
                            value =>
                            {
                                MeiLinSharedSettings.SetBattleReadyScale((float)value, persist: true);
                                MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
                            }),
                        minValue: 0.5d,
                        maxValue: 2d,
                        step: 0.05d,
                        valueFormatter: value => value.ToString("0.00"))
                        .AddSlider(
                            "battle_ready_offset_x",
                            SettingsText("MEILINMOD_RITSU_BATTLE_READY_OFFSET_X.title", "Portrait X Offset"),
                            DoubleBinding(
                                "battle_ready_offset_x",
                                () => MeiLinSharedSettings.BattleReadyOffsetX,
                                value =>
                                {
                                    MeiLinSharedSettings.SetBattleReadyOffsetX((float)value, persist: true);
                                    MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
                                }),
                            minValue: -400d,
                            maxValue: 400d,
                            step: 5d,
                            valueFormatter: value => value.ToString("0"))
                        .AddSlider(
                            "battle_ready_offset_y",
                            SettingsText("MEILINMOD_RITSU_BATTLE_READY_OFFSET_Y.title", "Portrait Y Offset"),
                            DoubleBinding(
                                "battle_ready_offset_y",
                                () => MeiLinSharedSettings.BattleReadyOffsetY,
                                value =>
                                {
                                    MeiLinSharedSettings.SetBattleReadyOffsetY((float)value, persist: true);
                                    MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
                                }),
                            minValue: -400d,
                            maxValue: 400d,
                            step: 5d,
                            valueFormatter: value => value.ToString("0"));
                });

                page.AddSection("audio", section =>
                {
                    section.WithTitle(SettingsText("MEILINMOD_RITSU_SECTION_AUDIO.title", "Audio"));
                    section.AddSlider(
                        "voice_volume",
                        SettingsText("MEILINMOD_RITSU_VOICE_VOLUME.title", "Voice Volume"),
                        DoubleBinding(
                            "voice_volume",
                            () => MeiLinSharedSettings.VoiceVolume,
                            value => MeiLinSharedSettings.SetVoiceVolume((float)value, persist: true)),
                        minValue: 0d,
                        maxValue: 1d,
                        step: 0.05d,
                        valueFormatter: value => $"{value:P0}");
                });

                page.AddSection("gameplay", section =>
                {
                    section.WithTitle(SettingsText("MEILINMOD_RITSU_SECTION_GAMEPLAY.title", "Gameplay"));
                    section.AddToggle(
                        "gloomy_encounter",
                        SettingsText("MEILINMOD_RITSU_GLOOMY_ENCOUNTER.title", "An Old Acquaintance"),
                        BoolBinding(
                            "gloomy_encounter",
                            () => GloomyEncounterSharedSettings.Enabled,
                            value => GloomyEncounterSharedSettings.SetEnabled(value, persist: true)),
                        SettingsText(
                            "MEILINMOD_RITSU_GLOOMY_ENCOUNTER.description",
                            "Enable this option and you may encounter an old acquaintance."));
                });
            },
            SettingsPageId);
    }

    private static ModSettingsText SettingsText(string key, string fallback)
    {
        return ModSettingsText.LocString("settings_ui", key, fallback);
    }

    private static IModSettingsValueBinding<bool> BoolBinding(
        string key,
        Func<bool> read,
        Action<bool> write)
    {
        return ModSettingsBindings.Callback(MainFile.ModId, key, read, write, SaveNoOp);
    }

    private static IModSettingsValueBinding<double> DoubleBinding(
        string key,
        Func<float> read,
        Action<double> write)
    {
        return ModSettingsBindings.Callback(MainFile.ModId, key, () => read(), write, SaveNoOp);
    }

    private static void SaveNoOp()
    {
        // The legacy shared settings setters persist immediately to stay compatible with Yuki/Chaos.
    }
}
