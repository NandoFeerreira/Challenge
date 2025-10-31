namespace DesafioAlgoritmo.Core.Servicos;

public interface IDependenciaExterna
{
    string Nome { get; }

    Task<string> ChamarAsync(CancellationToken cancellationToken = default);
}
