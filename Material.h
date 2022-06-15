#ifndef MATERIAL_H
#define MATERIAL_H

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>

class Material {
public:
	glm::vec3 k_d;
	glm::vec3 k_s;
	float n;

	Material(glm::vec3 k_d, glm::vec3 k_s, float n) :
		k_d(k_d),
		k_s(k_s),
		n(n) {}
};
#endif