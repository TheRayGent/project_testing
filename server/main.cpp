#include <crow.h>
#include "utils/Routes.hpp"

int main() {
    crow::SimpleApp app;

    setup_crow_routes(app);

    app.port(5000).multithreaded().run();
}
