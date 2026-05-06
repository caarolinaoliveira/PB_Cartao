namespace PB.Cartao.Application.Response
{
    public sealed record CartaoResponse
    {
        public decimal Limite { get; init; }
        public int Sequencial {get ; init;}
        public string NumeroCartao {get;init;}
        public DateTime CriadoEm { get; init; }
    }

}