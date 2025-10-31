namespace DesafioAlgoritmo.Infraestrutura.Persistencia;

public interface IRepositorioRegistroTrabalho
{
    Task AdicionarAsync(RegistroTrabalho registroTrabalho, CancellationToken tokenCancelamento = default);

    Task<RegistroTrabalho?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default);

    Task<IReadOnlyList<RegistroTrabalho>> ObterTodosAsync(CancellationToken tokenCancelamento = default);

    Task<IReadOnlyList<RegistroTrabalho>> ObterPorStatusAsync(string status, CancellationToken tokenCancelamento = default);

    Task<IReadOnlyList<RegistroTrabalho>> ObterPorIntervaloDataAsync(
        DateTime dataInicio,
        DateTime dataFim,
        CancellationToken tokenCancelamento = default);

    Task AtualizarAsync(RegistroTrabalho registroTrabalho, CancellationToken tokenCancelamento = default);

    Task<bool> DeletarAsync(int id, CancellationToken tokenCancelamento = default);
}
