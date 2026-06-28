#include <crow.h>
#include "routes/users_routes.hpp"
#include "routes/tests_routes.hpp"
#include "database/JSONDatabase.hpp"

int main() {
    crow::SimpleApp app;

    // Инициализация бд
    JSONDatabase users_db("users_db.json");
    JSONDatabase tests_db("tests_db.json");
    UnindexedJSONDatabase results_db("results_db.json");
    users_db.init();
    tests_db.init();
    results_db.init();

    // Создание роутов
    setup_users_routes(app, users_db, tests_db);
    setup_tests_routes(app, users_db, tests_db, results_db);

    // Запуск сервера
    app.port(5000).multithreaded().run();
}
