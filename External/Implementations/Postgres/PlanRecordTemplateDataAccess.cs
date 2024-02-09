﻿using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using Npgsql;
using System.Text.Json;

namespace CarCareTracker.External.Implementations
{
    public class PGPlanRecordTemplateDataAccess : IPlanRecordTemplateDataAccess
    {
        private NpgsqlConnection pgDataSource;
        private readonly ILogger<PGPlanRecordTemplateDataAccess> _logger;
        private static string tableName = "planrecordtemplates";
        public PGPlanRecordTemplateDataAccess(IConfiguration config, ILogger<PGPlanRecordTemplateDataAccess> logger)
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
        public List<PlanRecordInput> GetPlanRecordTemplatesByVehicleId(int vehicleId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE vehicleId = @vehicleId";
                var results = new List<PlanRecordInput>();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("vehicleId", vehicleId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            PlanRecordInput planRecord = JsonSerializer.Deserialize<PlanRecordInput>(reader["data"] as string);
                            results.Add(planRecord);
                        }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<PlanRecordInput>();
            }
        }
        public PlanRecordInput GetPlanRecordTemplateById(int planRecordId)
        {
            try
            {
                string cmd = $"SELECT data FROM app.{tableName} WHERE id = @id";
                var result = new PlanRecordInput();
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", planRecordId);
                    using (NpgsqlDataReader reader = ctext.ExecuteReader())
                        while (reader.Read())
                        {
                            PlanRecordInput planRecord = JsonSerializer.Deserialize<PlanRecordInput>(reader["data"] as string);
                            result = planRecord;
                        }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new PlanRecordInput();
            }
        }
        public bool DeletePlanRecordTemplateById(int planRecordId)
        {
            try
            {
                string cmd = $"DELETE FROM app.{tableName} WHERE id = @id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("id", planRecordId);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
        public bool SavePlanRecordTemplateToVehicle(PlanRecordInput planRecord)
        {
            try
            {
                if (planRecord.Id == default)
                {
                    string cmd = $"INSERT INTO app.{tableName} (vehicleId, data) VALUES(@vehicleId, CAST(@data AS jsonb)) RETURNING id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        ctext.Parameters.AddWithValue("vehicleId", planRecord.VehicleId);
                        ctext.Parameters.AddWithValue("data", "{}");
                        planRecord.Id = Convert.ToInt32(ctext.ExecuteScalar());
                        //update json data
                        if (planRecord.Id != default)
                        {
                            string cmdU = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                            using (var ctextU = new NpgsqlCommand(cmdU, pgDataSource))
                            {
                                var serializedData = JsonSerializer.Serialize(planRecord);
                                ctextU.Parameters.AddWithValue("id", planRecord.Id);
                                ctextU.Parameters.AddWithValue("data", serializedData);
                                return ctextU.ExecuteNonQuery() > 0;
                            }
                        }
                        return planRecord.Id != default;
                    }
                }
                else
                {
                    string cmd = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                    using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                    {
                        var serializedData = JsonSerializer.Serialize(planRecord);
                        ctext.Parameters.AddWithValue("id", planRecord.Id);
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
        public bool DeleteAllPlanRecordTemplatesByVehicleId(int vehicleId)
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
