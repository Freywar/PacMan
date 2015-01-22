varying vec3 position;
varying vec3 normal;
varying vec4 color;

void main(void)
{
	vec3 lightDir = normalize(gl_LightSource[0].position.xyz - position);
	vec4 result = color * (gl_LightSource[0].ambient + gl_LightSource[0].diffuse * max(dot(normal, lightDir), 0.0));
	result = clamp(result, 0.0, 1.0);
	gl_FragColor = result;
}