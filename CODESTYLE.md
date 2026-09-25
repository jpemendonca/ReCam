# ReCam: regras de código

Só entra aqui o que dá para checar lendo um diff.

## 1. Idioma

- Código, identificadores, comentários, mensagens de log e commits: inglês.
- Documentos do repositório para agentes (`AGENTS.md`, `SPECS.md`, `CODESTYLE.md`,
  `ROADMAP.md`): português. `README.md`: inglês.
- Nome do produto em texto lido por pessoas: **ReCam**. Em identificador técnico (esquema de
  URI, pacote Android, imagem Docker, namespace .NET) fica como já está: `recam`, `Recam`.
- Texto que o usuário vê no app: só via ARB, com a chave presente em `app_en.arb` e
  `app_pt.arb` no mesmo commit.

```dart
// errado
Text('Adicionar câmera')

// certo
Text(AppLocalizations.of(context).addCamera)
```

## 2. Comentários

- Comentário só quando o porquê não é óbvio pelo código.
- Proibido: comentário que repete o código, código comentado, banner de seção, cabeçalho de
  arquivo, TODO sem bullet no ROADMAP.
- Frase curta, voz ativa, sem enchimento. Fala do código, não da mudança que o criou.

```csharp
// errado
// Increment the counter
count++;
// Added this fix for the token bug
// ===== HELPERS =====

// certo
// MediaMTX returns an absolute Location; clients must only see the proxy path.
response.Headers.Location = RewriteLocation(upstreamLocation, cameraId);
```

Exceção única: `// arrange`, `// act`, `// assert` nos testes (seção 8).

## 3. C# (server/)

- `Directory.Build.props` define para todos os projetos: `net10.0`, `Nullable` enable,
  `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `EnforceCodeStyleInBuild` true,
  `AnalysisLevel` latest-recommended.
- Namespace com escopo de arquivo. Um tipo público por arquivo, com o mesmo nome do arquivo.
- Classes são `sealed`, salvo quando existe herança no código.
- DTOs de entrada e saída são `record`.
- Métodos assíncronos terminam em `Async` (exceto métodos públicos de hub SignalR, cujo nome é o
  protocolo) e recebem `CancellationToken` quando o chamador tem
  um. Proibido `.Result`, `.Wait()` e `async void`.
- Hora vem de `TimeProvider` injetado. Proibido `DateTime.Now`, `DateTime.UtcNow` e
  `DateTimeOffset.UtcNow` fora do `Program.cs`.
- Números aleatórios de segurança vêm de `RandomNumberGenerator`. Proibido `Random` para token,
  segredo ou id.
- Endpoints em minimal API. Cada feature expõe uma extensão `MapXxxEndpoints(this
  IEndpointRouteBuilder)` e registra seus serviços em `AddXxx(this IServiceCollection)`. O
  `Program.cs` só chama essas extensões.

```csharp
// errado: Program.cs com lógica de endpoint
app.MapPost("/api/pair", async (PairRequest req, RecamDbContext db) => { /* ... */ });

// certo
app.MapPairingEndpoints();
```

- Dependências por construtor (primary constructor permitido). `IServiceProvider.GetService` e
  `GetRequiredService` só no `Program.cs` e nas extensões de composição (`AddXxx`, `MapXxx`,
  `ApplyMigrationsAsync`). Serviço singleton que precisa de banco recebe
  `IDbContextFactory<RecamDbContext>`.
- Log estruturado com template, de preferência via `[LoggerMessage]`. Proibido interpolar string
  no log e proibido logar token, credencial ou `Authorization` (exceção única em `AGENTS.md`).

```csharp
// errado
logger.LogInformation($"Device {id} paired with token {token}");

// certo
logger.LogInformation("Device {DeviceId} paired as {Role}", device.Id, device.Role);
```

- Entidades de `Domain/` têm setters privados e construtor ou factory que garante estado
  válido. Regra de negócio é método da entidade. Proibido endpoint ou serviço alterar
  propriedade de entidade diretamente.

```csharp
// errado: regra no endpoint
if (token.UsedAt is not null || token.ExpiresAt <= now) return Results.Unauthorized();
token.UsedAt = now;

// certo
var consumed = token.Consume(timeProvider.GetUtcNow());
if (consumed.IsFailure) return consumed.Error.ToHttpResult();
```

- Erro esperado é `Result`/`Result<T>` com `DomainError` declarado numa classe estática da área
  (`PairingErrors`, `DeviceErrors`). Proibido `throw` para erro de usuário e proibido
  `try`/`catch` em endpoint.
- Proibido MediatR, AutoMapper e bibliotecas de Result (FluentResults, ErrorOr). O `Result` é
  do projeto.

### 3.1 Camadas do servidor

Conforme `SPECS.md` 2.1. Fiscalizado em `tests/Recam.Server.Tests/Architecture/`:

- `Recam.Server.Domain` não referencia `Recam.Server.Infrastructure` nem `Recam.Server.Features`.
- `Recam.Server.Infrastructure` não referencia `Recam.Server.Features`.
- `Recam.Server.Features.<X>` não referencia `Recam.Server.Features.<Y>`.

O teste descobre as features pelo namespace. Feature nova é coberta sem mudar o teste.

- Migrações ficam em `Infrastructure/Persistence/Migrations/`, geradas por
  `dotnet ef migrations add <Nome> --project src/Recam.Server --output-dir Infrastructure/Persistence/Migrations`
  (rodar em `server/` com `RECAM_DATA_DIR=.data`). O `.editorconfig` marca a pasta como código
  gerado. Nunca editar migração à mão.

## 4. Dart e Flutter (app/)

- Segue o Effective Dart e o `dart format` padrão.
- `analysis_options.yaml` inclui `package:flutter_lints/flutter.yaml` e liga `strict-casts`,
  `strict-inference` e `strict-raw-types`.
- Arquivos em `snake_case.dart`. Um widget público por arquivo.
- Estado em `ChangeNotifier`. A UI escuta com `ListenableBuilder`. Proibido pacote de gerência
  de estado (provider, riverpod, bloc, get).
- Dependências por construtor. Proibido service locator (get_it) e singleton global, exceto o
  `HttpOverrides.global` do `main.dart`.
- Plugin de plataforma só é importado dentro de `lib/core/`, na implementação da interface.
  Controllers e widgets dependem da interface.

```dart
// errado: controller falando direto com o plugin
import 'package:battery_plus/battery_plus.dart';
class CameraModeController { final _battery = Battery(); }

// certo
class CameraModeController {
  CameraModeController({required BatteryReader battery}) : _battery = battery;
  final BatteryReader _battery;
}
```

- `dispose()` fecha tudo o que o objeto abriu: stream subscription, timer, conexão, renderer.

### 4.1 Camadas do app

Conforme `SPECS.md` 2.2. Fiscalizado por `test/architecture_test.dart`:

- `lib/camera/` não importa nada de `lib/viewer/`, e vice-versa.
- `lib/core/` não importa `lib/camera/` nem `lib/viewer/`.

## 5. Erros

- Entrada de usuário ou de rede (QR, corpo de requisição) é validada por função pura que
  devolve todos os erros de uma vez.
- Exceção é reservada para estado impossível, o que indica bug.

```csharp
// errado: exceção para erro de usuário, e para no primeiro erro
if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("name");

// certo: função pura devolve todos os erros de validação
var validation = PairRequestValidator.Validate(req);
if (validation.IsFailure) return validation.Error.ToHttpResult();
```

```dart
// certo
sealed class QrParseResult {}
final class QrParseOk extends QrParseResult { QrParseOk(this.payload); final QrPayload payload; }
final class QrParseFailed extends QrParseResult { QrParseFailed(this.errors); final List<QrError> errors; }
```

## 6. Dependências

- Toda dependência nova (NuGet, pub, imagem Docker) é pedida por um bullet do ROADMAP.
- Versão fixada: NuGet com versão exata, `pubspec.yaml` com `^` e `pubspec.lock` versionado,
  imagem Docker com tag exata.

## 7. Segredos

- Nenhum valor real de segredo em arquivo versionado.
- Configuração nova vira chave em `deploy/.env.example`, com valor vazio, no mesmo commit.
- `.env`, `deploy/data/`, `*.jks`, `*.keystore` e `key.properties` ficam no `.gitignore`.

## 8. Testes

### 8.1 Nome e corpo

Nome no padrão `Metodo_Cenario_Comportamento`, em inglês. Corpo com `// arrange`, `// act`,
`// assert`, nessa ordem. Descrição legível em inglês.

O `.editorconfig` desliga o CA1707 (sublinhado em nome de membro) só em `server/tests/`, porque
ele contradiz este padrão. É a única regra de analisador desligada no projeto. Não serve de
precedente para desligar outras.

C# (xUnit):

```csharp
[Fact(DisplayName = "Pairing with an already used token is rejected")]
public async Task Pair_WithUsedToken_Returns401()
{
    // arrange
    var token = await factory.CreatePairingTokenAsync(DeviceRole.Camera);
    await client.PairAsync(token, "Kitchen");

    // act
    var response = await client.PairAsync(token, "Kitchen again");

    // assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

Dart: o `group` carrega o método, a string do `test` carrega cenário e comportamento.

```dart
group('QrPayload.parse', () {
  test('withoutToken_returnsMissingTokenError', () {
    // arrange
    const uri = 'recam://pair?v=1&u=https%3A%2F%2F192.168.0.10%3A8443';

    // act
    final result = QrPayload.parse(uri);

    // assert
    expect((result as QrParseFailed).errors, contains(QrError.missingToken));
  });
});
```

### 8.2 O que é real e o que é falso

- Servidor: SQLite real num arquivo temporário por teste. MediaMTX real via Testcontainers nos
  testes de `Features/Media`. `FakeTimeProvider` para tempo. Resto com fakes escritos à mão.
  Proibida biblioteca de mock (Moq, NSubstitute).
- App: fakes escritos à mão das interfaces de `lib/core/`. Proibido mockito e mocktail.
- Nunca apontar teste para servidor, banco ou MediaMTX compartilhado de desenvolvimento.

### 8.3 Onde ficam

- `server/tests/Recam.Server.Tests/` espelha `src/Recam.Server/` (ex.: `Features/Pairing/`).
- `app/test/` espelha `app/lib/`.

## 9. Git

- Conventional Commits com escopo da área: `feat(server): ...`, `fix(app): ...`,
  `chore(deploy): ...`, `docs: ...`, `test(server): ...`.
- Uma mudança lógica por commit. O commit do bullet inclui a marcação no ROADMAP.
- Nunca commitar com o gate quebrado. Nunca `--no-verify`.
