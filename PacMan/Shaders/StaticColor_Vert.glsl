uniform vec4 meshColor;

varying vec3 position;
varying vec3 normal;
varying vec4 color;

void main(void)
{
	position = vec3(gl_ModelViewMatrix * gl_Vertex);
	normal = normalize(gl_NormalMatrix * gl_Normal);
	color = meshColor;
	gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
}