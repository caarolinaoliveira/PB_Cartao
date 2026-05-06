// using Microsoft.EntityFrameworkCore;
// using PB.Cartao.Application.Interfaces;
// using PB.Cartao.Application.Services;
// using PB.Cartao.Domain.Interfaces;
// using PB.Cartao.Infrastructure.Context;
// using PB.Cartao.Infrastructure.Messaging;
// using PB.Cartao.Infrastructure.Repository;
// using PB.Cartao.Infrastructure.Services;
// using RabbitMQ.Client;

// var builder = Host.CreateApplicationBuilder(args);

// builder.Services.AddDbContext<CartaoDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddSingleton<IConnection>(sp =>
// {
//     var factory = new ConnectionFactory
//     {
//         HostName = builder.Configuration["RabbitMQ:Host"],
//         UserName = builder.Configuration["RabbitMQ:User"],
//         Password = builder.Configuration["RabbitMQ:Password"],
//         DispatchConsumersAsync = true
//     };
//     return factory.CreateConnection();
// });

// builder.Services.AddScoped<ICartaoRepository, CartaoRepository>();
// builder.Services.AddScoped<ICartaoService, CartaoService>();
// builder.Services.AddScoped<IMessagePublisher, RabbitMQPublisher>();
// builder.Services.AddScoped<IEmailService, EmailService>();

// builder.Services.AddHostedService<RabbitMQConsumer>();

// var host = builder.Build();

// using (var scope = host.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<CartaoDbContext>();
//     await db.Database.MigrateAsync();
// }

// await host.RunAsync();