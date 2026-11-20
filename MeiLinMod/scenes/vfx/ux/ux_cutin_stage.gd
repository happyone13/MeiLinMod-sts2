extends "../ug/ug_stage.gd"

const CutinWindow := preload("cutin_window.gd")
var opening: CutinWindow
var window: CutinWindow
var keep_cutin := Callable()
var frame_ends := PackedFloat64Array()
var first_draw := false
var wanted := 0
var displayed := -1

func prepare_cutin() -> void:
    if not config.has("cutin"):
        return
    var duration := 0.0
    for frame in config.cutin.frames:
        duration += float(frame.duration)
        frame_ends.append(duration)
    if bool(config.cutin.get("before_timeline", false)):
        cutin_lead_in = duration
    cutin_sprite = Sprite2D.new()
    cutin_sprite.name = "UxVideo"
    cutin_sprite.z_index = 3000
    cutin_sprite.visible = false
    $Stage.add_child(cutin_sprite)
    window = CutinWindow.new()
    add_child(window)
    window.configure(config.cutin.frames)
    if is_instance_valid(opening):
        for i in range(CutinWindow.OPENING_FRAMES):
            var tex := opening.get_frame(i)
            if tex != null:
                window.textures[i] = tex
    window.seek(0, CutinWindow.PLAYBACK_FRAMES)
    window.frame_available.connect(_on_frame_available)
    window._process(0)

func _on_frame_available(index: int, texture: Texture2D) -> void:
    if (first_draw and index == 0) or (running and index <= wanted and index > displayed):
        displayed = index
        cutin_sprite.texture = texture
        cutin_sprite.visible = true

func _show_frame(index: int) -> void:
    var texture := window.get_frame(index)
    if texture != null:
        displayed = index
        cutin_sprite.texture = texture
    # Keep the last drawable frame when IO falls behind; the gameplay clock never waits.
    cutin_sprite.visible = cutin_sprite.texture != null

func prepare_first_draw() -> void:
    super()
    if window != null:
        first_draw = true
        _show_frame(0)

func reset_first_draw() -> void:
    first_draw = false
    super()

func update_cutin(clock: float) -> void:
    if window == null:
        return
    if keep_cutin.is_valid() and not keep_cutin.call():
        cancel_cutin()
        return
    var local_time := clock - float(config.cutin.at)
    if local_time < 0.0:
        cutin_sprite.visible = false
        return
    var index := frame_ends.bsearch(local_time, false)
    if index >= frame_ends.size():
        cancel_cutin()
        return
    wanted = index
    window.seek(index, CutinWindow.PLAYBACK_FRAMES)
    _show_frame(index)

func cancel_cutin() -> void:
    first_draw = false
    opening = null
    keep_cutin = Callable()
    if is_instance_valid(window):
        window.close()
        if window.is_inside_tree():
            window.queue_free()
        window = null
    if is_instance_valid(cutin_sprite):
        cutin_sprite.texture = null
        cutin_sprite.visible = false

func finish() -> void:
    cancel_cutin()
    super()

func _exit_tree() -> void:
    cancel_cutin()
    super()
