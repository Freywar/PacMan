varying vec4 oposition;
varying vec3 position;
varying vec3 normal;
varying vec4 color;
varying float r;
varying float dr;
uniform float floorY;
uniform float useYOpacity;
uniform float totalTimeElapsed;
float DR(float y, float r)
{

	y -= 2.0*r*2.0 * totalTimeElapsed / 10.0;
	return (
		sin(y * 4.0 ) +
		sin(y * 8.0 ) +
		sin(y * 16.0) +
		sin(y * 32.0 )
		) / 16.0 ;
}

void main(void)
{
	vec4 result = color;
	result = clamp(result * DR(oposition.y+floorY, r) * 10.0 - useYOpacity * (0.5+oposition.y), 0.0, 1.0);
	gl_FragColor = result;
}