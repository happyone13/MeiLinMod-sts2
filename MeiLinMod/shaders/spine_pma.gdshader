shader_type canvas_item;
render_mode blend_premul_alpha, unshaded;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	float tint_alpha = COLOR.a;
	if (tex.a > 0.0001) {
		tint_alpha = clamp(COLOR.a / tex.a, 0.0, 1.0);
	}
	COLOR.rgb *= tint_alpha;
}
