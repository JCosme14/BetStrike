using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BettingAPI.Data;
using BettingAPI.Models;

namespace BettingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultadosController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public ResultadosController(DatabaseHelper db)
        {
            _db = db;
        }

        // POST: api/resultados
        [HttpPost]
        public IActionResult InsertResultado([FromBody] Resultado resultado)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_InsertResultado", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Jogo_ID", resultado.Jogo_ID);
            cmd.Parameters.AddWithValue("@Golos_Casa", resultado.Golos_Casa);
            cmd.Parameters.AddWithValue("@Golos_Fora", resultado.Golos_Fora);

            try
            {
                cmd.ExecuteNonQuery();
                return CreatedAtAction(nameof(GetResultado), new { jogoId = resultado.Jogo_ID }, resultado);
            }
            catch (SqlException ex) when (ex.Number == 50008)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/resultados/{jogoId}
        [HttpGet("{jogoId}")]
        public IActionResult GetResultado(int jogoId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetResultado", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Jogo_ID", jogoId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var resultado = new Resultado
                {
                    ID = (int)reader["ID"],
                    Jogo_ID = (int)reader["Jogo_ID"],
                    Golos_Casa = (int)reader["Golos_Casa"],
                    Golos_Fora = (int)reader["Golos_Fora"],
                    Data_Hora_Atualizacao = (DateTime)reader["Data_Hora_Atualizacao"]
                };
                return Ok(resultado);
            }

            return NotFound(new { message = $"Resultado para o jogo {jogoId} não encontrado." });
        }
    }
}