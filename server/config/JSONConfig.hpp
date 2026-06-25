#ifndef JSON_CONFIG_HPP
#define JSON_CONFIG_HPP

#include <nlohmann/json.hpp>
#include <iostream>
#include <fstream>
#include <string>

struct JSONConfig {
    const std::string JWT_SECRET;
    const std::string PASSWORD_SALT;
    const std::string ISSUER;
};

JSONConfig load_config();

#endif // JSON_CONFIG_HPP