namespace BettingAPI.Models
{
    public class Resultado
    {
        public int ID { get; set; }
        public int Jogo_ID { get; set; }
        public int Golos_Casa { get; set; }
        public int Golos_Fora { get; set; }
        public DateTime Data_Hora_Atualizacao { get; set; }
    }
}