using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    /// <summary>
    /// Класс для переноса строки продукта
    /// </summary>
    public class ProductRow
    {
        public Guid Id;
        public string Article = "";
        public string Name = "";
        public decimal Price;
        public DateTime Created;
        public DateTime Updated;
    }

    /// <summary>
    /// Класс для переноса строки передвижения
    /// </summary>
    public class MoveRow
    {
        public Guid Id;
        public Guid ProductId;
        public string MoveType = "";
        public int Quantity;
        public string Branch = "";
        public DateTime Created;
        public DateTime Updated;
    }

    /// <summary>
    /// Отчёт о резульататх синхронизации
    /// </summary>
    public class SyncReport
    {
        public long SentToClient;
        public long SentToMain;
        public long Conflicts;
        public override string ToString() => $"на филиал 2: {SentToClient}, на филиал 1: {SentToMain}, конфликтов: {Conflicts}";
    }
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
 
        /// <summary>
        /// Получение времени последней синхронизации
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <returns></returns>
        public static async Task<DateTime> GetSyncAsync(string conn)
        {
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand(SqlQueries.GetSync, c);
            return (DateTime)(await cmd.ExecuteScalarAsync())!;
        }

        /// <summary>
        /// Обновление времени последней синхронизации
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="ts">временная метка</param>
        /// <returns></returns>
        public static async Task SetSyncAsync(string conn, DateTime ts)
        {
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand(SqlQueries.SetSync, c);
            cmd.Parameters.AddWithValue("ts", ts);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Загрузка товаров после синхронизации
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="since">время последней синхронизации</param>
        /// <returns></returns>
        public static async Task<List<ProductRow>> LoadProductsSinceAsync(string conn, DateTime since)
        {
            var list = new List<ProductRow>();
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand(SqlQueries.LoadProductsSince, c);
            cmd.Parameters.AddWithValue("since", since);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(new ProductRow {Id = r.GetGuid(0), Article = r.GetString(1), Name = r.GetString(2), Price = r.GetDecimal(3), Created = r.GetDateTime(4), Updated = r.GetDateTime(5)});
            return list;
        }

        /// <summary>
        /// Загрузка передвижений после синхронизации
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="since">время последней синхронизации</param>
        /// <returns></returns>
        public static async Task<List<MoveRow>> LoadMovesSinceAsync(string conn, DateTime since)
        {
            var list = new List<MoveRow>();
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand(SqlQueries.LoadMovesSince, c);
            cmd.Parameters.AddWithValue("since", since);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(new MoveRow{Id = r.GetGuid(0), ProductId = r.GetGuid(1), MoveType = r.GetString(2), Quantity = r.GetInt32(3), Branch = r.GetString(4), Created = r.GetDateTime(5), Updated = r.GetDateTime(6)});
            return list;
        }

        /// <summary>
        /// Вставка и обновление товара
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="p">строка продукта</param>
        /// <returns></returns>
        public static async Task<long> UpsertProductAsync(string conn, ProductRow p)
        {
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand(SqlQueries.UpsertProduct, c);
            cmd.Parameters.AddWithValue("id", p.Id);
            cmd.Parameters.AddWithValue("a", p.Article);
            cmd.Parameters.AddWithValue("n", p.Name);
            cmd.Parameters.AddWithValue("p", p.Price);
            cmd.Parameters.AddWithValue("c", p.Created);
            cmd.Parameters.AddWithValue("u", p.Updated);
            return await cmd.ExecuteNonQueryAsync(); // Вернет 1, если записали, или 0, если наша версия уже новее
        }

        /// <summary>
        /// Вставка и обновление передвижения
        /// </summary>
        /// <param name="conn">строка соединения</param>
        /// <param name="m">строка передвижения</param>
        /// <returns></returns>
        public static async Task<long> UpsertMoveAsync(string conn, MoveRow m)
        {
            using var c = new NpgsqlConnection(conn);
            await c.OpenAsync();
            using var cmd = new NpgsqlCommand(SqlQueries.UpsertMove, c);
            cmd.Parameters.AddWithValue("id", m.Id);
            cmd.Parameters.AddWithValue("pid", m.ProductId);
            cmd.Parameters.AddWithValue("t", m.MoveType);
            cmd.Parameters.AddWithValue("q", m.Quantity);
            cmd.Parameters.AddWithValue("b", m.Branch);
            cmd.Parameters.AddWithValue("c", m.Created);
            cmd.Parameters.AddWithValue("u", m.Updated);
            return await cmd.ExecuteNonQueryAsync();
        }
    }
}
