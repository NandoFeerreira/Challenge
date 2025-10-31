namespace DesafioAlgoritmo.Infraestrutura.Persistencia;

public sealed class RegistroTrabalho
{
    public int Id { get; set; }

    public DateTime Data { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
