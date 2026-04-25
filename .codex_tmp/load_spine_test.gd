extends SceneTree

func _init() -> void:
	var paths = [
		"res://MeiLinMod/scenes/cards/attack_defense_unity_dynamic.tscn",
		"res://MeiLinMod/scenes/cards/huo_long_jing_tian_dynamic.tscn",
		"res://MeiLinMod/scenes/cards/sheng_long_jiao_dynamic.tscn",
	]
	for path in paths:
		var scene = load(path)
		print("scene=", path, " loaded=", scene != null)
		if scene == null:
			continue
		var root = scene.instantiate()
		var spine = root.get_node_or_null("SubViewport/SpineSprite")
		print("  spine node=", spine != null)
		if spine != null:
			print("  skeleton_data_res=", spine.skeleton_data_res)
		root.free()
	quit()
