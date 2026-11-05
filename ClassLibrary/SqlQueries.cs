using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClassLibrary
{
    public class SqlQueries
    {
        /// <summary>
        /// Схема создания таблиц
        /// </summary>
        public const string Schema = @"CREATE TABLE IF NOT EXISTS products (
                                     id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                     article VARCHAR(50) NOT NULL UNIQUE,
                                     name VARCHAR(255) NOT NULL,
                                     price NUMERIC(10,2) NOT NULL DEFAULT 0,
                                     created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                                     updated_at  TIMESTAMP NOT NULL DEFAULT NOW());

                                     CREATE TABLE IF NOT EXISTS stock_moves(
                                     id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                     product_id UUID NOT NULL REFERENCES products(id),
                                     move_type VARCHAR(10) NOT NULL,
                                     quantity INTEGER NOT NULL CHECK(quantity > 0),
                                     branch VARCHAR(50) NOT NULL,
                                     created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                                     updated_at  TIMESTAMP NOT NULL DEFAULT NOW());

                                     CREATE TABLE IF NOT EXISTS sync_state (
                                     id INTEGER PRIMARY KEY,
                                     last_sync TIMESTAMP NOT NULL);
    
                                     INSERT INTO sync_state (id, last_sync) VALUES (1, '2000-01-01')
                                     ON CONFLICT (id) DO UPDATE SET last_sync = EXCLUDED.last_sync;";


        /// <summary>
        /// Чтение таблицы products
        /// </summary>
        public const string Products = @"SELECT id, article AS ""Артикул"", name AS ""Название"", price AS ""Цена"", created_at AS ""Создан"" FROM products ORDER BY created_at";
        /// <summary>
        /// Чтение таблицы moves
        /// </summary>
        public const string Moves = @"SELECT m.id, m.created_at AS ""Дата"", p.article AS ""Артикул"", p.name AS ""Товар"",
                                    CASE m.move_type WHEN 'in' THEN 'Приход' ELSE 'Расход' END AS ""Операция"",
                                    m.quantity AS ""Кол-во"", m.branch AS ""Филиал"" FROM stock_moves m
                                    JOIN products p ON p.id = m.product_id ORDER BY m.created_at DESC";
        /// <summary>
        /// Склейка для комбобокса
        /// </summary>
        public const string ProductsForCombo = @"SELECT id, name || ' (' || article || ')' AS label FROM products ORDER BY name";
        /// <summary>
        /// Вставка в products
        /// </summary>
        public const string InsertProduct = "INSERT INTO products (article, name, price) VALUES (@a, @n, @p)";
        /// <summary>
        /// Вставка в moves
        /// </summary>
        public const string InsertMove = "INSERT INTO stock_moves (product_id, move_type, quantity, branch) VALUES (@pid, @t, @q, @b)";
        /// <summary>
        /// Удаление передвижения
        /// </summary>
        public const string DeleteMove = "DELETE FROM stock_moves WHERE id = @id";
        /// <summary>
        /// Удаление передвижений связанных с продуктом
        /// </summary>
        public const string DeleteMovesByProduct = "DELETE FROM stock_moves WHERE product_id = @pid";
        /// <summary>
        /// Удаление продукта
        /// </summary>
        public const string DeleteProduct = "DELETE FROM products WHERE id = @id";
        /// <summary>
        /// Получение времени последней синхронизации
        /// </summary>
        public const string GetSync= "SELECT last_sync FROM sync_state WHERE id = 1";
        /// <summary>
        /// Обновление времени последней синхронизации
        /// </summary>
        public const string SetSync= "UPDATE sync_state SET last_sync = @ts WHERE id = 1";
        /// <summary>
        /// Получение новых данных в products
        /// </summary>
        public const string LoadProductsSince = @"SELECT id, article, name, price, created_at, updated_at FROM products WHERE updated_at > @since";
        /// <summary>
        /// Получение новых данных в moves
        /// </summary>
        public const string LoadMovesSince = @"SELECT id, product_id, move_type, quantity, branch, created_at, updated_at FROM stock_moves WHERE updated_at > @since";
        /// <summary>
        /// Обновление данных (синхронизация) в products
        /// </summary>
        public const string UpsertProduct = @"INSERT INTO products (id, article, name, price, created_at, updated_at)
                                            VALUES (@id, @a, @n, @p, @c, @u) ON CONFLICT (id) DO UPDATE
                                            SET article = EXCLUDED.article, name = EXCLUDED.name,
                                            price = EXCLUDED.price, updated_at = EXCLUDED.updated_at
                                            WHERE products.updated_at < EXCLUDED.updated_at";
        /// <summary>
        /// /// Обновление данных (синхронизация) в moves
        /// </summary>
        public const string UpsertMove = @"INSERT INTO stock_moves (id, product_id, move_type, quantity, branch, created_at, updated_at)
                                         VALUES (@id, @pid, @t, @q, @b, @c, @u) ON CONFLICT (id) DO UPDATE
                                         SET quantity = EXCLUDED.quantity, branch = EXCLUDED.branch,
                                         updated_at = EXCLUDED.updated_at WHERE stock_moves.updated_at < EXCLUDED.updated_at";
    }
}
