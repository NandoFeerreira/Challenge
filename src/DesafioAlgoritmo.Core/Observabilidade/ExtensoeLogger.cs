using Microsoft.Extensions.Logging;

namespace DesafioAlgoritmo.Core.Observabilidade;

public static class ExtensoeLogger
{
    public static void RegistrarComContexto(
        this ILogger logger,
        LogLevel nivelLog,
        string mensagem,
        params object[] argumentos)
    {
        var idOperacao = ContextoOperacao.Atual?.IdOperacao ?? "nenhum";
        var mensagemComContexto = $"[IdOperacao={idOperacao}] {mensagem}";

        logger.Log(nivelLog, mensagemComContexto, argumentos);
    }

    public static void RegistrarInformacaoComContexto(
        this ILogger logger,
        string mensagem,
        params object[] argumentos)
    {
        logger.RegistrarComContexto(LogLevel.Information, mensagem, argumentos);
    }

    public static void RegistrarAvisoComContexto(
        this ILogger logger,
        string mensagem,
        params object[] argumentos)
    {
        logger.RegistrarComContexto(LogLevel.Warning, mensagem, argumentos);
    }

    public static void RegistrarErroComContexto(
        this ILogger logger,
        Exception excecao,
        string mensagem,
        params object[] argumentos)
    {
        var idOperacao = ContextoOperacao.Atual?.IdOperacao ?? "nenhum";
        var mensagemComContexto = $"[IdOperacao={idOperacao}] {mensagem}";

        logger.LogError(excecao, mensagemComContexto, argumentos);
    }
}
