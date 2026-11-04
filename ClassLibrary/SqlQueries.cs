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
                                     created_at TIMESTAMP NOT NULL DEFAULT NOW());

                                     CREATE TABLE IF NOT EXISTS stock_moves(
                                     id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                     product_id UUID NOT NULL REFERENCES products(id),
                                     move_type VARCHAR(10) NOT NULL,
                                     quantity INTEGER NOT NULL CHECK(quantity > 0),
                                     branch VARCHAR(50) NOT NULL,
                                     created_at TIMESTAMP NOT NULL DEFAULT NOW()
                                     );";
        /// <summary>
        /// Чтение таблицы products
        /// </summary>
        public const string Products = @"SELECT article AS ""Артикул"", name AS ""Название"", price AS ""Цена"", created_at AS ""Создан"" FROM products ORDER BY created_at";
        /// <summary>
        /// Чтение таблицы moves
        /// </summary>
        public const string Moves = @"SELECT m.created_at AS ""Дата"", p.article AS ""Артикул"", p.name AS ""Товар"",
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
    }
}
