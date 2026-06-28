#include "JSONConfig.hpp"
#include <nlohmann/json.hpp>
#include <iostream>
#include <fstream>

using json = nlohmann::json;

JSONConfig load_config()
{
    std::setlocale(LC_ALL, "Russian");
    std::ifstream file("config.json");
    json data;
    if (!file.is_open()){
        std::cout << "[WARNING] Не удалось открыть файл config.json, используются значения по умолчанию.\n";
    }
    else {
        file >> data;
    }

    std::string jwt_secret;
    std::string password_salt;
    std::string issuer;

    if (!data.contains("JWT_SECRET")) {
        std::cout << "[WARNING] Ключ JWT_SECRET не найден, используется значение по умолчанию.\n";
        jwt_secret = "very_secure_secret_key_2026_xyz";
    }
    else {
        jwt_secret = data["JWT_SECRET"].get<std::string>();
    }
    if (!data.contains("PASSWORD_SALT")) {
        std::cout << "[WARNING]  Ключ PASSWORD_SALT не найден, используется значение по умолчанию.\n";
        password_salt = "some_random_salt_string_123!";
    }
    else {
        password_salt = data["PASSWORD_SALT"].get<std::string>();
    }
    if (!data.contains("ISSUER")) {
        std::cout << "[WARNING] Ключ ISSUER не найден, используется значение по умолчанию.\n";
        issuer = "project_testing";
    }
    else {
        issuer = data["ISSUER"].get<std::string>();
    }

    return JSONConfig{
        std::move(jwt_secret), 
        std::move(password_salt), 
        std::move(issuer)
    };
}