using PB.Cartao.Application.Events;
using PB.Cartao.Application.Response;

namespace PB.Cartao.Application.Interfaces
{
    public interface ICartaoService
    {
        Task ProcessarAsync(CreditoAprovadoEvent evento);
        Task<List<CartaoResponse>> BuscarCartaoPorIdCliente(Guid clienteId);
    }
}