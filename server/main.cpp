#include <crow.h>
#include "utils/users_routes.hpp"
#include "utils/tests_routes.hpp"

int main() {
    crow::SimpleApp app;

    setup_users_routes(app);
    setup_tests_routes(app);

    app.port(5000).multithreaded().run();
}
