#ifndef ROUTES_HPP
#define ROUTES_HPP

#include <crow.h>

std::string hash_password(const std::string& password);

void setup_crow_routes(crow::SimpleApp& app);

#endif // ROUTES_HPP
