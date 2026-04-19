using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BettingAPI.Data;

namespace BettingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstatisticasController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public EstatisticasController(DatabaseHelper db)
        {
            _db = db;
        }

        // GET: api/estatisticas/jogo/{jogoId}
        [HttpGet("jogo/{jogoId}")]
        public IActionResult GetEstatisticasJogo(int jogoId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetEstatisticasJogo", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Jogo_ID", jogoId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Ok(new
                {
                    TotalApostado = reader["TotalApostado"],
                    NumApostas1 = reader["NumApostas1"],
                    NumApostasX = reader["NumApostasX"],
                    NumApostas2 = reader["NumApostas2"],
                    NumApostasPendentes = reader["NumApostasPendentes"],
                    NumApostasGanhas = reader["NumApostasGanhas"],
                    NumApostasPerdidas = reader["NumApostasPerdidas"],
                    NumApostasAnuladas = reader["NumApostasAnuladas"],
                    MargemPlataforma = reader["MargemPlataforma"]
                });
            }

            return NotFound(new { message = $"Estatísticas para o jogo {jogoId} não encontradas." });
        }

        // GET: api/estatisticas/competicao/{tipoCompeticao}
        [HttpGet("competicao/{tipoCompeticao}")]
        public IActionResult GetEstatisticasCompeticao(string tipoCompeticao)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetEstatisticasCompeticao", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Tipo_Competicao", tipoCompeticao);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Ok(new
                {
                    MediaGolosPorJogo = reader["MediaGolosPorJogo"],
                    TotalApostado = reader["TotalApostado"],
                    TaxaVitoria1 = reader["TaxaVitoria1"],
                    TaxaVitoriaX = reader["TaxaVitoriaX"],
                    TaxaVitoria2 = reader["TaxaVitoria2"]
                });
            }

            return NotFound(new { message = $"Estatísticas para a competição {tipoCompeticao} não encontradas." });
        }
    }
}