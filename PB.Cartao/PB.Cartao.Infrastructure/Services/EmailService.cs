using Microsoft.Extensions.Logging;
using PB.Cartao.Application.Interfaces;

namespace PB.Cartao.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task EnviarCartaoEmitidoAsync(
            string email, string nome, int numeroCartao, decimal limite)
        {
            _logger.LogInformation(
                "[EMAIL] Cartão #{Numero} emitido para {Email} | Cliente: {Nome} | Limite: {Limite:C}",
                numeroCartao, email, nome, limite);

            await Task.Delay(100);
        }
    }
}