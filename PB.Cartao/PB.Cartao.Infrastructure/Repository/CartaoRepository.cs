using Microsoft.EntityFrameworkCore;
using PB.Cartao.Domain.Entities;
using PB.Cartao.Domain.Interfaces;
using PB.Cartao.Infrastructure.Context;

namespace PB.Cartao.Infrastructure.Repository
{
    public class CartaoRepository : ICartaoRepository
    {
        private readonly CartaoDbContext _db;
        private readonly DbSet<CartaoEntity> _dbSet;

        public CartaoRepository(CartaoDbContext db)
        {
            _db = db;
            _dbSet = db.Set<CartaoEntity>();
        }

        public async Task AdicionarAsync(CartaoEntity cartao)
        {
            _dbSet.Add(cartao);
            await SaveChangesAsync();
        }

        public async Task<CartaoEntity?> ObterPorIdAsync(Guid id)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<CartaoEntity>> ObterPorClienteIdAsync(Guid clienteId)
        {
            return await _dbSet.AsNoTracking()
                .Where(c => c.ClienteId == clienteId)
                .OrderBy(c => c.Sequencial)
                .ToListAsync();
        }

        public async Task<List<CartaoEntity>> ObterTodosAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}