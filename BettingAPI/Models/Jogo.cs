namespace BettingAPI.Models
{
    public class Jogo
    {
        public int ID { get; set; }
        public string Codigo_Jogo { get; set; }
        public DateTime Data_Hora_Inicio { get; set; }
        public string Equipa_Casa { get; set; }
        public string Equipa_Fora { get; set; }
        public string Tipo_Competicao { get; set; }
        public int Estado { get; set; }
    }
}