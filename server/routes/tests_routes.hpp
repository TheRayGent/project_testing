#pragma once

#include <crow.h>
#include "JSONDatabase.hpp"

void setup_tests_routes(crow::SimpleApp& app, JSONDatabase& users_db, JSONDatabase& tests_db, UnindexedJSONDatabase& results_db);
