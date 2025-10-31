using FluentAssertions;
using DesafioAlgoritmo.Core.Algoritmos;

namespace DesafioAlgoritmo.Tests.Algoritmos;

public class TesteAnalisadorSequencia
{
    #region Testes EncontrarPrimeiroRepetido

    [Fact]
    public void EncontrarPrimeiroRepetido_ComDuplicatas_RetornaPrimeiroRepetido()
    {
        var numeros = new long[] { 1, 2, 3, 4, 2, 5, 3 };

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        resultado.Should().Be(2);
    }

    [Fact]
    public void EncontrarPrimeiroRepetido_SemDuplicatas_RetornaNull()
    {
        var numeros = new long[] { 1, 2, 3, 4, 5 };

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        resultado.Should().BeNull();
    }

    [Fact]
    public void EncontrarPrimeiroRepetido_ComSequenciaVazia_RetornaNull()
    {
        var numeros = Array.Empty<long>();

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        resultado.Should().BeNull();
    }

    [Fact]
    public void EncontrarPrimeiroRepetido_ComElementoUnico_RetornaNull()
    {
        var numeros = new long[] { 42 };

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        resultado.Should().BeNull();
    }

    [Fact]
    public void EncontrarPrimeiroRepetido_ComDuplicatasConsecutivas_RetornaPrimeiro()
    {
        var numeros = new long[] { 1, 1, 2, 3 };

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        resultado.Should().Be(1);
    }

    [Fact]
    public void EncontrarPrimeiroRepetido_ComTodosOsMesmosValores_RetornaValor()
    {
        var numeros = new long[] { 5, 5, 5, 5 };

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        resultado.Should().Be(5);
    }

    [Fact]
    public void EncontrarPrimeiroRepetido_ComNumerosNegativos_FuncionaCorretamente()
    {
        var numeros = new long[] { -1, -2, -3, -2, -4 };

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        resultado.Should().Be(-2);
    }

    [Fact]
    public void EncontrarPrimeiroRepetido_ComEntradaNula_LancaArgumentNullException()
    {
        IReadOnlyList<long>? numeros = null;

        Action act = () => AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Testes EncontrarMaiorSubsequenciaConsecutiva

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComSequenciaUnica_RetornaEla()
    {
        var numeros = new long[] { 1, 2, 3, 4, 5 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComMultiplasSequencias_RetornaAMaior()
    {
        var numeros = new long[] { 1, 2, 5, 3, 4, 5, 6, 7 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(3, 4, 5, 6, 7);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComComprimentosIguais_RetornaAPrimeira()
    {
        var numeros = new long[] { 1, 2, 3, 0, 10, 11, 12 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComSequenciaDecrescente_RetornaElementoUnico()
    {
        var numeros = new long[] { 5, 4, 3, 2, 1 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(5);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComSequenciaVazia_RetornaVazia()
    {
        var numeros = Array.Empty<long>();

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().BeEmpty();
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComElementoUnico_RetornaElementoUnico()
    {
        var numeros = new long[] { 42 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(42);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComTodosOsMesmosValores_RetornaPrimeiroElemento()
    {
        var numeros = new long[] { 5, 5, 5, 5 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(5);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComNumerosNegativos_FuncionaCorretamente()
    {
        var numeros = new long[] { -5, -4, -3, -2, 0, 5, -10, -8 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(-5, -4, -3, -2);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComGrandesGaps_FuncionaCorretamente()
    {
        var numeros = new long[] { 1, 100, 200, 5, 6, 7, 8 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(5, 6, 7, 8);
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_ComEntradaNula_LancaArgumentNullException()
    {
        IReadOnlyList<long>? numeros = null;

        Action act = () => AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EncontrarMaiorSubsequenciaConsecutiva_NoFinalDaLista_RetornaSequenciaCorreta()
    {
        var numeros = new long[] { 5, 4, 1, 2, 3, 4, 5, 6 };

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        resultado.Should().Equal(1, 2, 3, 4, 5, 6);
    }

    #endregion
}
