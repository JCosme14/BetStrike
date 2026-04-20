using BettingAPI.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BettingAPI.Services
{
    public class GameSyncService : BackgroundService
    {
        private readonly DatabaseHelper _db;
        private readonly HttpClient _httpClient;
        private readonly string _fpfApiUrl = "https://localhost:7023/api/jogos";

        public GameSyncService(DatabaseHelper db)
        {
            _db = db;
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncGames();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sync error: {ex.Message}");
                }

                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task SyncGames()
        {
            var response = await _httpClient.GetAsync(_fpfApiUrl);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            var jogos = JsonSerializer.Deserialize<List<JsonElement>>(json);

            if (jogos == null) return;

            foreach (var jogo in jogos)
            {
                var codigo = jogo.GetProperty("codigo_Jogo").GetString();
                var equipaCasa = jogo.GetProperty("equipa_Casa").GetString();
                var equipaFora = jogo.GetProperty("equipa_Fora").GetString();
                var dataHoraInicio = jogo.GetProperty("data_Hora_Inicio").GetDateTime();
                var estado = jogo.GetProperty("estado").GetInt32();
                var golosCasa = jogo.GetProperty("golos_Casa").GetInt32();
                var golosFora = jogo.GetProperty("golos_Fora").GetInt32();

                using var conn = _db.GetConnection();
                conn.Open();

                if (!JogoExists(conn, codigo))
                {
                    using var cmd = new SqlCommand("sp_InsertJogo", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
                    cmd.Parameters.AddWithValue("@Data_Hora_Inicio", dataHoraInicio);
                    cmd.Parameters.AddWithValue("@Equipa_Casa", equipaCasa);
                    cmd.Parameters.AddWithValue("@Equipa_Fora", equipaFora);
                    cmd.Parameters.AddWithValue("@Tipo_Competicao", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.ExecuteScalar();
                    Console.WriteLine($"New game synced: {codigo}");
                }
                else
                {
                    using var cmd = new SqlCommand("sp_UpdateJogo", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    try
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Game updated: {codigo} - Estado: {estado}");

                        if (estado == 3)
                        {
                            if (!ResultadoExists(conn, codigo))
                            {
                                using var cmdRes = new SqlCommand("sp_InsertResultado", conn);
                                cmdRes.CommandType = System.Data.CommandType.StoredProcedure;
                                var jogoId = GetJogoId(conn, codigo);
                                cmdRes.Parameters.AddWithValue("@Jogo_ID", jogoId);
                                cmdRes.Parameters.AddWithValue("@Golos_Casa", golosCasa);
                                cmdRes.Parameters.AddWithValue("@Golos_Fora", golosFora);
                                try
                                {
                                    cmdRes.ExecuteNonQuery();
                                    Console.WriteLine($"Resultado inserted: {codigo} - {golosCasa}:{golosFora}");
                                }
                                catch (SqlException ex)
                                {
                                    Console.WriteLine($"Error inserting resultado {codigo}: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"Error updating {codigo}: {ex.Message}");
                    }
                }
            }
        }

        private bool JogoExists(SqlConnection conn, string codigo)
        {
            using var cmd = new SqlCommand("SELECT COUNT(1) FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo", conn);
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
            return (int)cmd.ExecuteScalar() > 0;
        }

        private bool ResultadoExists(SqlConnection conn, string codigo)
        {
            using var cmd = new SqlCommand(@"SELECT COUNT(1) FROM Resultado r 
                INNER JOIN Jogo j ON r.Jogo_ID = j.ID 
                WHERE j.Codigo_Jogo = @Codigo_Jogo", conn);
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
            return (int)cmd.ExecuteScalar() > 0;
        }

        private int GetJogoId(SqlConnection conn, string codigo)
        {
            using var cmd = new SqlCommand("SELECT ID FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo", conn);
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
            return (int)cmd.ExecuteScalar();
        }
    }
}