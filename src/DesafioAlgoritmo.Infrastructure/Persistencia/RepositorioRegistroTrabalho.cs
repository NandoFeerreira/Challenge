using Microsoft.EntityFrameworkCore;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Infraestrutura.Persistencia;

public sealed class RepositorioRegistroTrabalho : IRepositorioRegistroTrabalho
{
    private readonly ContextoBdRegistroTrabalho _contexto;
    private readonly MetricasAplicacao _metricas;

    public RepositorioRegistroTrabalho(
        ContextoBdRegistroTrabalho contexto,
        MetricasAplicacao metricas)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        _metricas = metricas ?? throw new ArgumentNullException(nameof(metricas));
    }

    public async Task AdicionarAsync(RegistroTrabalho registroTrabalho, CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(registroTrabalho);

        var cronometro = MetricasAplicacao.IniciarCronometro();

        await _contexto.RegistrosTrabalho.AddAsync(registroTrabalho, tokenCancelamento);
        await _contexto.SaveChangesAsync(tokenCancelamento);

        cronometro.Stop();
        _metricas.RegistrarOperacao("Repositorio.AdicionarAsync", cronometro.Elapsed);
    }

    public async Task<RegistroTrabalho?> ObterPorIdAsync(int id, CancellationToken tokenCancelamento = default)
    {
        var cronometro = MetricasAplicacao.IniciarCronometro();

        var resultado = await _contexto.RegistrosTrabalho
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, tokenCancelamento);

        cronometro.Stop();
        _metricas.RegistrarOperacao("Repositorio.ObterPorIdAsync", cronometro.Elapsed);

        return resultado;
    }

    public async Task<IReadOnlyList<RegistroTrabalho>> ObterTodosAsync(CancellationToken tokenCancelamento = default)
    {
        return await _contexto.RegistrosTrabalho
            .AsNoTracking()
            .OrderByDescending(r => r.Data)
            .ToListAsync(tokenCancelamento);
    }

    public async Task<IReadOnlyList<RegistroTrabalho>> ObterPorStatusAsync(
        string status,
        CancellationToken tokenCancelamento = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        return await _contexto.RegistrosTrabalho
            .AsNoTracking()
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.Data)
            .ToListAsync(tokenCancelamento);
    }

    public async Task<IReadOnlyList<RegistroTrabalho>> ObterPorIntervaloDataAsync(
        DateTime dataInicio,
        DateTime dataFim,
        CancellationToken tokenCancelamento = default)
    {
        if (dataFim < dataInicio)
        {
            throw new ArgumentException("A data de fim deve ser maior ou igual à data de início.");
        }

        return await _contexto.RegistrosTrabalho
            .AsNoTracking()
            .Where(r => r.Data >= dataInicio && r.Data <= dataFim)
            .OrderByDescending(r => r.Data)
            .ToListAsync(tokenCancelamento);
    }

    public async Task AtualizarAsync(RegistroTrabalho registroTrabalho, CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(registroTrabalho);

        var cronometro = MetricasAplicacao.IniciarCronometro();

        var entidadeExistente = await _contexto.RegistrosTrabalho.FindAsync(new object[] { registroTrabalho.Id }, tokenCancelamento);

        if (entidadeExistente != null)
        {
            entidadeExistente.Mensagem = registroTrabalho.Mensagem;
            entidadeExistente.Status = registroTrabalho.Status;
            entidadeExistente.Data = registroTrabalho.Data;

            await _contexto.SaveChangesAsync(tokenCancelamento);
        }

        cronometro.Stop();
        _metricas.RegistrarOperacao("Repositorio.AtualizarAsync", cronometro.Elapsed);
    }

    public async Task<bool> DeletarAsync(int id, CancellationToken tokenCancelamento = default)
    {
        var cronometro = MetricasAplicacao.IniciarCronometro();

        var registroTrabalho = await _contexto.RegistrosTrabalho.FindAsync(new object[] { id }, tokenCancelamento);

        if (registroTrabalho == null)
        {
            cronometro.Stop();
            _metricas.RegistrarOperacao("Repositorio.DeletarAsync", cronometro.Elapsed);
            return false;
        }

        _contexto.RegistrosTrabalho.Remove(registroTrabalho);
        await _contexto.SaveChangesAsync(tokenCancelamento);

        cronometro.Stop();
        _metricas.RegistrarOperacao("Repositorio.DeletarAsync", cronometro.Elapsed);

        return true;
    }
}
