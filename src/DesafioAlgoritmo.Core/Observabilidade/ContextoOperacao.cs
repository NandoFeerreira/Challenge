namespace DesafioAlgoritmo.Core.Observabilidade;

public sealed class ContextoOperacao
{
    private static readonly AsyncLocal<ContextoOperacao?> _atual = new();

    public static ContextoOperacao? Atual
    {
        get => _atual.Value;
        private set => _atual.Value = value;
    }

    public string IdOperacao { get; }

    public DateTimeOffset HoraInicio { get; }

    public string? IdOperacaoPai { get; }

    private ContextoOperacao(string idOperacao, string? idOperacaoPai = null)
    {
        IdOperacao = idOperacao;
        IdOperacaoPai = idOperacaoPai;
        HoraInicio = DateTimeOffset.UtcNow;
    }

    public static IDisposable IniciarOperacao(string? idOperacaoPai = null)
    {
        var idOperacao = Guid.NewGuid().ToString("N")[..8];

        var anterior = Atual;

        Atual = new ContextoOperacao(idOperacao, idOperacaoPai ?? anterior?.IdOperacao);
        return new EscopoOperacao(anterior);
    }

    private sealed class EscopoOperacao : IDisposable
    {
        private readonly ContextoOperacao? _anterior;

        public EscopoOperacao(ContextoOperacao? anterior)
        {
            _anterior = anterior;
        }

        public void Dispose()
        {
            Atual = _anterior;
        }
    }
}
