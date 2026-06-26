#include "users_routes.hpp"
#include "utils.hpp"
#include <nlohmann/json.hpp>
#include <jwt-cpp/jwt.h>

using json = nlohmann::json;

void setup_users_routes(crow::SimpleApp& app, JSONDatabase& users_db, JSONDatabase& tests_db){
    // Создание аккаунта
    CROW_ROUTE(app, "/api/users/register").methods(crow::HTTPMethod::Post)([&](const crow::request& req) {
        try {
            // Чтение полей из тела
            auto body = json::parse(req.body);
            std::string username = body.at("username");
            std::string password = body.at("password");
            std::string role = body.at("role");
            std::string firstname = body.at("firstname");
            std::string lastname = body.at("lastname");

            // Проверка необходимых полей
            if (username.empty() || password.empty()) {
                return crow::response(400, "Логин и пароль не могут быть пустыми");
            }
            if (firstname.empty() || lastname.empty()) {
                return crow::response(400, "Имя и фамилия не могут быть пустыми");
            }
            if (role != "teacher" && role != "student") {
                return crow::response(400, "Недопустимая роль. Разрешено только 'teacher' или 'student'");
            }

            // Новая запись в бд
            bool is_success = false;
            std::string user_id = "";
            std::string password_hash = hash_password(password);

            users_db.update([&](json& users_data) {
                for (auto& [id, user] : users_data["data"].items()) {
                    if (user["username"] == username) {
                        return; // Пользователь с таким логином уже существует
                    }
                }

                int current_id = users_data["next_id"];
                users_data["next_id"] = current_id + 1;
                user_id = std::to_string(current_id);

                users_data["data"][user_id] = {
                    {"username", username},
                    {"password_hash", password_hash},
                    {"role", role},
                    {"firstname", firstname},
                    {"lastname", lastname},
                    {"available_tests", json::array()},
                    {"created_tests", json::array()}
                };

                is_success = true;
            });

            if (!is_success) {
                return crow::response(400, "Пользователь с таким логином уже существует");
            }

            // Создание токена
            std::string token = jwt::create()
                    .set_issuer(ISSUER)
                    .set_type("JWS")
                    .set_payload_claim("user_id", jwt::claim(user_id))
                    .set_payload_claim("role", jwt::claim(role))
                    .set_expires_at(std::chrono::system_clock::now() + std::chrono::hours(24))
                    .sign(jwt::algorithm::hs256{JWT_SECRET});

            // Создание ответа
            json response_json = {
                {"message", "Пользователь успешно зарегистрирован"},
                {"token", token}
            };

            crow::response res(201, response_json.dump());
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

    // Вход в аккаунт
    CROW_ROUTE(app, "/api/users/login").methods(crow::HTTPMethod::Post)([&](const crow::request& req) {
        try {
            // Чтение полей из тела
            auto body = json::parse(req.body);
            std::string username = body.at("username");
            std::string password = body.at("password");
            std::string found_id = "";
            std::string role = "";

            // Чтение бд
            json users_data = users_db.read();
            
            for (auto& [id, user] : users_data["data"].items()) {
                if (user["username"] == username) {
                    if (user["password_hash"] == hash_password(password)) {
                        found_id = id;
                        role = user["role"];
                        break;
                    }
                }
            }

            if (found_id.empty()) {
                return crow::response(401, "Неверный логин или пароль");
            }

            // Создание токена
            auto token = jwt::create()
                .set_issuer(ISSUER)
                .set_type("JWS")
                .set_payload_claim("user_id", jwt::claim(found_id))
                .set_payload_claim("role", jwt::claim(role))
                .set_expires_at(std::chrono::system_clock::now() + std::chrono::hours(24))
                .sign(jwt::algorithm::hs256{JWT_SECRET});

            // Создание ответа
            json response_json = {
                {"message", "Успешный вход в систему"},
                {"token", token}
            };

            crow::response res(200, response_json.dump());
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

    // Получение подробной информации пользователя
    CROW_ROUTE(app, "/api/users/profile").methods(crow::HTTPMethod::Post)([&](const crow::request& req) {
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

            std::string user_id = decoded.get_payload_claim("user_id").as_string(); // Получение user_id из токена
            
            // Чтение бд
            json users_data = users_db.read();
            
            if (!users_data["data"].contains(user_id)) {
                return crow::response(404, "Пользователь не найден");
            }

            auto user_data = users_data["data"][user_id];

            // Создание ответа
            json profile_info = {
                {"id", user_id},
                {"user", user_data["username"]},
                {"role", user_data["role"]},
                {"firstname", user_data["firstname"]},
                {"lastname", user_data["lastname"]},
                {"available_tests", user_data["available_tests"]},
                {"created_tests", user_data["created_tests"]}
            };

            crow::response res(200, profile_info.dump());
            res.add_header("Content-Type", "application/json");
            return res;
        } 
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const jwt::error::token_verification_error& e) {
            return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });
}
