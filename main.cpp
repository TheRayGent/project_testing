#include <crow.h>
#include <nlohmann/json.hpp>
#include <picosha2.h>
#include <jwt-cpp/jwt.h>
#include <fstream>
#include <mutex>
#include <string>

using json = nlohmann::json;

const std::string JWT_SECRET = "very_secure_secret_key_2026_xyz";
const std::string PASSWORD_SALT = "some_random_salt_string_123!";
const std::string ISSUER = "project_testing";

class JSONDatabase {
private:
    std::string db_file;
    std::mutex db_mutex;

public:
    explicit JSONDatabase(std::string file_path) : db_file(std::move(file_path)) {}

    json read() {
        std::lock_guard<std::mutex> lock(db_mutex);
        std::ifstream file(db_file);
        json data;

        if (!file.is_open()) {
            data = {{"next_id", 1}, {"data", json::object()}};
            return data; 
        }
        try {
            file >> data;
            if (!data.contains("data")) data["data"] = json::object();
            if (!data.contains("next_id")) data["next_id"] = 1;
        } catch (...) {
            data = {{"next_id", 1}, {"data", json::object()}};
        }
        return data;
    }

    void write(const json& data) {
        std::lock_guard<std::mutex> lock(db_mutex);
        std::ofstream file(db_file);
        if (file.is_open()) {
            file << data.dump(4);
        }
    }
};

JSONDatabase users_db("users_db.json");
JSONDatabase tests_db("tests_db.json");

std::string hash_password(const std::string& password) {
    std::string salted_password = password + PASSWORD_SALT;
    std::string hex_value;
    picosha2::hash256_hex_string(salted_password, hex_value);
    return hex_value;
}

int main() {
    std::setlocale(LC_ALL, "Russian");
    crow::SimpleApp app;

    CROW_ROUTE(app, "/api/users/register").methods(crow::HTTPMethod::Post)([](const crow::request& req) {
        try {
            auto body = json::parse(req.body);
            std::string username = body.at("username");
            std::string password = body.at("password");
            std::string role = body.at("role");
            std::string firstname = body.at("firstname");
            std::string lastname = body.at("lastname");

            if (username.empty() || password.empty()) {
                return crow::response(400, "Логин и пароль не могут быть пустыми");
            }

            if (firstname.empty() || lastname.empty()) {
                return crow::response(400, "Имя и фамилия не могут быть пустыми");
            }

            if (role != "teacher" && role != "student") {
                return crow::response(400, "Недопустимая роль. Разрешено только 'teacher' или 'student'");
            }

            json users_data = users_db.read();

            for (auto& [id, user] : users_data["data"].items()) {
                if (user["username"] == username) {
                    return crow::response(400, "Пользователь с таким логином уже существует");
                }
            }

            int current_id = users_data["next_id"];
            users_data["next_id"] = current_id + 1;

            std::string user_id = std::to_string(current_id);
            users_data["data"][user_id] = {
                {"username", username},
                {"password_hash", hash_password(password)},
                {"role", role},
                {"firstname", firstname},
                {"lastname", lastname}
            };

            users_db.write(users_data);

            auto token = jwt::create()
                .set_issuer(ISSUER)
                .set_type("JWS")
                .set_payload_claim("user_id", jwt::claim(user_id))
                .set_payload_claim("role", jwt::claim(role))
                .set_expires_at(std::chrono::system_clock::now() + std::chrono::hours(24))
                .sign(jwt::algorithm::hs256{JWT_SECRET});

            json response_json = {
                {"message", "Пользователь успешно зарегистрирован"},
                {"token", token}
            };

            crow::response res(201, response_json.dump());
            res.add_header("Content-Type", "application/json");
            return res;
        }
        catch (const std::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }

    });

    CROW_ROUTE(app, "/api/users/login").methods(crow::HTTPMethod::Post)([](const crow::request& req) {
        try {
            auto body = json::parse(req.body);
            std::string username = body.at("username");
            std::string password = body.at("password");
            std::string found_id = "";
            std::string role = "";

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

            auto token = jwt::create()
                .set_issuer(ISSUER)
                .set_type("JWS")
                .set_payload_claim("user_id", jwt::claim(found_id))
                .set_payload_claim("role", jwt::claim(role))
                .set_expires_at(std::chrono::system_clock::now() + std::chrono::hours(24))
                .sign(jwt::algorithm::hs256{JWT_SECRET});

            json response_json = {
                {"message", "Успешный вход в систему"},
                {"token", token}
            };

            crow::response res(200, response_json.dump());
            res.add_header("Content-Type", "application/json");
            return res;
        } 
        catch (...) {
            return crow::response(400, "Ошибка обработки запроса");
        }
    });

    CROW_ROUTE(app, "/api/users/profile").methods(crow::HTTPMethod::Post)([](const crow::request& req) {
        try {
            auto body = json::parse(req.body);
            std::string token = body.at("token");

            if (token.empty()) {
                return crow::response(401, "Ошибка: требуется авторизация");
            }
            
            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            
            verifier.verify(decoded);

            std::string user_id = decoded.get_payload_claim("user_id").as_string();
            
            json users_data = users_db.read();
            
            if (!users_data["data"].contains(user_id)) {
                return crow::response(404, "Пользователь не найден");
            }

            auto user_data = users_data["data"][user_id];

            json profile_info = {
                {"id", user_id},
                {"user", user_data["username"]},
                {"role", user_data["role"]},
                {"firstname", user_data["firstname"]},
                {"lastname", user_data["lastname"]}
            };

            crow::response res(200, profile_info.dump());
            res.add_header("Content-Type", "application/json");
            return res;
        } 
        catch (const std::exception& e) {
            return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
        }
    });

    CROW_ROUTE(app, "/api/tests/create").methods(crow::HTTPMethod::Post)([](const crow::request& req) {
        try {
            auto body = json::parse(req.body);
            std::string token = body.at("token");

            if (token.empty()) {
                return crow::response(401, "Ошибка: требуется авторизация");
            }

            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            
            verifier.verify(decoded);

            std::string role = decoded.get_payload_claim("role").as_string();
            std::string user_id = decoded.get_payload_claim("user_id").as_string();

            if (role != "teacher") {
                return crow::response(403, "Доступ запрещен: создавать тесты могут только преподаватели");
            }

            std::string title = body.at("title");
            std::string description = body.at("description");
            auto questions = body.at("questions");
            auto access = body.at("access");

            if (title.empty() || questions.empty() || !questions.is_array()) {
                return crow::response(400, "Название теста и массив не могут быть пустыми");
            }

            json tests_data = tests_db.read();

            int current_id = tests_data["next_id"];
            tests_data["next_id"] = current_id + 1;

            std::string test_id = std::to_string(current_id);
            tests_data["data"][test_id] = {
                {"title", title},
                {"description", description},
                {"teacher", user_id},
                {"access", access},
                {"questions", questions}
            };

            tests_db.write(tests_data);

            return crow::response(201, "Тест успешно создан");
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
        }
    });

    app.port(5000).multithreaded().run();
}
