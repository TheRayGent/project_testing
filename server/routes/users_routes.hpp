#pragma once

#include <crow.h>
#include "JSONDatabase.hpp"

void setup_users_routes(crow::SimpleApp& app, JSONDatabase& users_db, JSONDatabase& tests_db);
