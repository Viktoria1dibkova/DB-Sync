using ClassLibrary;
using Npgsql;
using System.Data;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace КР
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Подписываемся на событие обновления (async для пометки того, что мы подождем)
            Loaded += async (s, e) => await ReloadAllAsync();
        }

        private string CurrentConn => RadioClient.IsChecked == true ? DbConfig.ClientConn : DbConfig.MainConn;
        private string BranchName => RadioClient.IsChecked == true ? "client" : "main";

        /// <summary>
        /// Обновление всех элементов
        /// </summary>
        /// <returns></returns>
        private async Task ReloadAllAsync()
        {
            await DbService.EnsureSchemaAsync(CurrentConn);
            GridProducts.ItemsSource = (await DbService.QuerySafeAsync(CurrentConn, SqlQueries.Products)).DefaultView;
            GridMoves.ItemsSource = (await DbService.QuerySafeAsync(CurrentConn, SqlQueries.Moves)).DefaultView;
            var combo = await DbService.QuerySafeAsync(CurrentConn, SqlQueries.ProductsForCombo);
            ComboProducts.ItemsSource = combo.DefaultView;
            ComboProducts.SelectedValue = null;
        }

        /// <summary>
        /// Изменение ветки (переключеине radio button)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Branch_Click(object sender, RoutedEventArgs e)
        {
            await ReloadAllAsync();
            StatusText.Text = RadioClient.IsChecked == true ? "Работаем с филиалом 2 (clientdb)" : "Работаем с филиалом 1 (maindb)";
        }

        /// <summary>
        /// Добавление в таблицу products
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtArticle.Text) || string.IsNullOrWhiteSpace(TxtName.Text))
                {
                    StatusText.Text = "Заполните артикул и название";
                    return;
                }
                if (!decimal.TryParse(TxtPrice.Text, out var price))
                {
                    StatusText.Text = "Цена - не число";
                    return;
                }

                await DbService.ExecuteAsync(CurrentConn, SqlQueries.InsertProduct, ("a", TxtArticle.Text.Trim()), ("n", TxtName.Text.Trim()), ("p", price));
                TxtArticle.Clear(); 
                TxtName.Clear(); 
                TxtPrice.Clear();
                await ReloadAllAsync();
                StatusText.Text = "Товар добавлен";
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                StatusText.Text = "Ошибка: такой артикул уже существует";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка: " + ex.Message;
            }
        }

        /// <summary>
        /// Добавление в таблицу moves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ComboProducts.SelectedValue is not Guid productId)
                {
                    StatusText.Text = "Выберите товар из списка";
                    return;
                }
                if (!int.TryParse(TxtQty.Text, out var qty) || qty <= 0)
                {
                    StatusText.Text = "Количество - целое число больше нуля";
                    return;
                }
                var moveType = (ComboMoveType.SelectedItem as ComboBoxItem).Content.ToString() == "Приход" ? "in" : "out";
                await DbService.ExecuteAsync(CurrentConn, SqlQueries.InsertMove, ("pid", productId), ("t", moveType), ("q", qty), ("b", BranchName));
                TxtQty.Clear();
                await ReloadAllAsync();
                StatusText.Text = "Операция проведена";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка: " + ex.Message;
            }
        }

        /// <summary>
        /// Метод для того, чтобы не было видно ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridProducts_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "id")
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Удаление продукта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridProducts.SelectedItem is not DataRowView row)
                {
                    StatusText.Text = "Выберите товар для удаления";
                    return;
                }
                var productId = (Guid)row["id"];
                var article = row["Артикул"].ToString();
                var name = row["Название"].ToString();
                long movesCount = await DbService.CountMovesByProductAsync(CurrentConn, productId);
                string message = movesCount > 0 ? $"Товар \"{name}\" ({article}) имеет {movesCount} движение(й).\n" + 
                                                  "При удалении товара ВСЕ связанные движения будут удалены безвозвратно.\n\n" +
                                                  "Вы уверены, что хотите продолжить?"
                                                  : $"Удалить товар \"{name}\" ({article})?";

                var result = MessageBox.Show(message, "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    StatusText.Text = "Удаление отменено";
                    return;
                }
                await DbService.DeleteProductWithMovesAsync(CurrentConn, productId);
                await ReloadAllAsync();
                StatusText.Text = $"Товар '{article}' и все его движения удалены";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка удаления: " + ex.Message;
            }
        }

        /// <summary>
        /// Удаление передвижения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteMove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridMoves.SelectedItem is not DataRowView row)
                {
                    StatusText.Text = "Выберите движение для удаления";
                    return;
                }
                var moveId = (Guid)row["id"];
                var date = (DateTime)row["Дата"];
                var article = row["Артикул"].ToString();
                var operation = row["Операция"].ToString();
                var quantity = Convert.ToInt64(row["Кол-во"]);
                var branch = row["Филиал"].ToString();
                var result = MessageBox.Show($"Удалить движение: {operation} {quantity} ед. товара {article}?\n" +
                                             $"Дата: {date:dd.MM.yyyy HH:mm}\n" +
                                             $"Филиал: {branch}", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    StatusText.Text = "Удаление отменено";
                    return;
                }
                await DbService.DeleteMoveAsync(CurrentConn, moveId);
                await ReloadAllAsync();
                StatusText.Text = "Движение удалено";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка удаления: " + ex.Message;
            }
        }

    }
}