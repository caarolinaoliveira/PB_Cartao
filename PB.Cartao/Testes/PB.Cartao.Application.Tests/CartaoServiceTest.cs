using PB.Cartao.Application.Services;
using PB.Cartao.Application.Interfaces;
using PB.Cartao.Application.Events;
using PB.Cartao.Domain.Interfaces;
using PB.Cartao.Domain.Entities;
using PB.Cartao.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace PB.Cartao.Application.Tests;

public class CartaoServiceTests
{
    private readonly Mock<ICartaoRepository> _repositoryMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly CartaoService _service;

    public CartaoServiceTests()
    {
        _repositoryMock = new Mock<ICartaoRepository>();
        _publisherMock = new Mock<IMessagePublisher>();
        _emailServiceMock = new Mock<IEmailService>();
        _service = new CartaoService(
            _repositoryMock.Object,
            _publisherMock.Object,
            _emailServiceMock.Object);
    }

    private CreditoAprovadoEvent CriarEventoPadrao(int quantidadeCartoes = 1, decimal limite = 1000m) => new()
    {
        PropostaId = Guid.NewGuid(),
        ClienteId = Guid.NewGuid(),
        Nome = "Carolina Oliveira",
        Email = "carolina@teste.com",
        LimiteAprovado = limite,
        QuantidadeCartoes = quantidadeCartoes,
        OcorridoEm = DateTime.UtcNow
    };

    private CartaoEntity CriarCartao(Guid clienteId, Guid propostaId, int sequencial = 1) =>
        new CartaoEntity(clienteId, propostaId, 1000m, sequencial);



    [Fact]
    public async Task ProcessarAsync_UmCartaoAprovado_DeveEmitirUmCartao()
    {
        // Arrange
        var evento = CriarEventoPadrao(quantidadeCartoes: 1, limite: 1000m);

        _repositoryMock
            .Setup(r => r.ObterPorClienteIdAsync(evento.ClienteId))
            .ReturnsAsync(new List<CartaoEntity>());

        _publisherMock
            .Setup(p => p.PublicarAsync(It.IsAny<CartaoEmitidoEvent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessarAsync(evento);

        // Assert
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<CartaoEntity>()), Times.Once);

        _emailServiceMock.Verify(e => e.EnviarCartaoEmitidoAsync(
            evento.Email, evento.Nome, 1, evento.LimiteAprovado), Times.Once);

        _publisherMock.Verify(p => p.PublicarAsync(
            It.IsAny<CartaoEmitidoEvent>(), "cartao.emitido"), Times.Once);
    }

    [Fact]
    public async Task ProcessarAsync_DoisCartoesAprovados_DeveEmitirDoisCartoes()
    {
        // Arrange
        var evento = CriarEventoPadrao(quantidadeCartoes: 2, limite: 5000m);

        _repositoryMock
            .Setup(r => r.ObterPorClienteIdAsync(evento.ClienteId))
            .ReturnsAsync(new List<CartaoEntity>());

        _publisherMock
            .Setup(p => p.PublicarAsync(It.IsAny<CartaoEmitidoEvent>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessarAsync(evento);

        // Assert — persistiu 2 vezes
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<CartaoEntity>()), Times.Exactly(2));

        // enviou email para cartão #1 e #2
        _emailServiceMock.Verify(e => e.EnviarCartaoEmitidoAsync(
            evento.Email, evento.Nome, 1, evento.LimiteAprovado), Times.Once);

        _emailServiceMock.Verify(e => e.EnviarCartaoEmitidoAsync(
            evento.Email, evento.Nome, 2, evento.LimiteAprovado), Times.Once);

        // publicou evento 2 vezes
        _publisherMock.Verify(p => p.PublicarAsync(
            It.IsAny<CartaoEmitidoEvent>(), "cartao.emitido"), Times.Exactly(2));
    }


    [Fact]
    public async Task ProcessarAsync_CartoesJaEmitidos_DeveLancarConflictException()
    {
        // Arrange
        var evento = CriarEventoPadrao(quantidadeCartoes: 1);

        var cartaoJaEmitido = CriarCartao(evento.ClienteId, evento.PropostaId, sequencial: 1);

        _repositoryMock
            .Setup(r => r.ObterPorClienteIdAsync(evento.ClienteId))
            .ReturnsAsync(new List<CartaoEntity> { cartaoJaEmitido });

        // Act
        Func<Task> act = async () => await _service.ProcessarAsync(evento);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Quantidade de cartões já emitidos é igual ou superior à quantidade aprovada.");

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<CartaoEntity>()), Times.Never);
        _emailServiceMock.Verify(e => e.EnviarCartaoEmitidoAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        _publisherMock.Verify(p => p.PublicarAsync(
            It.IsAny<CartaoEmitidoEvent>(), It.IsAny<string>()), Times.Never);
    }
}