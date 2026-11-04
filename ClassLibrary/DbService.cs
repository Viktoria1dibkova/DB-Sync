using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class DbService
    {
        /// <summary>
        /// Создание таблиц, если их нет
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <returns></returns>
        public static async Task EnsureSchemaAsync(string conn)
        {
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand(SqlQueries.Schema, c);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Выполнение SELECT запроса
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="sql">строка запроса</param>
        /// <returns></returns>
        public static async Task<DataTable> QueryAsync(string conn, string sql)
        {
            using var connection = new NpgsqlConnection(conn);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, connection);
            var adapter = new NpgsqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        /// <summary>
        /// Безопасное выполнение SELECT запроса (если таблиц нет, то не падает)
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="sql">строка запроса</param>
        /// <returns></returns>
        public static async Task<DataTable> QuerySafeAsync(string conn, string sql)
        {
            try
            {
                return await QueryAsync(conn, sql);
            }
            catch (PostgresException ex)
            {
                return new DataTable();
            }
        }

        /// <summary>
        /// ВЫполнение INSERT, UPDATE, DELETE запросов
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="sql">строка запроса</param>
        /// <param name="parameters">параметры</param>
        /// <returns></returns>
        public static async Task ExecuteAsync(string conn, string sql, params (string name, object value)[] parameters)
        {
            using var connection = new NpgsqlConnection(conn);
            await connection.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, connection);
            foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
