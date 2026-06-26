#include <picosha2.h>
#include <iostream>
#include "JSONConfig.hpp"
#include "utils.hpp"

// Получения данных из конфига
const JSONConfig CONFIG = load_config();

const std::string JWT_SECRET = CONFIG.JWT_SECRET;
const std::string PASSWORD_SALT = CONFIG.PASSWORD_SALT;
const std::string ISSUER = CONFIG.ISSUER;

// Функция хеширования пароля
std::string hash_password(const std::string& password) {
    std::string salted_password = password + PASSWORD_SALT;
    std::string hex_value;
    picosha2::hash256_hex_string(salted_password, hex_value);
    return hex_value;
}