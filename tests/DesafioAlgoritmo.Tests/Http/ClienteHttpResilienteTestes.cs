using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DesafioAlgoritmo.Infrastructure.Http;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Tests.Http;

public class TesteClienteHttpResistente
{
    private readonly Mock<ILogger<ClienteHttpResistente>> _loggerMock;
    private readonly MetricasAplicacao _metricas;

    public TesteClienteHttpResistente()
    {
        _loggerMock = new Mock<ILogger<ClienteHttpResistente>>();
        _metricas = new MetricasAplicacao("DesafioAlgoritmo.Tests");
    }

    [Fact]
    public async Task ObterAsync_ComUrlNulaOuVazia_LancaArgumentException()
    {
        var httpClient = new HttpClient();
        var client = new ClienteHttpResistente(httpClient, _loggerMock.Object, _metricas);

        Func<Task> act1 = async () => await client.ObterAsync(null!);
        Func<Task> act2 = async () => await client.ObterAsync("");

        await act1.Should().ThrowAsync<ArgumentException>();
        await act2.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EnviarAsync_ComConteudoNulo_LancaArgumentNullException()
    {
        var httpClient = new HttpClient();
        var client = new ClienteHttpResistente(httpClient, _loggerMock.Object, _metricas);

        Func<Task> act = async () => await client.EnviarAsync("https://api.example.com/test", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EnviarAsync_ComUrlNulaOuVazia_LancaArgumentException()
    {
        var httpClient = new HttpClient();
        var client = new ClienteHttpResistente(httpClient, _loggerMock.Object, _metricas);
        var content = new StringContent("test");

        Func<Task> act1 = async () => await client.EnviarAsync(null!, content);
        Func<Task> act2 = async () => await client.EnviarAsync("", content);

        await act1.Should().ThrowAsync<ArgumentException>();
        await act2.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Construtor_ComClienteHttpNulo_LancaArgumentNullException()
    {
        Action act = () => new ClienteHttpResistente(null!, _loggerMock.Object, _metricas);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Construtor_ComLoggerNulo_LancaArgumentNullException()
    {
        var httpClient = new HttpClient();

        Action act = () => new ClienteHttpResistente(httpClient, null!, _metricas);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Construtor_DefineTempoPadraoEspera()
    {
        var httpClient = new HttpClient();

        var client = new ClienteHttpResistente(httpClient, _loggerMock.Object, _metricas);

        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Construtor_ComTempodEsperaCustomizado_DefineTempo()
    {
        var httpClient = new HttpClient();
        var tempodEsperaCustomizado = TimeSpan.FromSeconds(30);

        var client = new ClienteHttpResistente(httpClient, _loggerMock.Object, _metricas, tempodEsperaCustomizado);

        httpClient.Timeout.Should().Be(tempodEsperaCustomizado);
    }

    [Fact]
    public async Task ObterAsync_ComUrlInvalida_LancaExcecao()
    {
        var httpClient = new HttpClient();
        var client = new ClienteHttpResistente(httpClient, _loggerMock.Object, _metricas, TimeSpan.FromSeconds(1));

        Func<Task> act = async () => await client.ObterAsync("https://nonexistent-domain-12345678.com/test");

        // Assert - Vai lançar HttpRequestException devido à falha de DNS
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task EnviarAsync_ComUrlInvalida_LancaExcecao()
    {
        var httpClient = new HttpClient();
        var client = new ClienteHttpResistente(httpClient, _loggerMock.Object, _metricas, TimeSpan.FromSeconds(1));
        var content = new StringContent("test");

        Func<Task> act = async () => await client.EnviarAsync("https://nonexistent-domain-12345678.com/test", content);

        // Assert - Vai lançar HttpRequestException devido à falha de DNS
        await act.Should().ThrowAsync<Exception>();
    }
}
