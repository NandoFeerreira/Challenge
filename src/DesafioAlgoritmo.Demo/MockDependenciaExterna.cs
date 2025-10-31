using DesafioAlgoritmo.Core.Servicos;

namespace DesafioAlgoritmo.Demo;

internal class MockDependenciaExterna : IDependenciaExterna
{
    private readonly bool _deveFalhar;

    public MockDependenciaExterna(string nome, bool deveFalhar)
    {
        Nome = nome;
        _deveFalhar = deveFalhar;
    }

    public string Nome { get; }

    public async Task<string> ChamarAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(Random.Shared.Next(100, 300), cancellationToken);

        if (_deveFalhar)
        {
            throw new Exception($"{Nome} está temporariamente indisponível");
        }

        return $"Resposta bem-sucedida de {Nome}";
    }
}
