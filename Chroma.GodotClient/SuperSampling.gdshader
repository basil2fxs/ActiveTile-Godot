shader_type canvas_item;

// We want to sample a 4x4 block, but since it's symmetric, we only need the coefficients for one quadrant.
// The locations are in UV and already take into account the texture size.
const uint SAMPLE_COUNT = 4u;
uniform float[SAMPLE_COUNT] SAMPLE_COEFFICIENTS;
uniform vec2[SAMPLE_COUNT] SAMPLE_LOCATIONS;

//void vertex() {
	// Called for every vertex the material is visible on.
//}

void fragment() {
	vec4 total = vec4(0.0);
	for (float qx = -1.0; qx < 1.5; qx += 2.0) {
		for (float qy = -1.0; qy < 1.5; qy += 2.0) {
			for (uint i = 0u; i < SAMPLE_COUNT; ++i) {
				vec2 location = UV + SAMPLE_LOCATIONS[i] * vec2(qx, qy);
				total += SAMPLE_COEFFICIENTS[i] * texture(TEXTURE, location);
			}
		}
	}
	COLOR = total;
	//COLOR = SAMPLE_COEFFICIENTS[0] * texture(TEXTURE, UV);
	//COLOR.r = SAMPLE_LOCATIONS[0].x;
	//COLOR.g = SAMPLE_LOCATIONS[0].y;
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
