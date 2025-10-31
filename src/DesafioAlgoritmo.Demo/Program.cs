using DesafioAlgoritmo.Core.Algoritmos;
using DesafioAlgoritmo.Core.Concorrencia;
using DesafioAlgoritmo.Core.Observabilidade;
using DesafioAlgoritmo.Core.Servicos;
using DesafioAlgoritmo.Infraestrutura.Persistencia;
using DesafioAlgoritmo.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var serviceProvider = ConfigurarServicos();

ILogger ObterLoggerPorCategoria(string categoria) => serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(categoria);
MetricasAplicacao ObterMetricas() => serviceProvider.GetRequiredService<MetricasAplicacao>();

ServiceProvider ConfigurarServicos()
{
    var services = new ServiceCollection();

    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    services.AddSingleton<MetricasAplicacao>(sp => new MetricasAplicacao("DesafioAlgoritmo.Demo"));

    services.AddScoped<ProcessadorParalelo>();
    services.AddScoped<OrquestradorDependencias>();

    services.AddDbContext<ContextoBdRegistroTrabalho>(options =>
        options.UseInMemoryDatabase("DemoInterativa"));

    services.AddScoped<IRepositorioRegistroTrabalho, RepositorioRegistroTrabalho>();

    services.AddHttpClient<ClienteHttpResistente>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

    services.AddScoped<ClienteHttpResistente>(sp =>
    {
        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ClienteHttpResistente));
        var logger = sp.GetRequiredService<ILogger<ClienteHttpResistente>>();
        var metricas = sp.GetRequiredService<MetricasAplicacao>();
        return new ClienteHttpResistente(httpClient, logger, metricas);
    });

    return services.BuildServiceProvider();
}

bool continuar = true;

while (continuar)
{
    MostrarMenuPrincipal();
    var opcao = DesafioAlgoritmo.Demo.InterfaceConsole.LerOpcao(1, 7);

    Console.Clear();

    switch (opcao)
    {
        case 1:
            await TestarPrimeiroRepetido();
            break;
        case 2:
            TestarMaiorSequenciaCrescente();
            break;
        case 3:
            await TestarProcessamentoParalelo();
            break;
        case 4:
            await TestarOrquestrador();
            break;
        case 5:
            await TestarPersistencia();
            break;
        case 6:
            await TestarClienteHttpResiliente();
            break;
        case 7:
            continuar = false;
            Console.WriteLine("\n👋 Até logo! Obrigado por testar o Desafio de Algoritmos!");
            break;
    }

    if (continuar)
    {
        Console.WriteLine("\n\nPressione qualquer tecla para voltar ao menu...");
        Console.ReadKey(true);
        Console.Clear();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// FUNÇÕES DE TESTE
// ═══════════════════════════════════════════════════════════════════════════

async Task TestarPrimeiroRepetido()
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SEÇÃO 1.1 - PRIMEIRO NÚMERO REPETIDO                 ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

    Console.WriteLine("📝 Este algoritmo encontra o primeiro número que aparece");
    Console.WriteLine("   duas vezes em uma sequência, respeitando a ordem.\n");

    Console.WriteLine("💡 COMPLEXIDADE:");
    Console.WriteLine("   • Tempo: O(n) - percorre a lista uma vez");
    Console.WriteLine("   • Espaço: O(n) - usa HashSet para guardar números vistos\n");

    while (true)
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine("Escolha uma opção:");
        Console.WriteLine("  1 - Usar exemplo pronto");
        Console.WriteLine("  2 - Digitar seus próprios números");
        Console.WriteLine("  0 - Voltar ao menu");
        Console.Write("\nSua escolha: ");

        if (!int.TryParse(Console.ReadLine(), out int escolha))
        {
            DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Opção inválida! Digite um número.");
            continue;
        }

        if (escolha == 0) return;

        long[] numeros;

        if (escolha == 1)
        {
            numeros = new long[] { 4, 7, 2, 9, 2, 5, 7, 3 };
            Console.WriteLine($"\n✅ Usando exemplo: [{string.Join(", ", numeros)}]");
        }
        else if (escolha == 2)
        {
            numeros = DesafioAlgoritmo.Demo.InterfaceConsole.LerArrayDeInteiros();
            if (numeros.Length == 0) continue;
        }
        else
        {
            DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Opção inválida!");
            continue;
        }

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║  🔍 OBSERVABILIDADE ATIVA                                 ║");
        Console.ResetColor();
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine("📊 Métricas sendo coletadas:");
        Console.WriteLine("   • Tempo de execução do algoritmo");
        Console.WriteLine("   • Correlação via OperationId");
        Console.WriteLine();

        using var contexto = ContextoOperacao.IniciarOperacao();
        var logger = ObterLoggerPorCategoria("AnalisadorSequencia");
        var metricas = ObterMetricas();
        var cronometro = System.Diagnostics.Stopwatch.StartNew();

        logger.RegistrarInformacaoComContexto("Processando array de {Tamanho} elementos", numeros.Length);
        Console.WriteLine("🔄 Processando...\n");

        var resultado = AnalisadorSequencia.EncontrarPrimeiroRepetido(numeros);

        cronometro.Stop();
        logger.RegistrarInformacaoComContexto("Processamento concluído em {DuracaoMs}ms", cronometro.ElapsedMilliseconds);
        metricas.RegistrarOperacao("AnalisadorSequencia.EncontrarPrimeiroRepetido", cronometro.Elapsed);

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        if (resultado.HasValue)
        {
            Console.WriteLine($"✅ RESULTADO: {resultado.Value}");

            var primeiraPos = Array.IndexOf(numeros, resultado.Value);
            var segundaPos = Array.IndexOf(numeros, resultado.Value, primeiraPos + 1);

            Console.WriteLine($"\n📍 Primeira aparição: índice {primeiraPos} (elemento {primeiraPos + 1} de {numeros.Length})");
            Console.WriteLine($"📍 Segunda aparição: índice {segundaPos} (elemento {segundaPos + 1} de {numeros.Length})");

            Console.WriteLine("\n📊 Visualização do array:");

            var maxWidth = Math.Max(4, numeros.Max(n => n.ToString().Length));

            Console.Write("   Índices:  ");
            for (int i = 0; i < numeros.Length; i++)
            {
                Console.Write($"[{i}]".PadRight(maxWidth + 1));
            }
            Console.WriteLine();

            Console.Write("   Valores:  ");
            for (int i = 0; i < numeros.Length; i++)
            {
                if (i == primeiraPos || i == segundaPos)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(numeros[i].ToString().PadRight(maxWidth + 1));
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(numeros[i].ToString().PadRight(maxWidth + 1));
                }
            }
            Console.WriteLine();

            Console.Write("   Marcação: ");
            for (int i = 0; i < numeros.Length; i++)
            {
                if (i == primeiraPos)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("▲1°".PadRight(maxWidth + 1));
                    Console.ResetColor();
                }
                else if (i == segundaPos)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("▲2°".PadRight(maxWidth + 1));
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(new string(' ', maxWidth + 1));
                }
            }
            Console.WriteLine();
            Console.WriteLine($"\n💡 O número {resultado.Value} é o primeiro que se repete no array!");
        }
        else
        {
            Console.WriteLine("ℹ️  RESULTADO: Nenhum número repetido encontrado");
            Console.WriteLine("\n💡 Todos os números são únicos na sequência!");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📈 MÉTRICAS REGISTRADAS:");
        Console.ResetColor();
        Console.WriteLine($"   ✅ Operação: AnalisadorSequencia.EncontrarPrimeiroRepetido");
        Console.WriteLine($"   ⏱️  Duração: {cronometro.ElapsedMilliseconds}ms");
        Console.WriteLine($"   📊 Array tamanho: {numeros.Length}");
        Console.WriteLine($"   🔗 OperationId: {ContextoOperacao.Atual?.IdOperacao}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("💡 Veja os logs acima - todos têm [IdOperacao=...] para correlação!");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        Console.Write("Testar outro array? (s/n): ");
        if (Console.ReadLine()?.ToLower() != "s") break;
        Console.WriteLine();
    }
}

void TestarMaiorSequenciaCrescente()
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SEÇÃO 1.2 - MAIOR SEQUÊNCIA CRESCENTE                ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

    Console.WriteLine("📝 Este algoritmo encontra a maior sequência de números");
    Console.WriteLine("   onde cada um é maior que o anterior.\n");

    Console.WriteLine("💡 COMPLEXIDADE:");
    Console.WriteLine("   • Tempo: O(n) - percorre a lista uma vez");
    Console.WriteLine("   • Espaço: O(1) - só guarda índices e tamanhos\n");

    while (true)
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine("Escolha uma opção:");
        Console.WriteLine("  1 - Usar exemplo pronto");
        Console.WriteLine("  2 - Digitar seus próprios números");
        Console.WriteLine("  0 - Voltar ao menu");
        Console.Write("\nSua escolha: ");

        if (!int.TryParse(Console.ReadLine(), out int escolha))
        {
            DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Opção inválida! Digite um número.");
            continue;
        }

        if (escolha == 0) return;

        long[] numeros;

        if (escolha == 1)
        {
            numeros = new long[] { 1, 2, 5, 3, 4, 5, 6, 7, 2 };
            Console.WriteLine($"\n✅ Usando exemplo: [{string.Join(", ", numeros)}]");
        }
        else if (escolha == 2)
        {
            numeros = DesafioAlgoritmo.Demo.InterfaceConsole.LerArrayDeInteiros();
            if (numeros.Length == 0) continue;
        }
        else
        {
            DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Opção inválida!");
            continue;
        }

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║  🔍 OBSERVABILIDADE ATIVA                                 ║");
        Console.ResetColor();
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine("📊 Métricas sendo coletadas:");
        Console.WriteLine("   • Tempo de execução do algoritmo");
        Console.WriteLine("   • Correlação via OperationId");
        Console.WriteLine();

        using var contexto = ContextoOperacao.IniciarOperacao();
        var loggerSequencia = ObterLoggerPorCategoria("AnalisadorSequencia");
        var metricasSequencia = ObterMetricas();
        var cronometro = System.Diagnostics.Stopwatch.StartNew();

        loggerSequencia.RegistrarInformacaoComContexto("Processando array de {Tamanho} elementos", numeros.Length);
        Console.WriteLine("🔄 Processando...\n");

        var resultado = AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva(numeros);

        cronometro.Stop();
        loggerSequencia.RegistrarInformacaoComContexto("Processamento concluído em {DuracaoMs}ms. Sequência encontrada: {Tamanho} elementos", cronometro.ElapsedMilliseconds, resultado.Count);
        metricasSequencia.RegistrarOperacao("AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva", cronometro.Elapsed);

        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine($"✅ MAIOR SEQUÊNCIA CRESCENTE: [{string.Join(", ", resultado)}]");
        Console.WriteLine($"📊 Tamanho: {resultado.Count} elemento(s)");

        if (resultado.Count > 0)
        {
            var posicaoInicio = Array.IndexOf(numeros, resultado[0]);
            if (posicaoInicio >= 0)
            {
                var posicaoFim = posicaoInicio + resultado.Count - 1;
                Console.WriteLine($"📍 Posição no array: índice {posicaoInicio} até {posicaoFim} (elementos {posicaoInicio + 1}-{posicaoFim + 1} de {numeros.Length})");

                Console.WriteLine("\n📊 Visualização do array:");

                var maxWidth = Math.Max(4, numeros.Max(n => n.ToString().Length));

                Console.Write("   Índices:  ");
                for (int i = 0; i < numeros.Length; i++)
                {
                    Console.Write($"[{i}]".PadRight(maxWidth + 1));
                }
                Console.WriteLine();

                Console.Write("   Valores:  ");
                for (int i = 0; i < numeros.Length; i++)
                {
                    if (i >= posicaoInicio && i <= posicaoFim)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(numeros[i].ToString().PadRight(maxWidth + 1));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write(numeros[i].ToString().PadRight(maxWidth + 1));
                    }
                }
                Console.WriteLine();

                Console.Write("   Marcação: ");
                for (int i = 0; i < numeros.Length; i++)
                {
                    if (i >= posicaoInicio && i <= posicaoFim)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("▲▲▲".PadRight(maxWidth + 1));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write(new string(' ', maxWidth + 1));
                    }
                }
                Console.WriteLine();
            }
        }

        if (resultado.Count > 1)
        {
            Console.WriteLine($"\n💡 Sequência consecutiva onde cada número = anterior + 1");
            Console.WriteLine($"   Exemplo: {resultado[0]} → {resultado[1]}" +
                (resultado.Count > 2 ? $" → {resultado[2]}..." : ""));
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📈 MÉTRICAS REGISTRADAS:");
        Console.ResetColor();
        Console.WriteLine($"   ✅ Operação: AnalisadorSequencia.EncontrarMaiorSubsequenciaConsecutiva");
        Console.WriteLine($"   ⏱️  Duração: {cronometro.ElapsedMilliseconds}ms");
        Console.WriteLine($"   📊 Array tamanho: {numeros.Length}");
        Console.WriteLine($"   📈 Sequência encontrada: {resultado.Count} elementos");
        Console.WriteLine($"   🔗 OperationId: {ContextoOperacao.Atual?.IdOperacao}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("💡 Veja os logs acima - todos têm [IdOperacao=...] para correlação!");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        Console.Write("Testar outro array? (s/n): ");
        if (Console.ReadLine()?.ToLower() != "s") break;
        Console.WriteLine();
    }
}

async Task TestarProcessamentoParalelo()
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SEÇÃO 2 - PROCESSAMENTO PARALELO                     ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

    Console.WriteLine("📝 Processa milhares de itens em paralelo de forma segura");
    Console.WriteLine("   (thread-safe) usando ConcurrentDictionary.\n");

    Console.WriteLine("💡 SEGURANÇA:");
    Console.WriteLine("   • ConcurrentDictionary: operações atômicas lock-free");
    Console.WriteLine("   • Interlocked.Increment: contador thread-safe");
    Console.WriteLine("   • Sem race conditions!\n");

    Console.Write("Quantos itens processar (ex: 1000, 5000, 10000)? ");
    if (!int.TryParse(Console.ReadLine(), out int quantidade) || quantidade <= 0)
    {
        DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Quantidade inválida!");
        return;
    }

    Console.Write("Grau de paralelismo (1-8, recomendado 4): ");
    if (!int.TryParse(Console.ReadLine(), out int paralelismo) || paralelismo < 1 || paralelismo > 8)
    {
        paralelismo = 4;
        Console.WriteLine($"Usando padrão: {paralelismo}");
    }

    using var scope = serviceProvider.CreateScope();
    var processor = scope.ServiceProvider.GetRequiredService<ProcessadorParalelo>();

    Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("║  🔍 OBSERVABILIDADE ATIVA                                 ║");
    Console.ResetColor();
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
    Console.WriteLine("📊 Métricas sendo coletadas:");
    Console.WriteLine("   • Tempo de processamento");
    Console.WriteLine("   • Taxa de sucesso/erro");
    Console.WriteLine("   • Correlação via OperationId");
    Console.WriteLine();

    // 🎨 Cria itens com nomes variados para melhor visualização das categorias
    var nomesItens = new[]
    {
        "Apple", "Banana", "Cherry", "Dragon Fruit", "Elderberry",
        "Fig", "Grape", "Honeydew", "Iceberg Lettuce", "Jackfruit",
        "Kiwi", "Lemon", "Mango", "Nectarine", "Orange",
        "Papaya", "Quince", "Raspberry", "Strawberry", "Tangerine",
        "Ugli Fruit", "Vanilla", "Watermelon", "Xigua", "Yellow Squash", "Zucchini"
    };

    var items = Enumerable.Range(0, quantidade)
        .Select(i => nomesItens[i % nomesItens.Length])
        .ToList();

    // Mostra preview dos primeiros itens
    Console.WriteLine($"\n📦 Exemplos de itens que serão processados:");
    Console.WriteLine($"   {string.Join(", ", items.Take(10))}...\n");

    // 🔴 Pergunta se quer testar cancelamento
    Console.Write("Deseja testar CANCELAMENTO durante o processamento? (s/n): ");
    var testarCancelamento = Console.ReadLine()?.ToLower() == "s";

    var cts = new CancellationTokenSource();
    var options = new OpcoesProcessadorParalelo
    {
        GrauMaximoParalelismo = paralelismo,
        CancellationToken = cts.Token
    };

    if (testarCancelamento)
    {
        Console.Write("Cancelar após quantos segundos? (ex: 2, 5, 10): ");
        if (int.TryParse(Console.ReadLine(), out int segundos) && segundos > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(segundos));
            Console.WriteLine($"\n⏰ ATENÇÃO: O processamento será CANCELADO após {segundos} segundo(s)!\n");
        }
        else
        {
            Console.WriteLine("\n💡 Você também pode pressionar CTRL+C para cancelar manualmente!\n");
        }
    }

    Console.WriteLine($"🔄 Processando {quantidade} itens com {paralelismo} threads...");
    if (testarCancelamento)
    {
        Console.WriteLine($"🔴 Modo cancelamento ATIVO - aguardando interrupção...\n");
    }
    else
    {
        Console.WriteLine();
    }

    ResultadoProcessamento resultado;
    try
    {
        resultado = await processor.ProcessarAsync(items, options);
    }
    finally
    {
        cts.Dispose();
    }

    Console.WriteLine("\n═══════════════════════════════════════════════════════════");

    if (resultado.FoiCancelado)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠️  PROCESSAMENTO CANCELADO!");
        Console.ResetColor();
        Console.WriteLine($"📊 Progresso antes do cancelamento: {resultado.TotalProcessado}/{quantidade} itens ({(resultado.TotalProcessado * 100.0 / quantidade):F1}%)");
        Console.WriteLine($"⏱️  Tempo até cancelamento: {resultado.TempoProcessamento.TotalMilliseconds:F2}ms");
        Console.WriteLine($"\n💡 Os resultados parciais foram salvos com sucesso!");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ PROCESSAMENTO COMPLETO!");
        Console.ResetColor();
        Console.WriteLine($"📊 Total processado: {resultado.TotalProcessado} itens");
        Console.WriteLine($"⏱️  Tempo gasto: {resultado.TempoProcessamento.TotalMilliseconds:F2}ms");
        Console.WriteLine($"⚡ Velocidade: {(resultado.TotalProcessado / resultado.TempoProcessamento.TotalSeconds):F0} itens/segundo");
    }

    Console.WriteLine($"\n📊 Categorias processadas: {resultado.ContagemPorCategoria.Count} letras diferentes");
    Console.WriteLine($"─────────────────────────────────────────────────────────────");

    foreach (var cat in resultado.ContagemPorCategoria.OrderBy(x => x.Key))
    {
        var porcentagem = resultado.TotalProcessado > 0
            ? (cat.Value * 100.0 / resultado.TotalProcessado)
            : 0;
        var barra = new string('█', (int)(porcentagem / 2)); // Barra visual
        Console.WriteLine($"   {cat.Key} │{barra,-50}│ {cat.Value,5} itens ({porcentagem:F1}%)");
    }
    Console.WriteLine("═══════════════════════════════════════════════════════════");

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("📈 MÉTRICAS REGISTRADAS:");
    Console.ResetColor();
    Console.WriteLine($"   ✅ Operação: ProcessadorParalelo.ProcessarAsync");
    Console.WriteLine($"   ⏱️  Duração: {resultado.TempoProcessamento.TotalMilliseconds:F2}ms");
    Console.WriteLine($"   📊 Status: {(resultado.FoiCancelado ? "Cancelado" : "Sucesso")}");
    Console.WriteLine($"   🔗 Logs com correlação via OperationId");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("💡 Veja os logs acima - todos têm [IdOperacao=...] para correlação!");
    Console.ResetColor();
    Console.WriteLine("═══════════════════════════════════════════════════════════");
}

async Task TestarOrquestrador()
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SEÇÃO 3 - TRATAMENTO DE ERROS                        ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

    Console.WriteLine("📝 Orquestra chamadas a múltiplos serviços externos.");
    Console.WriteLine("   Se um falha, continua chamando os outros!\n");

    Console.WriteLine("💡 ESTRATÉGIA:");
    Console.WriteLine("   • Isolamento de falhas");
    Console.WriteLine("   • Respostas parciais sempre disponíveis");
    Console.WriteLine("   • Consumidor recebe máximo de dados possível\n");

    Console.Write("Quantos serviços simular (1-5)? ");
    if (!int.TryParse(Console.ReadLine(), out int numServicos) || numServicos < 1 || numServicos > 5)
    {
        numServicos = 3;
        Console.WriteLine($"Usando padrão: {numServicos}");
    }

    var dependencies = new List<IDependenciaExterna>();

    Console.WriteLine($"\nConfigurando {numServicos} serviços:");
    for (int i = 1; i <= numServicos; i++)
    {
        Console.Write($"  Serviço {i} deve falhar? (s/n): ");
        var deveFalhar = Console.ReadLine()?.ToLower() == "s";
        dependencies.Add(new DesafioAlgoritmo.Demo.MockDependenciaExterna($"Servico{i}", deveFalhar));
        Console.WriteLine($"    ✅ Servico{i} configurado para " + (deveFalhar ? "FALHAR ❌" : "SUCESSO ✅"));
    }

    using var scope = serviceProvider.CreateScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<OrquestradorDependencias>();

    Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("║  🔍 OBSERVABILIDADE ATIVA                                 ║");
    Console.ResetColor();
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
    Console.WriteLine("📊 Métricas sendo coletadas:");
    Console.WriteLine("   • Tempo de processamento por serviço");
    Console.WriteLine("   • Taxa de sucesso/erro por dependência");
    Console.WriteLine("   • Correlação via OperationId");
    Console.WriteLine();

    Console.WriteLine("🔄 Executando orquestração...\n");
    var resultado = await orchestrator.ExecutarAsync(dependencies);

    Console.WriteLine("\n═══════════════════════════════════════════════════════════");
    Console.WriteLine($"📊 SUMÁRIO: {resultado.ObterResumo()}");
    Console.WriteLine($"\n✅ Sucessos ({resultado.RespostasComSucesso.Count}):");
    foreach (var success in resultado.RespostasComSucesso)
    {
        Console.WriteLine($"   • {success.Key}: {success.Value}");
    }

    if (resultado.Falhas.Count > 0)
    {
        Console.WriteLine($"\n❌ Falhas ({resultado.Falhas.Count}):");
        foreach (var failure in resultado.Falhas)
        {
            Console.WriteLine($"   • {failure.Key}: {failure.Value}");
        }
    }

    Console.WriteLine("\n💡 Mesmo com falhas parciais, os dados dos serviços que");
    Console.WriteLine("   funcionaram foram retornados com sucesso!");

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("📈 MÉTRICAS REGISTRADAS:");
    Console.ResetColor();
    Console.WriteLine($"   ✅ Operação: OrquestradorDependencias.ExecutarAsync");
    Console.WriteLine($"   📊 Total de dependências: {numServicos}");
    Console.WriteLine($"   ✅ Sucessos: {resultado.RespostasComSucesso.Count}");
    Console.WriteLine($"   ❌ Falhas: {resultado.Falhas.Count}");
    Console.WriteLine($"   🔗 Logs com correlação via OperationId");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("💡 Veja os logs acima - todos têm [IdOperacao=...] para correlação!");
    Console.ResetColor();
    Console.WriteLine("═══════════════════════════════════════════════════════════");
}

async Task TestarPersistencia()
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     SEÇÃO 4 - PERSISTÊNCIA (EF CORE)                     ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

    Console.WriteLine("📝 Demonstra operações CRUD com Entity Framework Core");
    Console.WriteLine("   usando banco de dados InMemory.\n");

    using var scope = serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IRepositorioRegistroTrabalho>();

    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("║  🔍 OBSERVABILIDADE ATIVA                                 ║");
    Console.ResetColor();
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
    Console.WriteLine("📊 Métricas sendo coletadas:");
    Console.WriteLine("   • Tempo de cada operação CRUD");
    Console.WriteLine("   • Performance de queries EF Core");
    Console.WriteLine("   • Correlação via OperationId");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("💡 Cada operação abaixo terá suas métricas registradas!");
    Console.ResetColor();
    Console.WriteLine();

    while (true)
    {
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine("Operações disponíveis:");
        Console.WriteLine("  1 - Criar novo registro");
        Console.WriteLine("  2 - Listar todos os registros");
        Console.WriteLine("  3 - Buscar por status");
        Console.WriteLine("  4 - Atualizar registro");
        Console.WriteLine("  5 - Deletar registro");
        Console.WriteLine("  0 - Voltar ao menu");
        Console.Write("\nSua escolha: ");

        if (!int.TryParse(Console.ReadLine(), out int escolha))
        {
            DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Opção inválida!");
            continue;
        }

        Console.WriteLine();

        switch (escolha)
        {
            case 0:
                return;

            case 1: // Criar
                Console.Write("Mensagem: ");
                var mensagem = Console.ReadLine();
                Console.Write("Status (Pendente/EmAndamento/Concluido): ");
                var status = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(mensagem))
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Mensagem não pode ser vazia!");
                    break;
                }

                // Validar status
                var statusValido = DesafioAlgoritmo.Demo.InterfaceConsole.ValidarStatus(status);
                if (statusValido == null)
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Status inválido! Use: Pendente, EmAndamento ou Concluido");
                    break;
                }

                await repository.AdicionarAsync(new RegistroTrabalho
                {
                    Data = DateTime.Now,
                    Mensagem = mensagem!,
                    Status = statusValido
                });

                Console.WriteLine("\n✅ Registro criado com sucesso!");
                break;

            case 2: // Listar todos
                var todos = await repository.ObterTodosAsync();
                Console.WriteLine($"📋 Total: {todos.Count} registro(s)\n");

                if (todos.Count == 0)
                {
                    Console.WriteLine("ℹ️  Nenhum registro encontrado.");
                }
                else
                {
                    foreach (var log in todos)
                    {
                        Console.WriteLine($"─────────────────────────────────────────");
                        Console.WriteLine($"ID: {log.Id}");
                        Console.WriteLine($"Data: {log.Data:dd/MM/yyyy HH:mm}");
                        Console.WriteLine($"Status: {log.Status}");
                        Console.WriteLine($"Mensagem: {log.Mensagem}");
                    }
                    Console.WriteLine($"─────────────────────────────────────────");
                }
                break;

            case 3: // Buscar por status
                Console.Write("Status para buscar (Pendente/EmAndamento/Concluido): ");
                var statusBusca = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(statusBusca))
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Status não pode ser vazio!");
                    break;
                }

                var statusValidoBusca = DesafioAlgoritmo.Demo.InterfaceConsole.ValidarStatus(statusBusca);
                if (statusValidoBusca == null)
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Status inválido! Use: Pendente, EmAndamento ou Concluido");
                    break;
                }

                var encontrados = await repository.ObterPorStatusAsync(statusValidoBusca);
                Console.WriteLine($"\n📋 Encontrados: {encontrados.Count} registro(s) com status '{statusValidoBusca}'\n");

                foreach (var log in encontrados)
                {
                    Console.WriteLine($"  • ID {log.Id}: {log.Mensagem} ({log.Data:dd/MM HH:mm})");
                }
                break;

            case 4: // Atualizar
                Console.Write("ID do registro para atualizar: ");
                if (!int.TryParse(Console.ReadLine(), out int idAtualizar))
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("ID inválido!");
                    break;
                }

                var logAtualizar = await repository.ObterPorIdAsync(idAtualizar);
                if (logAtualizar == null)
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro($"Registro com ID {idAtualizar} não encontrado!");
                    break;
                }

                Console.WriteLine($"Registro atual: {logAtualizar.Mensagem} [{logAtualizar.Status}]");
                Console.Write("Nova mensagem (Enter para manter): ");
                var novaMensagem = Console.ReadLine();
                Console.Write("Novo status (Pendente/EmAndamento/Concluido, Enter para manter): ");
                var novoStatus = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(novaMensagem))
                    logAtualizar.Mensagem = novaMensagem;

                if (!string.IsNullOrWhiteSpace(novoStatus))
                {
                    var statusValidoAtualizar = DesafioAlgoritmo.Demo.InterfaceConsole.ValidarStatus(novoStatus);
                    if (statusValidoAtualizar == null)
                    {
                        DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Status inválido! Use: Pendente, EmAndamento ou Concluido. Registro não atualizado.");
                        break;
                    }
                    logAtualizar.Status = statusValidoAtualizar;
                }

                await repository.AtualizarAsync(logAtualizar);
                Console.WriteLine("\n✅ Registro atualizado com sucesso!");
                break;

            case 5: // Deletar
                Console.Write("ID do registro para deletar: ");
                if (!int.TryParse(Console.ReadLine(), out int idDeletar))
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("ID inválido!");
                    break;
                }

                var deletado = await repository.DeletarAsync(idDeletar);
                if (deletado)
                    Console.WriteLine("\n✅ Registro deletado com sucesso!");
                else
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro($"Registro com ID {idDeletar} não encontrado!");
                break;

            default:
                DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Opção inválida!");
                break;
        }

        Console.WriteLine();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SEÇÃO 6 - HTTP CLIENT RESILIENTE (COM OBSERVABILIDADE INTEGRADA)
// ═══════════════════════════════════════════════════════════════════════════

async Task TestarClienteHttpResiliente()
{
    bool continuarSubmenu = true;
    TimeSpan timeoutPersonalizado = TimeSpan.FromSeconds(30); // Padrão 30s (mantém valor entre iterações)

    while (continuarSubmenu)
    {
        Console.Clear();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     SEÇÃO 4.2 - CLIENTE HTTP RESILIENTE (POLLY)         ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        Console.WriteLine("📝 Demonstra cliente HTTP com retry resiliente usando Polly.\n");

        Console.WriteLine("💡 CONFIGURAÇÃO ATUAL:");
        Console.WriteLine("   • Retry: 3 tentativas (4 tentativas totais)");
        Console.WriteLine("   • Backoff exponencial: 1s, 2s, 4s");
        Console.WriteLine($"   • Timeout: {timeoutPersonalizado.TotalSeconds}s POR TENTATIVA (use opção 5 para alterar)");
        Console.WriteLine("   • Retry em: 5xx, 408, network errors");
        Console.WriteLine($"   • Tempo máximo total: ~{timeoutPersonalizado.TotalSeconds * 4 + 7}s (4 tentativas + delays)\n");

        Console.WriteLine("🎯 CENÁRIOS DE TESTE:\n");
        Console.WriteLine("  1 - Sucesso imediato (API que responde OK)");
        Console.WriteLine("  2 - Falha com retry (simula 503 → 503 → 200)");
        Console.WriteLine("  3 - Falha completa (simula 500 em todas tentativas)");
        Console.WriteLine("  4 - Timeout (URL que demora muito)");
        Console.WriteLine("  5 - Configurar timeout customizado");
        Console.WriteLine("  0 - Voltar ao menu principal\n");

        Console.Write("Escolha um cenário: ");
        if (!int.TryParse(Console.ReadLine(), out int cenario) || cenario < 0 || cenario > 5)
        {
            DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Opção inválida!");
            Console.WriteLine("Pressione qualquer tecla para continuar...");
            Console.ReadKey(true);
            continue;
        }

        if (cenario == 0)
        {
            continuarSubmenu = false;
            continue;
        }

        // Opção para configurar timeout customizado
        if (cenario == 5)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           CONFIGURAR TIMEOUT POR TENTATIVA               ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("📖 COMO FUNCIONA:");
            Console.WriteLine("   • Timeout é aplicado POR TENTATIVA individual");
            Console.WriteLine("   • Cada retry tem seu próprio timeout");
            Console.WriteLine("   • Delays entre retries: 1s, 2s, 4s\n");

            Console.WriteLine("💡 EXEMPLO:");
            Console.WriteLine("   Timeout configurado: 10s");
            Console.WriteLine("   ├─ Tentativa 1: até 10s");
            Console.WriteLine("   ├─ Delay: 1s");
            Console.WriteLine("   ├─ Tentativa 2: até 10s");
            Console.WriteLine("   ├─ Delay: 2s");
            Console.WriteLine("   ├─ Tentativa 3: até 10s");
            Console.WriteLine("   ├─ Delay: 4s");
            Console.WriteLine("   └─ Tentativa 4: até 10s");
            Console.WriteLine("   ");
            Console.WriteLine("   Tempo máximo total: (10s × 4) + (1s + 2s + 4s) = ~47s\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  Timeout atual: {timeoutPersonalizado.TotalSeconds}s por tentativa");
            Console.ResetColor();

            Console.Write("\n⏱️  Digite o novo timeout em segundos (Enter para manter): ");
            var input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (int.TryParse(input, out int timeoutSegundos) && timeoutSegundos > 0)
                {
                    timeoutPersonalizado = TimeSpan.FromSeconds(timeoutSegundos);
                    var tempoMaximo = (timeoutSegundos * 4) + 7;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✅ Timeout configurado para {timeoutSegundos}s por tentativa");
                    Console.ResetColor();
                    Console.WriteLine($"📊 Tempo máximo total (pior caso): ~{tempoMaximo}s");
                    Console.WriteLine($"   └─ ({timeoutSegundos}s × 4 tentativas) + (7s de delays)");
                }
                else
                {
                    DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro("Timeout inválido! Mantendo configuração anterior.");
                }
            }
            else
            {
                Console.WriteLine($"ℹ️  Mantendo timeout de {timeoutPersonalizado.TotalSeconds}s");
            }

            Console.WriteLine("\nPressione qualquer tecla para voltar ao menu...");
            Console.ReadKey(true);
            continue;
        }

        using var scope = serviceProvider.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(ClienteHttpResistente));
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ClienteHttpResistente>>();
        var metricas = scope.ServiceProvider.GetRequiredService<MetricasAplicacao>();
        var cliente = new ClienteHttpResistente(httpClient, logger, metricas, timeoutPersonalizado);

        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("║  🔍 OBSERVABILIDADE ATIVA                                 ║");
        Console.ResetColor();
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine("📊 Métricas sendo coletadas:");
        Console.WriteLine("   • Tempo de cada tentativa HTTP");
        Console.WriteLine("   • Contagem de retries (Polly)");
        Console.WriteLine("   • Sucesso/erro por operação");
        Console.WriteLine("   • Correlação via OperationId");
        Console.WriteLine();

    try
    {
        switch (cenario)
        {
            case 1: // Sucesso
                Console.WriteLine("🔄 Testando API que responde com sucesso...\n");
                Console.WriteLine("📍 URL: https://httpbin.org/status/200");
                Console.WriteLine("   (Serviço real que sempre retorna 200 OK)\n");

                var response1 = await cliente.ObterAsync("https://httpbin.org/status/200");

                Console.WriteLine("\n═══════════════════════════════════════════════════════════");

                if (response1.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ SUCESSO na primeira tentativa!");
                    Console.ResetColor();
                    Console.WriteLine($"📊 Status Code: {response1.StatusCode} ({(int)response1.StatusCode})");
                    Console.WriteLine($"⏱️  Sem retries necessários");
                    Console.WriteLine($"\n💡 API respondeu imediatamente com sucesso!");
                    Console.WriteLine($"   Nenhum retry foi necessário.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ ERRO INESPERADO!");
                    Console.ResetColor();
                    Console.WriteLine($"📊 Status Code: {response1.StatusCode} ({(int)response1.StatusCode})");
                    Console.WriteLine($"\n⚠️  A API deveria retornar 200, mas retornou {response1.StatusCode}!");
                }
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                break;

            case 2: // Retry - demonstra tentativas
                Console.WriteLine("🔄 Testando retry com erro 503 (Service Unavailable)...\n");
                Console.WriteLine("📍 URL: https://httpbin.org/status/503");
                Console.WriteLine("   (API que sempre retorna 503 para demonstrar retry)\n");
                Console.WriteLine("⚠️  OBSERVE nos logs acima:");
                Console.WriteLine("   Você verá 'Retry 1', 'Retry 2', 'Retry 3'");
                Console.WriteLine("   Com delays de 1s, 2s, 4s (backoff exponencial)\n");

                var response2 = await cliente.ObterAsync("https://httpbin.org/status/503");

                Console.WriteLine("\n═══════════════════════════════════════════════════════════");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  API RETORNOU ERRO APÓS 4 TENTATIVAS");
                Console.ResetColor();
                Console.WriteLine($"📊 Status Code: {response2.StatusCode} ({(int)response2.StatusCode})");
                Console.WriteLine($"\n💡 O que aconteceu:");
                Console.WriteLine($"   ✓ Tentativa 1: 503 Service Unavailable");
                Console.WriteLine($"   ✓ Aguardou 1 segundo (backoff)");
                Console.WriteLine($"   ✓ Tentativa 2: 503 Service Unavailable");
                Console.WriteLine($"   ✓ Aguardou 2 segundos (backoff)");
                Console.WriteLine($"   ✓ Tentativa 3: 503 Service Unavailable");
                Console.WriteLine($"   ✓ Aguardou 4 segundos (backoff)");
                Console.WriteLine($"   ✓ Tentativa 4: 503 Service Unavailable → Desistiu");
                Console.WriteLine($"\n⏱️  Total: ~7 segundos de retries");
                Console.WriteLine($"\n📖 Isso demonstra que o Polly está funcionando!");
                Console.WriteLine($"   Ele fez 3 retries com backoff exponencial como configurado.");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                break;

            case 3: // Falha completa
                Console.WriteLine("🔄 Testando erro permanente (500 Internal Server Error)...\n");
                Console.WriteLine("📍 URL: https://httpbin.org/status/500\n");

                try
                {
                    var response3 = await cliente.ObterAsync("https://httpbin.org/status/500");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n═══════════════════════════════════════════════════════════");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ FALHA PERMANENTE (mesmo após retries)");
                    Console.ResetColor();
                    Console.WriteLine($"\n💡 O Polly tentou 4 vezes:");
                    Console.WriteLine($"   • Original + 3 retries");
                    Console.WriteLine($"   • Delays: 1s, 2s, 4s");
                    Console.WriteLine($"   • Total: ~7 segundos");
                    Console.WriteLine($"\n📊 Erro: {ex.GetType().Name}");
                    Console.WriteLine($"💬 Mensagem: Servidor retornou 500 em todas tentativas");
                    Console.WriteLine("═══════════════════════════════════════════════════════════");
                }
                break;

            case 4: // Timeout
                Console.WriteLine("🔄 Testando timeout (URL que demora 15 segundos)...\n");
                Console.WriteLine("📍 URL: https://httpbin.org/delay/15");
                Console.WriteLine("   (Servidor espera 15s antes de responder)\n");

                Console.WriteLine("⚙️  CONFIGURAÇÃO:");
                Console.WriteLine($"   • Timeout: {timeoutPersonalizado.TotalSeconds}s POR TENTATIVA");
                Console.WriteLine($"   • Retries: 3 (4 tentativas totais)");
                Console.WriteLine($"   • Tempo máximo: ~{(timeoutPersonalizado.TotalSeconds * 4) + 7}s\n");

                if (timeoutPersonalizado.TotalSeconds >= 15)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ Timeout ({timeoutPersonalizado.TotalSeconds}s) > Delay do servidor (15s)");
                    Console.ResetColor();
                    Console.WriteLine("   Primeira tentativa deve ter SUCESSO!\n");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️  Timeout ({timeoutPersonalizado.TotalSeconds}s) < Delay do servidor (15s)");
                    Console.ResetColor();
                    Console.WriteLine("   Todas as 4 tentativas vão dar TIMEOUT!");
                    Console.WriteLine($"   Tempo total: ~{(timeoutPersonalizado.TotalSeconds * 4) + 7}s\n");
                    Console.WriteLine("💡 TIP: Use opção 5 para configurar timeout >= 16s\n");
                }

                try
                {
                    var response4 = await cliente.ObterAsync("https://httpbin.org/delay/15");

                    Console.WriteLine("\n═══════════════════════════════════════════════════════════");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ SUCESSO - Servidor respondeu dentro do timeout!");
                    Console.ResetColor();
                    Console.WriteLine($"📊 Status Code: {response4.StatusCode}");
                    Console.WriteLine($"\n💡 O que aconteceu:");
                    Console.WriteLine($"   • Servidor demorou 15s para responder");
                    Console.WriteLine($"   • Timeout de {timeoutPersonalizado.TotalSeconds}s foi suficiente");
                    Console.WriteLine($"   • Sucesso na primeira tentativa (sem retries necessários)");
                    Console.WriteLine("═══════════════════════════════════════════════════════════");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n═══════════════════════════════════════════════════════════");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ TODAS AS 4 TENTATIVAS FALHARAM POR TIMEOUT");
                    Console.ResetColor();
                    Console.WriteLine($"\n💡 O que aconteceu:");
                    Console.WriteLine($"   • Tentativa 1: Timeout após {timeoutPersonalizado.TotalSeconds}s");
                    Console.WriteLine($"   • Aguardou 1s (backoff)");
                    Console.WriteLine($"   • Tentativa 2: Timeout após {timeoutPersonalizado.TotalSeconds}s");
                    Console.WriteLine($"   • Aguardou 2s (backoff)");
                    Console.WriteLine($"   • Tentativa 3: Timeout após {timeoutPersonalizado.TotalSeconds}s");
                    Console.WriteLine($"   • Aguardou 4s (backoff)");
                    Console.WriteLine($"   • Tentativa 4: Timeout após {timeoutPersonalizado.TotalSeconds}s → Desistiu");

                    var tempoTotal = (timeoutPersonalizado.TotalSeconds * 4) + 7;
                    Console.WriteLine($"\n⏱️  Tempo total gasto: ~{tempoTotal}s");
                    Console.WriteLine($"   └─ ({timeoutPersonalizado.TotalSeconds}s × 4) + (1s + 2s + 4s)");

                    Console.WriteLine($"\n📊 Análise:");
                    Console.WriteLine($"   • Servidor precisa de 15s para responder");
                    Console.WriteLine($"   • Seu timeout está em {timeoutPersonalizado.TotalSeconds}s por tentativa");
                    Console.WriteLine($"   • {timeoutPersonalizado.TotalSeconds}s < 15s → Nunca vai dar certo!");

                    Console.WriteLine($"\n📊 Erro: {ex.GetType().Name}");
                    Console.WriteLine($"💬 Mensagem: {ex.Message}");

                    Console.WriteLine($"\n💡 SOLUÇÃO:");
                    Console.WriteLine($"   Use opção 5 para configurar timeout >= 16s");
                    Console.WriteLine($"   Exemplo: timeout de 20s → Sucesso na 1ª tentativa!");
                    Console.WriteLine("═══════════════════════════════════════════════════════════");
                }
                break;
        }
    }
    catch (Exception ex)
    {
        DesafioAlgoritmo.Demo.InterfaceConsole.MostrarErro($"Erro inesperado: {ex.Message}");
    }

        Console.WriteLine($"\n\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                    RESUMO TÉCNICO                         ║");
        Console.WriteLine($"╚═══════════════════════════════════════════════════════════╝\n");

        Console.WriteLine($"📊 CONFIGURAÇÃO POLLY:");
        Console.WriteLine($"   • Retries: 3 (4 tentativas totais: 1 original + 3 retries)");
        Console.WriteLine($"   • Backoff exponencial: 1s → 2s → 4s");
        Console.WriteLine($"   • Retry em: 5xx, 408, network errors, timeout");
        Console.WriteLine($"   • Timeout: {timeoutPersonalizado.TotalSeconds}s POR TENTATIVA");

        var tempoMaximoTotal = (timeoutPersonalizado.TotalSeconds * 4) + 7;
        Console.WriteLine($"\n⏱️  CÁLCULO DE TEMPO:");
        Console.WriteLine($"   • Timeout por tentativa: {timeoutPersonalizado.TotalSeconds}s");
        Console.WriteLine($"   • Total de tentativas: 4");
        Console.WriteLine($"   • Delays entre retries: 1s + 2s + 4s = 7s");
        Console.WriteLine($"   • Tempo máximo possível: ~{tempoMaximoTotal}s");
        Console.WriteLine($"     └─ ({timeoutPersonalizado.TotalSeconds}s × 4) + 7s");

        Console.WriteLine($"\n💡 IMPORTANTE:");
        Console.WriteLine($"   • Timeout é POR TENTATIVA, não global");
        Console.WriteLine($"   • Se API demora 15s, timeout deve ser >= 16s");
        Console.WriteLine($"   • Retry funciona para erros transitórios (conexão, 5xx)");
        Console.WriteLine($"   • Timeout não resolve problema de API lenta");

        Console.WriteLine($"\n📖 Ver ANALISE_RETRY_POLLY.md para detalhes completos!");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📈 MÉTRICAS REGISTRADAS:");
        Console.ResetColor();
        Console.WriteLine($"   ✅ Operação: ClienteHttpResistente.ObterAsync");
        Console.WriteLine($"   ⏱️  Timeout por tentativa: {timeoutPersonalizado.TotalSeconds}s");
        Console.WriteLine($"   🔁 Retries executados: Veja logs acima");
        Console.WriteLine($"   🔗 Logs com correlação via OperationId");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("💡 Veja os logs acima - todos têm [IdOperacao=...] para correlação!");
        Console.ResetColor();

        Console.WriteLine("\n\nPressione qualquer tecla para voltar ao menu de HTTP Client...");
        Console.ReadKey(true);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// FUNÇÕES AUXILIARES
// ═══════════════════════════════════════════════════════════════════════════

void MostrarMenuPrincipal()
{
    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║        DESAFIO TÉCNICO - ALGORITMOS E BACKEND            ║");
    Console.WriteLine("║              Demonstração Interativa                      ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

    Console.WriteLine("📚 SEÇÕES DISPONÍVEIS:\n");
    Console.WriteLine("  1️⃣  Algoritmo: Primeiro Número Repetido");
    Console.WriteLine("      └─ Encontra primeira duplicata em O(n)");
    Console.WriteLine();
    Console.WriteLine("  2️⃣  Algoritmo: Maior Sequência Crescente");
    Console.WriteLine("      └─ Encontra maior subsequência em O(n)");
    Console.WriteLine();
    Console.WriteLine("  3️⃣  Concorrência: Processamento Paralelo");
    Console.WriteLine("      └─ Thread-safe + Métricas + Correlação");
    Console.WriteLine();
    Console.WriteLine("  4️⃣  Design: Orquestrador com Tratamento de Erros");
    Console.WriteLine("      └─ Respostas parciais + Observabilidade");
    Console.WriteLine();
    Console.WriteLine("  5️⃣  Persistência: Entity Framework Core");
    Console.WriteLine("      └─ CRUD completo + Métricas de performance");
    Console.WriteLine();
    Console.WriteLine("  6️⃣  HTTP Client Resiliente: Polly + Retry");
    Console.WriteLine("      └─ Retry com backoff + Métricas + Correlação");
    Console.WriteLine();
    Console.WriteLine("  7️⃣  Sair");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("💡 Observabilidade integrada em TODAS as opções!");
    Console.ResetColor();
    Console.WriteLine("   • Métricas de tempo e taxa de erro");
    Console.WriteLine("   • Correlação via OperationId");
    Console.WriteLine("   • Logs estruturados com contexto");
    Console.WriteLine("\n─────────────────────────────────────────────────────────────");
}
