using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PB.Cartao.Application.Events;
using PB.Cartao.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PB.Cartao.Infrastructure.Messaging
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly IConnection _connection;
        private IModel? _channel;

        public RabbitMQConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMQConsumer> logger,
            IConnection connection)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _connection = connection;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "credito.aprovado",
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogInformation(
                    "[CONSUMER] Mensagem recebida na fila credito.aprovado: {Json}", json);

                try
                {
                    var evento = JsonSerializer.Deserialize<CreditoAprovadoEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evento is null)
                    {
                        _logger.LogWarning("[CONSUMER] Evento null — descartando.");
                        _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var cartaoService = scope.ServiceProvider
                        .GetRequiredService<ICartaoService>();

                    await cartaoService.ProcessarAsync(evento);

                    _channel.BasicAck(ea.DeliveryTag, false);

                    _logger.LogInformation(
                        "[CONSUMER] Cartão(s) emitido(s) com sucesso | ClienteId: {ClienteId}",
                        evento.ClienteId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[CONSUMER] Erro ao processar mensagem — reenfileirando.");

                    _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: "credito.aprovado",
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[CONSUMER] Aguardando mensagens na fila credito.aprovado...");

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }
}