@tool
extends EditorInspectorPlugin

const SPEED_PROPERTY := &"_fei_spine_preview_speed"
const CURVE_PROPERTY := &"_fei_spine_preview_curve"

const PRESETS := {
    "Constant": "res://addons/spine_speed_inspector/presets/constant.tres",
    "Ease In": "res://addons/spine_speed_inspector/presets/ease_in.tres",
    "Ease Out": "res://addons/spine_speed_inspector/presets/ease_out.tres",
    "Mid Fast": "res://addons/spine_speed_inspector/presets/middle_fast.tres",
    "Mid Slow": "res://addons/spine_speed_inspector/presets/middle_slow.tres",
}

func _can_handle(object: Object) -> bool:
    return is_instance_valid(object) and object.get_class() == "SpineSprite"

func _parse_begin(spine_sprite: Object) -> void:
    var container := VBoxContainer.new()
    container.add_theme_constant_override("separation", 8)

    var title := Label.new()
    title.text = "Spine Speed Control"
    title.add_theme_color_override("font_color", Color(0.7, 0.9, 1.0))
    container.add_child(title)

    # Speed slider
    var row := HBoxContainer.new()
    row.add_theme_constant_override("separation", 8)

    var label := Label.new()
    label.text = "Speed"
    label.custom_minimum_size = Vector2(48, 0)
    label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER

    var slider := HSlider.new()
    slider.min_value = 0.0
    slider.max_value = 5.0
    slider.step = 0.05
    slider.value = _get_speed(spine_sprite)
    slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL

    var value_label := Label.new()
    value_label.text = "%.2f" % slider.value
    value_label.custom_minimum_size = Vector2(40, 0)
    value_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER

    row.add_child(label)
    row.add_child(slider)
    row.add_child(value_label)
    container.add_child(row)

    slider.value_changed.connect(func(v: float) -> void:
        value_label.text = "%.2f" % v
        _set_speed(spine_sprite, v)
    )

    # Curve section
    var curve_label := Label.new()
    curve_label.text = "Speed Curve (X=进度, Y=倍率)"
    curve_label.add_theme_color_override("font_color", Color(0.75, 0.75, 0.75))
    container.add_child(curve_label)

    var curve_res := _load_curve(spine_sprite)
    var current_path := ""
    if spine_sprite.has_meta(CURVE_PROPERTY):
        current_path = spine_sprite.get_meta(CURVE_PROPERTY)

    # Curve preview (clickable to edit)
    var preview := TextureRect.new()
    preview.custom_minimum_size = Vector2(0, 64)
    preview.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_COVERED
    preview.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
    preview.tooltip_text = "Click to edit curve"
    container.add_child(preview)

    _update_preview(preview, curve_res)

    # Resource path label
    var path_label := Label.new()
    path_label.text = _format_path(current_path)
    path_label.add_theme_color_override("font_color", Color(0.6, 0.6, 0.6))
    path_label.add_theme_font_size_override("font_size", 12)
    container.add_child(path_label)

    # Curve action buttons
    var btn_row := HBoxContainer.new()
    btn_row.add_theme_constant_override("separation", 4)

    var select_btn := Button.new()
    select_btn.text = "Select"
    select_btn.tooltip_text = "Choose an existing Curve resource"
    btn_row.add_child(select_btn)

    var new_btn := Button.new()
    new_btn.text = "New"
    new_btn.tooltip_text = "Create a new Curve resource next to this scene"
    btn_row.add_child(new_btn)

    var edit_btn := Button.new()
    edit_btn.text = "Edit"
    edit_btn.tooltip_text = "Edit the current Curve in the inspector"
    edit_btn.disabled = curve_res == null
    btn_row.add_child(edit_btn)

    container.add_child(btn_row)

    # File dialog for Select
    var file_dialog := EditorFileDialog.new()
    file_dialog.file_mode = EditorFileDialog.FILE_MODE_OPEN_FILE
    file_dialog.add_filter("*.tres", "Curve Resource")
    file_dialog.title = "Select Curve Resource"
    container.add_child(file_dialog)

    file_dialog.file_selected.connect(func(path: String) -> void:
        var loaded := load(path) as Curve
        if loaded:
            _set_curve(spine_sprite, preview, path_label, edit_btn, path)
    )

    select_btn.pressed.connect(func() -> void:
        file_dialog.popup_file_dialog()
    )

    new_btn.pressed.connect(func() -> void:
        var new_path := _suggest_curve_path(spine_sprite)
        if new_path.is_empty():
            push_warning("Could not determine a save path for the new curve.")
            return

        var new_curve := Curve.new()
        new_curve.min_value = 0.0
        new_curve.max_value = 2.0
        new_curve.add_point(Vector2(0, 1))
        new_curve.add_point(Vector2(1, 1))

        var err := ResourceSaver.save(new_curve, new_path)
        if err == OK:
            _set_curve(spine_sprite, preview, path_label, edit_btn, new_path)
            EditorInterface.edit_resource(load(new_path))
        else:
            push_warning("Failed to save new curve: " + new_path)
    )

    edit_btn.pressed.connect(func() -> void:
        var curve := _load_curve(spine_sprite)
        if curve:
            EditorInterface.edit_resource(curve)
    )

    preview.gui_input.connect(func(event: InputEvent) -> void:
        if event is InputEventMouseButton:
            if event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
                var curve := _load_curve(spine_sprite)
                if curve:
                    EditorInterface.edit_resource(curve)
    )

    # Preset buttons
    var preset_label := Label.new()
    preset_label.text = "Presets"
    preset_label.add_theme_color_override("font_color", Color(0.75, 0.75, 0.75))
    container.add_child(preset_label)

    var preset_row := HBoxContainer.new()
    preset_row.add_theme_constant_override("separation", 4)

    for preset_name in PRESETS.keys():
        var btn := Button.new()
        btn.text = preset_name
        btn.pressed.connect(func() -> void:
            var path := PRESETS[preset_name] as String
            _set_curve(spine_sprite, preview, path_label, edit_btn, path)
        )
        preset_row.add_child(btn)

    container.add_child(preset_row)

    add_custom_control(container)

func _set_curve(spine_sprite: Object, preview: TextureRect, path_label: Label, edit_btn: Button, path: String) -> void:
    if not is_instance_valid(spine_sprite):
        return

    spine_sprite.set_meta(CURVE_PROPERTY, path)
    path_label.text = _format_path(path)

    var curve := load(path) as Curve
    _update_preview(preview, curve)
    edit_btn.disabled = curve == null

func _format_path(path: String) -> String:
    if path.is_empty():
        return "<none>"
    if path.begins_with("res://"):
        return path.substr(6)
    return path

func _suggest_curve_path(spine_sprite: Object) -> String:
    var scene_path: String = spine_sprite.get_scene_file_path()
    if scene_path.is_empty():
        return ""

    var dir: String = scene_path.get_base_dir()
    var file_name: String = scene_path.get_file().get_basename()
    return dir + "/" + file_name + "_speed_curve.tres"

func _update_preview(texture_rect: TextureRect, curve: Curve) -> void:
    if not curve:
        texture_rect.texture = null
        return

    var tex := CurveTexture.new()
    tex.width = 256
    tex.curve = curve
    texture_rect.texture = tex

func _set_speed(spine_sprite: Object, speed: float) -> void:
    if not is_instance_valid(spine_sprite):
        return
    spine_sprite.set_meta(SPEED_PROPERTY, speed)
    var state = spine_sprite.get_animation_state()
    if state:
        state.set_time_scale(speed)

func _get_speed(spine_sprite: Object) -> float:
    if spine_sprite.has_meta(SPEED_PROPERTY):
        return spine_sprite.get_meta(SPEED_PROPERTY)
    return 1.0

func _load_curve(spine_sprite: Object) -> Curve:
    if not spine_sprite.has_meta(CURVE_PROPERTY):
        return null
    var path := spine_sprite.get_meta(CURVE_PROPERTY) as String
    if path.is_empty():
        return null
    return load(path) as Curve
