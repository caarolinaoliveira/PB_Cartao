using System.Text;
using System.Text.Json;
using PB.Cartao.Application.Interfaces;
using PB.Cartao.Domain.Exceptions;
using RabbitMQ.Client;

namespace PB.Cartao.Infrastructure.Messaging
{
    public class RabbitMQPublisher : IMessagePublisher
    {
        private readonly IConnection _connection;

        public RabbitMQPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public Task PublicarAsync<T>(T evento, string fila) where T : class
        {
            using var channel = _connection.CreateModel();

            channel.QueueDeclarePassive(fila);
            channel.ConfirmSelect();

            var json = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;

            try
            {
                channel.BasicPublish(
                    exchange: "",
                    routingKey: fila,
                    basicProperties: props,
                    body: body
                );

                if (!channel.WaitForConfirms(timeout: TimeSpan.FromSeconds(5)))
                    throw new MessagePublishException($"Mensagem não confirmada pelo broker. Fila: '{fila}'.");
            }
            catch (MessagePublishException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MessagePublishException($"Falha ao publicar na fila '{fila}'.", ex);
            }

            return Task.CompletedTask;
        }
    }
}