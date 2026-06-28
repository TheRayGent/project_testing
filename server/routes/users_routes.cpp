#include "users_routes.hpp"
#include "utils.hpp"
#include <nlohmann/json.hpp>
#include <jwt-cpp/jwt.h>

using json = nlohmann::json;

crow::response get_user_profile_info(const crow::request& req, JSONDatabase& users_db, std::string target_user_id){
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
    
    // Получение user_id
    std::string user_id;
    if (target_user_id == ""){
        user_id = decoded.get_payload_claim("user_id").as_string();
    }
    else{
        user_id = target_user_id;
    }
    
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
        {"group", user_data["group"]},
        {"available_tests", user_data["available_tests"]},
        {"created_tests", user_data["created_tests"]}
    };

    crow::response res(200, profile_info.dump());
    res.add_header("Content-Type", "application/json");
    return res;
}

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
            std::string group;

            // Проверка необходимых полей
            if (username.empty() || password.empty()) {
                return crow::response(400, "Логин и пароль не могут быть пустыми");
            }
            if (firstname.empty() || lastname.empty()) {
                return crow::response(400, "Имя и фамилия не могут быть пустыми");
            }
            if (role != "teacher" && role != "student") {
                return crow::response(400, "Недопустимая роль. Разрешено только \"teacher\" или \"student\"");
            }

            if (role == "teacher") {
                group = "";
            }
            else if (!body.contains("group")){
                return crow::response(400, "Ошибка: отсутствует поле group в запросе");
            }
            else {
                group = body.at("group");
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
                    {"group", group},
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

    // Получение информации о пользователе
    CROW_ROUTE(app, "/api/users/profile").methods(crow::HTTPMethod::Post)([&](const crow::request& req) {
        try {
            // Повторный код вынесен в отдельную функцию
            crow::response res = get_user_profile_info(req, users_db, "");
            return res;
        } 
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });

    // Получение данных пользователя по id
    CROW_ROUTE(app, "/api/users/<string>").methods(crow::HTTPMethod::Post)([&](const crow::request& req, std::string target_user_id) {
        try {
            // Повторный код вынесен в отдельную функцию
            crow::response res = get_user_profile_info(req, users_db, target_user_id);
            return res;
        } 
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });

    // Получение списка пользователей по роли
    CROW_ROUTE(app, "/api/users/list/<string>").methods(crow::HTTPMethod::Post)([&](const crow::request& req, std::string target_role) {
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
            
            // Чтение бд
            json users_data = users_db.read()["data"];

            // Получение массива информации о пользователях
            json users_profile_info = json::object();
            users_profile_info["data"] = json::array();
            
            for (auto& item : users_data.items()) {
                json user_data = item.value();
                if (user_data["role"] != target_role) continue;
                std::string user_id = item.key();

                json profile_info = {
                    {"id", user_id},
                    {"user", user_data["username"]},
                    {"firstname", user_data["firstname"]},
                    {"lastname", user_data["lastname"]}
                };

                users_profile_info["data"].push_back(profile_info);
            }
            
            // Создание ответа
            crow::response res(200, users_profile_info.dump());
            res.add_header("Content-Type", "application/json");
            return res;
        } 
        catch (const json::exception& e) {
            std::cout << e.what() << std::endl;
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });

    // Получение списка студентов по группе
    CROW_ROUTE(app, "/api/users/list/student/<string>").methods(crow::HTTPMethod::Post)([&](const crow::request& req, std::string target_group) {
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
            
            // Чтение бд
            json users_data = users_db.read()["data"];

            // Получение массива информации о пользователях
            json users_profile_info = json::object();
            users_profile_info["data"] = json::array();
            
            for (auto& item : users_data.items()) {
                json user_data = item.value();
                if (user_data["role"] != "student") continue;
                if (user_data["group"] != target_group) continue;
                std::string user_id = item.key();

                json profile_info = {
                    {"id", user_id},
                    {"user", user_data["username"]},
                    {"firstname", user_data["firstname"]},
                    {"lastname", user_data["lastname"]}
                };

                users_profile_info["data"].push_back(profile_info);
            }
            
            // Создание ответа
            crow::response res(200, users_profile_info.dump());
            res.add_header("Content-Type", "application/json");
            return res;
        } 
        catch (const json::exception& e) {
            std::cout << e.what() << std::endl;
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: ") + e.what());
        }
    });
}
