using FluentAssertions;
using DesafioAlgoritmo.Core.Observabilidade;

namespace DesafioAlgoritmo.Tests.Observabilidade;

public class TesteContextoOperacao
{
    [Fact]
    public void Atual_InitialmentNulo()
    {
        ContextoOperacao.Atual.Should().BeNull();
    }

    [Fact]
    public void IniciarOperacao_CriaNovoContexto()
    {
        using var scope = ContextoOperacao.IniciarOperacao();

        ContextoOperacao.Atual.Should().NotBeNull();
        ContextoOperacao.Atual!.IdOperacao.Should().NotBeNullOrEmpty();
        ContextoOperacao.Atual.IdOperacao.Length.Should().Be(8);
    }

    [Fact]
    public void IniciarOperacao_DefineHoraInicio()
    {
        var antes = DateTimeOffset.UtcNow;

        using var scope = ContextoOperacao.IniciarOperacao();
        var depois = DateTimeOffset.UtcNow;

        ContextoOperacao.Atual.Should().NotBeNull();
        ContextoOperacao.Atual!.HoraInicio.Should().BeOnOrAfter(antes);
        ContextoOperacao.Atual.HoraInicio.Should().BeOnOrBefore(depois);
    }

    [Fact]
    public void IniciarOperacao_Descartar_RestabelececeContextoAnterior()
    {
        ContextoOperacao? contextoInicial = null;

        using (var scope = ContextoOperacao.IniciarOperacao())
        {
            contextoInicial = ContextoOperacao.Atual;
            contextoInicial.Should().NotBeNull();
        }

        ContextoOperacao.Atual.Should().BeNull();
    }

    [Fact]
    public void IniciarOperacao_Aninhada_CriaHierarquia()
    {
        using var escopo_externo = ContextoOperacao.IniciarOperacao();
        var contexto_externo = ContextoOperacao.Atual;
        contexto_externo.Should().NotBeNull();
        var id_operacao_externo = contexto_externo!.IdOperacao;

        using (var escopo_interno = ContextoOperacao.IniciarOperacao())
        {
            var contexto_interno = ContextoOperacao.Atual;
            contexto_interno.Should().NotBeNull();
            contexto_interno!.IdOperacaoPai.Should().Be(id_operacao_externo);
            contexto_interno.IdOperacao.Should().NotBe(id_operacao_externo);
        }

        // Após descartar contexto interno, contexto externo deve ser restaurado
        ContextoOperacao.Atual.Should().NotBeNull();
        ContextoOperacao.Atual!.IdOperacao.Should().Be(id_operacao_externo);
    }

    [Fact]
    public void IniciarOperacao_ComPaiExplicito_DefineIdOperacaoPai()
    {
        var idPai = "parent123";

        using var scope = ContextoOperacao.IniciarOperacao(idPai);

        ContextoOperacao.Atual.Should().NotBeNull();
        ContextoOperacao.Atual!.IdOperacaoPai.Should().Be(idPai);
    }

    [Fact]
    public void IniciarOperacao_MultiplosSequenciais_CriaIdsUnicos()
    {
        string? id1, id2, id3;

        using (var scope1 = ContextoOperacao.IniciarOperacao())
        {
            id1 = ContextoOperacao.Atual?.IdOperacao;
        }

        using (var scope2 = ContextoOperacao.IniciarOperacao())
        {
            id2 = ContextoOperacao.Atual?.IdOperacao;
        }

        using (var scope3 = ContextoOperacao.IniciarOperacao())
        {
            id3 = ContextoOperacao.Atual?.IdOperacao;
        }

        id1.Should().NotBeNullOrEmpty();
        id2.Should().NotBeNullOrEmpty();
        id3.Should().NotBeNullOrEmpty();
        id1.Should().NotBe(id2);
        id2.Should().NotBe(id3);
        id1.Should().NotBe(id3);
    }

    [Fact]
    public async Task IniciarOperacao_AtravesDeLimitesAsync_MantémContexto()
    {
        string? idOperacao = null;

        using (var scope = ContextoOperacao.IniciarOperacao())
        {
            idOperacao = ContextoOperacao.Atual?.IdOperacao;

            await Task.Delay(10);

            // Assert - Contexto é mantido através de async
            ContextoOperacao.Atual.Should().NotBeNull();
            ContextoOperacao.Atual!.IdOperacao.Should().Be(idOperacao);
        }

        // Após descartar escopo
        ContextoOperacao.Atual.Should().BeNull();
    }

    [Fact]
    public void IniciarOperacao_MultiploDescartar_NaoLanca()
    {
        var scope = ContextoOperacao.IniciarOperacao();

        Action act = () =>
        {
            scope.Dispose();
            scope.Dispose();
        };

        act.Should().NotThrow();
    }
}
