using Npgsql;

namespace ClassLibrary
{
    public class SyncService
    {
        /// <summary>
        /// Синхронизация данных
        /// </summary>
        /// <param name="mainConn">Филиал 1</param>
        /// <param name="clientConn">Филиал 2</param>
        /// <returns></returns>
        public static async Task<SyncReport> SynchronizeAsync(string mainConn, string clientConn)
        {
            var report = new SyncReport();
            DateTime now;
            // Запоминаем время сервера БД, а не компьютера, чтобы избежать рассинхрона
            using (var c = new NpgsqlConnection(mainConn))
            {
                await c.OpenAsync();
                using var cmd = new NpgsqlCommand("SELECT LOCALTIMESTAMP", c);
                now = (DateTime)(await cmd.ExecuteScalarAsync())!;
            }
            var sinceMain = await DbService.GetSyncAsync(mainConn);
            var sinceClient = await DbService.GetSyncAsync(clientConn);
            var productsFromMain = await DbService.LoadProductsSinceAsync(mainConn, sinceMain);
            var movesFromMain = await DbService.LoadMovesSinceAsync(mainConn, sinceMain);
            var productsFromClient = await DbService.LoadProductsSinceAsync(clientConn, sinceClient);
            var movesFromClient = await DbService.LoadMovesSinceAsync(clientConn, sinceClient);
            foreach (var p in productsFromMain) report.SentToClient += await DbService.UpsertProductAsync(clientConn, p);
            foreach (var m in movesFromMain) report.SentToClient += await DbService.UpsertMoveAsync(clientConn, m);
            foreach (var p in productsFromClient) report.SentToMain += await DbService.UpsertProductAsync(mainConn, p);
            foreach (var m in movesFromClient) report.SentToMain += await DbService.UpsertMoveAsync(mainConn, m);
            await DbService.SetSyncAsync(mainConn, now);
            await DbService.SetSyncAsync(clientConn, now);
            return report;
        }
    }
}
