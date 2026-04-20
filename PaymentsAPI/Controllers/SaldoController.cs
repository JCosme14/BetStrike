using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PaymentsAPI.Data;
using PaymentsAPI.Models;

namespace PaymentsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SaldoController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public SaldoController(DatabaseHelper db)
        {
            _db = db;
        }

        // GET: api/saldo/{utilizadorId}
        [HttpGet("{utilizadorId}")]
        public IActionResult GetSaldo(int utilizadorId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetSaldo", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Utilizador_ID", utilizadorId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var saldo = new SaldoUtilizador
                {
                    Utilizador_ID = (int)reader["Utilizador_ID"],
                    Saldo = (decimal)reader["Saldo"],
                    Data_Hora_Atualizacao = (DateTime)reader["Data_Hora_Atualizacao"]
                };
                return Ok(saldo);
            }

            return NotFound(new { message = $"Saldo para o utilizador {utilizadorId} não encontrado." });
        }

        // POST: api/saldo/deposito
        [HttpPost("deposito")]
        public IActionResult Deposito([FromBody] Transacao transacao)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_Deposito", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Utilizador_ID", transacao.Utilizador_ID);
            cmd.Parameters.AddWithValue("@Valor", transacao.Valor);

            try
            {
                cmd.ExecuteNonQuery();
                return Ok(new { message = "Depósito realizado com sucesso." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}