using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BettingAPI.Data;
using BettingAPI.Models;

namespace BettingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogosController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public JogosController(DatabaseHelper db)
        {
            _db = db;
        }

        // GET: api/jogos
        [HttpGet]
        public IActionResult GetJogos([FromQuery] DateTime? data, [FromQuery] int? estado, [FromQuery] string? tipoCompeticao)
        {
            var jogos = new List<Jogo>();

            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetJogos", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Data", data.HasValue ? data.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", estado.HasValue ? estado.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Tipo_Competicao", tipoCompeticao ?? (object)DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                jogos.Add(new Jogo
                {
                    ID = (int)reader["ID"],
                    Codigo_Jogo = reader["Codigo_Jogo"].ToString(),
                    Data_Hora_Inicio = (DateTime)reader["Data_Hora_Inicio"],
                    Equipa_Casa = reader["Equipa_Casa"].ToString(),
                    Equipa_Fora = reader["Equipa_Fora"].ToString(),
                    Tipo_Competicao = reader["Tipo_Competicao"] == DBNull.Value
                        ? null : reader["Tipo_Competicao"].ToString(),
                    Estado = (int)reader["Estado"],
                    Golos_Casa = (int)reader["Golos_Casa"],
                    Golos_Fora = (int)reader["Golos_Fora"]
                });
            }

            return Ok(jogos);
        }

        // GET: api/jogos/{codigo}
        // Returns full game data including pending bet count and total wagered
        [HttpGet("{codigo}")]
        public IActionResult GetJogo(string codigo)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetJogo", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);

            Jogo jogo;
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                    return NotFound(new { message = $"Jogo {codigo} nao encontrado." });

                jogo = new Jogo
                {
                    ID = (int)reader["ID"],
                    Codigo_Jogo = reader["Codigo_Jogo"].ToString(),
                    Data_Hora_Inicio = (DateTime)reader["Data_Hora_Inicio"],
                    Equipa_Casa = reader["Equipa_Casa"].ToString(),
                    Equipa_Fora = reader["Equipa_Fora"].ToString(),
                    Tipo_Competicao = reader["Tipo_Competicao"] == DBNull.Value
                        ? null : reader["Tipo_Competicao"].ToString(),
                    Estado = (int)reader["Estado"],
                    Golos_Casa = (int)reader["Golos_Casa"],
                    Golos_Fora = (int)reader["Golos_Fora"]
                };
            }

            // Fetch pending bet count and total wagered for this game
            using var statsCmd = new SqlCommand(@"
                SELECT
                    COUNT(CASE WHEN Estado = 1 THEN 1 END) AS ApostasPendentes,
                    ISNULL(SUM(Valor_Apostado), 0) AS TotalApostado
                FROM Aposta WHERE Jogo_ID = @Jogo_ID", conn);
            statsCmd.Parameters.AddWithValue("@Jogo_ID", jogo.ID);

            using var statsReader = statsCmd.ExecuteReader();
            int apostasPendentes = 0;
            decimal totalApostado = 0;
            if (statsReader.Read())
            {
                apostasPendentes = (int)statsReader["ApostasPendentes"];
                totalApostado = (decimal)statsReader["TotalApostado"];
            }

            return Ok(new
            {
                jogo,
                apostasPendentes,
                totalApostado
            });
        }

        // POST: api/jogos
        [HttpPost]
        public IActionResult CreateJogo([FromBody] Jogo jogo)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_InsertJogo", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Codigo_Jogo", jogo.Codigo_Jogo);
            cmd.Parameters.AddWithValue("@Data_Hora_Inicio", jogo.Data_Hora_Inicio);
            cmd.Parameters.AddWithValue("@Equipa_Casa", jogo.Equipa_Casa);
            cmd.Parameters.AddWithValue("@Equipa_Fora", jogo.Equipa_Fora);
            cmd.Parameters.AddWithValue("@Tipo_Competicao", jogo.Tipo_Competicao ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", jogo.Estado);

            try
            {
                var id = cmd.ExecuteScalar();
                return CreatedAtAction(nameof(GetJogo), new { codigo = jogo.Codigo_Jogo }, new { id, jogo.Codigo_Jogo });
            }
            catch (SqlException ex) when (ex.Number == 50001)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // PUT: api/jogos/{codigo}
        [HttpPut("{codigo}")]
        public IActionResult UpdateJogo(string codigo, [FromBody] Jogo jogo)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_UpdateJogo", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);
            cmd.Parameters.AddWithValue("@Estado", jogo.Estado);
            cmd.Parameters.AddWithValue("@Golos_Casa", jogo.Golos_Casa);
            cmd.Parameters.AddWithValue("@Golos_Fora", jogo.Golos_Fora);

            try
            {
                cmd.ExecuteNonQuery();
                return Ok(new { message = "Jogo atualizado com sucesso." });
            }
            catch (SqlException ex) when (ex.Number == 50002)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (SqlException ex) when (ex.Number == 50003)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/jogos/{codigo}
        [HttpDelete("{codigo}")]
        public IActionResult DeleteJogo(string codigo)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_DeleteJogo", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Codigo_Jogo", codigo);

            try
            {
                cmd.ExecuteNonQuery();
                return Ok(new { message = "Jogo removido com sucesso." });
            }
            catch (SqlException ex) when (ex.Number == 50004)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}