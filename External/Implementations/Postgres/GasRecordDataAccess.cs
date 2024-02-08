﻿using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using Npgsql;
using System.Text.Json;

namespace CarCareTracker.External.Implementations
{
    public class PGGasRecordDataAccess: IGasRecordDataAccess
    {
        private NpgsqlConnection pgDataSource;
        private readonly ILogger<PGGasRecordDataAccess> _logger;
        private static string tableName = "gasrecords";
        public PGGasRecordDataAccess(IConfiguration config, ILogger<PGGasRecordDataAccess> logger)
        {
            pgDataSource = new NpgsqlConnection(config["POSTGRES_CONNECTION"]);
            _logger = logger;
            try
            {
                pgDataSource.Open();
                //create table if not exist.
                string initCMD = $"CREATE TABLE IF NOT EXISTS app.{tableName} (id INT GENERATED ALWAYS AS IDENTITY primary key, vehicleId INT not null, data jsonb not null)";
                using (var ctext = new NpgsqlCommand(initCMD, pgDataSource))
                {
                    ctext.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        public List<GasRecord> GetGasRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE vehicleId = @vehicleId";
                var results = new List<GasRecord>();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("vehicleId", vehicleId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            GasRecord gasRecord = JsonSerializer.Deserialize<GasRecord>(reader["data"] as string);
                            results.Add(gasRecord);
                        }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<GasRecord>();
            }
        }
        public GasRecord GetGasRecordById(int gasRecordId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE id = @id";
                var result = new GasRecord();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", gasRecordId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            GasRecord gasRecord = JsonSerializer.Deserialize<GasRecord>(reader["data"] as string);
                            result = gasRecord;
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new GasRecord();
            }
        }
        public bool DeleteGasRecordById(int gasRecordId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE id = @id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", gasRecordId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool SaveGasRecordToVehicle(GasRecord gasRecord)
        {
            try
            {
                if (gasRecord.Id == default)
                {
                    string cmd = $"INSERT INTO app.{tableName} (vehicleId, data) VALUES(@vehicleId, CAST(@data AS jsonb)) RETURNING id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("vehicleId", gasRecord.VehicleId);
                        ctext.Parameters.AddWithValue("data", "{}");
                        gasRecord.Id = Convert.ToInt32(ctext.ExecuteScalar());
                        //update json data
                        if (gasRecord.Id != default)
                        {
                            string cmdU = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                            using (var ctextU = new NpgsqlCommand(cmdU, pgDataSource))
                            {
                                var serializedData = JsonSerializer.Serialize(gasRecord);
                                ctextU.Parameters.AddWithValue("id", gasRecord.Id);
                                ctextU.Parameters.AddWithValue("data", serializedData);
                                return ctextU.ExecuteNonQuery() > 0;
                            }
                        }
                        return gasRecord.Id != default;
                    }
                }
                else
                {
                    string cmd = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        var serializedData = JsonSerializer.Serialize(gasRecord);
                        ctext.Parameters.AddWithValue("id", gasRecord.Id);
                        ctext.Parameters.AddWithValue("data", serializedData);
                        return ctext.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool DeleteAllGasRecordsByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE vehicleId = @id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", vehicleId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
