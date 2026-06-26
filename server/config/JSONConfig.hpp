#pragma once

#include <string>

struct JSONConfig {
    const std::string JWT_SECRET;
    const std::string PASSWORD_SALT;
    const std::string ISSUER;
};

JSONConfig load_config();
