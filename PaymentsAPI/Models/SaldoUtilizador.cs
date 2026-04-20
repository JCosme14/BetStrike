namespace PaymentsAPI.Models
{
    public class SaldoUtilizador
    {
        public int Utilizador_ID { get; set; }
        public decimal Saldo { get; set; }
        public DateTime Data_Hora_Atualizacao { get; set; }
    }
}