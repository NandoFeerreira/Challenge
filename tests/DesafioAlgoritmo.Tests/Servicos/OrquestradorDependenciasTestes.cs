using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DesafioAlgoritmo.Core.Servicos;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Tests.Servicos;

public class TesteOrquestradorDependencias
{
    private readonly Mock<ILogger<OrquestradorDependencias>> _loggerMock;
    private readonly MetricasAplicacao _metricas;
    private readonly OrquestradorDependencias _orquestrador;

    public TesteOrquestradorDependencias()
    {
        _loggerMock = new Mock<ILogger<OrquestradorDependencias>>();
        _metricas = new MetricasAplicacao("DesafioAlgoritmo.Tests");
        _orquestrador = new OrquestradorDependencias(_loggerMock.Object, _metricas);
    }

    [Fact]
    public async Task ExecutarAsync_ComTodasAsDependenciasComSucesso_RetornaExitoCompleto()
    {
        var dep1 = CriarDependenciaMock("Dep1", "Response1");
        var dep2 = CriarDependenciaMock("Dep2", "Response2");
        var dep3 = CriarDependenciaMock("Dep3", "Response3");

        var dependencias = new[] { dep1.Object, dep2.Object, dep3.Object };

        var resultado = await _orquestrador.ExecutarAsync(dependencias);

        resultado.SucessoTotal.Should().BeTrue();
        resultado.RespostasComSucesso.Should().HaveCount(3);
        resultado.Falhas.Should().BeEmpty();
        resultado.ObterResumo().Should().Contain("Todas as 3 dependências foram bem-sucedidas");
    }

    [Fact]
    public async Task ExecutarAsync_ComFalhaParcial_RetornaExitoParcial()
    {
        var dep1 = CriarDependenciaMock("Dep1", "Response1");
        var dep2 = CriarDependenciaMock("Dep2", new InvalidOperationException("Service unavailable"));
        var dep3 = CriarDependenciaMock("Dep3", "Response3");

        var dependencias = new[] { dep1.Object, dep2.Object, dep3.Object };

        var resultado = await _orquestrador.ExecutarAsync(dependencias);

        resultado.SucessoParcial.Should().BeTrue();
        resultado.RespostasComSucesso.Should().HaveCount(2);
        resultado.Falhas.Should().HaveCount(1);
        resultado.Falhas.Should().ContainKey("Dep2");
        resultado.ObterResumo().Should().Contain("Sucesso parcial: 2 bem-sucedidas, 1 falharam");
    }

    [Fact]
    public async Task ExecutarAsync_ComTodasAsDependenciasComFalha_RetornaFalhaCompleta()
    {
        var dep1 = CriarDependenciaMock("Dep1", new Exception("Error 1"));
        var dep2 = CriarDependenciaMock("Dep2", new Exception("Error 2"));
        var dep3 = CriarDependenciaMock("Dep3", new Exception("Error 3"));

        var dependencias = new[] { dep1.Object, dep2.Object, dep3.Object };

        var resultado = await _orquestrador.ExecutarAsync(dependencias);

        resultado.FalhaCompleta.Should().BeTrue();
        resultado.RespostasComSucesso.Should().BeEmpty();
        resultado.Falhas.Should().HaveCount(3);
        resultado.ObterResumo().Should().Contain("Todas as 3 dependências falharam");
    }

    [Fact]
    public async Task ExecutarAsync_SemDependencias_RetornaResultadoVazio()
    {
        var dependencias = Array.Empty<IDependenciaExterna>();

        var resultado = await _orquestrador.ExecutarAsync(dependencias);

        resultado.RespostasComSucesso.Should().BeEmpty();
        resultado.Falhas.Should().BeEmpty();
        resultado.SucessoTotal.Should().BeTrue(); // Sem falhas quando não há dependências
    }

    [Fact]
    public async Task ExecutarAsync_ComCancelamento_LancaOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        var dep1 = CriarDependenciaMock("Dep1", "Response1");
        var dep2 = new Mock<IDependenciaExterna>();
        dep2.Setup(x => x.Nome).Returns("Dep2");
        dep2.Setup(x => x.ChamarAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var dependencias = new[] { dep1.Object, dep2.Object };

        Func<Task> act = async () => await _orquestrador.ExecutarAsync(dependencias, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecutarAsync_ComDependenciasNulas_LancaArgumentNullException()
    {
        IEnumerable<IDependenciaExterna>? dependencias = null;

        Func<Task> act = async () => await _orquestrador.ExecutarAsync(dependencias!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecutarAsync_RegistraInicioETermino()
    {
        var dep1 = CriarDependenciaMock("Dep1", "Response1");
        var dependencias = new[] { dep1.Object };

        await _orquestrador.ExecutarAsync(dependencias);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Iniciando orquestração")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Orquestração concluída")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_RegistraFalhasIndividuais()
    {
        var dep1 = CriarDependenciaMock("Dep1", new InvalidOperationException("Test error"));
        var dependencias = new[] { dep1.Object };

        await _orquestrador.ExecutarAsync(dependencias);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dependência falhou")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_PreservaMessagensExcecao()
    {
        var mensagemEsperada = "Specific error message";
        var dep1 = CriarDependenciaMock("Dep1", new Exception(mensagemEsperada));
        var dependencias = new[] { dep1.Object };

        var resultado = await _orquestrador.ExecutarAsync(dependencias);

        resultado.Falhas["Dep1"].Should().Be(mensagemEsperada);
    }

    [Fact]
    public async Task ExecutarAsync_ContinuaAposFalhasIndividuais()
    {
        var dep1 = CriarDependenciaMock("Dep1", "Response1");
        var dep2 = CriarDependenciaMock("Dep2", new Exception("Error"));
        var dep3 = CriarDependenciaMock("Dep3", "Response3");

        var dependencias = new[] { dep1.Object, dep2.Object, dep3.Object };

        var resultado = await _orquestrador.ExecutarAsync(dependencias);

        resultado.RespostasComSucesso.Should().ContainKeys("Dep1", "Dep3");
        resultado.Falhas.Should().ContainKey("Dep2");
        dep1.Verify(x => x.ChamarAsync(It.IsAny<CancellationToken>()), Times.Once);
        dep2.Verify(x => x.ChamarAsync(It.IsAny<CancellationToken>()), Times.Once);
        dep3.Verify(x => x.ChamarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IDependenciaExterna> CriarDependenciaMock(string nome, string resposta)
    {
        var mock = new Mock<IDependenciaExterna>();
        mock.Setup(x => x.Nome).Returns(nome);
        mock.Setup(x => x.ChamarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resposta);
        return mock;
    }

    private static Mock<IDependenciaExterna> CriarDependenciaMock(string nome, Exception excecao)
    {
        var mock = new Mock<IDependenciaExterna>();
        mock.Setup(x => x.Nome).Returns(nome);
        mock.Setup(x => x.ChamarAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(excecao);
        return mock;
    }
}
