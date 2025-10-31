using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DesafioAlgoritmo.Infraestrutura.Persistencia;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Tests.Persistencia;

public class TesteRepositorioRegistroTrabalho : IDisposable
{
    private readonly ContextoBdRegistroTrabalho _contexto;
    private readonly MetricasAplicacao _metricas;
    private readonly RepositorioRegistroTrabalho _repositorio;

    public TesteRepositorioRegistroTrabalho()
    {
        var opcoes = new DbContextOptionsBuilder<ContextoBdRegistroTrabalho>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _contexto = new ContextoBdRegistroTrabalho(opcoes);
        _metricas = new MetricasAplicacao("DesafioAlgoritmo.Tests");
        _repositorio = new RepositorioRegistroTrabalho(_contexto, _metricas);
    }

    [Fact]
    public async Task AdicionarAsync_AdicionaRegistroTrabalhoComSucesso()
    {
        var registroTrabalho = new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "Test message",
            Status = "Completed"
        };

        await _repositorio.AdicionarAsync(registroTrabalho);

        var salvo = await _contexto.RegistrosTrabalho.FirstOrDefaultAsync();
        salvo.Should().NotBeNull();
        salvo!.Mensagem.Should().Be("Test message");
        salvo.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task ObterPorIdAsync_RetornaRegistroTrabalhoCorreto()
    {
        var registroTrabalho = new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "Test",
            Status = "Active"
        };
        await _repositorio.AdicionarAsync(registroTrabalho);

        var resultado = await _repositorio.ObterPorIdAsync(registroTrabalho.Id);

        resultado.Should().NotBeNull();
        resultado!.Id.Should().Be(registroTrabalho.Id);
        resultado.Mensagem.Should().Be("Test");
    }

    [Fact]
    public async Task ObterPorIdAsync_RetornaNuloQuandoNaoEncontrado()
    {
        var resultado = await _repositorio.ObterPorIdAsync(999);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task ObterTodosAsync_RetornaTodosOsRegistrosTrabalho()
    {
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = DateTime.UtcNow.AddDays(-2),
            Mensagem = "Old",
            Status = "Completed"
        });
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "New",
            Status = "Active"
        });

        var resultados = await _repositorio.ObterTodosAsync();

        resultados.Should().HaveCount(2);
        resultados[0].Mensagem.Should().Be("New"); // Ordenado por data descendente
        resultados[1].Mensagem.Should().Be("Old");
    }

    [Fact]
    public async Task ObterPorStatusAsync_FiltraCorretamente()
    {
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "Active 1",
            Status = "Active"
        });
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "Completed 1",
            Status = "Completed"
        });
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "Active 2",
            Status = "Active"
        });

        var resultados = await _repositorio.ObterPorStatusAsync("Active");

        resultados.Should().HaveCount(2);
        resultados.Should().AllSatisfy(w => w.Status.Should().Be("Active"));
    }

    [Fact]
    public async Task ObterPorIntervaloDataAsync_FiltraCorretamente()
    {
        var dataBase = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = dataBase.AddDays(-1),
            Mensagem = "Before range",
            Status = "Completed"
        });
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = dataBase,
            Mensagem = "In range 1",
            Status = "Active"
        });
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = dataBase.AddDays(5),
            Mensagem = "In range 2",
            Status = "Active"
        });
        await _repositorio.AdicionarAsync(new RegistroTrabalho
        {
            Data = dataBase.AddDays(15),
            Mensagem = "After range",
            Status = "Completed"
        });

        var resultados = await _repositorio.ObterPorIntervaloDataAsync(
            dataBase,
            dataBase.AddDays(10));

        resultados.Should().HaveCount(2);
        resultados.Should().Contain(w => w.Mensagem == "In range 1");
        resultados.Should().Contain(w => w.Mensagem == "In range 2");
    }

    [Fact]
    public async Task ObterPorIntervaloDataAsync_LancaQuandoDataFinalAnteriorAoInicio()
    {
        var dataInicio = DateTime.UtcNow;
        var dataFinal = dataInicio.AddDays(-1);

        Func<Task> act = async () => await _repositorio.ObterPorIntervaloDataAsync(dataInicio, dataFinal);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AtualizarAsync_AtualizaRegistroTrabalhoComSucesso()
    {
        var registroTrabalho = new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "Original",
            Status = "Active"
        };
        await _repositorio.AdicionarAsync(registroTrabalho);

        registroTrabalho.Mensagem = "Updated";
        registroTrabalho.Status = "Completed";
        await _repositorio.AtualizarAsync(registroTrabalho);

        var atualizado = await _repositorio.ObterPorIdAsync(registroTrabalho.Id);
        atualizado.Should().NotBeNull();
        atualizado!.Mensagem.Should().Be("Updated");
        atualizado.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task DeletarAsync_DeletaRegistroTrabalhoComSucesso()
    {
        var registroTrabalho = new RegistroTrabalho
        {
            Data = DateTime.UtcNow,
            Mensagem = "To delete",
            Status = "Active"
        };
        await _repositorio.AdicionarAsync(registroTrabalho);

        var resultado = await _repositorio.DeletarAsync(registroTrabalho.Id);

        resultado.Should().BeTrue();
        var deletado = await _repositorio.ObterPorIdAsync(registroTrabalho.Id);
        deletado.Should().BeNull();
    }

    [Fact]
    public async Task DeletarAsync_RetornaFalsoQuandoNaoEncontrado()
    {
        var resultado = await _repositorio.DeletarAsync(999);

        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task AdicionarAsync_ComRegistroTrabalhoNulo_LancaArgumentNullException()
    {
        Func<Task> act = async () => await _repositorio.AdicionarAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ObterPorStatusAsync_ComStatusNulo_LancaArgumentException()
    {
        Func<Task> act = async () => await _repositorio.ObterPorStatusAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    public void Dispose()
    {
        _contexto.Database.EnsureDeleted();
        _contexto.Dispose();
    }
}
