namespace BettingAPI.Models
{
    public class Utilizador
    {
        public int ID { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }      // frontend sends this as "username"
        public string? Password { get; set; }  // ADD THIS
        public DateTime Data_Registo { get; set; }
    }
}