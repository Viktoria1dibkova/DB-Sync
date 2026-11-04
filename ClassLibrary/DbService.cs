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

        /// <summary>
        /// Удаление движения
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="moveId">ID записи передвижения</param>
        /// <returns></returns>
        public static async Task DeleteMoveAsync(string conn, Guid moveId)
        {
            await ExecuteAsync(conn, SqlQueries.DeleteMove, ("id", moveId));
        }

        /// <summary>
        /// Удаление товара с его передвижениями (если есть)
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="productId">ID продукта</param>
        /// <returns></returns>
        public static async Task DeleteProductWithMovesAsync(string conn, Guid productId)
        {
            await ExecuteAsync(conn, SqlQueries.DeleteMovesByProduct, ("pid", productId));
            await ExecuteAsync(conn, SqlQueries.DeleteProduct, ("id", productId));
        }

        /// <summary>
        /// Подсчет количества зависимых передвижений продукта
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="productId">ID продукта</param>
        /// <returns></returns>
        public static async Task<long> CountMovesByProductAsync(string conn, Guid productId)
        {
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM stock_moves WHERE product_id = @pid", c);
            cmd.Parameters.AddWithValue("pid", productId);
            return (long)(await cmd.ExecuteScalarAsync())!;
        }
    }
}
