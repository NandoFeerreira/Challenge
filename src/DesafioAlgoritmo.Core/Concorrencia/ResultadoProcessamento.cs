namespace DesafioAlgoritmo.Core.Concorrencia;

public sealed class ResultadoProcessamento
{
    public IReadOnlyDictionary<string, int> ContagemPorCategoria { get; init; } = new Dictionary<string, int>();

    public int TotalProcessado { get; init; }

    public bool FoiCancelado { get; init; }

    public TimeSpan TempoProcessamento { get; init; }
}
