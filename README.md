# Algorítimos

Este projeto foi desenvolvido como parte  técnico de fundamentos backend

---

## Índice

- [Requisitos do Sistema](#requisitos-do-sistema)
- [Como Executar](#como-executar)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Seção 1: Estruturas de Dados e Algoritmos](#seção-1-estruturas-de-dados-e-algoritmos)
- [Seção 2: Concorrência e Programação Assíncrona](#seção-2-concorrência-e-programação-assíncrona)
- [Seção 3: Design e Tratamento de Erros](#seção-3-design-e-tratamento-de-erros)
- [Seção 4: Persistência e Integração Externa](#seção-4-persistência-e-integração-externa)
- [Seção 5: Observabilidade e Testes](#seção-5-observabilidade-e-testes)
- [Testes Automatizados](#testes-automatizados)
- [Decisões Técnicas](#decisões-técnicas)

---

## Requisitos do Sistema

- **.NET 8 SDK** ou superior
- Sistema operacional: Windows, Linux ou macOS
- Editor de código (opcional): Visual Studio, VS Code ou Rider

---

## Como Executar

### 1. Clonar o repositório
```bash
git clone <url-do-repositorio>
cd Vinco
```

### 2. Restaurar dependências
```bash
dotnet restore
```

### 3. Compilar o projeto
```bash
dotnet build
```

### 4. Executar os testes
```bash
dotnet test
```

### 5. Executar o projeto demo
```bash
dotnet run --project src/DesafioAlgoritmo.Demo
```

---

## Estrutura do Projeto

```
Vinco/
├── src/
│   ├── DesafioAlgoritmo.Core/              # Lógica de negócio e algoritmos
│   │   ├── Algoritmos/                     # Seção 1: Algoritmos
│   │   ├── Concorrencia/                   # Seção 2: Processamento paralelo
│   │   ├── Servicos/                       # Seção 3: Orquestração de dependências
│   │   └── Observabilidade/                # Seção 5: Logs e métricas
│   ├── DesafioAlgoritmo.Infrastructure/    # Infraestrutura (DB, HTTP)
│   │   ├── Persistencia/                   # Seção 4: EF Core InMemory
│   │   └── HttpClient/                     # Seção 4: Cliente HTTP resiliente
│   └── DesafioAlgoritmo.Demo/              # Aplicação console de demonstração
│       ├── Program.cs                      # Menu interativo principal (1224 linhas)
│       ├── InterfaceConsole.cs             # Helpers para entrada/validação de dados
│       └── MockDependenciaExterna.cs       # Mock de dependências externas
└── tests/
    └── DesafioAlgoritmo.Tests/             # Testes automatizados completos
```

---

## Seção 1: Estruturas de Dados e Algoritmos

### Objetivo
Demonstrar domínio de coleções, análise de complexidade e clareza na resolução de problemas algorítmicos.

### Implementação

#### 1. Primeiro Número Repetido
**Arquivo**: `src/DesafioAlgoritmo.Core/Algoritmos/AnalisadorSequencia.cs`

```csharp
public static long? EncontrarPrimeiroRepetido(IEnumerable<long> numeros)
```

**Como funciona:**
- Percorre a lista uma única vez
- Usa um `HashSet` para armazenar números já vistos
- Retorna o primeiro número que já está no HashSet
- Retorna `null` se não encontrar repetição

**Análise de Complexidade:**
- **Tempo**: O(n) - Uma única passagem pela sequência
- **Espaço**: O(n) - HashSet pode armazenar até n elementos únicos no pior caso

**Justificativa da escolha:**
- `HashSet` oferece busca O(1), melhor que uma lista (O(n))
- Algoritmo de passagem única é mais eficiente que comparações aninhadas O(n²)
- Termina antecipadamente ao encontrar a primeira duplicata

**Tipo de dados:**
- Usa `long` (64 bits) para suportar números de -9.223.372.036.854.775.808 até 9.223.372.036.854.775.807
- Previne `OverflowException` com números grandes

#### 2. Maior Subsequência Consecutiva
**Arquivo**: `src/DesafioAlgoritmo.Core/Algoritmos/AnalisadorSequencia.cs`

```csharp
public static IReadOnlyList<long> EncontrarMaiorSubsequenciaConsecutiva(IReadOnlyList<long> numeros)
```

**Como funciona:**
- Percorre a lista uma vez mantendo duas sequências: atual e melhor
- Compara cada elemento com o anterior para verificar se é consecutivo (n+1)
- Se for consecutivo, continua a sequência atual
- Se não for, inicia nova sequência
- Retorna a maior sequência consecutiva encontrada

**Análise de Complexidade:**
- **Tempo**: O(n) - Uma única passagem pela sequência
- **Espaço**: O(1) - Apenas variáveis de rastreamento (índices e comprimentos)

**Justificativa da escolha:**
- Abordagem de janela deslizante é mais eficiente que gerar todas as subsequências
- Verifica consecutividade com operação simples: `numeros[i] == numeros[i-1] + 1`
- Não precisa de estruturas de dados auxiliares
- Lida corretamente com empates retornando a primeira sequência

**Nota importante:**
- O algoritmo procura números **consecutivos** (1,2,3,4...), não apenas crescentes
- Suporta números `long` (até 9.223.372.036.854.775.807)

---

## Seção 2: Concorrência e Programação Assíncrona

### Objetivo
Demonstrar capacidade de lidar com paralelismo, sincronização e cancelamento em cenários práticos.

### Implementação

**Arquivo**: `src/DesafioAlgoritmo.Core/Concorrencia/ProcessadorParalelo.cs`

```csharp
public async Task<ResultadoProcessamento> ProcessarAsync(
    IEnumerable<string> itens,
    OpcoesProcessadorParalelo? opcoes = null)
```

**Como funciona:**
- Processa itens em paralelo usando `Parallel.ForEachAsync`
- Permite configurar o grau máximo de paralelismo via `OpcoesProcessadorParalelo`
- Agrupa itens por categoria (primeira letra) sem race conditions
- Suporta cancelamento via `CancellationToken`
- Registra progresso a cada 1000 itens processados

### Estratégias de Sincronização (Thread-Safety)

#### 1. ConcurrentDictionary
```csharp
var contagemPorCategoria = new ConcurrentDictionary<string, int>();
```
- **Por quê?** Oferece operações atômicas thread-safe
- **Benefício**: Operação `AddOrUpdate` é livre de bloqueios (lock-free)
- **Alternativa descartada**: `Dictionary` comum + lock seria mais lento

#### 2. Interlocked.Increment
```csharp
var atual = Interlocked.Increment(ref quantidadeProcessada);
```
- **Por quê?** Incremento atômico sem necessidade de locks
- **Benefício**: Operação O(1) extremamente rápida a nível de CPU
- **Alternativa descartada**: `lock` seria overhead desnecessário

#### 3. Parallel.ForEachAsync
```csharp
await Parallel.ForEachAsync(itens, opcoesParalelas, async (item, ct) => { ... });
```
- **Por quê?** Gerencia automaticamente o pool de threads
- **Benefício**: Controle fino sobre paralelismo com `MaxDegreeOfParallelism`
- **Trade-off**: Paralelismo configurável vs simplicidade de `Task.WhenAll`

### Por que evita Race Conditions?

**Race condition** acontece quando múltiplas threads acessam/modificam dados compartilhados simultaneamente sem sincronização adequada.

**A solução evita isso porque:**

1. **ConcurrentDictionary** garante que operações de leitura/escrita são atômicas
2. **Interlocked.Increment** garante que o contador é incrementado atomicamente
3. **Nenhum estado mutável compartilhado** sem proteção
4. **Cada thread trabalha de forma independente** até consolidar resultados

### Validação
- Testes com 10.000 itens produzem resultados determinísticos
- Testes validam que diferentes graus de paralelismo (1, 4, 8) produzem resultados idênticos
- Cancelamento funciona corretamente e retorna progresso parcial

---

## Seção 3: Design e Tratamento de Erros

### Objetivo
Verificar aplicação de princípios de design, clareza em contratos e estratégias de tratamento de falhas.

### Implementação

**Arquivo**: `src/DesafioAlgoritmo.Core/Servicos/OrquestradorDependencias.cs`

```csharp
public async Task<ResultadoOrquestracao> ExecutarAsync(
    IEnumerable<IDependenciaExterna> dependencias,
    CancellationToken cancellationToken = default)
```

**Como funciona:**
- Orquestra chamadas a múltiplas dependências externas (3 APIs simuladas)
- Cada dependência implementa interface `IDependenciaExterna`
- **Falhas individuais não param a execução** - continua processando outras
- Agrega respostas bem-sucedidas e falhas em resultado único
- Logs estruturados em cada etapa

### Contratos e Interfaces

#### IDependenciaExterna
```csharp
public interface IDependenciaExterna
{
    string Nome { get; }
    Task<string> ChamarAsync(CancellationToken cancellationToken = default);
}
```

#### ResultadoOrquestracao
```csharp
public sealed class ResultadoOrquestracao
{
    public IReadOnlyDictionary<string, string> RespostasComSucesso { get; }
    public IReadOnlyDictionary<string, string> Falhas { get; }
    public bool SucessoTotal { get; }
    public bool SucessoParcial { get; }
    public bool FalhaCompleta { get; }
}
```

### Políticas de Retry - Análise e Justificativa

#### Estratégia Implementada
O retry **não está no orquestrador**, mas sim na camada de infraestrutura (`ClienteHttpResistente`).

**Por quê essa decisão?**
1. **Separação de responsabilidades**: Orquestrador agrega, cliente HTTP gerencia resiliência
2. **Permite políticas diferentes por dependência**: Cada cliente pode ter sua própria estratégia
3. **Previne timeouts em cascata**: Retry no cliente evita que o orquestrador fique esperando

#### Limites Aceitáveis de Retry

**Configuração implementada:**
```csharp
retryCount: 3
backoff: 1s, 2s, 4s (exponencial)
```

**Justificativa:**
- **Máximo 3 tentativas**: Evita atrasos excessivos (~7s no total)
- **Backoff exponencial**: Dá tempo para serviços se recuperarem
- **Timeout de 10s por requisição**: Previne requisições travadas
- **Não implementado (mas recomendado para produção)**: Circuit Breaker após N falhas consecutivas

**Limites para evitar efeitos colaterais:**
1. **Não fazer retry em operações não idempotentes** (ex: POST de pagamento)
2. **Timeout geral do orquestrador**: `(n_dependencias * 10s) + 20s buffer`
3. **Circuit breaker após 5 falhas consecutivas**: Previne sobrecarga do serviço

---

## Seção 4: Persistência e Integração Externa

### Objetivo
Avaliar modelagem simples de dados, uso de ORM em memória e consumo resiliente de APIs.

### Implementação

#### 1. Repositório com EF Core InMemory

**Arquivos**: `src/DesafioAlgoritmo.Infrastructure/Persistencia/`

**Entidade:**
```csharp
public sealed class RegistroTrabalho
{
    public int Id { get; set; }
    public DateTime Data { get; set; }
    public string Mensagem { get; set; }
    public string Status { get; set; }
    public DateTime CriadoEm { get; set; }
}
```

**Operações implementadas:**
- `AdicionarAsync` - Inserir novo registro
- `ObterPorIdAsync` - Buscar por ID
- `ObterTodosAsync` - Listar todos (ordenado por data)
- `ObterPorStatusAsync` - Filtrar por status
- `ObterPorIntervaloDataAsync` - Filtrar por intervalo de datas
- `AtualizarAsync` - Modificar registro existente
- `DeletarAsync` - Remover registro

**Configuração do contexto:**
```csharp
public sealed class ContextoBdRegistroTrabalho : DbContext
{
    protected override void OnModelCreating(ModelBuilder construtor)
    {
        // Restrições, índices e validações
    }
}
```

#### 2. Cliente HTTP Resiliente

**Arquivo**: `src/DesafioAlgoritmo.Infrastructure/HttpClient/ClienteHttpResistente.cs`

```csharp
public sealed class ClienteHttpResistente
{
    public async Task<HttpResponseMessage> ObterAsync(string url, ...)
    public async Task<HttpResponseMessage> EnviarAsync(string url, HttpContent conteudo, ...)
}
```

**Recursos implementados:**
- **HttpClient injetado via construtor** (compatível com `HttpClientFactory`)
- **Timeout configurável** (padrão: 10 segundos)
- **Retry exponencial com Polly**: 3 tentativas (1s, 2s, 4s)
- **Logs estruturados** em cada tentativa e resultado
- **Métricas** de tempo de resposta e taxa de erro

**Política de retry:**
```csharp
Policy
    .HandleResult<HttpResponseMessage>(r =>
        r.StatusCode >= HttpStatusCode.InternalServerError ||
        r.StatusCode == HttpStatusCode.RequestTimeout)
    .Or<HttpRequestException>()
    .Or<TaskCanceledException>()
    .WaitAndRetryAsync(retryCount: 3, ...)
```

**Quando faz retry:**
- Status 5xx (erro do servidor)
- Status 408 (Request Timeout)
- Exceções de rede (HttpRequestException)
- Timeouts (TaskCanceledException)

**Quando NÃO faz retry:**
- Status 4xx (erro do cliente - não adianta tentar de novo)
- Status 2xx/3xx (sucesso)

---

## Seção 5: Observabilidade e Testes

### Objetivo
Confirmar maturidade em instrumentação básica, métricas e cobertura de testes automatizados.

### Implementação

#### 1. Logs Estruturados com Correlação

**Arquivo**: `src/DesafioAlgoritmo.Core/Observabilidade/`

**Sistema de correlação:**
```csharp
public sealed class ContextoOperacao
{
    public string IdOperacao { get; }  // Ex: "a3f4b2c1"
    public static IDisposable IniciarOperacao()
}
```

**Como funciona:**
1. Cada operação cria um `ContextoOperacao` que gera um ID único de 8 caracteres
2. O ID é armazenado em `AsyncLocal<T>` (mantém o contexto através de chamadas assíncronas)
3. Todos os logs incluem automaticamente o `IdOperacao` via métodos de extensão

**Métodos de extensão:**
```csharp
_logger.RegistrarInformacaoComContexto("Processando {Quantidade} itens", 1000);
// Output: [IdOperacao=a3f4b2c1] Processando 1000 itens
```

**Formato dos logs:**
-  Chave-valor estruturado: `Nome={Valor}`
-  Correlação automática: `[IdOperacao=xxx]`
-  Níveis apropriados: Information, Warning, Error, Debug

#### 2. Métricas

**Arquivo**: `src/DesafioAlgoritmo.Core/Observabilidade/MetricasAplicacao.cs`

**Tecnologia utilizada:**
- `System.Diagnostics.Metrics` (biblioteca nativa do .NET 8)
- Compatível com OpenTelemetry para exportação

**Métricas implementadas:**

| Nome da Métrica | Tipo | Descrição |
|-----------------|------|-----------|
| `operacao.contagem` | Counter | Total de operações executadas (sucesso/erro) |
| `operacao.erro.contagem` | Counter | Total de erros por tipo de operação |
| `operacao.duracao` | Histogram | Distribuição de tempo de processamento (ms) |

**Como usar:**
```csharp
var cronometro = MetricasAplicacao.IniciarCronometro();
// ... operação ...
cronometro.Stop();
_metricas.RegistrarOperacao("MinhaOperacao", cronometro.Elapsed);
```

**Como seriam expostas em produção:**

1. **OpenTelemetry + Prometheus:**
```csharp
services.AddOpenTelemetry()
    .WithMetrics(builder => builder
        .AddMeter("DesafioAlgoritmo")
        .AddPrometheusExporter());
```

2. **Endpoint de métricas:**
```
GET /metrics
```

3. **Alertas sugeridos:**
- Taxa de erro > 5% em 5 minutos
- Latência p99 > 5 segundos
- Operações ativas > 1000

**Dashboards sugeridos:**
- Tempo médio de processamento por operação
- Taxa de sucesso/erro ao longo do tempo

---

## Testes Automatizados

### Cobertura

```
Total de testes: 79
Taxa de sucesso: 100%
```

### Organização dos Testes

```
tests/DesafioAlgoritmo.Tests/
├── Algoritmos/
│   └── AnalisadorSequenciaTestes.cs (21 testes)
├── Concorrencia/
│   └── ProcessadorParaleloTests.cs (10 testes)
├── Servicos/
│   └── OrquestradorDependenciasTestes.cs (10 testes)
├── Persistencia/
│   └── RepositorioRegistroTrabalhoTestes.cs (15 testes)
├── Http/
│   └── ClienteHttpResilienteTestes.cs (12 testes)
└── Observabilidade/
    ├── ContextoOperacaoTestes.cs (6 testes)
    └── MetricasAplicacaoTestes.cs (5 testes)
```

### Cenários Cobertos

**Seção 1 - Algoritmos:**
- Com duplicatas / sem duplicatas
- Sequências vazias / elemento único
- Números negativos
- Casos extremos e validação de entrada nula

**Seção 2 - Concorrência:**
- Processamento determinístico (10.000 itens)
- Diferentes graus de paralelismo (1, 4, 8 threads)
- Cancelamento ordenado
- Coleções vazias

**Seção 3 - Orquestração:**
- Sucesso total (todas dependências OK)
- Sucesso parcial (algumas falham)
- Falha completa (todas falham)
- Cancelamento de operação

**Seção 4 - Persistência:**
- Operações CRUD completas
- Filtros por status e data
- Validação de regras de negócio

**Seção 5 - Observabilidade:**
- Correlação de IDs funcionando
- Métricas sendo registradas
- Logs estruturados

### Executar Testes

```bash
# Todos os testes
dotnet test

```

---

## Decisões Técnicas

### Arquitetura

**Estrutura escolhida:** Clean Architecture simplificada
- **Core**: Lógica de negócio sem dependências externas
- **Infrastructure**: Implementações concretas (DB, HTTP)
- **Demo**: Aplicação console para demonstração

**Justificativa:**
- Testabilidade: Core pode ser testado sem infraestrutura
- Manutenibilidade: Mudanças em infraestrutura não afetam negócio
- Flexibilidade: Fácil trocar EF InMemory por SQL Server, por exemplo

### Dependências Utilizadas

| Pacote | Versão | Justificativa |
|--------|--------|---------------|
| `Microsoft.Extensions.Logging.Abstractions` | 9.0.10 | Logging estruturado padrão .NET |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.10 | Banco em memória para testes |
| `Microsoft.Extensions.Http.Polly` | 9.0.10 | Resiliência HTTP com retry |
| `FluentAssertions` | 7.0.0 | Assertions legíveis nos testes |
| `Moq` | 4.20.0 | Mocks para testes unitários |
| `xUnit` | 2.9.0 | Framework de testes |

**Todas as dependências são amplamente adotadas pela comunidade .NET.**

### Padrões de Código

- **Nomenclatura em português**: Facilita compreensão
- **Métodos curtos e focados**: Média de 10-15 linhas
- **Immutabilidade onde possível**: `sealed class`, `init`, `readonly`
- **Null-safety**: Uso de `?`, `??`, `ThrowIfNull`
- **Async/await consistente**: Todas operações I/O são assíncronas

### Trade-offs Identificados

1. **Orquestração paralela com Task.WhenAll**
   - Escolhido: Paralela
   - Motivo: Maximiza performance ao chamar múltiplas APIs simultaneamente
   - Benefício: ~3x mais rápido que chamadas sequenciais
   - Tratamento de erros: Cada tarefa captura sua própria exceção sem afetar as outras

2. **EF Core InMemory vs SQLite**
   - Escolhido: InMemory
   - Motivo: Requisito do desafio e simplicidade para testes
   - Trade-off: SQLite seria mais próximo de produção

3. **Logs síncronos vs assíncronos**
   - Escolhido: Síncronos
   - Motivo: Simplicidade e baixo volume esperado
   - Trade-off: Logs assíncronos teriam melhor performance em alta escala

---

