namespace BettingAPI.Models
{
    public class Aposta
    {
        public int ID { get; set; }
        public int Jogo_ID { get; set; }
        public int Utilizador_ID { get; set; }
        public string Tipo_Aposta { get; set; }
        public decimal Valor_Apostado { get; set; }
        public decimal Odd_Momento { get; set; }
        public int Estado { get; set; }
        public DateTime Data_Hora_Aposta { get; set; }
    }
}