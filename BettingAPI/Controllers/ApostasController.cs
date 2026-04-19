using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BettingAPI.Data;
using BettingAPI.Models;

namespace BettingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApostasController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public ApostasController(DatabaseHelper db)
        {
            _db = db;
        }

        // GET: api/apostas
        [HttpGet]
        public IActionResult GetApostas([FromQuery] int? utilizadorId, [FromQuery] int? jogoId, [FromQuery] int? estado, [FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            var apostas = new List<Aposta>();

            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetApostas", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Utilizador_ID", utilizadorId.HasValue ? utilizadorId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Jogo_ID", jogoId.HasValue ? jogoId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Estado", estado.HasValue ? estado.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Data_Inicio", dataInicio.HasValue ? dataInicio.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Data_Fim", dataFim.HasValue ? dataFim.Value : DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                apostas.Add(new Aposta
                {
                    ID = (int)reader["ID"],
                    Jogo_ID = (int)reader["Jogo_ID"],
                    Utilizador_ID = (int)reader["Utilizador_ID"],
                    Tipo_Aposta = reader["Tipo_Aposta"].ToString(),
                    Valor_Apostado = (decimal)reader["Valor_Apostado"],
                    Odd_Momento = (decimal)reader["Odd_Momento"],
                    Estado = (int)reader["Estado"],
                    Data_Hora_Aposta = (DateTime)reader["Data_Hora_Aposta"]
                });
            }

            return Ok(apostas);
        }

        // GET: api/apostas/{id}
        [HttpGet("{id}")]
        public IActionResult GetAposta(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetAposta", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var aposta = new Aposta
                {
                    ID = (int)reader["ID"],
                    Jogo_ID = (int)reader["Jogo_ID"],
                    Utilizador_ID = (int)reader["Utilizador_ID"],
                    Tipo_Aposta = reader["Tipo_Aposta"].ToString(),
                    Valor_Apostado = (decimal)reader["Valor_Apostado"],
                    Odd_Momento = (decimal)reader["Odd_Momento"],
                    Estado = (int)reader["Estado"],
                    Data_Hora_Aposta = (DateTime)reader["Data_Hora_Aposta"]
                };

                var premioPotencial = aposta.Valor_Apostado * aposta.Odd_Momento;
                return Ok(new { aposta, premioPotencial });
            }

            return NotFound(new { message = $"Aposta {id} não encontrada." });
        }

        // POST: api/apostas
        [HttpPost]
        public IActionResult CreateAposta([FromBody] Aposta aposta)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_InsertAposta", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Jogo_ID", aposta.Jogo_ID);
            cmd.Parameters.AddWithValue("@Utilizador_ID", aposta.Utilizador_ID);
            cmd.Parameters.AddWithValue("@Tipo_Aposta", aposta.Tipo_Aposta);
            cmd.Parameters.AddWithValue("@Valor_Apostado", aposta.Valor_Apostado);
            cmd.Parameters.AddWithValue("@Odd_Momento", aposta.Odd_Momento);

            try
            {
                var id = cmd.ExecuteScalar();
                return CreatedAtAction(nameof(GetAposta), new { id }, new { id });
            }
            catch (SqlException ex) when (ex.Number == 50006)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/apostas/{id}/cancelar
        [HttpPut("{id}/cancelar")]
        public IActionResult CancelarAposta(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_CancelarAposta", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID", id);

            try
            {
                cmd.ExecuteNonQuery();
                return Ok(new { message = "Aposta cancelada com sucesso." });
            }
            catch (SqlException ex) when (ex.Number == 50007)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}