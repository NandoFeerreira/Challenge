using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DesafioAlgoritmo.Core.Observabilidade;

public sealed class MetricasAplicacao : IDisposable
{
    private readonly Meter _medidor;
    private readonly Counter<long> _contadorOperacoes;
    private readonly Counter<long> _contadorErros;
    private readonly Histogram<double> _duracaoOperacao;

    public MetricasAplicacao(string nomeMedidor = "DesafioAlgoritmo")
    {
        _medidor = new Meter(nomeMedidor, "1.0.0");

        _contadorOperacoes = _medidor.CreateCounter<long>(
            name: "operacao.contagem",
            unit: "operacoes",
            description: "Número total de operações executadas");

        _contadorErros = _medidor.CreateCounter<long>(
            name: "operacao.erro.contagem",
            unit: "erros",
            description: "Número total de erros ocorridos");

        _duracaoOperacao = _medidor.CreateHistogram<double>(
            name: "operacao.duracao",
            unit: "ms",
            description: "Duração das operações em milissegundos");
    }

    public void RegistrarOperacao(string tipoOperacao, TimeSpan duracao)
    {
        _contadorOperacoes.Add(1, new KeyValuePair<string, object?>("tipo", tipoOperacao),
            new KeyValuePair<string, object?>("status", "sucesso"));

        _duracaoOperacao.Record(duracao.TotalMilliseconds,
            new KeyValuePair<string, object?>("tipo", tipoOperacao));
    }

    public void RegistrarErro(string tipoOperacao, string tipoErro, TimeSpan duracao)
    {
        _contadorOperacoes.Add(1,
            new KeyValuePair<string, object?>("tipo", tipoOperacao),
            new KeyValuePair<string, object?>("status", "erro"));

        _contadorErros.Add(1,
            new KeyValuePair<string, object?>("operacao", tipoOperacao),
            new KeyValuePair<string, object?>("tipo_erro", tipoErro));

        _duracaoOperacao.Record(duracao.TotalMilliseconds,
            new KeyValuePair<string, object?>("tipo", tipoOperacao));
    }

    public static Stopwatch IniciarCronometro() => Stopwatch.StartNew();

    public void Dispose()
    {
        _medidor.Dispose();
    }
}
