namespace PB.Cartao.Application.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublicarAsync<T>(T evento, string fila) where T : class;
    }
}