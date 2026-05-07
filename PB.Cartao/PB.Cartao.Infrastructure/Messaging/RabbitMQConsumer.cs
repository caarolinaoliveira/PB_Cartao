using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PB.Cartao.Application.Events;
using PB.Cartao.Application.Interfaces;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PB.Cartao.Infrastructure.Messaging
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly IConnection _connection;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
        private IModel? _channel;

        public RabbitMQConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMQConsumer> logger,
            IConnection connection,
            AsyncCircuitBreakerPolicy circuitBreaker)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _connection = connection;
            _circuitBreaker = circuitBreaker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("dlx.cartao", ExchangeType.Direct, durable: true);

            _channel.QueueDeclare(
                queue: "credito.aprovado.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            _channel.QueueBind("credito.aprovado.dlq", "dlx.cartao", "credito.aprovado");

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dlx.cartao" },
                { "x-dead-letter-routing-key", "credito.aprovado" }
            };

            _channel.QueueDeclare(
                queue: "credito.aprovado",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args
            );

            _channel.QueueDeclare(
                queue: "cartao.emitido",
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                if (_circuitBreaker.CircuitState == CircuitState.Open)
                {
                    _logger.LogWarning("[CONSUMER] Circuito aberto — reenfileirando mensagem.");
                    _channel!.BasicNack(ea.DeliveryTag, false, requeue: true);
                    return;
                }

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var tentativas = 0;

                if (ea.BasicProperties.Headers != null &&
                    ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryObj))
                {
                    tentativas = Convert.ToInt32(retryObj);
                }

                _logger.LogInformation(
                    "[CONSUMER] Mensagem recebida na fila credito.aprovado: {Json}", json);

                try
                {
                    var evento = JsonSerializer.Deserialize<CreditoAprovadoEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (evento is null)
                    {
                        _logger.LogWarning("[CONSUMER] Evento null — descartando.");
                        _channel!.BasicNack(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var cartaoService = scope.ServiceProvider
                        .GetRequiredService<ICartaoService>();

                    await cartaoService.ProcessarAsync(evento);

                    _channel!.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation(
                        "[CONSUMER] Cartao(s) emitido(s) com sucesso | ClienteId: {ClienteId}",
                        evento.ClienteId);
                }
                catch (BrokenCircuitException ex)
                {
                    _logger.LogError(ex, "[CONSUMER] Circuit Breaker aberto — descartando para DLQ.");
                    _channel!.BasicNack(ea.DeliveryTag, false, requeue: false);
                }
                catch (Exception ex)
                {
                    tentativas++;

                    if (tentativas >= 3)
                    {
                        _logger.LogError(ex, "[CONSUMER] Maximo de tentativas atingido — enviando para DLQ.");
                        _channel!.BasicNack(ea.DeliveryTag, false, requeue: false);
                        return;
                    }

                    _logger.LogWarning("[CONSUMER] Tentativa {N} falhou — reenfileirando.", tentativas);

                    var props = _channel!.CreateBasicProperties();
                    props.Persistent = true;
                    props.Headers = new Dictionary<string, object>
                    {
                        { "x-retry-count", tentativas }
                    };

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativas)));

                    _channel.BasicPublish("", "credito.aprovado", props, ea.Body);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            };

            _channel.BasicConsume(
                queue: "credito.aprovado",
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("[CONSUMER] Aguardando mensagens na fila credito.aprovado...");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }
}