#include <crow.h>
#include <nlohmann/json.hpp>
#include <picosha2.h>
#include <jwt-cpp/jwt.h>
#include <fstream>
#include <mutex>
#include <string>

using json = nlohmann::json;

const std::string USERS_DB = "users_db.json";
const std::string JWT_SECRET = "very_secure_secret_key_2026_xyz";
const std::string PASSWORD_SALT = "some_random_salt_string_123!";
const std::string ISSUER = "project_testing";

std::mutex db_mutex;

std::string hash_password(const std::string& password) {
    std::string salted_password = password + PASSWORD_SALT;
    std::string hex_value;
    picosha2::hash256_hex_string(salted_password, hex_value);
    return hex_value;
}

json load_db() {
    std::lock_guard<std::mutex> lock(db_mutex);
    std::ifstream file(USERS_DB);
    if (!file.is_open()) {
        return json::object(); 
    }
    try {
        json db;
        file >> db;
        return db;
    } catch (...) {
        return json::object();
    }
}

void save_db(const json& db) {
    std::lock_guard<std::mutex> lock(db_mutex);
    std::ofstream file(USERS_DB);
    if (file.is_open()) {
        file << db.dump(4);
    }
}

std::string get_token_from_cookie(const std::string& cookie_header) {
    std::string search_str = "token=";
    size_t pos = cookie_header.find(search_str);
    if (pos == std::string::npos) return "";
    
    size_t start = pos + search_str.length();
    size_t end = cookie_header.find(";", start);
    if (end == std::string::npos) {
        return cookie_header.substr(start);
    }
    return cookie_header.substr(start, end - start);
}

int main() {
    std::setlocale(LC_ALL, "Russian");
    crow::SimpleApp app;

    CROW_ROUTE(app, "/api/register").methods(crow::HTTPMethod::Post)([](const crow::request& req) {
        try {
            auto body = json::parse(req.body);
            std::string username = body.at("username");
            std::string password = body.at("password");
            std::string role = body.at("role");

            if (username.empty() || password.empty()) {
                return crow::response(400, "Логин и пароль не могут быть пустыми");
            }

            if (role != "teacher" && role != "student") {
                return crow::response(400, "Недопустимая роль. Разрешено только 'teacher' или 'student'");
            }

            json db = load_db();

            if (db.contains(username)) {
                return crow::response(400, "Пользователь с таким логином уже существует");
            }

            db[username] = {
                {"password_hash", hash_password(password)},
                {"role", role}
            };

            save_db(db);

            return crow::response(201, "Пользователь успешно зарегистрирован");
        } 
        catch (const std::exception& e) {
            return crow::response(400, "Неверный формат запроса JSON");
        }
    });

    CROW_ROUTE(app, "/api/login").methods(crow::HTTPMethod::Post)([](const crow::request& req) {
        try {
            auto body = json::parse(req.body);
            std::string username = body.at("username");
            std::string password = body.at("password");

            json db = load_db();

            if (!db.contains(username) || db[username]["password_hash"] != hash_password(password)) {
                return crow::response(401, "Неверный логин или пароль");
            }

            std::string role = db[username]["role"];

            auto token = jwt::create()
                .set_issuer(ISSUER)
                .set_type("JWS")
                .set_payload_claim("username", jwt::claim(username))
                .set_payload_claim("role", jwt::claim(role))
                .set_expires_at(std::chrono::system_clock::now() + std::chrono::hours(168))
                .sign(jwt::algorithm::hs256{JWT_SECRET});

            crow::response res(200, "Успешный вход в систему");
            res.add_header("Set-Cookie", "token=" + token + "; Path=/; HttpOnly; SameSite=Lax");
            return res;
        } 
        catch (...) {
            return crow::response(400, "Ошибка обработки запроса");
        }
    });

    CROW_ROUTE(app, "/api/profile")([](const crow::request& req) {
        std::string cookie_header = req.get_header_value("Cookie");
        std::string token = get_token_from_cookie(cookie_header);

        if (token.empty()) {
            return crow::response(401, "Ошибка: требуется авторизация");
        }

        try {
            auto decoded = jwt::decode(token);
            auto verifier = jwt::verify()
                .allow_algorithm(jwt::algorithm::hs256{JWT_SECRET})
                .with_issuer(ISSUER);
            
            verifier.verify(decoded);

            std::string username = decoded.get_payload_claim("username").as_string();
            std::string role = decoded.get_payload_claim("role").as_string();
            
            json profile_info = {
                {"message", "Добро пожаловать в личный кабинет"},
                {"user", username},
                {"role", role}
            };
            return crow::response(200, profile_info.dump());
        } 
        catch (const std::exception& e) {
            return crow::response(401, "Сессия устарела или токен поврежден. Войдите заново.");
        }
    });

    app.port(5000).multithreaded().run();
}
