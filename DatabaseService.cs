using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace StudentLibraryApp
{
    // Статичний клас для взаємодії з базою даних
    public static class DatabaseService
    {
        // Рядок підключення до бази даних
        private static string connString = "Server=PES-PATRON;Database=StudentLibrary;Trusted_Connection=True;TrustServerCertificate=True;";

        // Виконує запит, який повертає дані (наприклад, SELECT)
        public static DataTable ExecuteQuery(string query)
        {
            // Використання using для автоматичного закриття підключення
            using var conn = new SqlConnection(connString);
            using var cmd = new SqlCommand(query, conn);
            var dt = new DataTable();
            // Відкриття підключення
            conn.Open();
            // Завантаження даних у DataTable
            dt.Load(cmd.ExecuteReader());
            return dt;
        }

        // Виконує запит, який не повертає дані (INSERT, UPDATE, DELETE)
        public static void ExecuteNonQuery(string query)
        {
            using var conn = new SqlConnection(connString);
            using var cmd = new SqlCommand(query, conn);
            conn.Open();
            // Виконання запиту
            cmd.ExecuteNonQuery();
        }

        // Виконує запит без повернення даних з параметрами для запобігання SQL-ін'єкціям
        public static void ExecuteNonQuery(string query, Dictionary<string, object> parameters)
        {
            using var conn = new SqlConnection(connString);
            using var cmd = new SqlCommand(query, conn);
            // Додавання параметрів до команди
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                }
            }
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // Виконує запит з параметрами, який повертає дані
        public static DataTable ExecuteQuery(string query, Dictionary<string, object> parameters)
        {
            using var conn = new SqlConnection(connString);
            using var cmd = new SqlCommand(query, conn);
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                }
            }
            var dt = new DataTable();
            conn.Open();
            dt.Load(cmd.ExecuteReader());
            return dt;
        }

        // Отримує ID автора за ім'ям, або створює нового автора, якщо він не існує
        public static int GetOrCreateAuthorId(string authorName)
        {
            // Видалення зайвих пробілів
            authorName = authorName.Trim();
            var parameters = new Dictionary<string, object> { { "@FullName", authorName } };
            // Пошук автора в базі даних
            var dt = ExecuteQuery("SELECT AuthorID FROM Authors WHERE FullName = @FullName", parameters);
            // Якщо автор знайдений, повернути його ID
            if (dt.Rows.Count > 0)
            {
                return (int)dt.Rows[0]["AuthorID"];
            }
            // Інакше створити нового автора
            else
            {
                ExecuteNonQuery("INSERT INTO Authors (FullName) VALUES (@FullName)", parameters);
                // Отримання ID щойно створеного автора
                dt = ExecuteQuery("SELECT IDENT_CURRENT('Authors')");
                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
                throw new Exception("Не вдалося створити нового автора.");
            }
        }

        // Отримує ID жанру за назвою, або створює новий жанр, якщо він не існує
        public static int GetOrCreateGenreId(string genreName)
        {
            genreName = genreName.Trim();
            var parameters = new Dictionary<string, object> { { "@Name", genreName } };
            var dt = ExecuteQuery("SELECT GenreID FROM Genres WHERE Name = @Name", parameters);
            if (dt.Rows.Count > 0)
            {
                return (int)dt.Rows[0]["GenreID"];
            }
            else
            {
                ExecuteNonQuery("INSERT INTO Genres (Name) VALUES (@Name)", parameters);
                dt = ExecuteQuery("SELECT IDENT_CURRENT('Genres')");
                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
                throw new Exception("Не вдалося створити новий жанр.");
            }
        }

        // Отримує ID видавництва за назвою, або створює нове, якщо воно не існує
        public static int GetOrCreatePublisherId(string publisherName)
        {
            publisherName = publisherName.Trim();
            var parameters = new Dictionary<string, object> { { "@Name", publisherName } };
            var dt = ExecuteQuery("SELECT PublisherID FROM Publishers WHERE Name = @Name", parameters);
            if (dt.Rows.Count > 0)
            {
                return (int)dt.Rows[0]["PublisherID"];
            }
            else
            {
                ExecuteNonQuery("INSERT INTO Publishers (Name) VALUES (@Name)", parameters);
                dt = ExecuteQuery("SELECT IDENT_CURRENT('Publishers')");
                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToInt32(dt.Rows[0][0]);
                }
                throw new Exception("Не вдалося створити нове видавництво.");
            }
        }
    }
}