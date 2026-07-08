using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
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
        combatAnimationPatcher.RegisterPatch<MeiLinBattleAnimationGenerateAnimatorPatch>();
        combatAnimationPatcher.RegisterPatch<MeiLinBattleAnimationSetAnimationPrefixPatch>();
        combatAnimationPatcher.RegisterPatch<MeiLinBattleAnimationSetAnimationPostfixPatch>();
        combatAnimationPatcher.RegisterPatch<MeiLinTriggerAnimPatch>();
        combatAnimationPatcher.PatchAll();

        var scenePatcher = RitsuLibFramework.CreatePatcher(MainFile.ModId, "optional-scene", "optional scene");
        scenePatcher.RegisterPatch<GameOverAnimationFallbackOnMegaStatePatch>();
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
                    .WithTitle(ModSettingsText.Literal("美铃设置"))
                    .WithModDisplayName(ModSettingsText.Literal("MeiLinMod"))
                    .WithDescription(ModSettingsText.Literal("通过 RitsuLib 注册的 MeiLinMod 设置页。相关设置继续复用 Yuki/Chaos 共享配置。"));

                page.AddSection("visuals", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("显示"));
                    section.AddToggle(
                        "battle_ready_overlay",
                        ModSettingsText.Literal("背身立绘"),
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
                            ModSettingsText.Literal("战斗特效"),
                            BoolBinding(
                                "combat_effects",
                                () => MeiLinSharedSettings.CombatEffectsEnabled,
                                value => MeiLinSharedSettings.SetCombatEffectsEnabled(value, persist: true)))
                        .AddToggle(
                            "dynamic_card_portraits",
                            ModSettingsText.Literal("动态卡图"),
                            BoolBinding(
                                "dynamic_card_portraits",
                                () => MeiLinSharedSettings.DynamicCardPortraitsEnabled,
                                value => MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(value, persist: true)));
                });

                page.AddSection("portrait_transform", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("背身立绘调整"));
                    section.AddSlider(
                        "battle_ready_scale",
                        ModSettingsText.Literal("立绘缩放"),
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
                            ModSettingsText.Literal("立绘 X 偏移"),
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
                            ModSettingsText.Literal("立绘 Y 偏移"),
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
                    section.WithTitle(ModSettingsText.Literal("音频"));
                    section.AddSlider(
                        "voice_volume",
                        ModSettingsText.Literal("语音音量"),
                        DoubleBinding(
                            "voice_volume",
                            () => MeiLinSharedSettings.VoiceVolume,
                            value => MeiLinSharedSettings.SetVoiceVolume((float)value, persist: true)),
                        minValue: 0d,
                        maxValue: 1d,
                        step: 0.05d,
                        valueFormatter: value => $"{value:P0}");
                });
            },
            SettingsPageId);
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
