using DesafioAlgoritmo.Core.Observabilidade;
using Microsoft.Extensions.Logging;

namespace DesafioAlgoritmo.Core.Servicos;

public sealed class OrquestradorDependencias
{
    private readonly ILogger<OrquestradorDependencias> _logger;
    private readonly MetricasAplicacao _metricas;

    public OrquestradorDependencias(ILogger<OrquestradorDependencias> logger, MetricasAplicacao metricas)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
    }

    public async Task<ResultadoOrquestracao> ExecutarAsync(IEnumerable<IDependenciaExterna> dependencias, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dependencias);

        using var contextoOperacao = ContextoOperacao.IniciarOperacao();
        var cronometro = MetricasAplicacao.IniciarCronometro();
        var listaDependencias = dependencias.ToList();

        _logger.RegistrarInformacaoComContexto("Iniciando orquestração paralela. ContagemDependencias={ContagemDependencias}", listaDependencias.Count);

        var tarefas = listaDependencias.Select(async dependencia =>
        {
            var cronometroDependencia = MetricasAplicacao.IniciarCronometro();

            try
            {
                _logger.RegistrarComContexto(LogLevel.Debug, "Chamando dependência. NomeDependencia={NomeDependencia}", dependencia.Nome);

                var resposta = await dependencia.ChamarAsync(cancellationToken);
                cronometroDependencia.Stop();

                _logger.RegistrarInformacaoComContexto("Dependência bem-sucedida. NomeDependencia={NomeDependencia}, Duracao={Duracao}ms", dependencia.Nome, cronometroDependencia.ElapsedMilliseconds);

                _metricas.RegistrarOperacao($"OrquestradorDependencias.{dependencia.Nome}", cronometroDependencia.Elapsed);

                return (dependencia.Nome, Sucesso: true, Resposta: resposta, Erro: (string?)null);
            }
            catch (OperationCanceledException ex)
            {
                cronometroDependencia.Stop();
                _logger.RegistrarAvisoComContexto("Orquestração cancelada. NomeDependencia={NomeDependencia}", dependencia.Nome);
                _metricas.RegistrarErro("OrquestradorDependencias.ExecutarAsync", ex.GetType().Name, cronometro.Elapsed);
                throw;
            }
            catch (Exception ex)
            {
                cronometroDependencia.Stop();
                _logger.RegistrarErroComContexto(ex, "Dependência falhou. NomeDependencia={NomeDependencia}, MensagemErro={MensagemErro}", dependencia.Nome, ex.Message);
                _metricas.RegistrarErro($"OrquestradorDependencias.{dependencia.Nome}", ex.GetType().Name, cronometroDependencia.Elapsed);

                return (dependencia.Nome, Sucesso: false, Resposta: (string?)null, Erro: ex.Message);
            }
        });

        var resultados = await Task.WhenAll(tarefas);

        var respostasComSucesso = resultados
            .Where(r => r.Sucesso)
            .ToDictionary(r => r.Nome, r => r.Resposta!);

        var falhas = resultados
            .Where(r => !r.Sucesso)
            .ToDictionary(r => r.Nome, r => r.Erro!);

        cronometro.Stop();

        var resultado = new ResultadoOrquestracao
        {
            RespostasComSucesso = respostasComSucesso,
            Falhas = falhas
        };

        _logger.RegistrarInformacaoComContexto("Orquestração concluída. Resumo={Resumo}, Duracao={Duracao}ms", resultado.ObterResumo(), cronometro.ElapsedMilliseconds);

        _metricas.RegistrarOperacao("OrquestradorDependencias.ExecutarAsync", cronometro.Elapsed);

        return resultado;
    }
}
