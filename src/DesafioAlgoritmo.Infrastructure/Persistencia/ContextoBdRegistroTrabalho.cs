using Microsoft.EntityFrameworkCore;

namespace DesafioAlgoritmo.Infraestrutura.Persistencia;

public sealed class ContextoBdRegistroTrabalho : DbContext
{
    public ContextoBdRegistroTrabalho(DbContextOptions<ContextoBdRegistroTrabalho> opcoes)
        : base(opcoes)
    {
    }

    public DbSet<RegistroTrabalho> RegistrosTrabalho => Set<RegistroTrabalho>();

    protected override void OnModelCreating(ModelBuilder construtor)
    {
        base.OnModelCreating(construtor);

        construtor.Entity<RegistroTrabalho>(entidade =>
        {
            entidade.HasKey(e => e.Id);

            entidade.Property(e => e.Mensagem)
                .IsRequired()
                .HasMaxLength(500);

            entidade.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entidade.Property(e => e.Data)
                .IsRequired();

            entidade.Property(e => e.CriadoEm)
                .IsRequired();

            entidade.HasIndex(e => e.Data);
            entidade.HasIndex(e => e.Status);
        });
    }
}
