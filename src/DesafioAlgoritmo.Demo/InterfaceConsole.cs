namespace DesafioAlgoritmo.Demo;

internal static class InterfaceConsole
{
    public static int LerOpcao(int min, int max)
    {
        while (true)
        {
            Console.Write($"Escolha uma opção ({min}-{max}): ");
            if (int.TryParse(Console.ReadLine(), out int opcao) && opcao >= min && opcao <= max)
            {
                return opcao;
            }
            MostrarErro($"Opção inválida! Digite um número entre {min} e {max}.");
        }
    }

    public static long[] LerArrayDeInteiros()
    {
        Console.WriteLine("\n💡 Digite os números separados por espaço ou vírgula");
        Console.WriteLine("   Exemplo: 1 2 3 4 5  ou  1, 2, 3, 4, 5");
        Console.Write("\nSeus números: ");

        var entrada = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(entrada))
        {
            MostrarErro("Entrada vazia!");
            return Array.Empty<long>();
        }

        try
        {
            var numeros = entrada
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(long.Parse)
                .ToArray();

            if (numeros.Length == 0)
            {
                MostrarErro("Nenhum número válido encontrado!");
                return Array.Empty<long>();
            }

            Console.WriteLine($"✅ {numeros.Length} número(s) lido(s): [{string.Join(", ", numeros)}]");
            return numeros;
        }
        catch (FormatException)
        {
            MostrarErro("Formato inválido! Certifique-se de digitar apenas números.");
            return Array.Empty<long>();
        }
        catch (OverflowException)
        {
            MostrarErro("Um ou mais números são muito grandes! Use números entre -9.223.372.036.854.775.808 e 9.223.372.036.854.775.807.");
            return Array.Empty<long>();
        }
    }

    public static string? ValidarStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        var statusNormalizado = status.Trim();

        if (statusNormalizado.Equals("Pendente", StringComparison.OrdinalIgnoreCase))
            return "Pendente";

        if (statusNormalizado.Equals("EmAndamento", StringComparison.OrdinalIgnoreCase))
            return "EmAndamento";

        if (statusNormalizado.Equals("Concluido", StringComparison.OrdinalIgnoreCase) ||
            statusNormalizado.Equals("Concluído", StringComparison.OrdinalIgnoreCase))
            return "Concluido";

        return null;
    }

    public static void MostrarErro(string mensagem)
    {
        Console.WriteLine();
        var corOriginal = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ ERRO: {mensagem}");
        Console.ForegroundColor = corOriginal;
        Console.WriteLine("─────────────────────────────────────────────────────────────");
    }
}
