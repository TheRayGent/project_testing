#pragma once

#include <nlohmann/json.hpp>
#include <fstream>
#include <shared_mutex>
#include <string>
#include <functional>

using json = nlohmann::json;

class JSONDatabase {
protected:
    std::string db_file;
    mutable std::shared_mutex db_mutex;
    json cache_data;

    virtual void load_from_disk();

    void save_to_disk_internal() const; 

public:
    explicit JSONDatabase(std::string file_path);
    virtual ~JSONDatabase() = default;
    
    void init();
    
    json read() const;

    void update(std::function<void(json&)> transform_func);
};

class UnindexedJSONDatabase : public JSONDatabase {
protected:
    void load_from_disk() override;

public:
    explicit UnindexedJSONDatabase(std::string file_path);
};