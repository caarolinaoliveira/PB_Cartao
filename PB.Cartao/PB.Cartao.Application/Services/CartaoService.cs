using PB.Cartao.Application.Events;
using PB.Cartao.Application.Interfaces;
using PB.Cartao.Application.Response;
using PB.Cartao.Domain.Entities;
using PB.Cartao.Domain.Interfaces;
using PB.Cartao.Domain.Exceptions;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Logging;

namespace PB.Cartao.Application.Services
{
    public class CartaoService : ICartaoService
    {
        private readonly ICartaoRepository _cartaoRepository;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IEmailService _emailService;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
        private readonly ILogger<CartaoService> _logger;

        public CartaoService(
            ICartaoRepository cartaoRepository,
            IMessagePublisher messagePublisher,
            IEmailService emailService,
            AsyncCircuitBreakerPolicy circuitBreaker,
            ILogger<CartaoService> logger)
        {
            _cartaoRepository = cartaoRepository;
            _messagePublisher = messagePublisher;
            _emailService = emailService;
            _circuitBreaker = circuitBreaker;
            _logger = logger;
        }

        public async Task ProcessarAsync(CreditoAprovadoEvent evento)
        {
            _logger.LogInformation("Processando evento de crédito aprovado para cliente {ClienteId} com proposta {PropostaId}", evento.ClienteId, evento.PropostaId);

            var cartoesExistentes = await _cartaoRepository
                .ObterPorClienteIdAsync(evento.ClienteId);
                
            if (cartoesExistentes.Count >= evento.QuantidadeCartoes)
            {
                _logger.LogWarning("[CARTAO] Já processado — ignorando | ClienteId: {Id}", evento.ClienteId);
                return;
            }

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
                _logger.LogInformation("[CARTAO] Emitido cartão {Seq}/{Total} | ClienteId: {Id}", i, evento.QuantidadeCartoes, evento.ClienteId);

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

                await _circuitBreaker.ExecuteAsync(() => _messagePublisher.PublicarAsync(cartaoEmitido, "cartao.emitido"));
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