using BettingAPI.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace BettingAPI.Services
{
    public class GameSyncService : BackgroundService
    {
        private readonly DatabaseHelper _db;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _fpfApiUrl;

        public GameSyncService(DatabaseHelper db, IConfiguration configuration)
        {
            _db = db;
            _fpfApiUrl = configuration["ExternalServices:FpfApiUrl"]
                ?? "http://localhost:5221/api/jogos";
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
                var estadoFpf = jogo.GetProperty("estado").GetInt32();
                var golosCasa = jogo.GetProperty("golos_Casa").GetInt32();
                var golosFora = jogo.GetProperty("golos_Fora").GetInt32();
                string? tipoCompeticao = null;
                if (jogo.TryGetProperty("tipo_Competicao", out var tc) && tc.ValueKind != JsonValueKind.Null)
                    tipoCompeticao = tc.GetString();

                using var conn = _db.GetConnection();
                conn.Open();

                var existing = GetJogoState(conn, codigo);

                if (existing == null)
                {
                    // New game: insert it
                    using var cmd = new SqlCommand("sp_InsertJogo", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
                    cmd.Parameters.AddWithValue("@Data_Hora_Inicio", dataHoraInicio);
                    cmd.Parameters.AddWithValue("@Equipa_Casa", equipaCasa);
                    cmd.Parameters.AddWithValue("@Equipa_Fora", equipaFora);
                    cmd.Parameters.AddWithValue("@Tipo_Competicao", (object?)tipoCompeticao ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Estado", estadoFpf);
                    cmd.ExecuteScalar();
                    Console.WriteLine($"New game synced: {codigo}");
                    continue;
                }

                // Skip games already in terminal states (Finalizado, Cancelado, Adiado)
                if (existing.Estado == 3 || existing.Estado == 4 || existing.Estado == 5)
                    continue;

                // Skip if nothing meaningful changed
                if (existing.Estado == estadoFpf
                    && existing.GolosCasa == golosCasa
                    && existing.GolosFora == golosFora)
                    continue;

                // Update state and live score
                using (var cmd = new SqlCommand("sp_UpdateJogo", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
                    cmd.Parameters.AddWithValue("@Estado", estadoFpf);
                    cmd.Parameters.AddWithValue("@Golos_Casa", golosCasa);
                    cmd.Parameters.AddWithValue("@Golos_Fora", golosFora);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Game updated: {codigo} - Estado: {estadoFpf} - Score: {golosCasa}:{golosFora}");
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"Error updating {codigo}: {ex.Message}");
                        continue;
                    }
                }

                // When game just finished, also insert the final result into Resultado table
                if (estadoFpf == 3 && !ResultadoExists(conn, codigo))
                {
                    var jogoId = GetJogoId(conn, codigo);

                    using var cmdRes = new SqlCommand("sp_InsertResultado", conn);
                    cmdRes.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdRes.Parameters.AddWithValue("@Jogo_ID", jogoId);
                    cmdRes.Parameters.AddWithValue("@Golos_Casa", golosCasa);
                    cmdRes.Parameters.AddWithValue("@Golos_Fora", golosFora);

                    try
                    {
                        cmdRes.ExecuteNonQuery();
                        Console.WriteLine($"Result inserted: {codigo} - {golosCasa}:{golosFora}");
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"Error inserting result {codigo}: {ex.Message}");
                    }
                }
            }
        }

        private class JogoState
        {
            public int Estado { get; set; }
            public int GolosCasa { get; set; }
            public int GolosFora { get; set; }
        }

        private JogoState GetJogoState(SqlConnection conn, string codigo)
        {
            using var cmd = new SqlCommand(
                "SELECT Estado, Golos_Casa, Golos_Fora FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo", conn);
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new JogoState
            {
                Estado = (int)reader["Estado"],
                GolosCasa = (int)reader["Golos_Casa"],
                GolosFora = (int)reader["Golos_Fora"]
            };
        }

        private bool ResultadoExists(SqlConnection conn, string codigo)
        {
            using var cmd = new SqlCommand(@"
                SELECT COUNT(1) FROM Resultado r
                INNER JOIN Jogo j ON r.Jogo_ID = j.ID
                WHERE j.Codigo_Jogo = @Codigo_Jogo", conn);
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
            return (int)cmd.ExecuteScalar() > 0;
        }

        private int GetJogoId(SqlConnection conn, string codigo)
        {
            using var cmd = new SqlCommand(
                "SELECT ID FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo", conn);
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
            return (int)cmd.ExecuteScalar();
        }
    }
}