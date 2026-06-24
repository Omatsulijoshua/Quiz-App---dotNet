using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace QuizApp.Backend.Services
{
    public class SqliteDatabaseService
    {
        private readonly string _connectionString;

        public SqliteDatabaseService()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quizApp.db");
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // 1. tbl_admin
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_admin (
                        ad_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ad_name TEXT NOT NULL,
                        ad_password TEXT NOT NULL
                    );");

                // 2. tbl_exams (Courses)
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_exams (
                        ex_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ex_name TEXT NOT NULL
                    );");

                // 3. student_record
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS student_record (
                        std_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        std_name TEXT NOT NULL,
                        std_batch_code TEXT NULL,
                        std_password TEXT NOT NULL,
                        std_id_fk INTEGER NULL,
                        update_date TEXT NULL
                    );");

                // 4. score
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS score (
                        SCORE_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        score INTEGER NOT NULL,
                        percentage REAL NOT NULL,
                        stud_fk_id INTEGER NULL,
                        exam_fk_id INTEGER NULL,
                        theory_score REAL NULL,
                        combined_score REAL NULL,
                        theory_details TEXT NULL
                    );");

                // 5. tbl_questions
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_questions (
                        ques_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        q_title TEXT NOT NULL,
                        q_opA TEXT NOT NULL,
                        q_opB TEXT NOT NULL,
                        q_opC TEXT NOT NULL,
                        q_opD TEXT NOT NULL,
                        q_correctOpn TEXT NOT NULL,
                        q_correctDate TEXT NULL,
                        ad_id_fk INTEGER NULL,
                        ex_id_fk INTEGER NULL,
                        q_image BLOB NULL
                    );");

                // 6. set_exam
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS set_exam (
                        set_exam_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        set_exam_date TEXT NULL,
                        stud_id_fk INTEGER NULL,
                        exam_id_fk INTEGER NULL
                    );");

                // 7. tbl_shortanswer
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_shortanswer (
                        sa_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        exam_id INTEGER NOT NULL,
                        ques_title TEXT NOT NULL,
                        correct_answer TEXT NOT NULL,
                        ques_image BLOB NULL
                    );");

                // 8. tbl_theory_questions
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_theory_questions (
                        theory_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        exam_fk_id INTEGER NOT NULL,
                        question_text TEXT NOT NULL,
                        mark INTEGER NOT NULL DEFAULT 0,
                        question_number INTEGER NOT NULL DEFAULT 1,
                        model_answer TEXT NULL,
                        question_image BLOB NULL,
                        created_at TEXT NULL DEFAULT CURRENT_TIMESTAMP
                    );");

                // 9. tbl_theory_answers
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_theory_answers (
                        answer_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        theory_fk_id INTEGER NOT NULL,
                        student_fk_id INTEGER NOT NULL,
                        exam_fk_id INTEGER NOT NULL,
                        answer_text TEXT NULL,
                        score REAL NULL,
                        teacher_comment TEXT NULL,
                        is_submitted INTEGER NOT NULL DEFAULT 0,
                        last_saved_at TEXT NULL,
                        submitted_at TEXT NULL,
                        graded_at TEXT NULL,
                        created_at TEXT NULL DEFAULT CURRENT_TIMESTAMP,
                        UNIQUE(theory_fk_id, student_fk_id, exam_fk_id)
                    );");

                // 10. tbl_exam_settings
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_exam_settings (
                        ex_id INTEGER PRIMARY KEY,
                        duration_minutes INTEGER NULL,
                        theory_duration_minutes INTEGER NULL,
                        theory_exam_enabled INTEGER NOT NULL DEFAULT 1,
                        total_questions INTEGER NULL
                    );");

                // 11. tbl_past_questions
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_past_questions (
                        ques_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        q_title TEXT NOT NULL,
                        q_opA TEXT NOT NULL,
                        q_opB TEXT NOT NULL,
                        q_opC TEXT NOT NULL,
                        q_opD TEXT NOT NULL,
                        q_correctOpn TEXT NOT NULL,
                        q_correctDate TEXT NULL,
                        ad_id_fk INTEGER NULL,
                        ex_id_fk INTEGER NULL,
                        q_image BLOB NULL
                    );");

                // 12. tbl_past_shortanswer
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS tbl_past_shortanswer (
                        sa_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        exam_id INTEGER NOT NULL,
                        ques_title TEXT NOT NULL,
                        correct_answer TEXT NOT NULL,
                        ques_image BLOB NULL
                    );");

                // 13. subscription
                ExecuteNonQueryInternal(connection, @"
                    CREATE TABLE IF NOT EXISTS subscription (
                        subscription_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        depositor_name TEXT NOT NULL,
                        duration_months INTEGER NOT NULL,
                        amount REAL NOT NULL,
                        status TEXT NOT NULL,
                        request_date TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        start_date TEXT NULL,
                        end_date TEXT NULL
                    );");

                // Seed Default Admin (admin/admin)
                long adminCount = ExecuteScalarInternal(connection, "SELECT COUNT(*) FROM tbl_admin");
                if (adminCount == 0)
                {
                    ExecuteNonQueryInternal(connection, "INSERT INTO tbl_admin (ad_name, ad_password) VALUES ('admin', 'admin');");
                }

                // Seed Default Student (1/password)
                long studentCount = ExecuteScalarInternal(connection, "SELECT COUNT(*) FROM student_record");
                if (studentCount == 0)
                {
                    ExecuteNonQueryInternal(connection, "INSERT INTO student_record (std_id, std_name, std_password, std_batch_code) VALUES (1, 'Default Student', 'password', 'Batch A');");
                }
            }
        }

        private void ExecuteNonQueryInternal(SqliteConnection connection, string sql)
        {
            using (var command = new SqliteCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private long ExecuteScalarInternal(SqliteConnection connection, string sql)
        {
            using (var command = new SqliteCommand(sql, connection))
            {
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
            }
        }

        public string TranslateQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            // 1. Remove dbo. schema prefix
            sql = sql.Replace("dbo.", "");

            // 2. Translate SELECT TOP (N) or SELECT TOP N to LIMIT
            // Regex matches: SELECT TOP (12) * FROM ... -> SELECT * FROM ... LIMIT 12
            var topRegex = new Regex(@"SELECT\s+TOP\s+\(?(\d+)\)?\s+(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var match = topRegex.Match(sql);
            if (match.Success)
            {
                string limit = match.Groups[1].Value;
                string rest = match.Groups[2].Value;
                sql = $"SELECT {rest} LIMIT {limit}";
            }

            // 3. Translate INFORMATION_SCHEMA.TABLES check
            if (sql.Contains("INFORMATION_SCHEMA.TABLES"))
            {
                sql = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = @table";
            }

            // 4. Translate INFORMATION_SCHEMA.COLUMNS check
            if (sql.Contains("INFORMATION_SCHEMA.COLUMNS"))
            {
                sql = "SELECT COUNT(*) FROM pragma_table_info(@table) WHERE name = @column";
            }

            // 5. Translate DB_ID database check
            if (sql.Contains("DB_ID("))
            {
                sql = "SELECT 1";
            }

            // 6. Translate CAST(request_date AS DATE) to date(request_date)
            sql = Regex.Replace(sql, @"CAST\(([^)]+)\s+AS\s+DATE\)", "date($1)", RegexOptions.IgnoreCase);

            // 7. Translate GETDATE() to CURRENT_TIMESTAMP
            sql = Regex.Replace(sql, @"GETDATE\(\)", "CURRENT_TIMESTAMP", RegexOptions.IgnoreCase);

            // 8. Fix GPA select column typo in original code (s.percentagestud_fk_id)
            sql = sql.Replace("s.percentagestud_fk_id", "s.stud_fk_id");

            return sql;
        }

        public DataTable ExecuteTable(string sql, Dictionary<string, object> parameters)
        {
            string translated = TranslateQuery(sql);
            var dt = new DataTable();

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(translated, connection))
                {
                    BindParameters(command, parameters);
                    using (var reader = command.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }

            return dt;
        }

        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
        {
            string translated = TranslateQuery(sql);

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(translated, connection))
                {
                    BindParameters(command, parameters);
                    var result = command.ExecuteScalar();
                    return result ?? DBNull.Value;
                }
            }
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            string translated = TranslateQuery(sql);

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand(translated, connection))
                {
                    BindParameters(command, parameters);
                    return command.ExecuteNonQuery();
                }
            }
        }

        public void ExecuteUpsertTheoryScores(int? examId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Calculate totals from tbl_theory_answers
                        string selectSql = @"
                            SELECT
                                student_fk_id,
                                exam_fk_id,
                                SUM(COALESCE(score, 0)) AS theory_total,
                                SUM(CASE WHEN score IS NOT NULL THEN 1 ELSE 0 END) AS graded_answers,
                                COUNT(*) AS answer_count
                            FROM tbl_theory_answers
                            WHERE (@examId IS NULL OR exam_fk_id = @examId)
                            GROUP BY student_fk_id, exam_fk_id";

                        var totals = new List<TheoryTotalRecord>();

                        using (var selectCmd = new SqliteCommand(selectSql, connection, transaction))
                        {
                            selectCmd.Parameters.AddWithValue("@examId", examId.HasValue ? (object)examId.Value : DBNull.Value);
                            using (var reader = selectCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    totals.Add(new TheoryTotalRecord
                                    {
                                        StudentId = reader.GetInt32(0),
                                        ExamId = reader.GetInt32(1),
                                        TheoryTotal = reader.GetDecimal(2),
                                        GradedAnswers = reader.GetInt32(3),
                                        AnswerCount = reader.GetInt32(4)
                                    });
                                }
                            }
                        }

                        // 2. Upsert into score table for each record
                        foreach (var total in totals)
                        {
                            string checkSql = "SELECT COUNT(*) FROM score WHERE stud_fk_id = @studId AND exam_fk_id = @examId";
                            long exists = 0;
                            using (var checkCmd = new SqliteCommand(checkSql, connection, transaction))
                            {
                                checkCmd.Parameters.AddWithValue("@studId", total.StudentId);
                                checkCmd.Parameters.AddWithValue("@examId", total.ExamId);
                                exists = (long)checkCmd.ExecuteScalar();
                            }

                            string details = $"Graded answers: {total.GradedAnswers} of {total.AnswerCount}";

                            if (exists > 0)
                            {
                                string updateSql = @"
                                    UPDATE score
                                    SET theory_score = @theoryScore,
                                        combined_score = COALESCE(score, 0) + @theoryScore,
                                        theory_details = @details
                                    WHERE stud_fk_id = @studId AND exam_fk_id = @examId";

                                using (var updateCmd = new SqliteCommand(updateSql, connection, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@theoryScore", total.TheoryTotal);
                                    updateCmd.Parameters.AddWithValue("@details", details);
                                    updateCmd.Parameters.AddWithValue("@studId", total.StudentId);
                                    updateCmd.Parameters.AddWithValue("@examId", total.ExamId);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string insertSql = @"
                                    INSERT INTO score (score, percentage, stud_fk_id, exam_fk_id, theory_score, combined_score, theory_details)
                                    VALUES (0, 0, @studId, @examId, @theoryScore, @theoryScore, @details)";

                                using (var insertCmd = new SqliteCommand(insertSql, connection, transaction))
                                {
                                    insertCmd.Parameters.AddWithValue("@studId", total.StudentId);
                                    insertCmd.Parameters.AddWithValue("@examId", total.ExamId);
                                    insertCmd.Parameters.AddWithValue("@theoryScore", total.TheoryTotal);
                                    insertCmd.Parameters.AddWithValue("@details", details);
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void BindParameters(SqliteCommand command, Dictionary<string, object> parameters)
        {
            if (parameters == null) return;

            foreach (var kvp in parameters)
            {
                string name = kvp.Key;
                if (!name.StartsWith("@"))
                {
                    name = "@" + name;
                }

                object value = kvp.Value;

                // Handle JSON deserialized types (like JObject/Newtonsoft conversion or System.Text.Json)
                if (value is System.Text.Json.JsonElement element)
                {
                    value = ConvertJsonElement(element);
                }

                if (value == null)
                {
                    command.Parameters.AddWithValue(name, DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue(name, value);
                }
            }
        }

        private object ConvertJsonElement(System.Text.Json.JsonElement element)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String:
                    // If it is a base64 encoded byte array from client, try to parse it
                    string val = element.GetString();
                    if (val != null && val.Length > 20 && IsBase64String(val, out byte[] bytes))
                    {
                        return bytes;
                    }
                    return val;
                case System.Text.Json.JsonValueKind.Number:
                    if (element.TryGetInt64(out long l)) return l;
                    if (element.TryGetDouble(out double d)) return d;
                    return element.GetRawText();
                case System.Text.Json.JsonValueKind.True:
                    return 1;
                case System.Text.Json.JsonValueKind.False:
                    return 0;
                case System.Text.Json.JsonValueKind.Null:
                    return DBNull.Value;
                default:
                    return element.GetRawText();
            }
        }

        private bool IsBase64String(string s, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(s) || s.Length % 4 != 0
                || s.Contains(" ") || s.Contains("\t") || s.Contains("\r") || s.Contains("\n"))
                return false;

            try
            {
                bytes = Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private class TheoryTotalRecord
        {
            public int StudentId { get; set; }
            public int ExamId { get; set; }
            public decimal TheoryTotal { get; set; }
            public int GradedAnswers { get; set; }
            public int AnswerCount { get; set; }
        }
    }
}
