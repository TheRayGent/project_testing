#include <nlohmann/json.hpp>
#include <jwt-cpp/jwt.h>
#include "tests_routes.hpp"
#include "utils.hpp"

using json = nlohmann::json;

void setup_tests_routes(crow::SimpleApp& app, JSONDatabase& users_db, JSONDatabase& tests_db){
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
            
            verifier.verify(decoded);

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

            // Запись нового записи в бд
            tests_db.update([&, new_test = std::move(new_test)](json& tests_data) {
                int current_id = tests_data["next_id"];
                tests_data["next_id"] = current_id + 1;

                std::string test_id = std::to_string(current_id);
                tests_data["data"][test_id] = std::move(new_test);
            });

            // Ответ
            return crow::response(201, "Тест успешно создан");
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
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
            
            verifier.verify(decoded);

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
}
