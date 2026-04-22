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
        private readonly PagamentosDatabaseHelper _pagamentosDb;

        public ApostasController(DatabaseHelper db, PagamentosDatabaseHelper pagamentosDb)
        {
            _db = db;
            _pagamentosDb = pagamentosDb;
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

            return NotFound(new { message = $"Aposta {id} nao encontrada." });
        }

        // POST: api/apostas
        // Accepts Codigo_Jogo instead of internal Jogo_ID for easier integration
        [HttpPost]
        public IActionResult CreateAposta([FromBody] ApostaCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Codigo_Jogo))
                return BadRequest(new { message = "codigo_Jogo e obrigatorio." });

            // Step 1: Resolve Codigo_Jogo -> internal Jogo_ID
            int jogoId;
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                using var lookupCmd = new SqlCommand(
                    "SELECT ID FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo", conn);
                lookupCmd.Parameters.AddWithValue("@Codigo_Jogo", request.Codigo_Jogo);

                var result = lookupCmd.ExecuteScalar();
                if (result == null)
                    return NotFound(new { message = $"Jogo com codigo {request.Codigo_Jogo} nao encontrado." });

                jogoId = (int)result;
            }

            // Step 2: Check balance BEFORE inserting the bet
            decimal saldoAtual;
            using (var pagConn = _pagamentosDb.GetConnection())
            {
                pagConn.Open();
                using var saldoCmd = new SqlCommand("sp_GetSaldo", pagConn);
                saldoCmd.CommandType = System.Data.CommandType.StoredProcedure;
                saldoCmd.Parameters.AddWithValue("@Utilizador_ID", request.Utilizador_ID);

                using var saldoReader = saldoCmd.ExecuteReader();
                if (!saldoReader.Read())
                    return BadRequest(new { message = "Utilizador nao encontrado no sistema de pagamentos." });

                saldoAtual = (decimal)saldoReader["Saldo"];
            }

            if (saldoAtual < request.Valor_Apostado)
                return BadRequest(new { message = $"Saldo insuficiente. Saldo atual: {saldoAtual:F2}€, valor apostado: {request.Valor_Apostado:F2}€." });

            // Step 3: Insert the bet into Apostas DB
            int newApostaId;
            using (var conn = _db.GetConnection())
            {
                conn.Open();

                using var cmd = new SqlCommand("sp_InsertAposta", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Jogo_ID", jogoId);
                cmd.Parameters.AddWithValue("@Utilizador_ID", request.Utilizador_ID);
                cmd.Parameters.AddWithValue("@Tipo_Aposta", request.Tipo_Aposta);
                cmd.Parameters.AddWithValue("@Valor_Apostado", request.Valor_Apostado);
                cmd.Parameters.AddWithValue("@Odd_Momento", request.Odd_Momento);

                try
                {
                    var result = cmd.ExecuteScalar();
                    newApostaId = Convert.ToInt32(result);
                }
                catch (SqlException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            // Step 4: Debit the balance in Pagamentos DB
            try
            {
                using var pagConn = _pagamentosDb.GetConnection();
                pagConn.Open();

                using var debitCmd = new SqlCommand("sp_DebitarAposta", pagConn);
                debitCmd.CommandType = System.Data.CommandType.StoredProcedure;
                debitCmd.Parameters.AddWithValue("@Utilizador_ID", request.Utilizador_ID);
                debitCmd.Parameters.AddWithValue("@Aposta_ID", newApostaId);
                debitCmd.Parameters.AddWithValue("@Valor", request.Valor_Apostado);
                debitCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Rollback: delete the bet so the system stays consistent
                try
                {
                    using var rollbackConn = _db.GetConnection();
                    rollbackConn.Open();
                    using var rollbackCmd = new SqlCommand(
                        "DELETE FROM Aposta WHERE ID = @ID", rollbackConn);
                    rollbackCmd.Parameters.AddWithValue("@ID", newApostaId);
                    rollbackCmd.ExecuteNonQuery();
                }
                catch { /* best effort rollback */ }

                return StatusCode(500, new
                {
                    message = "Aposta registada mas falhou o debito do saldo. Operacao revertida.",
                    detail = ex.Message
                });
            }

            return CreatedAtAction(nameof(GetAposta), new { id = newApostaId }, new
            {
                id = newApostaId,
                codigo_Jogo = request.Codigo_Jogo,
                jogo_ID = jogoId,
                premioPotencial = request.Valor_Apostado * request.Odd_Momento
            });
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
                // Saldo credit on cancellation is handled automatically by the trigger
                return Ok(new { message = "Aposta cancelada com sucesso." });
            }
            catch (SqlException ex) when (ex.Number == 50007)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}