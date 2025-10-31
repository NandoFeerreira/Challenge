namespace DesafioAlgoritmo.Core.Concorrencia;

public sealed class OpcoesProcessadorParalelo
{
    public int GrauMaximoParalelismo { get; set; } = Environment.ProcessorCount;

    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
