#version 330 core
#define NUM_SPHERES	7
#define NUM_PLANES	5
#define NUM_LIGHTS	1
#define MAX_BOUNCE 5
#define SAMPLE_PER_PIXEL 1
#define PI        3.14159265358979323

out vec4 FragColor;
in vec3 pixelPos;

struct Ray{
	vec3 pos;
	vec3 dir;
};

struct Material{
	vec3 k_d;
	vec3 k_s;
	float n;
};

struct HitInfo{
	float t;
	vec3 position;
	vec3 normal;
	Material mtl;
	bool frontFace;
};

struct Sphere{
	vec3 center;
	float radius;
	Material mtl;
};

struct Plane{
	vec3 normal;
	vec3 position;
	float lenght;
	Material mtl;
};

struct Light{
	vec3 position;
	vec3 intensity;
};

uniform Sphere spheres[NUM_SPHERES];
uniform Plane planes[NUM_SPHERES];
uniform Light lights[NUM_LIGHTS];
uniform mat4 c2w;
uniform float view_pixel_width;												//width of viewport pixel
uniform float view_pixel_height;		
uniform vec2 randomVector;

Ray GeneratePrimaryRay();
bool IntersectRay(inout HitInfo hit,Ray ray);
vec3 Shade(vec3 position, vec3 normal, vec3 view, Material mtl);

vec3 cameraPos;

void main(){
	cameraPos = (c2w*vec4(0.0f, 0.0f, 0.0f,1.0f)).xyz;
	vec3 color = vec3(0.0f,0.0f,0.0f);
		
	Ray ray;
	ray.pos = pixelPos;
	ray.dir = normalize(ray.pos - cameraPos);

	HitInfo hit;
	if(IntersectRay(hit, ray)){
		vec3 view = -ray.dir;
		color += Shade(hit.position, hit.normal, view, hit.mtl);
																	
		vec3 ks = hit.mtl.k_s;												//reflection
		for(int i = 0 ; i < MAX_BOUNCE ; i++){		
			Ray refRay;
			refRay.pos = hit.position + 1e-2 * hit.normal;
			refRay.dir = 2*dot(view,hit.normal)*hit.normal - view;			//perfect reflection direction
			HitInfo refHit;
			if(IntersectRay(refHit, refRay)){
				view = -refRay.dir;
				color += ks * Shade(refHit.position, refHit.normal, view, refHit.mtl);
				hit = refHit;
				ks *= hit.mtl.k_s;
			}else{
				//float t = 0.5*(ray.dir.y + 1.0);							//sky
				//color += ks*((1.0-t)*vec3(1.0, 1.0, 1.0) + t*vec3(0.5, 0.7, 1.0));
				break;
			}
		}
	}	
	//else{																	//sky
		//float t = 0.5*(ray.dir.y + 1.0);
		//color = (1.0-t)*vec3(1.0, 1.0, 1.0) + t*vec3(0.5, 0.7, 1.0);
	//}

	FragColor = vec4(color, 1.0f);
}


bool IntersectRay(inout HitInfo hit,Ray ray){
	hit.t = 1e30;
	bool foundHit = false;
	for(int i = 0 ; i < NUM_SPHERES ; i++){									
		vec3 center = spheres[i].center;
		vec3 tmp = ray.pos - center;
		float a = dot(ray.dir, ray.dir);
		float b = 2 * dot(ray.dir,tmp);
		float c = dot(tmp, tmp) - spheres[i].radius*spheres[i].radius;
		float delta = b*b - 4*a*c;
		if(delta >= 0.0f){
			float t;
			if(length(ray.pos - center) < spheres[i].radius){				//ray origin is inside the sphere																	
				t = (-b + sqrt(delta))/ 2.0 * a; 
			}
			else{
				t = (-b - sqrt(delta))/ 2.0 * a; 
			}
			if( t < hit.t && t > 0.0f){
				hit.t = t;
				hit.position = ray.pos + t*ray.dir;
				hit.normal = normalize(hit.position - center);
				hit.frontFace = dot(ray.dir,hit.normal) < 0.0f;
				hit.normal = hit.frontFace ? hit.normal : -hit.normal;
				hit.mtl = spheres[i].mtl;
				foundHit = true;
			}
		}
	}
	for(int i = 0 ; i < NUM_PLANES ; i++){									
		float denominator = dot(ray.dir, planes[i].normal);
		if(denominator != 0.0f){											//plane and ray are not perpendicular			
			float c = dot(planes[i].normal, planes[i].position);
			float t = (c - dot(ray.pos, planes[i].normal)) / denominator;
			vec3 positionOnPlane = ray.pos + t*ray.dir;
			vec3 distance = positionOnPlane - planes[i].position;
			if(abs(distance.x) < planes[i].lenght && abs(distance.y) < planes[i].lenght && abs(distance.z) < planes[i].lenght){
				if( t < hit.t && t > 0.0f ){
					hit.t = t;
					hit.position = positionOnPlane;
					hit.normal = planes[i].normal;
					hit.frontFace = dot(ray.dir,planes[i].normal) < 0.0f;
					hit.normal = hit.frontFace ? hit.normal : -hit.normal;
					hit.mtl = planes[i].mtl;
					foundHit = true;
				}
			}
		}
	}
	return foundHit;
}

vec3 Shade(vec3 position, vec3 normal, vec3 view, Material mtl){
	vec3 color = vec3(0,0,0);
	for(int i = 0 ; i < NUM_LIGHTS ; i++){									
		Ray shadowRay;
		shadowRay.pos = position + 1e-3 * normal;
		shadowRay.dir = normalize(lights[i].position - position);

		HitInfo hit;
		if(!IntersectRay(hit, shadowRay)){
			vec3 lightDir = normalize(lights[i].position - position);
			vec3 h = normalize(lightDir + view);
			float geometry = dot(normal, lightDir) ;
			float cosfi = dot(normal, h);
			color += geometry > 0.0f ? lights[i].intensity*(geometry*mtl.k_d + mtl.k_s* (cosfi > 0.0f ? pow(cosfi,mtl.n) : 0.0)) : vec3(0.0f,0.0f,0.0f);
		}
	}
	return color;
}