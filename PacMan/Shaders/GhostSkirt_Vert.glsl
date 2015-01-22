uniform vec4 meshColor;
uniform float radius;
uniform float yStep;
uniform float delta;
uniform float totalTimeElapsed;

varying vec3 position;
varying vec3 normal;
varying vec4 color;

float DR(float y, float beta)
{
	if (y <= 0.0)
		return 0.0;

	y = 4.0 * y * y * 0.2;

	return (
		cos(beta * 4.0 + totalTimeElapsed) +
		cos(beta * 8.0 + totalTimeElapsed * 2.0) +
		cos(beta * 16.0 + totalTimeElapsed * 4.0) +
		cos(beta * 32.0 + totalTimeElapsed * 8.0)
		) * y / 4.0 + delta*y;
}

void main(void)
{
	float prevDr = DR(gl_Vertex.y - yStep, gl_Vertex.x);
	float dr = DR(gl_Vertex.y, gl_Vertex.x);
	float alpha = atan(yStep, dr - DR(gl_Vertex.y - yStep, gl_Vertex.x));

	float c = cos(gl_Vertex.x), s = sin(gl_Vertex.x), as = sin(alpha);
	float sr = (radius + dr);
	vec4 cylVert = vec4(c * sr, -gl_Vertex.y, s * sr, 1.0);
	vec3 cylNormal = vec3(c * as, cos(alpha), s * as);

	position = vec3(gl_ModelViewMatrix * cylVert);
	normal = normalize(gl_NormalMatrix * cylNormal);
	color = meshColor;
	gl_Position = gl_ModelViewProjectionMatrix * cylVert;
}