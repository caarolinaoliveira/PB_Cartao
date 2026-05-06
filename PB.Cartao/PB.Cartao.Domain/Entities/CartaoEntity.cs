namespace PB.Cartao.Domain.Entities
{
    public class CartaoEntity
    {
        public Guid Id { get; private set; }
        public Guid ClienteId { get; private set; }
        public Guid PropostaId { get; private set; }
        public string Numero { get; private set; } = string.Empty;
        public decimal Limite { get; private set; }
        public int Sequencial { get; private set; } 
        public DateTime CriadoEm { get; private set; }

        // EF Core
        protected CartaoEntity() { }

        public CartaoEntity(Guid clienteId, Guid propostaId, decimal limite, int sequencial)
        {
            Id = Guid.NewGuid();
            ClienteId = clienteId;
            PropostaId = propostaId;
            Limite = limite;
            Sequencial = sequencial;
            Numero = GerarNumeroCartao();
            CriadoEm = DateTime.UtcNow;
        }

        private string GerarNumeroCartao()
        {
            var random = new Random();
            return string.Format("{0:0000} {1:0000} {2:0000} {3:0000}",
                random.Next(1000, 9999),
                random.Next(1000, 9999),
                random.Next(1000, 9999),
                random.Next(1000, 9999));
        }
    }
}