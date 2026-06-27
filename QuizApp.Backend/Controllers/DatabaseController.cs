using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Backend.Services;

namespace QuizApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly SqliteDatabaseService _dbService;

        public DatabaseController(SqliteDatabaseService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("health")]
        public IActionResult GetHealth()
        {
            try
            {
                // Verify we can talk to the database
                _dbService.ExecuteScalar("SELECT 1");
                return Ok(new { status = "Healthy", message = "Connected successfully to SQLite database." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "Unhealthy", error = ex.Message });
            }
        }

        [HttpPost("execute-table")]
        public IActionResult ExecuteTable([FromBody] QueryRequest request)
        {
            try
            {
                var dt = _dbService.ExecuteTable(request.Sql, request.Parameters);
                
                // Convert DataTable to list of dictionaries for clean JSON serialization
                var list = new List<Dictionary<string, object?>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        object value = row[col];
                        if (value is byte[] bytes)
                        {
                            dict[col.ColumnName] = Convert.ToBase64String(bytes);
                        }
                        else
                        {
                            dict[col.ColumnName] = value == DBNull.Value ? null : value;
                        }
                    }
                    list.Add(dict);
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("execute-scalar")]
        public IActionResult ExecuteScalar([FromBody] QueryRequest request)
        {
            try
            {
                var result = _dbService.ExecuteScalar(request.Sql, request.Parameters);
                if (result is byte[] bytes)
                {
                    return Ok(new { value = Convert.ToBase64String(bytes), isBinary = true });
                }
                return Ok(new { value = result == DBNull.Value ? null : result, isBinary = false });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("execute-nonquery")]
        public IActionResult ExecuteNonQuery([FromBody] QueryRequest request)
        {
            try
            {
                int rows = _dbService.ExecuteNonQuery(request.Sql, request.Parameters);
                return Ok(new { rowsAffected = rows });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("execute-procedure")]
        public IActionResult ExecuteProcedure([FromBody] ProcedureRequest request)
        {
            try
            {
                string procName = request.ProcedureName.Replace("dbo.", "").Trim();

                if (string.Equals(procName, "insert_questions", StringComparison.OrdinalIgnoreCase))
                {
                    // Map stored procedure parameters to direct SQL insert
                    string insertSql = @"
                        INSERT INTO tbl_questions (q_title, q_opA, q_opB, q_opC, q_opD, q_correctOpn, q_correctDate, ad_id_fk, ex_id_fk)
                        VALUES (@q_title, @q_opA, @q_opB, @q_opC, @q_opD, @q_correctOpn, @q_correctDate, @ad_id_fk, @ex_id_fk)";
                    
                    int rows = _dbService.ExecuteNonQuery(insertSql, request.Parameters);
                    return Ok(new { rowsAffected = rows });
                }
                
                if (string.Equals(procName, "insert_set_exam", StringComparison.OrdinalIgnoreCase))
                {
                    string insertSql = @"
                        INSERT INTO set_exam (set_exam_date, stud_id_fk, exam_id_fk)
                        VALUES (@set_exam_date, @stud_id_fk, @exam_id_fk)";
                    
                    // Add standard @ if missing
                    var parameters = new Dictionary<string, object?>();
                    foreach (var kvp in request.Parameters)
                    {
                        string name = kvp.Key;
                        if (name.Equals("stud_id_fk", StringComparison.OrdinalIgnoreCase))
                        {
                            parameters["@stud_id_fk"] = kvp.Value;
                        }
                        else
                        {
                            parameters[name] = kvp.Value;
                        }
                    }

                    int rows = _dbService.ExecuteNonQuery(insertSql, parameters);
                    return Ok(new { rowsAffected = rows });
                }

                if (string.Equals(procName, "usp_UpsertTheoryScores", StringComparison.OrdinalIgnoreCase))
                {
                    int? examId = null;
                    if (request.Parameters != null)
                    {
                        if (request.Parameters.TryGetValue("@ExamId", out object? examIdObj) ||
                            request.Parameters.TryGetValue("ExamId", out examIdObj))
                        {
                            if (examIdObj != null && examIdObj != DBNull.Value && int.TryParse(examIdObj.ToString(), out int parsedId))
                            {
                                examId = parsedId;
                            }
                        }
                    }

                    _dbService.ExecuteUpsertTheoryScores(examId);
                    return Ok(new { message = "Theory scores upserted successfully." });
                }

                return BadRequest(new { error = $"Stored procedure '{request.ProcedureName}' is not supported." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class QueryRequest
    {
        public string Sql { get; set; } = string.Empty;
        public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    }

    public class ProcedureRequest
    {
        public string ProcedureName { get; set; } = string.Empty;
        public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    }
}
