using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DesafioAlgoritmo.Core.Concorrencia;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Tests.Concorrencia;

public class TesteProcessadorParalelo
{
    private readonly Mock<ILogger<ProcessadorParalelo>> _loggerMock;
    private readonly MetricasAplicacao _metricas;
    private readonly ProcessadorParalelo _processador;

    public TesteProcessadorParalelo()
    {
        _loggerMock = new Mock<ILogger<ProcessadorParalelo>>();
        _metricas = new MetricasAplicacao("DesafioAlgoritmo.Tests");
        _processador = new ProcessadorParalelo(_loggerMock.Object, _metricas);
    }

    [Fact]
    public async Task ProcessarAsync_ComConjuntoPequeno_ProcessaTodosItens()
    {
        var itens = new[] { "apple", "banana", "apricot", "blueberry", "cherry" };
        var opcoes = new OpcoesProcessadorParalelo { GrauMaximoParalelismo = 2 };

        var resultado = await _processador.ProcessarAsync(itens, opcoes);

        resultado.TotalProcessado.Should().Be(5);
        resultado.FoiCancelado.Should().BeFalse();
        resultado.ContagemPorCategoria.Should().ContainKey("A").WhoseValue.Should().Be(2);
        resultado.ContagemPorCategoria.Should().ContainKey("B").WhoseValue.Should().Be(2);
        resultado.ContagemPorCategoria.Should().ContainKey("C").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task ProcessarAsync_ComConjuntoGrande_ProcessaDeterministicamente()
    {
        var itens = Enumerable.Range(0, 10_000)
            .Select(i => $"item{i % 26}")
            .ToList();

        var opcoes = new OpcoesProcessadorParalelo { GrauMaximoParalelismo = 4 };

        var resultado = await _processador.ProcessarAsync(itens, opcoes);

        resultado.TotalProcessado.Should().Be(10_000);
        resultado.FoiCancelado.Should().BeFalse();
        resultado.ContagemPorCategoria.Values.Sum().Should().Be(10_000);
    }

    [Fact]
    public async Task ProcessarAsync_ComColecaoVazia_RetornaResultadoVazio()
    {
        var itens = Array.Empty<string>();

        var resultado = await _processador.ProcessarAsync(itens);

        resultado.TotalProcessado.Should().Be(0);
        resultado.ContagemPorCategoria.Should().BeEmpty();
        resultado.FoiCancelado.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessarAsync_ComCancelamento_ParaProcessamentoGraciosamente()
    {
        var cts = new CancellationTokenSource();
        var itens = Enumerable.Range(0, 10_000).Select(i => $"item{i}");
        var opcoes = new OpcoesProcessadorParalelo
        {
            GrauMaximoParalelismo = 4,
            CancellationToken = cts.Token
        };

        // Cancela após um pequeno atraso
        _ = Task.Run(async () =>
        {
            await Task.Delay(10);
            cts.Cancel();
        });

        var resultado = await _processador.ProcessarAsync(itens, opcoes);

        resultado.FoiCancelado.Should().BeTrue();
        resultado.TotalProcessado.Should().BeLessThan(10_000);
    }

    [Fact]
    public async Task ProcessarAsync_ComDiferentesNiveisParalelismo_ProduzeResultadosConsistentes()
    {
        var itens = Enumerable.Range(0, 1_000).Select(i => $"item{i % 10}").ToList();

        var opcoes1 = new OpcoesProcessadorParalelo { GrauMaximoParalelismo = 1 };
        var opcoes2 = new OpcoesProcessadorParalelo { GrauMaximoParalelismo = 8 };

        var resultado1 = await _processador.ProcessarAsync(itens, opcoes1);
        var resultado2 = await _processador.ProcessarAsync(itens, opcoes2);

        resultado1.TotalProcessado.Should().Be(resultado2.TotalProcessado);
        resultado1.ContagemPorCategoria.Should().BeEquivalentTo(resultado2.ContagemPorCategoria);
    }

    [Fact]
    public async Task ProcessarAsync_ComItensNulos_LancaArgumentNullException()
    {
        IEnumerable<string>? itens = null;

        Func<Task> act = async () => await _processador.ProcessarAsync(itens!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessarAsync_ComStringsVazias_CategorizaComoVazio()
    {
        var itens = new[] { "", "", "test", "" };

        var resultado = await _processador.ProcessarAsync(itens);

        resultado.TotalProcessado.Should().Be(4);
        resultado.ContagemPorCategoria.Should().ContainKey("vazio").WhoseValue.Should().Be(3);
        resultado.ContagemPorCategoria.Should().ContainKey("T").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task ProcessarAsync_ComItensMaiusculasMinusculas_NormalizaCategorias()
    {
        var itens = new[] { "Apple", "apple", "APPLE", "banana" };

        var resultado = await _processador.ProcessarAsync(itens);

        resultado.TotalProcessado.Should().Be(4);
        resultado.ContagemPorCategoria.Should().ContainKey("A").WhoseValue.Should().Be(3);
        resultado.ContagemPorCategoria.Should().ContainKey("B").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task ProcessarAsync_RegistraProgressoParaConjuntoGrande()
    {
        var itens = Enumerable.Range(0, 5_000).Select(i => $"item{i}");

        await _processador.ProcessarAsync(itens);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Iniciando processamento paralelo")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processamento concluído")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessarAsync_ComOpcoesDefault_UsaContagemProcessadorSistema()
    {
        var itens = new[] { "test1", "test2", "test3" };

        var resultado = await _processador.ProcessarAsync(itens);

        resultado.TotalProcessado.Should().Be(3);
        resultado.FoiCancelado.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessarAsync_RegistraTempoProcessamento()
    {
        var itens = Enumerable.Range(0, 100).Select(i => $"item{i}");

        var resultado = await _processador.ProcessarAsync(itens);

        resultado.TempoProcessamento.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
