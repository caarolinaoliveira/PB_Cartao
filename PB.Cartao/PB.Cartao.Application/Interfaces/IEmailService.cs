namespace PB.Cartao.Application.Interfaces
{
    public interface IEmailService
    {
        Task EnviarCartaoEmitidoAsync(string email, string nome, int numeroCartao, decimal limite);
    }
}