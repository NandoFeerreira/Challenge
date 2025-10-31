using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Core.Concorrencia;

public sealed class ProcessadorParalelo
{
    private readonly ILogger<ProcessadorParalelo> _logger;
    private readonly MetricasAplicacao _metricas;

    public ProcessadorParalelo(
        ILogger<ProcessadorParalelo> logger,
        MetricasAplicacao metricas)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
    }

    public async Task<ResultadoProcessamento> ProcessarAsync(
        IEnumerable<string> itens,
        OpcoesProcessadorParalelo? opcoes = null)
    {
        ArgumentNullException.ThrowIfNull(itens);
        opcoes ??= new OpcoesProcessadorParalelo();

        using var contextoOperacao = ContextoOperacao.IniciarOperacao();
        var cronometro = MetricasAplicacao.IniciarCronometro();

        _logger.RegistrarInformacaoComContexto("Iniciando processamento paralelo. ParalelismoMaximo={ParalelismoMaximo}", opcoes.GrauMaximoParalelismo);

        var contagemPorCategoria = new ConcurrentDictionary<string, int>();
        var quantidadeProcessada = 0;
        var foiCancelado = false;

        try
        {
            var opcoesParalelas = new ParallelOptions
            {
                MaxDegreeOfParallelism = opcoes.GrauMaximoParalelismo,
                CancellationToken = opcoes.CancellationToken
            };

            await Parallel.ForEachAsync(
                itens,
                opcoesParalelas,
                async (item, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(1, cancellationToken);

                    var categoria = string.IsNullOrEmpty(item)
                        ? "vazio"
                        : item[0].ToString().ToUpperInvariant();

                    contagemPorCategoria.AddOrUpdate(
                        categoria,
                        1,
                        (_, contagem) => contagem + 1);

                    var atual = Interlocked.Increment(ref quantidadeProcessada);

                    if (atual % 1000 == 0)
                    {
                        _logger.RegistrarComContexto(LogLevel.Debug, "Progresso do processamento. Processado={Processado}", atual);
                    }
                });
        }
        catch (OperationCanceledException ex)
        {
            foiCancelado = true;
            cronometro.Stop();

            _logger.RegistrarAvisoComContexto("Processamento cancelado. ProcessadoAntesCancelamento={QuantidadeProcessada}", quantidadeProcessada);

            _metricas.RegistrarErro("ProcessadorParalelo.ProcessarAsync", ex.GetType().Name, cronometro.Elapsed);

            return new ResultadoProcessamento
            {
                ContagemPorCategoria = contagemPorCategoria,
                TotalProcessado = quantidadeProcessada,
                FoiCancelado = foiCancelado,
                TempoProcessamento = cronometro.Elapsed
            };
        }
        catch (Exception ex)
        {
            cronometro.Stop();

            _logger.RegistrarErroComContexto(ex, "Erro durante processamento paralelo");

            _metricas.RegistrarErro("ProcessadorParalelo.ProcessarAsync", ex.GetType().Name, cronometro.Elapsed);

            throw;
        }

        cronometro.Stop();

        _logger.RegistrarInformacaoComContexto("Processamento concluído. TotalProcessado={TotalProcessado}, FoiCancelado={FoiCancelado}, " +
            "Duracao={Duracao}ms, Categorias={ContagemCategorias}", 
            quantidadeProcessada, foiCancelado, cronometro.ElapsedMilliseconds, contagemPorCategoria.Count);

        _metricas.RegistrarOperacao("ProcessadorParalelo.ProcessarAsync", cronometro.Elapsed);

        return new ResultadoProcessamento
        {
            ContagemPorCategoria = contagemPorCategoria,
            TotalProcessado = quantidadeProcessada,
            FoiCancelado = foiCancelado,
            TempoProcessamento = cronometro.Elapsed
        };
    }
}
