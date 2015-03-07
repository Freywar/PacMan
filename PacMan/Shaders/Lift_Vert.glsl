uniform vec4 meshColor;

varying vec4 oposition;
varying vec3 position;
varying vec3 normal;
varying vec4 color;
varying float dr;

varying float r;
uniform float floorY;
uniform float totalTimeElapsed;
uniform float useYOpacity;

float DR(float y, float r)
{

	y -= r * 2.0 * totalTimeElapsed / 10.0;
	return (
		sin(y * 4.0 ) +
		sin(y * 8.0 ) +
		sin(y * 16.0) +
		sin(y * 32.0 )
		) / 16.0 ;
}

void main(void)
{
	oposition = gl_Vertex;
	dr = DR(gl_Vertex.y+floorY, r=length(gl_Vertex.xz));
	position = vec3(gl_ModelViewMatrix * (gl_Vertex* vec4(1.0+dr, 1.0, 1.0+dr,1.0))) ;
	normal = normalize(gl_NormalMatrix * gl_Normal);
	color = meshColor;
	gl_Position = gl_ModelViewProjectionMatrix * (gl_Vertex* vec4(1.0+dr, 1.0, 1.0+dr,1.0));
}