using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PaymentsAPI.Data;
using PaymentsAPI.Models;

namespace PaymentsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransacoesController : ControllerBase
    {
        private readonly DatabaseHelper _db;

        public TransacoesController(DatabaseHelper db)
        {
            _db = db;
        }

        // GET: api/transacoes/{utilizadorId}
        [HttpGet("{utilizadorId}")]
        public IActionResult GetTransacoes(int utilizadorId)
        {
            var transacoes = new List<Transacao>();

            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetTransacoes", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Utilizador_ID", utilizadorId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                transacoes.Add(new Transacao
                {
                    ID = (int)reader["ID"],
                    Aposta_ID = reader["Aposta_ID"] == DBNull.Value ? null : (int?)reader["Aposta_ID"],
                    Utilizador_ID = (int)reader["Utilizador_ID"],
                    Tipo_Transacao = reader["Tipo_Transacao"].ToString(),
                    Valor = (decimal)reader["Valor"],
                    Data_Hora = (DateTime)reader["Data_Hora"],
                    Estado = reader["Estado"].ToString()
                });
            }

            return Ok(transacoes);
        }
    }
}