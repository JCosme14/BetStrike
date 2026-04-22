using Microsoft.Data.SqlClient;

namespace BettingAPI.Data
{
    public class PagamentosDatabaseHelper
    {
        private readonly string _connectionString;

        public PagamentosDatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Pagamentos");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}