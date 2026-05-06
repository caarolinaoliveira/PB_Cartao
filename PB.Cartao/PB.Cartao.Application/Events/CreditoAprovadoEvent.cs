namespace PB.Cartao.Application.Events
{
    public class CreditoAprovadoEvent
    {
        public Guid PropostaId { get; init; }
        public Guid ClienteId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public decimal LimiteAprovado { get; init; }
        public int QuantidadeCartoes { get; init; }
        public DateTime OcorridoEm { get; init; }
    }
}