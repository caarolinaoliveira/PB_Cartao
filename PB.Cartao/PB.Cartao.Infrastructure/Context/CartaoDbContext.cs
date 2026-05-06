using Microsoft.EntityFrameworkCore;
using PB.Cartao.Domain.Entities;

namespace PB.Cartao.Infrastructure.Context
{
    public class CartaoDbContext : DbContext
    {
        public CartaoDbContext(DbContextOptions<CartaoDbContext> options)
            : base(options) { }

        public DbSet<CartaoEntity> Cartoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CartaoEntity>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ClienteId)
                    .IsRequired();

                entity.Property(e => e.PropostaId)
                    .IsRequired();

                entity.Property(e => e.Numero)
                    .IsRequired()
                    .HasMaxLength(19); // formato "0000 0000 0000 0000"

                entity.Property(e => e.Limite)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Sequencial)
                    .IsRequired();

                entity.Property(e => e.CriadoEm)
                    .IsRequired();

                entity.HasIndex(e => e.ClienteId);

                entity.HasIndex(e => new { e.ClienteId, e.Sequencial })
                    .IsUnique();
            });
        }
    }
}