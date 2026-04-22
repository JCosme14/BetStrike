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

        public UtilizadoresController(DatabaseHelper db)
        {
            _db = db;
        }

        // POST: api/utilizadores/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] Utilizador utilizador)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            // Simple lookup by email — add password check if you have hashing
            using var cmd = new SqlCommand("sp_GetUtilizadorByEmail", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", utilizador.Email);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return Ok(new
                {
                    id = (int)reader["ID"],
                    username = reader["Nome"].ToString(),
                    email = reader["Email"].ToString()
                });
            }
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        // POST: api/utilizadores
        [HttpPost]
        public IActionResult CreateUtilizador([FromBody] Utilizador utilizador)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand("sp_InsertUtilizador", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Nome", utilizador.Nome);
            cmd.Parameters.AddWithValue("@Email", utilizador.Email);

            try
            {
                var id = cmd.ExecuteScalar();
                return CreatedAtAction(nameof(GetUtilizador), new { id }, new { id, utilizador.Nome, utilizador.Email });
            }
            catch (SqlException ex) when (ex.Number == 50005)
            {
                return Conflict(new { message = ex.Message });
            }
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

            return NotFound(new { message = $"Utilizador {id} não encontrado." });
        }
    }
}