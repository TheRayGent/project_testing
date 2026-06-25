#pragma once

extern const std::string JWT_SECRET;
extern const std::string PASSWORD_SALT;
extern const std::string ISSUER;

std::string hash_password(const std::string& password);
