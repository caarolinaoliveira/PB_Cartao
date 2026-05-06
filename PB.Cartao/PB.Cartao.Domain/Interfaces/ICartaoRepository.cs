using PB.Cartao.Domain.Entities;

namespace PB.Cartao.Domain.Interfaces
{
    public interface ICartaoRepository : IDisposable
    {
        Task AdicionarAsync(CartaoEntity cartao);
        Task<CartaoEntity?> ObterPorIdAsync(Guid id);
        Task<List<CartaoEntity>> ObterPorClienteIdAsync(Guid clienteId);
        Task<List<CartaoEntity>> ObterTodosAsync();
        Task<int> SaveChangesAsync();
    }
    
}