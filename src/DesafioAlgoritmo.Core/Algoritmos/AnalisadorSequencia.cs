namespace DesafioAlgoritmo.Core.Algoritmos;

public static class AnalisadorSequencia
{
    public static long? EncontrarPrimeiroRepetido(IEnumerable<long> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);

        var vistos = new HashSet<long>();

        foreach (var numero in numeros)
        {
            if (!vistos.Add(numero))
            {
                return numero;
            }
        }

        return null;
    }

    public static IReadOnlyList<long> EncontrarMaiorSubsequenciaConsecutiva(IReadOnlyList<long> numeros)
    {
        ArgumentNullException.ThrowIfNull(numeros);

        if (numeros.Count == 0)
        {
            return [];
        }

        if (numeros.Count == 1)
        {
            return numeros;
        }

        int melhorInicio = 0;
        int melhorComprimento = 1;
        int atualInicio = 0;
        int atualComprimento = 1;

        for (int i = 1; i < numeros.Count; i++)
        {
            if (numeros[i] == numeros[i - 1] + 1)
            {
                atualComprimento++;

                if (atualComprimento > melhorComprimento)
                {
                    melhorInicio = atualInicio;
                    melhorComprimento = atualComprimento;
                }
            }
            else
            {
                atualInicio = i;
                atualComprimento = 1;
            }
        }

        var resultado = new long[melhorComprimento];
        for (int i = 0; i < melhorComprimento; i++)
        {
            resultado[i] = numeros[melhorInicio + i];
        }

        return resultado;
    }
}
