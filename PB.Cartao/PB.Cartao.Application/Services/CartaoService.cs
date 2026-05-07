using PB.Cartao.Application.Events;
using PB.Cartao.Application.Interfaces;
using PB.Cartao.Application.Response;
using PB.Cartao.Domain.Entities;
using PB.Cartao.Domain.Interfaces;
using PB.Cartao.Domain.Exceptions;
using Polly.CircuitBreaker;
using Polly;

namespace PB.Cartao.Application.Services
{
    public class CartaoService : ICartaoService
    {
        private readonly ICartaoRepository _cartaoRepository;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IEmailService _emailService;

        private static readonly AsyncCircuitBreakerPolicy _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, duration) =>
                    Console.WriteLine($"[CIRCUIT BREAKER] Aberto por {duration.TotalSeconds}s"),
                onReset: () =>
                    Console.WriteLine("[CIRCUIT BREAKER] Fechado — retomando operacao"),
                onHalfOpen: () =>
                    Console.WriteLine("[CIRCUIT BREAKER] Half-open — testando")
            );

        public CartaoService(
            ICartaoRepository cartaoRepository,
            IMessagePublisher messagePublisher,
            IEmailService emailService)
        {
            _cartaoRepository = cartaoRepository;
            _messagePublisher = messagePublisher;
            _emailService = emailService;
        }

        public async Task ProcessarAsync(CreditoAprovadoEvent evento)
        {
            var cartoesExistentes = await _cartaoRepository
                .ObterPorClienteIdAsync(evento.ClienteId);
            
     
            if (cartoesExistentes == null || !cartoesExistentes.Any())
                throw new NotFoundException($"Nenhum cartão encontrado para o cliente {evento.ClienteId}.");
                
            if (cartoesExistentes.Count >= evento.QuantidadeCartoes)
                throw new ConflictException("Quantidade de cartoes ja emitidos e igual ou superior a quantidade aprovada.");

            for (int i = cartoesExistentes.Count + 1; i <= evento.QuantidadeCartoes; i++)
            {
                var cartao = new CartaoEntity(
                    evento.ClienteId,
                    evento.PropostaId,
                    evento.LimiteAprovado,
                    sequencial: i
                );

                await _cartaoRepository.AdicionarAsync(cartao);

                await _emailService.EnviarCartaoEmitidoAsync(
                    evento.Email,
                    evento.Nome,
                    numeroCartao: i,
                    cartao.Limite
                );

                var cartaoEmitido = new CartaoEmitidoEvent
                {
                    CartaoId = cartao.Id,
                    ClienteId = cartao.ClienteId,
                    PropostaId = cartao.PropostaId,
                    Email = evento.Email,
                    Nome = evento.Nome,
                    Limite = cartao.Limite,
                    Numero = i,
                    OcorridoEm = DateTime.UtcNow
                };

                await _circuitBreaker.ExecuteAsync(async () =>
                    await _messagePublisher.PublicarAsync(cartaoEmitido, "cartao.emitido")
                );
            }
        }

        public async Task<List<CartaoResponse>> BuscarCartaoPorIdCliente(Guid clienteId)
        {
            var cartoes = await _cartaoRepository.ObterPorClienteIdAsync(clienteId);

            if (cartoes == null || !cartoes.Any())
                throw new NotFoundException("Nenhum cartao encontrado para o cliente.");

            return cartoes.Select(cartao => new CartaoResponse
            {
                Limite = cartao.Limite,
                Sequencial = cartao.Sequencial,
                NumeroCartao = cartao.Numero,
                CriadoEm = cartao.CriadoEm
            }).ToList();
        }
    }
}