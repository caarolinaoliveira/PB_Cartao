namespace PB.Cartao.Application.Events
{
    public class CartaoEmitidoEvent
    {
        public Guid CartaoId { get; init; }
        public Guid ClienteId { get; init; }
        public Guid PropostaId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public decimal Limite { get; init; }
        public int Numero { get; init; }
        public DateTime OcorridoEm { get; init; } = DateTime.UtcNow;
    }
}