extends Node

signal frame_available(index: int, texture: Texture2D)

const OPENING_FRAMES := 3
const PLAYBACK_FRAMES := 4
const MAX_REQUESTS := 2
static var pending_paths: Dictionary = {}

var paths := PackedStringArray()
var textures: Dictionary = {}
var failed: Dictionary = {}
var first := 0
var limit := 0
var pending := -1
var complete := false
var keep_open := Callable()
var next_check := 0

func warm_opening(timeline: String, allowed: Callable) -> void:
    var data = JSON.parse_string(FileAccess.get_file_as_string(timeline))
    if data is Dictionary and data.has("cutin"):
        keep_open = allowed
        configure(data.cutin.frames)
        seek(0, OPENING_FRAMES)
        _process(0)

func configure(frames: Array) -> void:
    for frame in frames:
        paths.append(str(frame.path))

func seek(index: int, width: int) -> void:
    var end := mini(index + width, paths.size())
    if first == index and limit == end:
        return
    first = index
    limit = end
    complete = false
    for key in textures.keys():
        if key < first or key >= limit:
            textures.erase(key)
    set_process(true)

func get_frame(index: int) -> Texture2D:
    return textures.get(index)

func _process(_delta: float) -> void:
    if keep_open.is_valid() and Time.get_ticks_msec() >= next_check:
        next_check = Time.get_ticks_msec() + 200
        if not keep_open.call():
            close()
            queue_free()
            return
    if complete:
        return
    if pending >= 0:
        var path := paths[pending]
        var status := ResourceLoader.load_threaded_get_status(path)
        if status == ResourceLoader.THREAD_LOAD_IN_PROGRESS:
            return
        var index := pending
        pending = -1
        var tex: Texture2D
        if status != ResourceLoader.THREAD_LOAD_INVALID_RESOURCE:
            tex = ResourceLoader.load_threaded_get(path) as Texture2D
        pending_paths.erase(path)
        if tex == null:
            failed[index] = true
        else:
            if index >= first and index < limit:
                textures[index] = tex
            frame_available.emit(index, tex)
    for index in range(first, limit):
        if textures.has(index) or failed.has(index):
            continue
        var path := paths[index]
        if pending_paths.has(path):
            return
        var cached := ResourceLoader.get_cached_ref(path) as Texture2D
        if cached != null:
            textures[index] = cached
            frame_available.emit(index, cached)
            return
        if pending_paths.size() >= MAX_REQUESTS:
            return
        if ResourceLoader.load_threaded_request(path, "Texture2D") == OK:
            pending = index
            pending_paths[path] = true
        else:
            failed[index] = true
        return
    # A room prefix keeps only its settings guard; a full playback window sleeps until seek.
    complete = true
    set_process(keep_open.is_valid())

func close() -> void:
    set_process(false)
    if pending >= 0:
        var path := paths[pending]
        pending = -1
        _drain(path, get_tree())
    keep_open = Callable()
    textures.clear()
    failed.clear()
    paths.clear()
    first = 0
    limit = 0
    complete = true

func _exit_tree() -> void:
    close()

# Godot has no cancellation API. Balance the request after it finishes without
# blocking room teardown or retaining the stage, its sprite, or its textures.
static func _drain(path: String, tree: SceneTree) -> void:
    while is_instance_valid(tree):
        var status := ResourceLoader.load_threaded_get_status(path)
        if status == ResourceLoader.THREAD_LOAD_IN_PROGRESS:
            await tree.process_frame
        else:
            if status != ResourceLoader.THREAD_LOAD_INVALID_RESOURCE:
                ResourceLoader.load_threaded_get(path)
            break
    pending_paths.erase(path)
