namespace DesafioAlgoritmo.Core.Servicos;

public sealed class ResultadoOrquestracao
{
    public IReadOnlyDictionary<string, string> RespostasComSucesso { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> Falhas { get; init; } =  new Dictionary<string, string>();

    public bool SucessoTotal => Falhas.Count == 0;

    public bool SucessoParcial => RespostasComSucesso.Count > 0 && Falhas.Count > 0;

    public bool FalhaCompleta => RespostasComSucesso.Count == 0 && Falhas.Count > 0;

    public string ObterResumo()
    {
        if (SucessoTotal)
        {
            return $"Todas as {RespostasComSucesso.Count} dependências foram bem-sucedidas.";
        }

        if (FalhaCompleta)
        {
            return $"Todas as {Falhas.Count} dependências falharam.";
        }

        if (SucessoParcial)
        {
            return $"Sucesso parcial: {RespostasComSucesso.Count} bem-sucedidas, {Falhas.Count} falharam.";
        }

        return "Nenhuma dependência foi chamada.";
    }
}
