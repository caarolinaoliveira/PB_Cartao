using System.Text;
using System.Text.Json;
using PB.Cartao.Application.Interfaces;
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

            channel.ConfirmSelect();

            var json = JsonSerializer.Serialize(evento);
            var body = Encoding.UTF8.GetBytes(json);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;

            channel.BasicPublish(
                exchange: "",
                routingKey: fila,
                basicProperties: props,
                body: body
            );

            var confirmado = channel.WaitForConfirms(timeout: TimeSpan.FromSeconds(5));
            if (!confirmado)
                throw new Exception("Falha ao publicar mensagem no RabbitMQ.");

            return Task.CompletedTask;
        }
    }
}