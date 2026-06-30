using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

namespace MeiLinMod.MeiLinModCode.Mechanics.Settings;

[HarmonyPatch(typeof(NSettingsScreen), nameof(NSettingsScreen._Ready))]
public static class MeiLinSharedSettingsUiPatch
{
	private const string ClipperPath = "ScrollContainer/Mask/Clipper";
	private const string SoundSettingsVBoxPath = ClipperPath + "/SoundSettings/VBoxContainer";
	private const string SettingsTabManagerPath = "%SettingsTabManager";
	private const string ModTabScenePath = "res://scenes/screens/settings_tab.tscn";
	private const string SettingsSliderScenePath = "res://scenes/screens/settings_slider.tscn";
	private const string SettingsTickboxScenePath = "res://scenes/screens/settings_tickbox.tscn";
	private const string ScrollContainerPath = "ScrollContainer";
	private const string TemplateLabelPath = "SfxVolume/Label";
	private static readonly string TemplateLabelFullPath = SoundSettingsVBoxPath + "/" + TemplateLabelPath;
	private const string TickboxTickedPath = "TickboxVisuals/Ticked";
	private const string TickboxNotTickedPath = "TickboxVisuals/NotTicked";
	private const string TickboxReticlePath = "SelectionReticle";
	private const float UiFontScale = 1.45f;
	private const float RowMinHeight = 64f;
	private const float RowSeparation = 24f;
	private const float LabelMinHeight = 32f;
	private const float LabelWidth = 340f;
	private const float SliderMinWidth = 420f;
	private const float TickboxMinWidth = 96f;
	private const float ResetButtonWidth = 320f;
	private const float ResetButtonHeight = 56f;
	private const float ScrollBarWidth = 18f;
	private const float SliderValueWidth = 96f;
	private const float SliderValueGap = 12f;

	private const string ModTabName = "XCskin_ModSettingsTab";
	private const string ModPanelName = "XCskin_ModSettingsPanel";
	private const string VoiceSectionName = "ChaosModVoiceVolume";
	private const string VoiceSliderName = "ChaosModVoiceSlider";
	private const string VoiceLineName = "Line_ChaosModVoice";
	private const string ActionVfxSectionName = "ChaosModActionVfxEnabled";
	private const string ActionVfxTickboxName = "ChaosModActionVfxTickbox";
	private const string ActionVfxLineName = "Line_ChaosModActionVfxEnabled";
	private const string PortraitsSectionName = "ChaosModPortraitsEnabled";
	private const string PortraitsTickboxName = "ChaosModPortraitsTickbox";
	private const string PortraitsLineName = "Line_ChaosModPortraitsEnabled";
	private const string ScaleSectionName = "ChaosModBattleReadyScale";
	private const string ScaleSliderName = "ChaosModBattleReadyScaleSlider";
	private const string ScaleLineName = "Line_ChaosModBattleReadyScale";
	private const string OffsetYSectionName = "ChaosModBattleReadyOffsetY";
	private const string OffsetYSliderName = "ChaosModBattleReadyOffsetYSlider";
	private const string OffsetYLineName = "Line_ChaosModBattleReadyOffsetY";
	private const string OffsetXSectionName = "ChaosModBattleReadyOffsetX";
	private const string OffsetXSliderName = "ChaosModBattleReadyOffsetXSlider";
	private const string OffsetXLineName = "Line_ChaosModBattleReadyOffsetX";
	private const string DynamicCardSectionName = "ChaosModDynamicCardPortraitsEnabled";
	private const string DynamicCardTickboxName = "ChaosModDynamicCardPortraitsTickbox";
	private const string DynamicCardLineName = "Line_ChaosModDynamicCardPortraitsEnabled";
	private const string ResetSectionName = "ChaosModBattleReadyReset";
	private const string ResetButtonName = "ChaosModBattleReadyResetButton";
	private const string ResetLineName = "Line_ChaosModBattleReadyReset";
	private const string ControlWiredMeta = "XCskin_SettingsWired";

	[HarmonyPostfix]
	public static void Postfix(NSettingsScreen __instance)
	{
		TryInject(__instance, "_Ready");
	}

	public static void TryInject(NSettingsScreen screen, string source)
	{
		try
		{
			TryInjectInner(screen, source);
		}
		catch (Exception ex)
		{
			Log.Warn("[MeiLinMod] Settings inject failed (" + source + "): " + ex);
		}
	}

	private static void TryInjectInner(NSettingsScreen screen, string source)
	{
		if (!EnsureModSettingsTabAndPanel(screen, out NSettingsPanel? panel, source))
			return;

		VBoxContainer? vbox = panel!.GetNodeOrNull<VBoxContainer>("VBoxContainer");
		if (vbox == null)
		{
			Log.Warn("[MeiLinMod] Settings inject skipped (" + source + "): panel missing VBoxContainer");
			return;
		}

		RichTextLabel? templateLabel = screen.GetNodeOrNull<RichTextLabel>(TemplateLabelFullPath);
		PackedScene? settingsSliderScene = ResourceLoader.Load<PackedScene>(SettingsSliderScenePath);
		PackedScene? settingsTickboxScene = ResourceLoader.Load<PackedScene>(SettingsTickboxScenePath);
		if (settingsSliderScene == null || settingsTickboxScene == null)
		{
			Log.Warn("[MeiLinMod] Settings inject skipped (" + source + "): missing slider or tickbox scene");
			return;
		}

		EnsureSliderSection(vbox, templateLabel, settingsSliderScene, VoiceLineName, VoiceSectionName, VoiceSliderName, "角色音量");
		EnsureTickboxSection(vbox, templateLabel, settingsTickboxScene, ActionVfxLineName, ActionVfxSectionName, ActionVfxTickboxName, "特效开关");
		EnsureTickboxSection(vbox, templateLabel, settingsTickboxScene, PortraitsLineName, PortraitsSectionName, PortraitsTickboxName, "立绘开关");
		EnsureSliderSection(vbox, templateLabel, settingsSliderScene, ScaleLineName, ScaleSectionName, ScaleSliderName, "背身立绘缩放");
		EnsureSliderSection(vbox, templateLabel, settingsSliderScene, OffsetYLineName, OffsetYSectionName, OffsetYSliderName, "背身立绘位置上下");
		EnsureSliderSection(vbox, templateLabel, settingsSliderScene, OffsetXLineName, OffsetXSectionName, OffsetXSliderName, "背身立绘位置左右");
		EnsureTickboxSection(vbox, templateLabel, settingsTickboxScene, DynamicCardLineName, DynamicCardSectionName, DynamicCardTickboxName, "启动动态卡图 [font_size=18]进入战斗时生效[/font_size]");
		EnsureResetSection(vbox, templateLabel);
		ConfigurePanelLayout(screen, panel!, vbox);

		Control? voiceSliderRoot = vbox.GetNodeOrNull<Control>(VoiceSectionName + "/" + VoiceSliderName);
		Control? actionVfxTickboxRoot = vbox.GetNodeOrNull<Control>(ActionVfxSectionName + "/" + ActionVfxTickboxName);
		Control? portraitsTickboxRoot = vbox.GetNodeOrNull<Control>(PortraitsSectionName + "/" + PortraitsTickboxName);
		Control? scaleSliderRoot = vbox.GetNodeOrNull<Control>(ScaleSectionName + "/" + ScaleSliderName);
		Control? offsetYSliderRoot = vbox.GetNodeOrNull<Control>(OffsetYSectionName + "/" + OffsetYSliderName);
		Control? offsetXSliderRoot = vbox.GetNodeOrNull<Control>(OffsetXSectionName + "/" + OffsetXSliderName);
		Control? dynamicCardTickboxRoot = vbox.GetNodeOrNull<Control>(DynamicCardSectionName + "/" + DynamicCardTickboxName);
		Control? resetButtonRoot = vbox.GetNodeOrNull<Control>(ResetSectionName + "/" + ResetButtonName);

		if (voiceSliderRoot != null)
			Callable.From(() => WireVoiceSliderWhenReady(voiceSliderRoot, source, 0)).CallDeferred();
		if (actionVfxTickboxRoot != null)
			Callable.From(() => WireActionVfxTickboxWhenReady(actionVfxTickboxRoot, source, 0)).CallDeferred();
		if (portraitsTickboxRoot != null)
			Callable.From(() => WirePortraitsTickboxWhenReady(portraitsTickboxRoot, source, 0)).CallDeferred();
		if (scaleSliderRoot != null)
			Callable.From(() => WireScaleSliderWhenReady(scaleSliderRoot, source, 0)).CallDeferred();
		if (offsetYSliderRoot != null)
			Callable.From(() => WireOffsetYSliderWhenReady(offsetYSliderRoot, source, 0)).CallDeferred();
		if (offsetXSliderRoot != null)
			Callable.From(() => WireOffsetXSliderWhenReady(offsetXSliderRoot, source, 0)).CallDeferred();
		if (dynamicCardTickboxRoot != null)
			Callable.From(() => WireDynamicCardPortraitsTickboxWhenReady(dynamicCardTickboxRoot, source, 0)).CallDeferred();
		if (resetButtonRoot != null)
			Callable.From(() => WireResetButtonWhenReady(vbox, resetButtonRoot, source, 0)).CallDeferred();

		RefreshFocusNeighbors(voiceSliderRoot, actionVfxTickboxRoot, portraitsTickboxRoot, scaleSliderRoot,
			offsetYSliderRoot, offsetXSliderRoot, dynamicCardTickboxRoot, resetButtonRoot);
	}

	private static ColorRect CreateLine(string name)
	{
		ColorRect line = new()
		{
			Name = name,
			CustomMinimumSize = new Vector2(0f, 4f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.909804f, 0.862745f, 0.745098f, 0.25098f)
		};
		line.Set("layout_mode", 2);
		return line;
	}

	private static HBoxContainer CreateRow(string name)
	{
		HBoxContainer section = new()
		{
			Name = name,
			CustomMinimumSize = new Vector2(0f, RowMinHeight),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		section.Set("layout_mode", 2);
		section.AddThemeConstantOverride("separation", (int)RowSeparation);
		return section;
	}

	private static RichTextLabel CreateLabel(RichTextLabel? templateLabel, string text)
	{
		RichTextLabel label = new()
		{
			Name = "Label",
			BbcodeEnabled = true,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Text = text,
			VerticalAlignment = VerticalAlignment.Center,
			CustomMinimumSize = new Vector2(LabelWidth, LabelMinHeight),
			SizeFlagsHorizontal = Control.SizeFlags.Fill
		};
		label.Set("layout_mode", 2);
		if (templateLabel != null)
		{
			label.Theme = templateLabel.Theme;
			label.AddThemeFontOverride("normal_font", templateLabel.GetThemeFont("normal_font"));
			label.AddThemeFontOverride("bold_font", templateLabel.GetThemeFont("bold_font"));
			label.AddThemeFontSizeOverride("normal_font_size", ScaleFontSize(templateLabel.GetThemeFontSize("normal_font_size")));
			label.AddThemeFontSizeOverride("bold_font_size", ScaleFontSize(templateLabel.GetThemeFontSize("bold_font_size")));
		}
		return label;
	}

	private static Control CreateResetButton(RichTextLabel? templateLabel)
	{
		NButton button = new()
		{
			Name = ResetButtonName,
			CustomMinimumSize = new Vector2(ResetButtonWidth, ResetButtonHeight),
			FocusMode = Control.FocusModeEnum.All
		};
		button.Set("layout_mode", 2);
		button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
		button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

		Texture2D? texture = ResourceLoader.Load<Texture2D>("res://images/ui/reward_screen/reward_skip_button.png");
		if (texture != null)
		{
			TextureRect bg = new()
			{
				Name = "Image",
				Texture = texture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			bg.Set("layout_mode", 2);
			bg.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
			button.AddChild(bg);
		}

		MegaLabel label = new()
		{
			Name = "Label",
			Text = "重置立绘",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.Set("layout_mode", 2);
		label.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
		label.MaxFontSize = ScaleFontSize(28);
		if (templateLabel != null)
		{
			label.Theme = templateLabel.Theme;
			label.AddThemeFontOverride("font", templateLabel.GetThemeFont("normal_font"));
			label.AddThemeFontSizeOverride("font_size", ScaleFontSize(templateLabel.GetThemeFontSize("normal_font_size")));
		}
		button.AddChild(label);

		return button;
	}

	private static int ScaleFontSize(int baseSize)
	{
		return Math.Max(1, (int)Math.Round(baseSize * UiFontScale));
	}

	private static void EnsureTickboxSection(
		VBoxContainer vbox,
		RichTextLabel? templateLabel,
		PackedScene tickboxScene,
		string lineName,
		string sectionName,
		string tickboxName,
		string labelText)
	{
		if (vbox.GetNodeOrNull(sectionName) != null)
			return;

		vbox.AddChild(CreateLine(lineName));
		HBoxContainer section = CreateRow(sectionName);
		section.AddChild(CreateLabel(templateLabel, labelText));
		Control tickbox = tickboxScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		PrepareInlineControl(tickbox, tickboxName, TickboxMinWidth, expand: false);
		TryHideInlineLabel(tickbox);
		PrepareInlineTickboxLayout(tickbox);
		section.AddChild(tickbox);
		vbox.AddChild(section);
	}

	private static void EnsureSliderSection(
		VBoxContainer vbox,
		RichTextLabel? templateLabel,
		PackedScene sliderScene,
		string lineName,
		string sectionName,
		string sliderName,
		string labelText)
	{
		if (vbox.GetNodeOrNull(sectionName) != null)
			return;

		vbox.AddChild(CreateLine(lineName));
		HBoxContainer section = CreateRow(sectionName);
		section.AddChild(CreateLabel(templateLabel, labelText));
		Control slider = sliderScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		PrepareInlineControl(slider, sliderName, SliderMinWidth, expand: true);
		TryHideInlineLabel(slider);
		PrepareInlineSliderLayout(slider);
		section.AddChild(slider);
		vbox.AddChild(section);
	}

	private static void EnsureResetSection(VBoxContainer vbox, RichTextLabel? templateLabel)
	{
		if (vbox.GetNodeOrNull(ResetSectionName) != null)
			return;

		vbox.AddChild(CreateLine(ResetLineName));
		HBoxContainer section = CreateRow(ResetSectionName);
		section.AddChild(CreateLabel(templateLabel, "重置立绘"));
		section.AddChild(CreateResetButton(templateLabel));
		vbox.AddChild(section);
	}

	private static bool EnsureModSettingsTabAndPanel(NSettingsScreen screen, out NSettingsPanel? panel, string source)
	{
		panel = null;
		NSettingsTabManager? tabManager = screen.GetNodeOrNull<NSettingsTabManager>(SettingsTabManagerPath) ??
			screen.GetNodeOrNull<NSettingsTabManager>("SettingsTabManager");
		if (tabManager == null)
		{
			Log.Warn("[MeiLinMod] Settings inject skipped (" + source + "): missing SettingsTabManager");
			return false;
		}

		Control? clipper = screen.GetNodeOrNull<Control>(ClipperPath);
		if (clipper == null)
		{
			Log.Warn("[MeiLinMod] Settings inject skipped (" + source + "): missing Clipper");
			return false;
		}

		NSettingsTab? tab = tabManager.GetNodeOrNull<NSettingsTab>(ModTabName);
		NSettingsPanel? settingsPanel = clipper.GetNodeOrNull<NSettingsPanel>(ModPanelName);

		if (settingsPanel == null)
		{
			NSettingsPanel? templatePanel = screen.GetNodeOrNull<NSettingsPanel>("%SoundSettings") ??
				screen.GetNodeOrNull<NSettingsPanel>(ClipperPath + "/SoundSettings");
			if (templatePanel == null)
			{
				Log.Warn("[MeiLinMod] Settings inject skipped (" + source + "): missing SoundSettings template panel");
				return false;
			}

			try
			{
				settingsPanel = (NSettingsPanel)templatePanel.Duplicate();
			}
			catch (Exception ex)
			{
				Log.Warn("[MeiLinMod] Duplicate settings panel failed (" + source + "): " + ex.Message);
				return false;
			}

			settingsPanel.Name = ModPanelName;
			settingsPanel.UniqueNameInOwner = true;
			settingsPanel.Visible = false;

			VBoxContainer? vbox = settingsPanel.GetNodeOrNull<VBoxContainer>("VBoxContainer");
			if (vbox == null)
			{
				Log.Warn("[MeiLinMod] Settings inject skipped (" + source + "): duplicated panel missing VBoxContainer");
				return false;
			}

			foreach (Node child in vbox.GetChildren())
			{
				vbox.RemoveChild(child);
				child.QueueFree();
			}

			clipper.AddChild(settingsPanel);
		}

		if (tab == null)
		{
			PackedScene? tabScene = ResourceLoader.Load<PackedScene>(ModTabScenePath);
			if (tabScene == null)
			{
				Log.Warn("[MeiLinMod] Settings inject skipped (" + source + "): missing tab scene");
				return false;
			}

			try
			{
				tab = tabScene.Instantiate<NSettingsTab>(PackedScene.GenEditState.Disabled);
			}
			catch (Exception ex)
			{
				Log.Warn("[MeiLinMod] Instantiate tab failed (" + source + "): " + ex.Message);
				return false;
			}

			tab.Name = ModTabName;
			tab.UniqueNameInOwner = true;
			int rightIconIndex = -1;
			Node? rightIcon = tabManager.GetNodeOrNull("RightTriggerIcon");
			if (rightIcon != null)
				rightIconIndex = rightIcon.GetIndex();

			tabManager.AddChild(tab);
			if (rightIconIndex >= 0)
				tabManager.MoveChild(tab, rightIconIndex);

			tab.Set("layout_mode", 2);
			Callable.From(() =>
			{
				if (!GodotObject.IsInstanceValid(tab) || !tab.IsNodeReady())
					return;

				tab.SetLabel("卡厄思mod");
			}).CallDeferred();
		}
		else
		{
			Node? rightIcon = tabManager.GetNodeOrNull("RightTriggerIcon");
			if (rightIcon != null)
			{
				int rightIconIndex = rightIcon.GetIndex();
				if (tab.GetIndex() > rightIconIndex)
					tabManager.MoveChild(tab, rightIconIndex);
			}

			tab.Set("layout_mode", 2);
		}

		panel = settingsPanel;
		EnsureTabBinding(tabManager, tab, settingsPanel);
		return true;
	}

	private static void ConfigurePanelLayout(NSettingsScreen screen, Control panel, VBoxContainer vbox)
	{
		vbox.CustomMinimumSize = Vector2.Zero;
		panel.CustomMinimumSize = Vector2.Zero;
		vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		vbox.AddThemeConstantOverride("separation", 10);

		ScrollContainer? scrollContainer = screen.GetNodeOrNull<ScrollContainer>(ScrollContainerPath);
		if (scrollContainer == null)
			return;

		scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		scrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
		VScrollBar? scrollBar = scrollContainer.GetVScrollBar();
		if (scrollBar != null)
		{
			scrollBar.CustomMinimumSize = new Vector2(ScrollBarWidth, 0f);
			scrollBar.Show();
		}

		scrollContainer.QueueSort();
	}

	private static void PrepareInlineControl(Control control, string name, float minWidth, bool expand)
	{
		control.Name = name;
		control.Set("layout_mode", 2);
		control.FocusMode = Control.FocusModeEnum.All;
		control.MouseFilter = Control.MouseFilterEnum.Stop;
		control.CustomMinimumSize = new Vector2(minWidth, 0f);
		control.SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkEnd;
		control.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
	}

	private static void TryHideInlineLabel(Control root)
	{
		Control? inlineLabel = root.GetNodeOrNull<Control>("Label");
		if (inlineLabel == null)
			return;

		inlineLabel.Visible = false;
		inlineLabel.CustomMinimumSize = Vector2.Zero;
	}

	private static void PrepareInlineSliderLayout(Control sliderRoot)
	{
		sliderRoot.CustomMinimumSize = new Vector2(SliderMinWidth, RowMinHeight);

		Control? slider = sliderRoot.GetNodeOrNull<Control>("Slider");
		if (slider != null)
		{
			slider.Set("layout_mode", 1);
			slider.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
			slider.AnchorLeft = 0f;
			slider.AnchorTop = 0f;
			slider.AnchorRight = 1f;
			slider.AnchorBottom = 1f;
			slider.OffsetLeft = 0f;
			slider.OffsetTop = 0f;
			slider.OffsetRight = -(SliderValueWidth + SliderValueGap);
			slider.OffsetBottom = 0f;
			slider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			slider.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		}

		Label? valueLabel = sliderRoot.GetNodeOrNull<Label>("SliderValue");
		if (valueLabel != null)
		{
			valueLabel.Set("layout_mode", 1);
			valueLabel.AnchorLeft = 1f;
			valueLabel.AnchorRight = 1f;
			valueLabel.AnchorTop = 0.5f;
			valueLabel.AnchorBottom = 0.5f;
			valueLabel.OffsetLeft = -SliderValueWidth;
			valueLabel.OffsetTop = -32f;
			valueLabel.OffsetRight = 0f;
			valueLabel.OffsetBottom = 32f;
			valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
			valueLabel.VerticalAlignment = VerticalAlignment.Center;
			valueLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
			ApplyScaledValueLabelStyle(valueLabel);
		}
	}

	private static void PrepareInlineTickboxLayout(Control tickboxRoot)
	{
		tickboxRoot.CustomMinimumSize = new Vector2(TickboxMinWidth, RowMinHeight);

		Control? visuals = tickboxRoot.GetNodeOrNull<Control>("TickboxVisuals");
		if (visuals != null)
		{
			visuals.Set("layout_mode", 1);
			visuals.MouseFilter = Control.MouseFilterEnum.Stop;
			visuals.AnchorLeft = 0.5f;
			visuals.AnchorTop = 0.5f;
			visuals.AnchorRight = 0.5f;
			visuals.AnchorBottom = 0.5f;
			visuals.OffsetLeft = -32f;
			visuals.OffsetTop = -32f;
			visuals.OffsetRight = 32f;
			visuals.OffsetBottom = 32f;
		}

		Control? ticked = tickboxRoot.GetNodeOrNull<Control>(TickboxTickedPath);
		if (ticked != null)
			ticked.MouseFilter = Control.MouseFilterEnum.Ignore;

		Control? notTicked = tickboxRoot.GetNodeOrNull<Control>(TickboxNotTickedPath);
		if (notTicked != null)
			notTicked.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	private static void EnsureTabBinding(NSettingsTabManager tabManager, NSettingsTab tab, NSettingsPanel panel)
	{
		try
		{
			var field = AccessTools.Field(typeof(NSettingsTabManager), "_tabs");
			if (field?.GetValue(tabManager) is not Dictionary<NSettingsTab, NSettingsPanel> dict)
				return;

			dict[tab] = panel;

			Callable callable = Callable.From<NButton>(_ => SwitchTabTo(tabManager, tab));
			if (!tab.IsConnected(NClickableControl.SignalName.Released, callable))
				tab.Connect(NClickableControl.SignalName.Released, callable);
		}
		catch
		{
		}
	}

	private static void SwitchTabTo(NSettingsTabManager tabManager, NSettingsTab tab)
	{
		try
		{
			var method = AccessTools.Method(typeof(NSettingsTabManager), "SwitchTabTo");
			method?.Invoke(tabManager, new object[] { tab });
		}
		catch
		{
		}
	}

	private static void WireVoiceSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
			return;

		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WireVoiceSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();

			return;
		}

		WireVoiceSlider(sliderRoot);
	}

	private static void WireVoiceSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		Label? valueLabel = GetSliderValueLabel(sliderRoot);
		NSelectionReticle? reticle = sliderRoot.GetNodeOrNull<NSelectionReticle>("SelectionReticle");

		if (!sliderRoot.HasMeta(ControlWiredMeta))
		{
			sliderRoot.SetMeta(ControlWiredMeta, true);
			slider.MinValue = 0.0;
			slider.MaxValue = 100.0;
			slider.Step = 5.0;
			slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
			{
				SetSliderValueText(valueLabel, $"{value}%");
				MeiLinSharedSettings.SetVoiceVolume((float)value * 0.01f, persist: false);
			}));
			slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
			{
				if (valueChanged)
					MeiLinSharedSettings.SetVoiceVolume(MeiLinSharedSettings.VoiceVolume, persist: true);
			}));
			sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
			{
				if (input.IsActionPressed(MegaInput.left))
					slider.Value -= 5.0;
				if (input.IsActionPressed(MegaInput.right))
					slider.Value += 5.0;
			}));
			WireFocusReticle(sliderRoot, reticle);
		}

		SetVoiceSliderValue(sliderRoot, MeiLinSharedSettings.VoiceVolume);
	}

	private static void WireActionVfxTickboxWhenReady(Control tickboxRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(tickboxRoot))
			return;
		if (!tickboxRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WireActionVfxTickboxWhenReady(tickboxRoot, source, attempt + 1)).CallDeferred();
			return;
		}

		WireActionVfxTickbox(tickboxRoot);
	}

	private static void WireActionVfxTickbox(Control tickboxRoot)
	{
		ApplySettingsTickboxState(tickboxRoot, MeiLinSharedSettings.CombatEffectsEnabled);
		if (tickboxRoot.HasMeta(ControlWiredMeta))
			return;

		tickboxRoot.SetMeta(ControlWiredMeta, true);
		tickboxRoot.FocusMode = Control.FocusModeEnum.All;
		WireFocusReticle(tickboxRoot, tickboxRoot.GetNodeOrNull<NSelectionReticle>(TickboxReticlePath));
		if (tickboxRoot is NTickbox tickbox)
		{
			tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(_ =>
			{
				bool enabled = tickbox.IsTicked;
				MeiLinSharedSettings.SetCombatEffectsEnabled(enabled, persist: true);
				ApplySettingsTickboxState(tickboxRoot, enabled);
			}));
			return;
		}

		WireControllerTickboxToggle(tickboxRoot, () =>
		{
			bool enabled = !MeiLinSharedSettings.CombatEffectsEnabled;
			MeiLinSharedSettings.SetCombatEffectsEnabled(enabled, persist: true);
			ApplySettingsTickboxState(tickboxRoot, enabled);
		});
		WireClickableTickboxToggle(tickboxRoot, () =>
		{
			bool enabled = !MeiLinSharedSettings.CombatEffectsEnabled;
			MeiLinSharedSettings.SetCombatEffectsEnabled(enabled, persist: true);
			ApplySettingsTickboxState(tickboxRoot, enabled);
		});
	}

	private static void WirePortraitsTickboxWhenReady(Control tickboxRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(tickboxRoot))
			return;
		if (!tickboxRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WirePortraitsTickboxWhenReady(tickboxRoot, source, attempt + 1)).CallDeferred();
			return;
		}

		WirePortraitsTickbox(tickboxRoot);
	}

	private static void WirePortraitsTickbox(Control tickboxRoot)
	{
		ApplySettingsTickboxState(tickboxRoot, MeiLinSharedSettings.BattleReadyOverlayEnabled);
		if (tickboxRoot.HasMeta(ControlWiredMeta))
			return;

		tickboxRoot.SetMeta(ControlWiredMeta, true);
		tickboxRoot.FocusMode = Control.FocusModeEnum.All;
		WireFocusReticle(tickboxRoot, tickboxRoot.GetNodeOrNull<NSelectionReticle>(TickboxReticlePath));
		if (tickboxRoot is NTickbox tickbox)
		{
			tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(_ =>
			{
				bool enabled = tickbox.IsTicked;
				MeiLinSharedSettings.SetBattleReadyOverlayEnabled(enabled, persist: true);
				ApplySettingsTickboxState(tickboxRoot, enabled);
				if (enabled)
					MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
				else
					MeiLinBattleReadyOverlay.NotifyCombatEnded();
			}));
			return;
		}

		WireControllerTickboxToggle(tickboxRoot, () =>
		{
			bool enabled = !MeiLinSharedSettings.BattleReadyOverlayEnabled;
			MeiLinSharedSettings.SetBattleReadyOverlayEnabled(enabled, persist: true);
			ApplySettingsTickboxState(tickboxRoot, enabled);
			if (enabled)
				MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
			else
				MeiLinBattleReadyOverlay.NotifyCombatEnded();
		});
		WireClickableTickboxToggle(tickboxRoot, () =>
		{
			bool enabled = !MeiLinSharedSettings.BattleReadyOverlayEnabled;
			MeiLinSharedSettings.SetBattleReadyOverlayEnabled(enabled, persist: true);
			ApplySettingsTickboxState(tickboxRoot, enabled);
			if (enabled)
				MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
			else
				MeiLinBattleReadyOverlay.NotifyCombatEnded();
		});
	}

	private static void WireDynamicCardPortraitsTickboxWhenReady(Control tickboxRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(tickboxRoot))
			return;
		if (!tickboxRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WireDynamicCardPortraitsTickboxWhenReady(tickboxRoot, source, attempt + 1)).CallDeferred();
			return;
		}

		WireDynamicCardPortraitsTickbox(tickboxRoot);
	}

	private static void WireDynamicCardPortraitsTickbox(Control tickboxRoot)
	{
		ApplySettingsTickboxState(tickboxRoot, MeiLinSharedSettings.DynamicCardPortraitsEnabled);
		if (tickboxRoot.HasMeta(ControlWiredMeta))
			return;

		tickboxRoot.SetMeta(ControlWiredMeta, true);
		tickboxRoot.FocusMode = Control.FocusModeEnum.All;
		WireFocusReticle(tickboxRoot, tickboxRoot.GetNodeOrNull<NSelectionReticle>(TickboxReticlePath));
		if (tickboxRoot is NTickbox tickbox)
		{
			tickbox.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(_ =>
			{
				bool enabled = tickbox.IsTicked;
				MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(enabled, persist: true);
				ApplySettingsTickboxState(tickboxRoot, enabled);
			}));
			return;
		}

		WireControllerTickboxToggle(tickboxRoot, () =>
		{
			bool enabled = !MeiLinSharedSettings.DynamicCardPortraitsEnabled;
			MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(enabled, persist: true);
			ApplySettingsTickboxState(tickboxRoot, enabled);
		});
		WireClickableTickboxToggle(tickboxRoot, () =>
		{
			bool enabled = !MeiLinSharedSettings.DynamicCardPortraitsEnabled;
			MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(enabled, persist: true);
			ApplySettingsTickboxState(tickboxRoot, enabled);
		});
	}

	private static void WireControllerTickboxToggle(Control tickboxRoot, Action onToggle)
	{
		tickboxRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
		{
			if (input is InputEventMouseButton)
				return;
			if (!input.IsActionReleased(MegaInput.select))
				return;

			onToggle();
			tickboxRoot.AcceptEvent();
		}));
	}

	private static void WireClickableTickboxToggle(Control tickboxRoot, Action onToggle)
	{
		if (tickboxRoot is NClickableControl clickable)
		{
			clickable.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
			{
				if (input is not InputEventMouseButton mouseInput || mouseInput.ButtonIndex != MouseButton.Left)
					return;
			}));
			clickable.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => onToggle()));
			return;
		}

		tickboxRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
		{
			if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
				return;
			onToggle();
			tickboxRoot.AcceptEvent();
		}));

		Control? visuals = tickboxRoot.GetNodeOrNull<Control>("TickboxVisuals");
		if (visuals != null)
		{
			visuals.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
			{
				if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
					return;
				onToggle();
				visuals.AcceptEvent();
			}));
		}
	}

	private static void ApplySettingsTickboxState(Control tickboxRoot, bool enabled)
	{
		Control? ticked = tickboxRoot.GetNodeOrNull<Control>(TickboxTickedPath);
		Control? notTicked = tickboxRoot.GetNodeOrNull<Control>(TickboxNotTickedPath);
		if (ticked == null || notTicked == null)
			return;

		ticked.Visible = enabled;
		notTicked.Visible = !enabled;
	}

	private static void WireScaleSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
			return;
		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WireScaleSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();
			return;
		}
		WireScaleSlider(sliderRoot);
	}

	private static void WireScaleSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		Label? valueLabel = GetSliderValueLabel(sliderRoot);
		NSelectionReticle? reticle = sliderRoot.GetNodeOrNull<NSelectionReticle>("SelectionReticle");

		if (!sliderRoot.HasMeta(ControlWiredMeta))
		{
			sliderRoot.SetMeta(ControlWiredMeta, true);
			slider.MinValue = 50.0;
			slider.MaxValue = 200.0;
			slider.Step = 5.0;
			slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
			{
				SetSliderValueText(valueLabel, $"{value}%");
				MeiLinSharedSettings.SetBattleReadyScale((float)value * 0.01f, persist: false);
				MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
			}));
			slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
			{
				if (valueChanged)
					MeiLinSharedSettings.SetBattleReadyScale(MeiLinSharedSettings.BattleReadyScale, persist: true);
			}));
			sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
			{
				if (input.IsActionPressed(MegaInput.left))
					slider.Value -= 5.0;
				if (input.IsActionPressed(MegaInput.right))
					slider.Value += 5.0;
			}));
			WireFocusReticle(sliderRoot, reticle);
		}

		SetScaleSliderValue(sliderRoot, MeiLinSharedSettings.BattleReadyScale);
	}

	private static void WireOffsetYSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
			return;
		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WireOffsetYSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();
			return;
		}
		WireOffsetYSlider(sliderRoot);
	}

	private static void WireOffsetYSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		Label? valueLabel = GetSliderValueLabel(sliderRoot);
		NSelectionReticle? reticle = sliderRoot.GetNodeOrNull<NSelectionReticle>("SelectionReticle");

		if (!sliderRoot.HasMeta(ControlWiredMeta))
		{
			sliderRoot.SetMeta(ControlWiredMeta, true);
			slider.MinValue = 0.0;
			slider.MaxValue = 800.0;
			slider.Step = 10.0;
			slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
			{
				int display = (int)Math.Round(value - 400.0);
				SetSliderValueText(valueLabel, $"{display:+0;-0;0}px");
				MeiLinSharedSettings.SetBattleReadyOffsetY((float)value - 400f, persist: false);
				MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
			}));
			slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
			{
				if (valueChanged)
					MeiLinSharedSettings.SetBattleReadyOffsetY(MeiLinSharedSettings.BattleReadyOffsetY, persist: true);
			}));
			sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
			{
				if (input.IsActionPressed(MegaInput.left))
					slider.Value -= 10.0;
				if (input.IsActionPressed(MegaInput.right))
					slider.Value += 10.0;
			}));
			WireFocusReticle(sliderRoot, reticle);
		}

		SetOffsetSliderValue(sliderRoot, MeiLinSharedSettings.BattleReadyOffsetY);
	}

	private static void WireOffsetXSliderWhenReady(Control sliderRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(sliderRoot))
			return;
		if (!sliderRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WireOffsetXSliderWhenReady(sliderRoot, source, attempt + 1)).CallDeferred();
			return;
		}
		WireOffsetXSlider(sliderRoot);
	}

	private static void WireOffsetXSlider(Control sliderRoot)
	{
		NSlider slider = sliderRoot.GetNode<NSlider>("Slider");
		Label? valueLabel = GetSliderValueLabel(sliderRoot);
		NSelectionReticle? reticle = sliderRoot.GetNodeOrNull<NSelectionReticle>("SelectionReticle");

		if (!sliderRoot.HasMeta(ControlWiredMeta))
		{
			sliderRoot.SetMeta(ControlWiredMeta, true);
			slider.MinValue = 0.0;
			slider.MaxValue = 800.0;
			slider.Step = 10.0;
			slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From<double>(value =>
			{
				int display = (int)Math.Round(value - 400.0);
				SetSliderValueText(valueLabel, $"{display:+0;-0;0}px");
				MeiLinSharedSettings.SetBattleReadyOffsetX((float)value - 400f, persist: false);
				MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
			}));
			slider.Connect(NSlider.SignalName.MouseReleased, Callable.From<bool>(valueChanged =>
			{
				if (valueChanged)
					MeiLinSharedSettings.SetBattleReadyOffsetX(MeiLinSharedSettings.BattleReadyOffsetX, persist: true);
			}));
			sliderRoot.Connect(Control.SignalName.GuiInput, Callable.From<InputEvent>(input =>
			{
				if (input.IsActionPressed(MegaInput.left))
					slider.Value -= 10.0;
				if (input.IsActionPressed(MegaInput.right))
					slider.Value += 10.0;
			}));
			WireFocusReticle(sliderRoot, reticle);
		}

		SetOffsetSliderValue(sliderRoot, MeiLinSharedSettings.BattleReadyOffsetX);
	}

	private static void WireResetButtonWhenReady(VBoxContainer vbox, Control buttonRoot, string source, int attempt)
	{
		if (!GodotObject.IsInstanceValid(buttonRoot))
			return;
		if (!buttonRoot.IsNodeReady())
		{
			if (attempt < 8)
				Callable.From(() => WireResetButtonWhenReady(vbox, buttonRoot, source, attempt + 1)).CallDeferred();
			return;
		}
		WireResetButton(vbox, buttonRoot);
	}

	private static void WireResetButton(VBoxContainer vbox, Control buttonRoot)
	{
		if (buttonRoot.HasMeta(ControlWiredMeta))
			return;
		if (buttonRoot is not NClickableControl clickable)
			return;

		buttonRoot.SetMeta(ControlWiredMeta, true);
		clickable.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => ResetBattleReadyPosition(vbox)));
	}

	private static void ResetBattleReadyPosition(VBoxContainer vbox)
	{
		MeiLinSharedSettings.SetBattleReadyScale(1f, persist: false);
		MeiLinSharedSettings.SetBattleReadyOffsetX(0f, persist: false);
		MeiLinSharedSettings.SetBattleReadyOffsetY(0f, persist: true);
		MeiLinBattleReadyOverlay.ApplyTransformFromSettings();

		Control? scaleRoot = vbox.GetNodeOrNull<Control>(ScaleSectionName + "/" + ScaleSliderName);
		if (scaleRoot != null)
			SetScaleSliderValue(scaleRoot, 1f);

		Control? offsetXRoot = vbox.GetNodeOrNull<Control>(OffsetXSectionName + "/" + OffsetXSliderName);
		if (offsetXRoot != null)
			SetOffsetSliderValue(offsetXRoot, 0f);

		Control? offsetYRoot = vbox.GetNodeOrNull<Control>(OffsetYSectionName + "/" + OffsetYSliderName);
		if (offsetYRoot != null)
			SetOffsetSliderValue(offsetYRoot, 0f);
	}

	private static void SetScaleSliderValue(Control sliderRoot, float scale)
	{
		if (!sliderRoot.IsNodeReady())
			return;

		NSlider? slider = sliderRoot.GetNodeOrNull<NSlider>("Slider");
		Label? valueLabel = GetSliderValueLabel(sliderRoot);
		if (slider == null || valueLabel == null)
			return;

		double v = Mathf.Clamp(scale * 100f, 50f, 200f);
		slider.SetValueWithoutAnimation(v);
		SetSliderValueText(valueLabel, $"{v}%");
	}

	private static void SetOffsetSliderValue(Control sliderRoot, float offset)
	{
		if (!sliderRoot.IsNodeReady())
			return;

		NSlider? slider = sliderRoot.GetNodeOrNull<NSlider>("Slider");
		Label? valueLabel = GetSliderValueLabel(sliderRoot);
		if (slider == null || valueLabel == null)
			return;

		double v = Mathf.Clamp(offset + 400f, 0f, 800f);
		slider.SetValueWithoutAnimation(v);
		int display = (int)Math.Round(v - 400.0);
		SetSliderValueText(valueLabel, $"{display:+0;-0;0}px");
	}

	private static void SetVoiceSliderValue(Control sliderRoot, float volume)
	{
		if (!sliderRoot.IsNodeReady())
			return;

		NSlider? slider = sliderRoot.GetNodeOrNull<NSlider>("Slider");
		Label? valueLabel = GetSliderValueLabel(sliderRoot);
		if (slider == null || valueLabel == null)
			return;

		double v = Mathf.Clamp(volume * 100f, 0f, 100f);
		slider.SetValueWithoutAnimation(v);
		SetSliderValueText(valueLabel, $"{v}%");
	}

	private static Label? GetSliderValueLabel(Control sliderRoot)
	{
		Label? label = sliderRoot.GetNodeOrNull<Label>("SliderValue");
		ApplyScaledValueLabelStyle(label);
		return label;
	}

	private static void SetSliderValueText(Label? label, string text)
	{
		if (label == null)
			return;

		if (label is MegaLabel megaLabel)
		{
			megaLabel.SetTextAutoSize(text);
			return;
		}

		label.Text = text;
	}

	private static void ApplyScaledValueLabelStyle(Label? label)
	{
		if (label == null)
			return;

		label.CustomMinimumSize = new Vector2(Math.Max(label.CustomMinimumSize.X, 84f), Math.Max(label.CustomMinimumSize.Y, LabelMinHeight));
		if (label is MegaLabel megaLabel)
			megaLabel.MaxFontSize = Math.Max(megaLabel.MaxFontSize, ScaleFontSize(28));
	}

	private static void WireFocusReticle(Control root, NSelectionReticle? reticle)
	{
		if (reticle == null)
			return;

		reticle.Visible = false;
		reticle.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	private static void RefreshFocusNeighbors(params Control?[] controls)
	{
		List<Control> focusables = new();
		foreach (Control? control in controls)
		{
			if (control != null)
				focusables.Add(control);
		}

		for (int i = 0; i < focusables.Count; i++)
		{
			Control current = focusables[i];
			current.FocusNeighborLeft = current.GetPath();
			current.FocusNeighborRight = current.GetPath();
			current.FocusNeighborTop = (i > 0 ? focusables[i - 1] : current).GetPath();
			current.FocusNeighborBottom = (i < focusables.Count - 1 ? focusables[i + 1] : current).GetPath();
		}
	}
}

[HarmonyPatch(typeof(NSettingsScreen), nameof(NSettingsScreen.OnSubmenuOpened))]
public static class MeiLinSharedSettingsUiOpenPatch
{
	[HarmonyPostfix]
	public static void Postfix(NSettingsScreen __instance)
	{
		MeiLinSharedSettingsUiPatch.TryInject(__instance, "OnSubmenuOpened");
	}
}
