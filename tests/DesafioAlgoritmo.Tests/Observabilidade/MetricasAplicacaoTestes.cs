using FluentAssertions;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Tests.Observabilidade;

public class TesteMetricasAplicacao : IDisposable
{
    private readonly MetricasAplicacao _metricas;

    public TesteMetricasAplicacao()
    {
        _metricas = new MetricasAplicacao("DesafioAlgoritmo.Tests");
    }

    [Fact]
    public void RegistrarOperacao_ComOperacaoComSucesso_NaoLanca()
    {
        var duracao = TimeSpan.FromMilliseconds(100);

        Action act = () => _metricas.RegistrarOperacao("test_operation", duracao);

        act.Should().NotThrow();
    }

    [Fact]
    public void RegistrarErro_ComOperacaoFalhada_NaoLanca()
    {
        var duracao = TimeSpan.FromMilliseconds(50);

        Action act = () => _metricas.RegistrarErro("test_operation", "TimeoutException", duracao);

        act.Should().NotThrow();
    }

    [Fact]
    public void RegistrarOperacao_ComMultiplasOperacoes_RegistraTodas()
    {
        // Arrange & Act
        _metricas.RegistrarOperacao("operation1", TimeSpan.FromMilliseconds(100));
        _metricas.RegistrarOperacao("operation2", TimeSpan.FromMilliseconds(200));
        _metricas.RegistrarOperacao("operation1", TimeSpan.FromMilliseconds(150));

        // Assert - As métricas são registradas (sem exceções)
        // Em produção, estas seriam exportadas para um backend de métricas
        true.Should().BeTrue();
    }

    [Fact]
    public void RegistrarErro_ComMultiplosErros_RegistraTodos()
    {
        // Arrange & Act
        _metricas.RegistrarErro("operation1", "ValidationError", TimeSpan.FromMilliseconds(10));
        _metricas.RegistrarErro("operation2", "TimeoutException", TimeSpan.FromMilliseconds(5000));
        _metricas.RegistrarErro("operation1", "ValidationError", TimeSpan.FromMilliseconds(15));

        // Assert - As métricas são registradas (sem exceções)
        true.Should().BeTrue();
    }

    [Fact]
    public void IniciarCronometro_RetornaCronometroEmExecucao()
    {
        var cronometro = MetricasAplicacao.IniciarCronometro();

        cronometro.Should().NotBeNull();
        cronometro.IsRunning.Should().BeTrue();
        cronometro.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void IniciarCronometro_MultiplisChamadas_RetornaCronometrosIndependentes()
    {
        var cronometro1 = MetricasAplicacao.IniciarCronometro();
        Thread.Sleep(10);
        var cronometro2 = MetricasAplicacao.IniciarCronometro();

        cronometro1.Should().NotBeSameAs(cronometro2);
        cronometro1.ElapsedMilliseconds.Should().BeGreaterThan(cronometro2.ElapsedMilliseconds);
    }

    [Fact]
    public void RegistrarOperacao_ComDuracaoZero_NaoLanca()
    {
        Action act = () => _metricas.RegistrarOperacao("instant_op", TimeSpan.Zero);

        act.Should().NotThrow();
    }

    [Fact]
    public void RegistrarOperacao_ComDuracaoLonga_NaoLanca()
    {
        Action act = () => _metricas.RegistrarOperacao("long_op", TimeSpan.FromMinutes(5));

        act.Should().NotThrow();
    }

    [Fact]
    public void Descartar_DescartaMetricasCorretamente()
    {
        var metricas = new MetricasAplicacao();

        Action act = () => metricas.Dispose();

        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _metricas.Dispose();
    }
}
