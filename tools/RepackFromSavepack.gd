extends SceneTree

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() < 2:
		push_error("Usage: RepackFromSavepack.gd <savepack_log> <output_pck>")
		quit(1)
		return

	var log_path := ProjectSettings.globalize_path(args[0])
	var output_pck := ProjectSettings.globalize_path(args[1])
	var file := FileAccess.open(log_path, FileAccess.READ)
	if file == null:
		push_error("Failed to open log: %s" % log_path)
		quit(2)
		return

	var seen := {}
	var targets: Array[String] = []
	while not file.eof_reached():
		var line := file.get_line()
		var res_path := ""
		if line.begins_with("res://"):
			res_path = line
		else:
			var marker := "保存文件：res://"
			var marker_index := line.find(marker)
			if marker_index == -1:
				continue

			res_path = "res://" + line.substr(marker_index + marker.length())
			var ansi_index := res_path.find(String.chr(27))
			if ansi_index != -1:
				res_path = res_path.substr(0, ansi_index)
		res_path = res_path.strip_edges()
		if res_path.is_empty() or seen.has(res_path):
			continue

		seen[res_path] = true
		targets.append(res_path)

	file.close()

	var packer := PCKPacker.new()
	var start_err := packer.pck_start(output_pck)
	if start_err != OK:
		push_error("Failed to start pck %s: %s" % [output_pck, error_string(start_err)])
		quit(3)
		return

	var packed_count := 0
	var skipped_count := 0
	for res_path in targets:
		var source_path := ProjectSettings.globalize_path(res_path)
		if not FileAccess.file_exists(source_path):
			push_warning("Skipping missing source file: %s" % source_path)
			skipped_count += 1
			continue

		var add_err := packer.add_file(res_path, source_path)
		if add_err != OK:
			push_error("Failed to add %s: %s" % [res_path, error_string(add_err)])
			quit(4)
			return
		packed_count += 1

	var flush_err := packer.flush()
	if flush_err != OK:
		push_error("Failed to flush pck %s: %s" % [output_pck, error_string(flush_err)])
		quit(5)
		return

	print("Packed %d files into %s (skipped %d missing files)" % [packed_count, output_pck, skipped_count])
	quit(0)
