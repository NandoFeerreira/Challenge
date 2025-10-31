using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Infrastructure.Http;

public sealed class ClienteHttpResistente
{
    private readonly HttpClient _clienteHttp;
    private readonly ILogger<ClienteHttpResistente> _registrador;
    private readonly MetricasAplicacao _metricas;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _politicaRetry;

    public ClienteHttpResistente(
        HttpClient clienteHttp,
        ILogger<ClienteHttpResistente> registrador,
        MetricasAplicacao metricas,
        TimeSpan? timeout = null)
    {
        _clienteHttp = clienteHttp ?? throw new ArgumentNullException(nameof(clienteHttp));
        _registrador = registrador ?? throw new ArgumentNullException(nameof(registrador));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));

        _clienteHttp.Timeout = timeout ?? TimeSpan.FromSeconds(10);

        _politicaRetry = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode >= HttpStatusCode.InternalServerError ||
                r.StatusCode == HttpStatusCode.RequestTimeout)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: tentativaRetry =>
                    TimeSpan.FromSeconds(Math.Pow(2, tentativaRetry - 1)),
                onRetry: (resultado, tempoEspera, contagemRetry, contexto) =>
                {
                    _registrador.RegistrarAvisoComContexto("Retry {ContagemRetry} após {Atraso}s devido a {Razao}", contagemRetry, tempoEspera.TotalSeconds, resultado.Exception?.Message ?? resultado.Result?.StatusCode.ToString() ?? "Desconhecido");
                });
    }

    public async Task<HttpResponseMessage> ObterAsync(string url, CancellationToken tokenCancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        using var contextoOperacao = ContextoOperacao.IniciarOperacao();
        var cronometro = MetricasAplicacao.IniciarCronometro();

        _registrador.RegistrarInformacaoComContexto("Requisição HTTP GET. Url={Url}", url);

        try
        {
            var resposta = await _politicaRetry.ExecuteAsync(async () =>
            {
                return await _clienteHttp.GetAsync(url, tokenCancelamento);
            });

            cronometro.Stop();

            _registrador.RegistrarInformacaoComContexto("HTTP GET concluído. CodigoStatus={CodigoStatus}, Duracao={Duracao}ms", resposta.StatusCode, cronometro.ElapsedMilliseconds);

            _metricas.RegistrarOperacao("ClienteHttpResistente.ObterAsync", cronometro.Elapsed);

            return resposta;
        }
        catch (Exception ex)
        {
            cronometro.Stop();

            _registrador.RegistrarErroComContexto(ex, "HTTP GET falhou. Url={Url}", url);

            _metricas.RegistrarErro("ClienteHttpResistente.ObterAsync", ex.GetType().Name, cronometro.Elapsed);

            throw;
        }
    }

    public async Task<HttpResponseMessage> EnviarAsync(string url, HttpContent conteudo, CancellationToken tokenCancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(conteudo);

        using var contextoOperacao = ContextoOperacao.IniciarOperacao();
        var cronometro = MetricasAplicacao.IniciarCronometro();

        _registrador.RegistrarInformacaoComContexto("Requisição HTTP POST. Url={Url}", url);

        try
        {
            var resposta = await _politicaRetry.ExecuteAsync(async () =>
            {
                return await _clienteHttp.PostAsync(url, conteudo, tokenCancelamento);
            });

            cronometro.Stop();

            _registrador.RegistrarInformacaoComContexto("HTTP POST concluído. CodigoStatus={CodigoStatus}, Duracao={Duracao}ms", resposta.StatusCode, cronometro.ElapsedMilliseconds);

            _metricas.RegistrarOperacao("ClienteHttpResistente.EnviarAsync", cronometro.Elapsed);

            return resposta;
        }
        catch (Exception ex)
        {
            cronometro.Stop();

            _registrador.RegistrarErroComContexto(ex, "HTTP POST falhou. Url={Url}", url);

            _metricas.RegistrarErro("ClienteHttpResistente.EnviarAsync", ex.GetType().Name, cronometro.Elapsed);

            throw;
        }
    }
}
