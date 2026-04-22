namespace BettingAPI.Models
{
    // DTO for bet creation - uses Codigo_Jogo (public code) instead of internal Jogo_ID
    public class ApostaCreateRequest
    {
        public string Codigo_Jogo { get; set; }
        public int Utilizador_ID { get; set; }
        public string Tipo_Aposta { get; set; }
        public decimal Valor_Apostado { get; set; }
        public decimal Odd_Momento { get; set; }
    }
}