#include <crow.h>
#include <nlohmann/json.hpp>
#include <picosha2.h>
#include <jwt-cpp/jwt.h>
#include <fstream>
#include <shared_mutex>
#include <string>
#include <functional>

using json = nlohmann::json;

const std::string JWT_SECRET = "very_secure_secret_key_2026_xyz";
const std::string PASSWORD_SALT = "some_random_salt_string_123!";
const std::string ISSUER = "project_testing";

class JSONDatabase {
private:
    std::string db_file;
    mutable std::shared_mutex db_mutex;
    json cache_data;

    void load_from_disk() {
        std::ifstream file(db_file);
        if (file.is_open() && file.peek() != std::ifstream::traits_type::eof()) {
            try {
                file >> cache_data;

                if (!cache_data.contains("data")) cache_data["data"] = json::object();
                if (!cache_data.contains("next_id")) cache_data["next_id"] = 1;

                return;
            }
            catch (const json::parse_error& e) {
                cache_data = {{"next_id", 1}, {"data", json::object()}};
            }
        } else {
            cache_data = {{"next_id", 1}, {"data", json::object()}};
        }
    }

    void save_to_disk_internal() const {
        std::ofstream file(db_file);
        file << cache_data.dump(4);
    }

public:
    explicit JSONDatabase(std::string file_path) : db_file(std::move(file_path)) {
        load_from_disk();
    }

    json read() const {
        std::shared_lock<std::shared_mutex> lock(db_mutex);
        return cache_data;
    }

    void update(std::function<void(json&)> transform_func) {
        std::unique_lock<std::shared_mutex> lock(db_mutex);
        transform_func(cache_data);
        save_to_disk_internal();
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

            bool is_success = false;
            std::string user_id = "";
            std::string password_hash = hash_password(password);

            users_db.update([&](json& users_data) {
                for (auto& [id, user] : users_data["data"].items()) {
                    if (user["username"] == username) {
                        return;
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
                    {"lastname", lastname}
                };

                is_success = true;
            });

            if (!is_success) {
                return crow::response(400, "Пользователь с таким логином уже существует");
            }

            std::string token = jwt::create()
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
            
            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }
            
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
            
            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }
            
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

            json new_test = {
                {"title", title},
                {"description", description},
                {"teacher", user_id},
                {"access", access},
                {"questions", questions}
            };

            tests_db.update([&, new_test = std::move(new_test)](json& tests_data) {
                int current_id = tests_data["next_id"];
                tests_data["next_id"] = current_id + 1;

                std::string test_id = std::to_string(current_id);
                tests_data["data"][test_id] = std::move(new_test);
            });

            return crow::response(201, "Тест успешно создан");
        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
        }
    });

    CROW_ROUTE(app, "/api/tests/<string>").methods(crow::HTTPMethod::Post)([](const crow::request& req, std::string test_id) {
        try {
            auto body = json::parse(req.body);
            
            if (!body.contains("token")) {
                return crow::response(400, "Ошибка: отсутствует поле token в запросе");
            }

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

            json tests_data = tests_db.read();

            if (!tests_data["data"].contains(test_id)) {
                return crow::response(404, "Тест с таким ID не найден");
            }

            json test = tests_data["data"][test_id];

            if (!test.contains("teacher") || test["teacher"].get<std::string>() != user_id) {
                return crow::response(403, "У вас нет доступа к этому тесту");
            }

            crow::response res(200, test.dump());
            res.add_header("Content-Type", "application/json");
            return res;

        }
        catch (const json::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
        catch (const std::exception& e) {
            return crow::response(500, std::string("Внутренняя ошибка сервера: "));
        }
    });

    app.port(5000).multithreaded().run();
}
