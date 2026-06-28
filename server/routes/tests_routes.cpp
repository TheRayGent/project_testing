#include <nlohmann/json.hpp>
#include <jwt-cpp/jwt.h>
#include "tests_routes.hpp"
#include "utils.hpp"

using json = nlohmann::json;

void setup_tests_routes(crow::SimpleApp& app, JSONDatabase& users_db, JSONDatabase& tests_db, UnindexedJSONDatabase& results_db){
    
    // Создание нового теста
    CROW_ROUTE(app, "/api/tests/create").methods(crow::HTTPMethod::Post)([&](const crow::request& req) {
        try {

            // Попытка чтения токена из тела
            auto body = json::parse(req.body);
            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }
            
            std::string token = body.at("token");
            if (token.empty()) {
                return crow::response(401, "Ошибка: требуется авторизация");
            }

            // Расшифровка токена
            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            try {
                verifier.verify(decoded);
            }
            catch (const std::exception& e) {
                return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
            }

            // Получение role и user_id из токена
            std::string role = decoded.get_payload_claim("role").as_string();
            std::string user_id = decoded.get_payload_claim("user_id").as_string();

            if (role != "teacher") {
                return crow::response(403, "Доступ запрещен: создавать тесты могут только преподаватели");
            }

            // Получение полей теста из токена
            std::string title = body.at("title");
            std::string description = body.at("description");
            auto questions = body.at("questions");

            // Проверка необходимых полей
            if (title.empty()) {
                return crow::response(400, "Название теста не может быть пустым");
            }
            if (!questions.is_array() || questions.empty()) {
                return crow::response(400, "Массив вопросов не может быть пустым");
            }

            for (const auto& q : questions) {
                if (!q.is_object() || !q.contains("text") || !q.contains("options") || !q.contains("correct_option")) {
                    return crow::response(400, "Ошибка структуры вопроса");
                }
                if (q["text"].get<std::string>().empty() || !q["options"].is_array() || q["options"].empty()) {
                    return crow::response(400, "Неверный формат вопроса или вариантов ответа");
                }
                int correct_opt = q["correct_option"].get<int>();
                if (correct_opt < 0 || static_cast<size_t>(correct_opt) >= q["options"].size()) {
                    return crow::response(400, "Неверный индекс правильного ответа");
                }
            }

            // Создание новой записи
            json new_test = {
                {"title", title},
                {"description", description},
                {"teacher", user_id},
                {"questions", questions}
            };

            std::string test_id;

            // Запись нового записи в бд
            tests_db.update([&, new_test = std::move(new_test)](json& tests_data) {
                int current_id = tests_data["next_id"];
                tests_data["next_id"] = current_id + 1;

                test_id = std::to_string(current_id);
                tests_data["data"][test_id] = std::move(new_test);
            });

            // Запись доступа к тесту пользователям
            users_db.update([&](json& users_data) {
                auto& created_tests = users_data["data"][user_id]["created_tests"];
                if (!created_tests.is_array()) {
                    created_tests = json::array();
                }
                created_tests.push_back(test_id);
            });

            // Ответ
            return crow::response(201, "Тест успешно создан");
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });

    // Получение теста по id
    CROW_ROUTE(app, "/api/tests/<string>").methods(crow::HTTPMethod::Post)([&](const crow::request& req, std::string test_id) {
        try {

            // Попытка чтения токена из тела
            auto body = json::parse(req.body);
            
            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }

            std::string token = body.at("token");

            if (token.empty()) {
                return crow::response(401, "Ошибка: требуется авторизация");
            }

            // Расшифровка токена
            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            try {
                verifier.verify(decoded);
            }
            catch (const std::exception& e) {
                return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
            }

            // Получение role и user_id из токена
            std::string role = decoded.get_payload_claim("role").as_string();
            std::string user_id = decoded.get_payload_claim("user_id").as_string();

            // Чтение бд
            json tests_data = tests_db.read();

            if (!tests_data["data"].contains(test_id)) {
                return crow::response(404, "Тест с таким ID не найден");
            }

            // Получение данных теста
            json test = tests_data["data"][test_id];

            if (!test.contains("teacher") || test["teacher"].get<std::string>() != user_id) {
                return crow::response(403, "У вас нет доступа к этому тесту");
            }

            // Создание ответа
            crow::response res(200, test.dump());
            res.add_header("Content-Type", "application/json");
            return res;
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });

    CROW_ROUTE(app, "/api/tests/<string>/give-access").methods(crow::HTTPMethod::Post)([&](const crow::request& req, std::string test_id) {
        try {

            // Попытка чтения токена из тела
            auto body = json::parse(req.body);

            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }
            if (!body.contains("users_ids")) {
                return crow::response(400, "Ошибка: отсутствует поле users_ids в запросе");
            }
            std::string token = body.at("token");
            if (token.empty()) {
                return crow::response(401, "Ошибка: требуется авторизация");
            }

            // Расшифровка токена
            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            try {
                verifier.verify(decoded);
            }
            catch (const std::exception& e) {
                return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
            }

            // Получение role и user_id из токена
            std::string role = decoded.get_payload_claim("role").as_string();
            std::string user_id = decoded.get_payload_claim("user_id").as_string();

            // Чтение бд
            json tests_data = tests_db.read();
            if (!tests_data["data"].contains(test_id)) {
                return crow::response(404, "Тест с таким ID не найден");
            }

            // Получение данных теста
            json test = tests_data["data"][test_id];
            if (!test.contains("teacher") || test["teacher"].get<std::string>() != user_id) {
                return crow::response(403, "У вас нет доступа к этому тесту");
            }

            // Попытка чтения users_ids из тела
            auto users_ids = body.at("users_ids");
            if (!users_ids.is_array() || users_ids.empty()) {
                return crow::response(400, "Массив users_ids не может быть пустым");
            }

            // Запись доступа к тесту пользователям
            users_db.update([&](json& users_data) {
                auto& users_map = users_data["data"];

                for (const auto& u_node : users_ids) {
                    if (!u_node.is_string()) continue;
                    std::string u = u_node.get<std::string>();

                    if (user_id == u || !users_map.contains(u)) continue;

                    auto& user = users_map[u];
                    if (user["role"] == "teacher") continue;
                    auto& available_tests = user["available_tests"];

                    if (!available_tests.is_array()) {
                        available_tests = json::array();
                    }

                    auto it = std::find(available_tests.begin(), available_tests.end(), test_id);
                    
                    if (it == available_tests.end()) {
                        available_tests.push_back(test_id);
                    }
                }
            });

            // Создание ответа
            return crow::response(200, "Доступ к тесту успешно выдан");
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });

    // Сохранение ресультата пользователя о прохождении теста
    CROW_ROUTE(app, "/api/tests/<string>/set_result").methods(crow::HTTPMethod::Post)([&](const crow::request& req, std::string test_id) {
        try {
            // Попытка чтения токена из тела
            auto body = json::parse(req.body);

            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }
            if (!body.contains("result")) {
                return crow::response(400, "Ошибка: отсутствует поле result в запросе");
            }
            std::string token = body.at("token");
            if (token.empty()) {
                return crow::response(401, "Ошибка: требуется авторизация");
            }

            // Расшифровка токена
            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            try {
                verifier.verify(decoded);
            }
            catch (const std::exception& e) {
                return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
            }

            // Получение user_id из токена
            std::string user_id = decoded.get_payload_claim("user_id").as_string();

            std::string result = body.at("result");
            if (result.empty()) {
                return crow::response(400, "Результат не может быть пустым");
            }

            // Проверка что тест существует
            json tests_data = tests_db.read();
            if (!tests_data["data"].contains(test_id)){
                return crow::response(404, "Тест с таким ID не найден");
            }

            // Сохранение результата в бд
            results_db.update([&](json& results_data) {
                if (!results_data.contains(test_id)){
                    results_data[test_id] = json::object();
                }
                results_data[test_id][user_id] = result;
            });

            // Создание ответа
            return crow::response(200, "Результат успешно сохранён");
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });

    // Получение результатов теста
    CROW_ROUTE(app, "/api/tests/<string>/results").methods(crow::HTTPMethod::Post)([&](const crow::request& req, std::string test_id) {
        try {
            // Попытка чтения токена из тела
            auto body = json::parse(req.body);

            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }
            std::string token = body.at("token");
            if (token.empty()) {
                return crow::response(401, "Ошибка: требуется авторизация");
            }

            // Расшифровка токена
            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            try {
                verifier.verify(decoded);
            }
            catch (const std::exception& e) {
                return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
            }

            // Получение user_id из токена
            std::string user_id = decoded.get_payload_claim("user_id").as_string();

            // Чтение бд
            json results_data = results_db.read();
            json tests_data = tests_db.read();

            if (!tests_data["data"].contains(test_id)){
                return crow::response(404, "Тест с таким ID не найден");
            }
            if (!results_data.contains(test_id)){
                return crow::response(404, "Результаты теста с таким ID не найдены!");
            }

            // Получение результатов теста
            json results = results_data[test_id];

            // Создание ответа
            crow::response res(200, results.dump());
            res.add_header("Content-Type", "application/json");
            return res;

            // Создание ответа
            return crow::response(200, "Результат успешно сохранён");
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });
}
