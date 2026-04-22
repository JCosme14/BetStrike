using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BettingAPI.Data;
using BettingAPI.Models;

namespace BettingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilizadoresController : ControllerBase
    {
        private readonly DatabaseHelper _db;
        private readonly PagamentosDatabaseHelper _pagamentosDb;

        public UtilizadoresController(DatabaseHelper db, PagamentosDatabaseHelper pagamentosDb)
        {
            _db = db;
            _pagamentosDb = pagamentosDb;
        }

        // POST: api/utilizadores
        [HttpPost]
        public IActionResult CreateUtilizador([FromBody] Utilizador utilizador)
        {
            // Step 1: Insert user into Apostas DB
            int newUserId;

            using (var conn = _db.GetConnection())
            {
                conn.Open();

                using var cmd = new SqlCommand("sp_InsertUtilizador", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Nome", utilizador.Nome);
                cmd.Parameters.AddWithValue("@Email", utilizador.Email);

                try
                {
                    var result = cmd.ExecuteScalar();
                    newUserId = Convert.ToInt32(result);
                }
                catch (SqlException ex) when (ex.Number == 50005)
                {
                    return Conflict(new { message = ex.Message });
                }
            }

            // Step 2: Create Saldo in Pagamentos DB with 50 euro promotion
            // If this fails we rollback the user creation to keep systems consistent
            try
            {
                using var pagConn = _pagamentosDb.GetConnection();
                pagConn.Open();

                using var pagCmd = new SqlCommand("sp_CriarSaldoUtilizador", pagConn);
                pagCmd.CommandType = System.Data.CommandType.StoredProcedure;
                pagCmd.Parameters.AddWithValue("@Utilizador_ID", newUserId);
                pagCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Rollback: delete the user we just created so the system stays consistent
                try
                {
                    using var rollbackConn = _db.GetConnection();
                    rollbackConn.Open();
                    using var rollbackCmd = new SqlCommand(
                        "DELETE FROM Utilizador WHERE ID = @ID", rollbackConn);
                    rollbackCmd.Parameters.AddWithValue("@ID", newUserId);
                    rollbackCmd.ExecuteNonQuery();
                }
                catch { /* best effort rollback */ }

                return StatusCode(500, new
                {
                    message = "Utilizador criado mas falhou a inicializacao do saldo. Operacao revertida.",
                    detail = ex.Message
                });
            }

            return CreatedAtAction(nameof(GetUtilizador), new { id = newUserId }, new
            {
                id = newUserId,
                utilizador.Nome,
                utilizador.Email,
                saldoInicial = 50.00
            });
        }

        // GET: api/utilizadores/{id}
        [HttpGet("{id}")]
        public IActionResult GetUtilizador(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_GetUtilizador", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ID", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var utilizador = new Utilizador
                {
                    ID = (int)reader["ID"],
                    Nome = reader["Nome"].ToString(),
                    Email = reader["Email"].ToString(),
                    Data_Registo = (DateTime)reader["Data_Registo"]
                };
                return Ok(utilizador);
            }

            return NotFound(new { message = $"Utilizador {id} nao encontrado." });
        }
    }
}