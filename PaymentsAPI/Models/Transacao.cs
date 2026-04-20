namespace PaymentsAPI.Models
{
    public class Transacao
    {
        public int ID { get; set; }
        public int? Aposta_ID { get; set; }
        public int Utilizador_ID { get; set; }
        public string Tipo_Transacao { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data_Hora { get; set; }
        public string Estado { get; set; }
    }
}