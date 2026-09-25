# ReCam: roadmap

## Definição de pronto

Um bullet ganha `[x]` quando o **código está escrito**:
- `bash scripts/gate.sh` passa;
- os testes pedidos no aceite existem e passam, conforme a política do `SPECS.md` seção 8;
- todo código novo tem chamador no código de produção;
- a nota `> Validação (AAAA-MM-DD): ...` está escrita embaixo do bullet;
- o commit está feito em Conventional Commits.

**Caminho percorrido** é outra coisa: alguém abriu o produto e usou aquilo. A nota de validação
diz qual dos dois aconteceu. Um bullet que só tem código escrito diz isso com todas as letras:
`> Validação (AAAA-MM-DD): só código escrito. Não percorrido no aparelho.`

Não confunda código escrito com caminho validado.

Bullets marcados como **[aparelho]** dependem do usuário com os celulares na mão. O agente
não marca esses bullets. Se chegar num deles, escreve `> Bloqueado (AAAA-MM-DD): aguardando
validação no aparelho` e segue para o próximo.

Quem executa cada bullet:

- **[junto]**: Claude Opus, com o autor na conversa. São os bullets .NET que fixam o padrão
  que o autor precisa saber defender em entrevista. Desde 2026-09-25 o autor não acompanha mais:
  a marcação só registra a história, e o agente implementa direto.
- **[opus]**: Claude Opus, sem precisar do autor. São bullets difíceis demais para um modelo
  mais barato (integração WebRTC no celular).
- Sem marcação: qualquer modelo, inclusive um mais barato rodando em loop.

Um modelo que não seja Claude Opus para no primeiro bullet `[junto]` ou `[opus]` não marcado e
avisa o autor para trocar de modelo. Depois do 1.12, nenhum bullet tem essas marcações.

Formato de cada bullet: **Origem** (de onde veio), **Escopo** (o que entra e o que fica fora),
**Aceite** (quando ganha `[x]`).

---

## Fase 0: fundação

- [x] **0.1 Documentos de fundação**
  - Origem: bootstrap do projeto.
  - Escopo: `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `SPECS.md`, `CODESTYLE.md`, `ROADMAP.md`,
    `README.md`, `scripts/gate.sh`, `.githooks/pre-commit`.
  - Aceite: arquivos existem e são coerentes entre si.
  > Validação (2026-09-24): documentos criados. Gate ainda sem área para rodar.

- [x] **0.2 Arquivos-base do repositório**
  - Origem: fundação.
  - Escopo: `.gitignore` (padrões de .NET e Flutter, mais `.env`, `deploy/data/`, `*.jks`,
    `*.keystore`, `key.properties`), `.gitattributes` (`* text=auto eol=lf`), `.editorconfig`
    (indentação de 4 para `*.cs` e 2 para `*.dart`, `*.yaml` e `*.json`), `LICENSE` com o texto
    oficial da AGPL-3.0, copiado sem alteração de https://www.gnu.org/licenses/agpl-3.0.txt.
  - Aceite: `git check-ignore .env deploy/data/x` lista os dois caminhos. O `LICENSE` começa
    com "GNU AFFERO GENERAL PUBLIC LICENSE" e "Version 3, 19 November 2007".
  > Validação (2026-09-24): arquivos criados. `git check-ignore` lista `.env` e
  > `deploy/data/x`. `LICENSE` baixado de gnu.org, 661 linhas, cabeçalho conferido. O
  > `.editorconfig` também fixa namespace com escopo de arquivo e `_camelCase` em campo privado.

- [x] **0.3 [junto] Esqueleto do servidor**
  - Origem: fundação.
  - Escopo: `server/Recam.slnx`, `server/Directory.Build.props` (conforme `CODESTYLE.md` 3),
    `server/src/Recam.Server/` (ASP.NET Core vazio, `Program.cs` com `GET /health` retornando
    `200 "ok"`), `server/tests/Recam.Server.Tests/` (xUnit v3,
    `Microsoft.AspNetCore.Mvc.Testing`, `NetArchTest.Rules`), `Features/Health/`
    (`HealthEndpoints.cs` com `MapHealthEndpoints`), teste de arquitetura com as regras do
    `CODESTYLE.md` 3.1.
  - Fora: TLS, banco, Docker.
  - Aceite: `Health_WhenCalled_ReturnsOk` passa. Os testes de arquitetura existem e passam. O
    gate roda a área `server/`.
  > Validação (2026-09-24): só código escrito. Gate verde com 4 testes. O teste de arquitetura
  > foi conferido criando uma feature temporária que dependia de `Features.Health`: ele falhou
  > apontando o tipo, e a feature foi removida. Ajustes feitos no caminho: `global.json` liga o
  > Microsoft Testing Platform (exigido pelo xUnit v3 no .NET 10), o gate usa
  > `dotnet test --solution`, e os pacotes `Microsoft.NET.Test.Sdk` e
  > `xunit.runner.visualstudio` saíram. O CA1707 foi desligado só em `server/tests/`
  > (`CODESTYLE.md` 8.1).

- [x] **0.4 Esqueleto do app**
  - Origem: fundação.
  - Escopo: `flutter create --org io.recam --project-name recam --platforms android app`.
    `applicationId` `io.recam.app`, `minSdk 28`. `analysis_options.yaml` conforme
    `CODESTYLE.md` 4. Localização com `flutter_localizations` e `intl` (`l10n.yaml`,
    `lib/l10n/app_en.arb`, `lib/l10n/app_pt.arb`). `lib/app.dart` com `NavigationBar` de duas
    abas, "Camera"/"Câmera" e "Watch"/"Assistir", cada uma com um texto de placeholder vindo do
    ARB. Pastas `lib/core/`, `lib/camera/`, `lib/viewer/`. `test/architecture_test.dart` com as
    regras do `CODESTYLE.md` 4.1. Remover o contador de exemplo do template.
  - Aceite: widget test `App_WhenTappingWatchTab_ShowsWatchPlaceholder` passa. O teste de
    arquitetura passa. `flutter build apk --debug` termina sem erro. O gate roda a área `app/`.
  > Validação (2026-09-24): só código escrito. Não aberto num aparelho. Gate verde com 5
  > testes (`RecamApp` / `whenTappingWatchTab_showsWatchPlaceholder`, no formato Dart do
  > `CODESTYLE.md` 8.1). O teste de arquitetura falhou como esperado com um import temporário
  > de `viewer/` dentro de `camera/`. `flutter build apk --debug` gerou o APK. Localização
  > gerada em `lib/l10n/generated/` (fora do git, o gate roda `flutter gen-l10n` antes).
  > `lib/core/` fica para o primeiro bullet que colocar código lá. `HomeShell` foi para
  > `lib/home_shell.dart` (um widget público por arquivo).

- [x] **0.5 [junto] HTTPS com certificado autoassinado**
  - Origem: `SPECS.md` 5.3.
  - Escopo: `Infrastructure/Tls/CertificateStore.cs` gera o certificado em
    `{RECAM_DATA_DIR}/tls/server.pfx` no primeiro start (RSA 2048, 10 anos) e o reaproveita nos
    seguintes. Expõe o fingerprint SHA-256 do DER em hexadecimal minúsculo. O Kestrel escuta
    HTTPS em 8443 com esse certificado. `RECAM_DATA_DIR` tem padrão `/data`. O argumento
    `healthcheck` do binário faz `GET https://localhost:8443/health` e sai com 0 ou 1.
  - Aceite: testes `GetOrCreate_OnFirstRun_CreatesPfx`, `GetOrCreate_OnSecondRun_ReusesSameFingerprint`.
    Rodando local, `curl -k https://localhost:8443/health` responde `ok`.
  > Validação (2026-09-24): código escrito e servidor rodado localmente. Gate verde com 9 testes.
  > `dotnet run` criou `.data/tls/server.pfx` e `curl -k https://localhost:8443/health`
  > respondeu `ok`. `Recam.Server healthcheck` saiu com 0 com o servidor no ar e 1 com ele
  > parado. O healthcheck confere o fingerprint do certificado salvo em vez de aceitar qualquer
  > certificado. Testes usam `RecamApiFactory` com pasta de dados temporária.

- [x] **0.6 Docker e compose**
  - Origem: `SPECS.md` 7.
  - Escopo: `server/Dockerfile` (multi-stage, não-root, `EXPOSE 8443`, `HEALTHCHECK` com o
    argumento `healthcheck`). `deploy/mediamtx.yml` só com WebRTC ligado (`webrtcLocalUDPAddress
    :8189`, HTTP em 8889), RTSP, RTMP, HLS, SRT, API e métricas desligados, autenticação interna
    liberando publish e read só para loopback e redes privadas. Confirmar os nomes das chaves na
    documentação da versão fixada. `deploy/compose.yaml` (host network, MediaMTX com HTTP em
    `127.0.0.1:8889`). `deploy/compose.bridge.yaml` (portas `8443/tcp` e `8189/udp`, `RECAM_HOST`
    obrigatória, repassada ao MediaMTX via `MTX_WEBRTCADDITIONALHOSTS`). `deploy/.env.example`
    com `RECAM_HOST=` e `RECAM_PUBLIC_URLS=`. Volume `./data:/data`. Imagem
    `bluenviron/mediamtx` com a tag estável mais recente, exata.
  - Aceite: nesta máquina (Windows), `docker compose -f deploy/compose.bridge.yaml up -d
    --build` sobe os dois containers. `docker compose ps` mostra o servidor `healthy`.
    `curl -k https://localhost:8443/health` responde `ok`.
  > Validação (2026-09-24): containers rodados nesta máquina. `compose.bridge.yaml` com
  > `RECAM_HOST=192.168.100.15` subiu os dois serviços, o servidor ficou `healthy` e
  > `curl -k https://localhost:8443/health` respondeu `ok`. MediaMTX 1.21.1 carregou a
  > configuração e só abriu WebRTC (8889 TCP interno, 8189 UDP). `compose.yaml` (host) foi
  > validado com `docker compose config`, mas não rodado: Docker Desktop não tem rede host real.
  > Mudança em relação ao escopo: volume nomeado `recam-data` no lugar de `./data` (revisão no
  > `SPECS.md` 12). Paths do MediaMTX restritos a `cam-<32 hex>`.

## Fase 1: caminho principal, ponta a ponta, na rede local

- [x] **1.1 [junto] Servidor: setup do dono**
  - Origem: caminho principal, passo 2.
  - Escopo: EF Core com SQLite (`Microsoft.EntityFrameworkCore.Sqlite`, ferramenta
    `dotnet-ef` como tool local em `server/.config/dotnet-tools.json`). `Domain/Device.cs`,
    `Domain/PairingToken.cs`, `Domain/DeviceRole.cs` conforme `SPECS.md` 3.
    `Infrastructure/Persistence/RecamDbContext.cs`, migração inicial, `Database.Migrate()` no
    startup. `Infrastructure/Network/PublicUrlResolver.cs` conforme `SPECS.md` 5.1.
    `Features/Setup/`: serviço hospedado que, sem dono, cria um token do dono (10 min), renova
    ao expirar e imprime no log a URI `recam://pair?...` e o QR em ASCII (QRCoder). `GET /setup`
    com a regra de acesso de `SPECS.md` 5.5 e 6, mostrando o QR em SVG.
  - Fora: consumir o token (1.2).
  - Aceite: testes `Setup_WithoutOwner_CreatesOwnerToken`, `Setup_WhenTokenExpires_CreatesNewToken`
    (com `FakeTimeProvider`), `SetupPage_FromForwardedRequest_Returns403`,
    `SetupPage_WithOwner_ShowsAlreadyConfigured`, `Resolve_WithEnvVar_UsesEnvVarUrls`. No
    container, `docker compose logs server` mostra o QR.
  > Validação (2026-09-24): código escrito e container rodado. Gate verde com 22 testes. Com
  > `compose.bridge.yaml`, `docker compose logs server` mostra o QR em ASCII e a URL
  > `https://192.168.100.15:8443`. `curl -k https://localhost:8443/setup` devolveu a página com o
  > QR em SVG. Não lido por um celular ainda (isso é o 1.4). Decisões no caminho: o token vive
  > só em memória e no banco fica o hash; ao renovar, os tokens de dono não usados são apagados;
  > `OwnerSetup` usa `IDbContextFactory` por ser singleton; `DateTimeOffset` gravado como binário
  > porque o SQLite não compara `DateTimeOffset`; o log do QR é a única exceção à regra de token
  > em log. O teste `SetupPage_WithOwner_ShowsAlreadyConfigured` foi para o 1.2, porque criar um
  > dono depende de `Device.Pair`. O manifesto do `dotnet-ef` ficou em `server/dotnet-tools.json`
  > (local padrão do SDK 10).

- [x] **1.2 [junto] Servidor: pareamento e autenticação**
  - Origem: caminho principal, passo 2.
  - Escopo: `POST /api/pair` e `GET /api/me` conforme `SPECS.md` 5.5. Introduz os padrões do
    `SPECS.md` 2.1: `Domain/Result.cs` e `Domain/Error.cs` (escritos no projeto),
    `PairingErrors`, `PairingToken.Consume(now)` e `Device.Pair(...)` como métodos da entidade,
    `Infrastructure/Http/ResultExtensions.ToHttpResult()` e o middleware de exceção não tratada.
    `Infrastructure/Auth/DeviceAuthenticationHandler.cs` e as políticas `OwnerOnly`,
    `ViewerOrOwner`, `CameraOnly`. Rate limiter nativo do ASP.NET Core em `/api/pair` (5 por
    minuto por IP).
  - Aceite: testes `SetupPage_WithOwner_ShowsAlreadyConfigured` (vindo do 1.1),
    `Pair_WithOwnerToken_CreatesOwner`, `Pair_WithUsedToken_Returns401`,
    `Pair_WithExpiredToken_Returns401`, `Pair_WithInvalidName_ReturnsAllValidationErrors`,
    `Pair_AboveRateLimit_Returns429`, `Me_WithRevokedDevice_Returns401`,
    `Me_WithValidCredential_ReturnsDevice`. Testes de unidade da entidade sem banco nem HTTP:
    `Consume_WhenExpired_ReturnsTokenExpired`, `Consume_WhenUsed_ReturnsTokenAlreadyUsed`,
    `Consume_WhenValid_MarksUsed`.
  > Validação (2026-09-24): código escrito e container rodado. Gate verde com 39 testes. No
  > container, via HTTPS: token desconhecido → 401 com `pairing.invalid_token`; campos vazios →
  > 400 com os dois campos no mesmo `ValidationProblem`; `/api/me` sem credencial → 401. Não
  > pareado por um celular ainda (1.4). Decisões no caminho: o tipo de erro virou `DomainError`
  > (o analisador CA1716 proíbe `Error`, palavra reservada no VB); token desconhecido, expirado e
  > usado respondem igual, e o motivo real vai só para o log; `UsedAt` é token de concorrência;
  > o handler de exceção é o nativo (`UseExceptionHandler` + `AddProblemDetails`); `serverName`
  > é a constante `"Recam"`. Testes extras: `Pair_WithUnknownToken_Returns401`,
  > `Me_WithWrongSecret_Returns401`, `Me_WithoutCredential_Returns401`, `DevicePairTests`.

- [x] **1.3 App: pareamento do dono na aba Assistir**
  - Origem: caminho principal, passo 2.
  - Escopo: dependências `mobile_scanner`, `flutter_secure_storage`, `http`.
    `core/pairing/qr_payload.dart` (parse conforme `SPECS.md` 5.2).
    `core/network/pinned_http_overrides.dart` (conforme 5.3), instalado no `main.dart`.
    `core/storage/credential_store.dart` (credencial, URL escolhida e fingerprint, separados por
    aba). `core/network/api_client.dart` (`health`, `pair`, `me`). `viewer/`: tela "não
    pareado" com botão de ler QR, e tela "pareado" mostrando o nome do servidor e o papel.
  - Aceite: testes `QrPayload.parse` (válido, sem token, sem URL, fingerprint inválido, vários
    erros juntos), `PinnedHttpOverrides` (aceita fingerprint igual, recusa diferente),
    `ViewerPairingController` (pareia com fake de `ApiClient` e salva credencial).
  > Validação (2026-09-24): só código escrito. Não aberto num aparelho (isso é o 1.4). Gate verde
  > com testes de `QrPayload.parse` (9), `PinnedHttpOverrides` (5), `PairingService` (7),
  > `ViewerPairingController` (6), `PairedSession` (2) e o widget test do shell. Decisões no
  > caminho: `crypto` entrou como quarta dependência, porque o pinning precisa de SHA-256 e o
  > `dart:io` não tem; `PairingService` (previsto no `SPECS.md` 2.2) concentra pin, escolha da
  > primeira URL que responde `/health` e `POST /api/pair`, e será reusado pela aba Câmera; a aba
  > Assistir recusa token de câmera (`wrongRole`) sem salvar nada; ao abrir, o app mostra o
  > pareamento salvo e chama `GET /api/me` em seguida, e um 401 apaga a credencial; o celular do
  > dono se chama "Owner phone"/"Celular do dono" (ARB), sem campo de nome; o leitor de QR
  > (`mobile_scanner`) mora em `core/scanner/`, por causa da regra de plugin só em `core/`. O
  > `AndroidManifest.xml` ganhou `INTERNET` (só existia no manifesto de debug) e `CAMERA`.

- [x] **1.4 [aparelho] Validar pareamento do dono num celular real**
  - Origem: provar TLS, pinning e rede antes de construir em cima.
  - Escopo: servidor rodando com `compose.bridge.yaml` no PC. APK debug instalado no celular
    novo. Ler o QR de `https://<ip-do-pc>:8443/setup`.
  - Aceite: o app mostra "pareado" e `GET /api/me` responde com papel `Owner`. Anotar o modelo
    do celular e o que foi observado.
  > Validação (2026-09-24): caminho percorrido no aparelho. Samsung Galaxy A10 (SM-A105M,
  > Android 11), APK debug, servidor com `compose.bridge.yaml` no PC (`RECAM_HOST=192.168.100.15`)
  > e regras de firewall para 8443/tcp e 8189/udp. O QR de `/setup` foi lido pelo app e a aba
  > Assistir mostrou "Pareado com Recam" e o papel dono, com TLS e pinning funcionando pela rede
  > local. O app não foi fechado e reaberto, e a resposta de `GET /api/me` não foi lida direto (o
  > papel exibido vem do pareamento). A regra de firewall foi criada sem restringir a origem.

- [x] **1.5 Adicionar câmera: QR gerado pelo dono**
  - Origem: caminho principal, passo 3.
  - Escopo: servidor: `POST /api/pairing-tokens` (`OwnerOnly`, token de câmera de 10 min, devolve
    `qrUri`). App: dependência `qr_flutter`. Botão "Adicionar câmera" na aba Assistir, que abre
    a tela com o QR e a contagem regressiva, e gera um novo ao expirar.
  - Aceite: testes `CreatePairingToken_AsOwner_ReturnsQrUri`,
    `CreatePairingToken_AsCamera_Returns403`, e o teste do controller da tela de QR com
    `ApiClient` fake.
  > Validação (2026-09-24): só código escrito. O QR da câmera não foi gerado num aparelho ainda.
  > Gate verde com 49 testes do servidor e 41 do app. Servidor: `Device.IssuePairingToken` guarda a
  > regra (só dono ativo emite, nunca token de dono, `CreatedByDeviceId` preenchido), o endpoint
  > só orquestra, e o teste `CreatePairingToken_AsOwner_ReturnsQrUri` pareia uma câmera com o token
  > do QR devolvido. App: `AddCameraController` (contagem regressiva e renovação ao expirar) e
  > `AddCameraScreen` com `qr_flutter`; o botão "Adicionar câmera" só aparece para o dono.
  > Decisões no caminho: `role` chega como texto e o validador aceita só `camera` por enquanto
  > (o 3.2, hoje 4.2, libera `viewer`); o app conta o tempo restante a partir da hora do servidor, lida do
  > header `Date` da resposta, para o relógio do celular não encurtar nem esticar o QR; o QR é
  > renovado ao chegar em zero, sem margem.

- [x] **1.6 App: pareamento da câmera na aba Câmera**
  - Origem: caminho principal, passo 4.
  - Escopo: `camera/`: tela "não pareado" (ler QR, campo de nome, 1..40 caracteres, padrão
    "Camera"/"Câmera" vindo do ARB) e tela "pareado" com o botão "Iniciar modo câmera", ainda
    sem ação de vídeo. Reusa `QrPayload`, `ApiClient` e `CredentialStore` de `core/`.
  - Aceite: teste `CameraPairingController` (pareia com fake e salva a credencial da aba
    Câmera, separada da aba Assistir).
  > Validação (2026-09-24): só código escrito. Não percorrido no aparelho. Gate verde com 49
  > testes do servidor e 54 do app. `CameraPairingController` (pareia com fake, salva na aba
  > Câmera sem tocar na aba Assistir, recusa token de dono) e `DeviceNameValidator` (1..40, sem
  > caractere de controle, todos os erros juntos). Decisões no caminho: o botão "Iniciar modo
  > câmera" aparece desabilitado, porque o escopo diz "ainda sem ação de vídeo" e um botão ligado
  > sem efeito enganaria (o 1.8 liga); o nome é validado antes de abrir o leitor de QR, e o texto
  > digitado sobrevive a uma falha de pareamento; os textos de erro de pareamento e o nome do
  > papel foram para `core/pairing/pairing_labels.dart`, compartilhados pelas duas abas; a aba
  > Câmera reusa `PairingService`, e o servidor recusa nome inválido de novo (o app não confia
  > só na validação local).

- [x] **1.6.1 Código de pareamento em texto (colar e copiar)**
  - Origem: pedido do autor (2026-09-25). O teste passa a usar um celular físico só, como câmera,
    e o emulador Android no PC como visualizador. O emulador não lê QR direito.
  - Escopo: servidor: o log do dono imprime também a URI `recam://pair?...` em texto, e a página
    `/setup` mostra a URI num campo de texto selecionável abaixo do QR. App: a tela do leitor de
    QR (`core/scanner/qr_scanner_screen.dart`) ganha a ação "Colar código", que abre um diálogo
    com campo de texto e devolve o texto como se fosse lido do QR. A tela "Adicionar câmera" ganha
    o botão "Copiar código", que põe a URI na área de transferência. Textos em `en` e `pt`.
  - Fora: mudar o formato da URI ou o fluxo dos controllers.
  - Aceite: teste de servidor `SetupPage_FromLocalNetwork_ShowsPairingUriAsText`. Widget tests
    do diálogo (devolve o texto aparado; cancelar devolve nada) e do botão de copiar.
  > Validação (2026-09-25): só código escrito. Gate verde (50 testes no servidor, 58 no app).
  > A ação "Colar código" fica na tela do leitor de QR, então vale para as duas abas sem mudar os
  > controllers. O log do dono imprime a URI depois do QR em ASCII. Testes:
  > `SetupPage_FromLocalNetwork_ShowsPairingUriAsText`, `showPairingCodeDialog` (texto aparado,
  > cancelar, texto em branco) e `whenTappingCopyCode_putsPairingUriInClipboard`.

- [x] **1.6.3 Pareamento na aba errada não gasta o token**
  - Origem: teste do autor em 2026-09-25. O código do dono foi colado na aba Câmera. O servidor
    consumiu o token e criou o dono; o app recusou o papel depois. Ficou um dono que nenhum
    aparelho tem, e o servidor precisou ser zerado.
  - Escopo: o app manda `expectedRoles` no `POST /api/pair` (aba Assistir: `owner`, `viewer`;
    aba Câmera: `camera`). `PairingToken.Consume` recebe os papéis aceitos e, depois de checar
    uso e validade, recusa com `pairing.wrong_role` (409) sem marcar o token como usado. O
    endpoint repassa esse erro (os outros continuam virando `pairing.invalid_token`). Atualizar
    `SPECS.md` 5.5.
  - Aceite: `Pair_WithWrongExpectedRole_Returns409AndKeepsToken` (o mesmo token funciona depois
    com o papel certo), `Consume_WithWrongRole_ReturnsWrongRoleAndKeepsToken`, e o app mapeando
    409 para a mensagem de papel errado.
  > Validação (2026-09-25): só código escrito. Gate verde (52 testes no servidor, 60 no app).
  > O papel é checado depois de uso e validade, então um token vencido na aba errada continua
  > respondendo "inválido". Testes no app: `withExpectedRoles_sendsThemInTheBody` e
  > `withTokenForOtherTab_returnsWrongRoleAndSavesNothing`.

- [x] **1.6.4 Link `recam://pair` abre o app e pareia na aba certa**
  - Origem: pedido do autor (2026-09-25): testar sem copiar e colar código. Também serve para
    quem lê o QR com a câmera nativa do Android.
  - Escopo: o QR ganha o parâmetro `r` com o papel que o token concede (`owner`, `viewer`,
    `camera`); `SPECS.md` 5.2 atualizado, e `r` é opcional para o app. Android: intent filter
    para `recam://pair`. App: dependência `app_links`; ao receber o link (app fechado ou
    aberto), troca para a aba do papel (`camera` → Câmera, senão Assistir) e pareia sozinho se a
    aba ainda não estiver pareada. Na aba Câmera o nome é o padrão do ARB.
  - Aceite: teste do roteador de link (papel → aba, aba já pareada é ignorada), teste do parse
    de `r`, teste de servidor do `r` na URI. `adb shell am start -d "recam://pair?..."` abre o
    app no emulador e pareia.
  > Validação (2026-09-25): só código escrito; o `adb` no emulador é conferido no 1.6.5. Gate
  > verde (52 no servidor, 68 no app). O Flutter tem roteamento de deep link próprio; ele foi
  > desligado no manifest (`flutter_deeplinking_enabled=false`) para o `app_links` receber o link.
  > A `HomeShell` espera as duas abas carregarem antes de parear, para o link que abre o app
  > frio não ser ignorado.

- [x] **1.6.5 Script de desenvolvimento em um comando**
  - Origem: pedido do autor (2026-09-25): não ficar rodando comando e copiando código.
  - Escopo: `scripts/dev.ps1`. Sobe o servidor (`-Reset` zera os dados antes), espera o código
    do dono no log, liga o emulador se nenhum estiver rodando, compila e instala o APK debug no
    emulador e em todo celular conectado por USB, e manda o link do dono para o emulador com
    `adb`. No fim, imprime em PT-BR o que fazer no celular câmera. O script é só para o autor
    testar: fica no `.gitignore`, não entra no `README.md` e é apagado antes do lançamento.
  - Aceite: `./scripts/dev.ps1 -Reset` numa máquina com o emulador criado termina com o
    emulador pareado como dono, sem nenhum outro comando.
  > Validação (2026-09-25): caminho percorrido. `./scripts/dev.ps1 -Reset` zerou o servidor,
  > instalou o APK no emulador e no A10, mandou o link pelo `adb`, e o emulador mostrou "Paired
  > with Recam" como dono (log: `paired as Owner`). Isso também valida o 1.6.4 no emulador.
  > Com `-Reset` o script roda `pm clear` nos aparelhos, porque o servidor zerado invalida as
  > credenciais salvas.

- [x] **1.6.6 Textos sem "celular velho" e "celular novo"**
  - Origem: pedido do autor (2026-09-25). O app não sabe a idade do aparelho; alguém pode usar
    dois celulares novos.
  - Escopo: trocar por "celular câmera" e "celular que assiste" nos ARB, no `README.md`, no
    caminho principal do `AGENTS.md`, no `SPECS.md`, na descrição do `pubspec.yaml` e nos nomes
    usados nos testes. A frase do produto passa a falar em celulares parados, não antigos.
  - Aceite: `git grep -i -E "old phone|celular velho|celular novo"` não encontra nada.
  > Validação (2026-09-25): só código escrito. Busca vazia, gate verde.

- [x] **1.6.7 Nome exibido "ReCam"**
  - Origem: pedido do autor (2026-09-25). O repositório no GitHub passou a se chamar ReCam.
  - Escopo: "ReCam" em todo texto lido por pessoas: nome e título do app, ARB, página `/setup`,
    log do dono, `serverName` da API, `README.md` e títulos dos documentos. Identificadores
    técnicos continuam como estão: `recam://`, `io.recam.app`, `recam-server`, `Recam.Server`.
  - Aceite: nenhum texto exibido com "Recam"; gate verde.
  > Validação (2026-09-25): só código escrito. Gate verde. Remote do git atualizado para
  > `github.com/jpemendonca/ReCam`.

- [x] **1.6.2 [aparelho] Validar pareamento com emulador como dono e celular como câmera**
  - Origem: nova forma de teste (1.6.1).
  - Escopo: `./scripts/dev.ps1 -Reset` (1.6.5) deixa o emulador pareado como dono. O emulador abre
    "Adicionar câmera". O A10 lê esse QR na tela do PC pela aba Câmera.
  - Aceite: o emulador mostra "pareado" como dono e o A10 mostra "pareado" como câmera.
  > Bloqueado (2026-09-25): aguardando o autor com o A10 desbloqueado. O link de câmera no A10
  > já foi conferido pelo agente (log `paired as Camera`).
  > Validação (2026-09-25): caminho percorrido pelo autor, com o Redmi 6A no lugar do emulador
  > (`./scripts/dev.ps1 -Reset -Viewer`). O 6A virou dono pelo link, gerou o QR em "Adicionar
  > câmera", e o A10 leu esse QR na aba Câmera e pareou.

- [x] **1.7 [junto] Servidor: hub, presença e telemetria**
  - Origem: caminho principal, passo 5.
  - Escopo: `Features/Realtime/DeviceHub.cs` em `/hubs/devices`, autenticado por
    `access_token`. Presença em memória (online e offline, `LastSeenAt`). `ReportTelemetry`
    grava no `Device` e emite `CameraStatusChanged`. `Features/Devices/`: `GET /api/cameras`.
  - Aceite: testes com cliente SignalR real contra a `WebApplicationFactory`:
    `ReportTelemetry_FromCamera_NotifiesViewers`, `ReportTelemetry_FromViewer_IsRejected`,
    `Cameras_AfterCameraDisconnects_ShowsOffline`.
  > Validação (2026-09-25): só código escrito; nenhum celular conectou ao hub ainda (isso vem no
  > 1.8). Gate verde, 63 testes no servidor, rodados três vezes sem falha. Decisões: presença em
  > `Infrastructure/Presence` e `CameraStatus` no domínio, para `Devices` e `Realtime` não
  > dependerem uma da outra; erro esperado do hub volta como `HubResult`; `access_token` na query
  > só vale em `/hubs`; migração `CameraTelemetry`. Testes extras: `ReportTelemetry_WithInvalidLevel_ReturnsFailure`,
  > `Cameras_AsCamera_Returns403`, `Negotiate_WithQueryToken_IsAccepted`,
  > `Me_WithQueryToken_Returns401`, `DevicePresenceTests`, `DeviceTelemetryTests`. O helper de
  > teste agora espera o setup do dono antes de criar tokens, porque o worker apagava o token de
  > dono do teste numa corrida.

- [x] **1.8 App: modo câmera ocioso**
  - Origem: caminho principal, passo 4.
  - Escopo: dependências `signalr_netcore`, `battery_plus`, `wakelock_plus`,
    `screen_brightness`, `flutter_foreground_task`. `core/network/hub_connection_factory.dart`.
    Interfaces `BatteryReader` e `ScreenController` em `core/device/`. `camera/`: "Iniciar modo
    câmera" liga o foreground service (tipo `camera`), o wakelock, a tela preta com brilho
    mínimo e o overlay de 10 s ao tocar. Conecta ao hub e manda telemetria ao conectar, a cada
    60 s e quando o nível muda. Reconexão com o backoff de `SPECS.md` 2.3. Permissões no
    `AndroidManifest.xml`.
  - Aceite: testes `ReconnectBackoff` (sequência 1, 2, 4, 8, 16, 30, 30) e
    `CameraModeController` (manda telemetria ao conectar e quando o nível muda, com fakes).
  > Validação (2026-09-25): código escrito, gate verde (79 testes no app), APK compila. Entrou
  > também `permission_handler` (fixado em `^12.0.1`, porque a 13 exige compilar contra a API 37):
  > o Android 14+ recusa foreground service de câmera sem a permissão de câmera concedida antes.
  > A reconexão fica em `core/network/hub_session.dart` e será reusada pela aba Assistir. O
  > overlay começa visível e some em 10 s.
  > Validação no aparelho (2026-09-25): caminho percorrido no emulador como câmera (dono pareado
  > pela API com `curl`). O link de câmera pareou o emulador, "Start camera mode" abriu a tela
  > preta, as permissões de câmera e notificação foram aceitas, e `GET /api/cameras` mostrou a
  > câmera `online: true` com `batteryLevel: 100`. Isso prova hub, TLS com pinning e foreground
  > service rodando. O A10 também pareou como câmera pelo link, mas estava bloqueado com senha e
  > o modo câmera não foi aberto nele.

- [x] **1.9 App: lista de câmeras na aba Assistir**
  - Origem: caminho principal, passo 5.
  - Escopo: `viewer/`: lista via `GET /api/cameras`, atualizada por `CameraStatusChanged`.
    Cada item mostra nome, online ou offline e bateria com o ícone de carregando. Lista vazia
    com o botão "Adicionar câmera".
  - Aceite: teste `CameraListController` (aplica `CameraStatusChanged` sobre a lista inicial,
    com fakes).
  > Validação (2026-09-25): só código escrito, gate verde (83 testes no app). A lista recarrega
  > pela API a cada (re)conexão do hub, porque mensagens enviadas enquanto offline se perdem. O
  > botão "Adicionar câmera" foi para o topo da lista (e aparece no meio quando está vazia).
  > `CameraInfo` é o mesmo formato para a API e para o hub.

- [x] **1.10 [junto] Servidor: proxy WHIP/WHEP e transmissão sob demanda**
  - Origem: caminho principal, passo 6.
  - Escopo: `Features/Media/`: proxy conforme `SPECS.md` 5.5, com `IHttpClientFactory` e
    `RECAM_MEDIAMTX_URL` (padrão `http://127.0.0.1:8889`). `Features/Realtime/`: `WatchCamera`,
    `UnwatchCamera`, leases por conexão, `StartPublishing` e `StopPublishing` com carência de
    30 s, reenvio de `StartPublishing` quando a câmera reconecta com lease aberto,
    `ReportPublishing`. Dependência `Testcontainers` no projeto de testes.
  - Aceite: com o MediaMTX real via Testcontainers, `Whip_ToOtherCameraPath_Returns403`,
    `Whep_WithoutCredential_Returns401`, `Whep_ForCameraNotPublishing_ReturnsUpstream404`
    (prova que o proxy chega no MediaMTX). No hub, com `FakeTimeProvider`:
    `WatchCamera_FirstLease_SendsStartPublishing`, `UnwatchCamera_LastLease_SendsStopAfterGrace`,
    `WatchCamera_DuringGrace_CancelsStop`.
  > Validação (2026-09-25): código escrito; testes contra MediaMTX 1.21.1 real (Testcontainers)
  > com a mesma configuração de `deploy/`. Gate verde, 74 testes no servidor, 4 rodadas sem falha.
  > O proxy chega ao MediaMTX: WHEP de câmera parada volta 404 do MediaMTX e WHIP com SDP inválido
  > volta 400. Extras: `Whip_WithBogusOffer_ReachesMediaMtx`, `MediaMtxImage_MatchesComposeFiles`,
  > `CameraReconnect_WithOpenLease_SendsStartPublishing`, `WatchCamera_UnknownCamera_ReturnsNotFound`,
  > `ReportPublishing_True_ShowsPublishingInList`. Achados: o SignalR usa o mesmo `TimeProvider`
  > do DI para timeouts (os testes aumentam esses timeouts); e `StartPublishing` pode chegar duas
  > vezes quando o lease coincide com a conexão da câmera (a câmera deve ignorar o repetido).
  > O compose de Windows passou a definir `RECAM_MEDIAMTX_URL=http://mediamtx:8889`.

- [x] **1.11 [opus] App: câmera transmite por WHIP**
  - Origem: caminho principal, passo 6.
  - Escopo: dependência `flutter_webrtc`. Interface `WebRtcPublisher` em `core/media/` e a
    implementação com WHIP (POST do offer, PATCH de ICE, DELETE ao parar). Captura conforme
    `SPECS.md` 2.3: 1280x720, 15 fps, H.264 preferido, sem áudio, 700 kbps. `StartPublishing`
    abre, `StopPublishing` fecha e libera a câmera. `ReportPublishing` nos dois casos.
  - Aceite: teste `CameraModeController` (com `WebRtcPublisher` fake, `StartPublishing` inicia e
    reporta, `StopPublishing` para e reporta).
  > Validação (2026-09-25): caminho percorrido no emulador como câmera, com um cliente SignalR
  > descartável fazendo o papel de visualizador (lease pelo hub). O servidor mandou
  > `StartPublishing`, o app publicou por WHIP através do proxy, e o MediaMTX registrou "stream is
  > available and online, 1 track (VP8)". O visualizador recebeu `publishing: true`. Ao soltar o
  > lease, a sessão fechou 28 s depois (carência de 30 s) e o estado voltou a `publishing: false`.
  > Achado: sem `OfferToReceiveAudio/Video: false`, o plugin no Android punha áudio e um segundo
  > vídeo na oferta, e o MediaMTX recusava. O emulador não tem encoder H.264 e publicou em VP8
  > (revisão no `SPECS.md` 12). Gate verde, 88 testes no app. H.264 no A10 fica para o 1.14.

- [x] **1.12 [opus] App: vídeo ao vivo na aba Assistir**
  - Origem: caminho principal, passo 6.
  - Escopo: interface `WebRtcViewer` em `core/media/` e a implementação com WHEP. Tela de vídeo
    ao vivo com `RTCVideoView`. Chama `WatchCamera` ao abrir e `UnwatchCamera` ao sair. Tenta o
    WHEP a cada 1 s, até 10 vezes, enquanto a câmera não reporta que está publicando. Mostra
    "conectando" e erro com botão de tentar de novo.
  - Aceite: teste `LiveViewController` (com fakes: abre lease, espera publicação, conecta e
    fecha o lease no dispose).
  > Validação (2026-09-25): só código escrito, gate verde (92 testes no app), APK compila.
  > Tocar numa câmera online abre a tela ao vivo, que pega o lease, tenta o WHEP a cada 1 s e
  > reconecta se o stream cair. Foram 20 tentativas em vez de 10, porque um aparelho fraco pode
  > levar mais de 10 s para abrir a câmera. O `WhepViewer` entrega o próprio widget de vídeo, e a
  > tela não importa o plugin. O teste com dois emuladores achou a queda silenciosa do hub; a
  > correção é o 1.12.1, e a imagem ao vivo será conferida depois dele.

- [x] **1.12.1 Conexão com o hub que percebe queda silenciosa**
  - Origem: teste com dois emuladores em 2026-09-25. Depois de um restart do servidor, o
    visualizador continuou mostrando "connected" por minutos sem reconectar: o cliente SignalR do
    Dart não disparou `onclose`. A lista ficou com a câmera "Offline" enquanto ela estava online.
  - Escopo: hub ganha `Heartbeat()` (qualquer aparelho, devolve `HubResult` ok). `HubSession`
    chama o batimento a cada 20 s enquanto conectada; sem resposta em 10 s, derruba a conexão e
    reconecta com o backoff. O `Completer` de fechamento passa a existir antes do `connect`, para
    um fechamento imediato não se perder. `CameraModeScreen` e `LiveViewScreen` passam a criar o
    controller no `initState`, não no `builder` da rota (o Flutter pode chamar o builder de novo).
  - Aceite: `Heartbeat_FromAnyDevice_ReturnsOk` no servidor; no app,
    `whenHeartbeatFails_reconnects` e `whenConnectionClosesRightAway_reconnects`.
  > Validação (2026-09-25): código escrito, gate verde (75 no servidor, 95 no app). Com dois
  > emuladores ligados, o log do app mostrou o servidor fechando a conexão aos 15 s sem o cliente
  > saber, e o batimento detectando a queda (`alive=false`) e reconectando. A causa dos 15 s
  > ainda não está provada (suspeita: NAT dos emuladores com o Docker Desktop); o teste com o A10
  > no 1.14 decide. O hub passou a registrar em log a conexão e a desconexão de cada aparelho.

- [x] **1.12.2 Modo câmera sem tela escura**
  - Origem: pedido do autor ao testar no A10 (2026-09-25): a tela preta com brilho mínimo não deixa
    ver nada.
  - Escopo: o modo câmera deixa o brilho e as barras do sistema como estão, e mostra o status o
    tempo todo (conectado, bateria, transmitindo, botão de parar). A tela continua sempre ligada
    (wakelock). Some o overlay que aparecia e sumia. `SPECS.md` 2.3 revisado.
  - Aceite: `ScreenController` só liga e desliga o wakelock; widget test da tela mostrando o status
    sem toque.
  > Validação (2026-09-25): só código escrito, gate verde (96 testes no app). A dependência
  > `screen_brightness` saiu, porque não é mais usada.

- [x] **1.13 Lanterna**
  - Origem: caminho principal, passo 7.
  - Escopo: hub: `SetTorch` e `ReportTorch` conforme `SPECS.md` 5.6. App câmera: aplica com
    `Helper.setTorch` na trilha de vídeo ativa e reporta. App visualizador: botão de lanterna na
    tela ao vivo, refletindo `TorchChanged`.
  - Aceite: testes `SetTorch_WhenCameraNotPublishing_ReturnsError`,
    `SetTorch_WhenPublishing_ForwardsToCamera` e o controller da câmera aplicando a lanterna
    via fake.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (83 testes
  > no servidor, com MediaMTX real via Testcontainers; 105 no app). A regra fica na entidade:
  > `Device.AcceptTorchCommand(publishing)` recusa quem não é câmera (`media.camera_not_found`) e
  > câmera parada (`media.camera_not_publishing`, código no padrão dos outros erros, revisão no
  > `SPECS.md` 12); o hub só carrega, pergunta à presença se está publicando e repassa. O
  > `flutter_webrtc` 1.6 não tem `Helper.setTorch`: a câmera usa `MediaStreamTrack.hasTorch` e
  > `setTorch` na trilha publicada. A câmera sempre responde com `ReportTorch` do estado real (sem
  > lanterna ou parada, reporta desligada) e reporta desligada ao parar de transmitir. O botão no
  > ao vivo só fica ativo com o vídeo tocando e mostra o que a câmera reportou, não o que foi
  > pedido. Limitação: o estado da lanterna não fica guardado no servidor; quem abre o ao vivo
  > com a lanterna já ligada vê o botão desligado até a próxima mudança. Extras:
  > `SetTorch_ForUnknownCamera_ReturnsNotFound`, `SetTorch_FromCamera_IsRejected`,
  > `ReportTorch_FromCamera_NotifiesViewers`, `DeviceTorchTests`, testes da lanterna no
  > `LiveViewController`.

- [x] **1.12.3 Pareamento que não vale mais volta para a tela de pareamento**
  - Origem: teste no A10 em 2026-09-25. Depois de zerar o servidor, o A10 ficou em "Conectando ao
    servidor…" para sempre, mesmo fechando e abrindo o app. O servidor novo tem outro certificado;
    o pinning recusa a conexão antes de qualquer requisição, e o app trata isso como "servidor fora
    do ar", não como "pareamento inválido".
  - Escopo: distinguir "certificado não bate com o fingerprint salvo" de "servidor inalcançável".
    No primeiro caso, e também quando o hub ou a API respondem 401, a aba volta para a tela de
    pareamento com uma mensagem clara ("Este servidor mudou. Pareie de novo."). O modo câmera sai
    sozinho quando a credencial é recusada.
  - Aceite: testes do `PairingService.verify` (fingerprint diferente → pareamento inválido) e do
    modo câmera saindo com credencial recusada.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (83 no
  > servidor, 117 no app). O `PinnedHttpOverrides` lembra, por host e porta, se o último
  > certificado apresentado não bateu com o pino; é assim que "servidor mudou" se separa de
  > "servidor fora do ar" (os dois chegam como erro de rede). O `HubClient.connect` devolve
  > `connected`, `unreachable` ou `rejected` (401 no negotiate ou certificado trocado), e a
  > `HubSession` para de tentar quando é `rejected`. Ao abrir o app, `verify` esquece o pareamento
  > com 401 ou certificado trocado. O modo câmera fecha sozinho e a aba Câmera volta ao
  > pareamento; a aba Assistir faz o mesmo quando o hub recusa ou `GET /api/cameras` volta 401, e
  > fecha um ao vivo que esteja aberto. As duas mostram "Este servidor mudou ou não reconhece mais
  > este celular. Pareie de novo." Consequência aceita: alguém no meio da rede com outro
  > certificado também faz o app esquecer o pareamento, mas não ganha acesso a nada.

- [x] **1.12.4 Oferta WHEP pedindo vídeo**
  - Origem: teste do autor em 2026-09-25 (A10 câmera, 6A visualizador): a câmera transmitia, mas o
    visualizador ficava preto. A oferta do visualizador saía com `m=video 0` (vídeo recusado),
    porque usava `OfferToReceiveVideo: false`, a mesma opção da câmera.
  - Escopo: o visualizador passa a usar `OfferToReceiveVideo: true` e `OfferToReceiveAudio: false`.
  - Aceite: vídeo ao vivo aparecendo num celular real.
  > Validação (2026-09-25): caminho percorrido pelo autor. A10 transmitindo em H.264, Redmi 6A
  > assistindo ao vivo. A suspeita anterior (NAT do emulador, repasse de UDP do Docker Desktop)
  > estava errada: o emulador falhava pelo mesmo motivo. Isso valida os passos 1 a 6 do caminho
  > principal com dois celulares.

- [x] **1.12.5 Depois de ler o QR, ir direto para o modo câmera**
  - Origem: feedback do autor no teste de 2026-09-25.
  - Escopo: na aba Câmera, quando o pareamento pelo QR (ou pelo link) dá certo, o app abre o modo
    câmera na hora, sem parar na tela "Pareado".
  - Aceite: teste do fluxo de pareamento abrindo o modo câmera.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (83 no
  > servidor, 120 no app). A aba Câmera observa o próprio controller de pareamento e abre o modo
  > câmera na passagem de "pareando" para "pareado", então vale para o leitor, para o código
  > colado e para o link `recam://pair`. Abrir o app já pareado continua mostrando "Iniciar modo
  > câmera" (não entra sozinho). Testes: `CameraTab` (abre depois do QR, não abre com falha, não
  > abre com pareamento salvo) e o teste do link de câmera no `app_test` agora confere a tela do
  > modo câmera.

- [x] **1.12.6 Botão de reiniciar o app**
  - Origem: pedido do autor no teste de 2026-09-25.
  - Escopo: um botão "Reiniciar" (com confirmação) que apaga os pareamentos das duas abas neste
    aparelho e volta o app ao estado de recém-instalado. No servidor, o aparelho continua registrado
    até ser revogado (3.1, hoje 4.1).
  - Aceite: teste do controller apagando as duas credenciais e voltando as abas para "não pareado".
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (83 no
  > servidor, 123 no app). O app ganhou uma barra de título "ReCam" com um menu; "Reiniciar o app"
  > pede confirmação e chama o `AppReset`, que fica na raiz de `lib/` porque só ela pode conhecer
  > as duas abas. Cada controller de pareamento ganhou `reset()` (apaga a credencial da própria aba
  > e volta a "não pareado" sem mensagem de erro). Depois de reiniciar, o app volta para a aba
  > Câmera. Testes: `AppReset.reset` e o fluxo no `app_test` (confirmar apaga, cancelar mantém).

- [x] **1.12.7 Status mais claro nos dois lados, e atualizar a lista**
  - Origem: pedido do autor no teste de 2026-09-25.
  - Escopo: no celular câmera, mostrar se há alguém assistindo e quantos. Na lista da aba Assistir,
    deixar claro online/offline e transmitindo/parada para cada câmera, e oferecer "Atualizar"
    (puxar a lista para baixo e um botão), que recarrega pela API.
  - Aceite: testes do controller da lista recarregando e da tela da câmera mostrando quantos
    assistem.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (86 no
  > servidor, 129 no app). A câmera não tinha como saber quantos assistem, então o servidor ganhou
  > a mensagem `WatchersChanged(int count)` (revisão no `SPECS.md` 12): o `WatchLeases` avisa a
  > câmera a cada lease aberto ou fechado, e o hub manda a contagem atual quando a câmera conecta.
  > `Unwatch` e `RemoveConnection` viraram `UnwatchAsync` e `RemoveConnectionAsync`, e `HasWatchers`
  > deu lugar a `WatcherCount` (reusado no painel do 1.12.11). A tela da câmera mostra "Conectado",
  > "Enviando vídeo" ou "Conectando", mais "Ninguém está assistindo" / "N pessoas estão assistindo".
  > Na lista, cada câmera mostra "Offline" ou "Online · transmitindo/parada" (ícone vermelho
  > transmitindo); "Atualizar" existe como botão no topo e puxando a lista para baixo. Testes no
  > servidor: `WatchCamera_TwoViewers_SendsWatcherCountToCamera`,
  > `ViewerDisconnect_WithOpenLease_SendsLowerCount`, `CameraConnect_SendsCurrentWatcherCount`.

- [x] **1.12.8 Um só "Ler QR" que decide o papel do celular**
  - Origem: conversa com o autor em 2026-09-25: trocar entre câmera e visualizador está confuso.
  - Escopo: ler qualquer QR do ReCam, em qualquer aba, usa o `r=` para decidir: QR de câmera pareia
    a aba Câmera e abre o modo câmera (1.12.5); QR de visualizador ou de dono pareia a aba Assistir
    e abre a lista. O leitor e o roteamento ficam num lugar só, reusado pelas duas abas e pelo link
    `recam://pair` (1.6.4).
  - Aceite: testes do roteamento pelo papel do QR a partir de cada aba.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (86 no
  > servidor, 136 no app). O leitor de QR saiu das abas e ficou no `HomeShell`, que é o único a
  > abri-lo; as abas só pedem "ler QR". O `PairingRouter` (raiz de `lib/`, porque conhece as duas
  > abas) decide a aba pelo `r=`: câmera vai para a aba Câmera (e o 1.12.5 abre o modo câmera),
  > dono ou visualizador vão para a aba Assistir. O link `recam://pair` passa pelo mesmo caminho.
  > Código que não é do ReCam mostra o erro na aba que leu; link que não é do ReCam é ignorado.
  > Aba já pareada não é pareada de novo (a mesma regra que o link tinha): o app só troca para ela.
  > Código lido na aba Assistir que é de câmera usa o nome padrão "Câmera". Testes: `PairingRouter`
  > (câmera a partir da aba Assistir, visualizador e dono a partir da aba Câmera, código inválido,
  > aba já pareada, link estranho).

- [x] **1.12.9 Qualquer celular que assiste adiciona câmeras e visualizadores**
  - Origem: conversa com o autor em 2026-09-25: não dava para inverter os papéis dos celulares.
    Traz para agora o que era o bullet 3.2 (hoje 4.2).
  - Escopo: `POST /api/pairing-tokens` aceita `role: "camera"` e `role: "viewer"` e passa a valer para
    `Owner` e `Viewer` (a regra fica em `Device.IssuePairingToken`; dono continua não sendo
    concedível). No app, "Adicionar" oferece "Outra câmera" e "Outro celular para assistir", cada um
    com seu QR e "Copiar código".
  - Aceite: `CreatePairingToken_AsViewer_ForCamera_ReturnsQrUri`,
    `CreatePairingToken_ForViewer_PairsAsViewer`, `CreatePairingToken_AsCamera_Returns403`, e o
    teste da tela de adicionar com os dois tipos.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (90 no
  > servidor, 140 no app). No servidor, a regra ficou em `Device.IssuePairingToken`: dono ou
  > visualizador ativo emite; câmera recebe `pairing.issuer_cannot_invite` (novo nome do antigo
  > `pairing.issuer_not_owner`); papel de dono continua `pairing.owner_role_not_grantable`. O
  > validador aceita `camera` e `viewer`, e o endpoint passou de `OwnerOnly` para `ViewerOrOwner`.
  > A política `OwnerOnly` ficou sem uso por enquanto; ela continua registrada porque o 3.1 (hoje 4.1)
  > (revogar) é só do dono. No app, `createCameraPairingToken` virou `createPairingToken(role)`, e a
  > tela "Adicionar" (antes "Adicionar câmera") tem a escolha "Outra câmera" / "Outro celular para
  > assistir", cada uma com seu QR, contagem e "Copiar código". O botão aparece para qualquer
  > celular da aba Assistir, não só para o dono. Extras: testes de domínio do visualizador emitindo
  > e da câmera recusada, `forViewer_sendsViewerRole` e `start_forViewer_asksForAViewerQr`.

- [x] **1.12.10 Primeira abertura pergunta o uso do celular**
  - Origem: conversa com o autor em 2026-09-25.
  - Escopo: com as duas abas sem pareamento, o app abre numa tela "Este celular vai ser: [Câmera]
    [Para assistir]". Cada opção explica em uma frase onde achar o QR e abre o leitor. Depois do
    pareamento, o app segue com as duas abas como hoje. O conceito de "dono" não aparece em
    nenhuma tela.
  - Aceite: widget test da primeira abertura levando ao leitor e, pareado, sumindo.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (90 no
  > servidor, 145 no app). Com as duas abas sem pareamento, o `HomeShell` mostra a
  > `FirstRunScreen` sem a barra de abas: "Câmera" (com o campo de nome, para as câmeras não se
  > chamarem todas "Câmera") e "Para assistir", cada uma com a frase de onde achar o QR. Os dois
  > botões abrem o mesmo leitor do 1.12.8, e o QR lido continua decidindo a aba. As abas ficam
  > montadas por baixo (fora do palco), para a aba Câmera ver o pareamento terminar e abrir o modo
  > câmera. Erro de pareamento aparece na própria primeira abertura. O leitor virou dependência
  > injetada (`PairingCodeReader`, padrão `readPairingCode`), o que deixou o widget test percorrer
  > o fluxo. "Dono" sumiu das telas: saíram "Papel: ..." e as chaves de papel; o nome do aparelho
  > que assiste passou de "Celular do dono" para "Celular que assiste"; os textos de "não pareado"
  > falam em "Adicionar" e no QR do servidor. Depois de "Reiniciar o app", a primeira abertura
  > volta. Testes: primeira abertura (pergunta, "Para assistir" lê e mostra a lista, "Câmera" lê e
  > abre o modo câmera, leitor fechado, código inválido).

- [x] **1.12.11 Painel de acompanhamento no servidor (só leitura)**
  - Origem: conversa com o autor em 2026-09-25: o PC serve para instalar e acompanhar; o resto é
    pelo celular.
  - Escopo: a página `/setup` vira o painel, na mesma regra de acesso (só IP local, sem
    `X-Forwarded-For`). Sem dono: mostra o QR do primeiro celular, como hoje. Com dono: lista os
    aparelhos (nome, câmera ou visualizador, online, transmitindo, quantos assistem, bateria) e se
    atualiza sozinha a cada 5 s. HTML gerado no servidor, sem framework de front-end e sem ações.
  - Aceite: testes da página com e sem dono e da lista refletindo presença e transmissão.
  > Validação (2026-09-25): só código escrito. Não aberto num navegador. Gate verde (93 no
  > servidor, 145 no app). `SetupPage` virou `RenderPending` (QR, como antes, com o texto "escolha
  > Para assistir") e `RenderPanel` (tabela). O endpoint só orquestra: pergunta ao `OwnerSetup` o
  > estado e, com dono, carrega os aparelhos não revogados e monta um `PanelDevice` por aparelho
  > com o `DevicePresence`. Para o painel saber "quantos assistem" sem `Setup` depender de
  > `Realtime`, o `WatchLeases` passou a gravar a contagem no `DevicePresence` (dentro do mesmo
  > lock), e o hub também lê dali. Atualização sem script: `<meta http-equiv="refresh">`, 5 s no
  > painel e 30 s na página do QR (para ela virar o painel depois do primeiro pareamento sem
  > atrapalhar quem copia o código). O log do QR diz "escolha Para assistir". Testes:
  > `SetupPage_WithOwner_ListsDevices`, `SetupPage_WithCameraStreaming_ShowsPresenceAndTransmission`,
  > `SetupPage_WithOwner_FromPublicAddress_Returns403`, `SetWatchers_ThenZero_ForgetsTheCount`; os
  > testes antigos sem dono seguem valendo.

- [x] **1.12.12 Nomes que não se confundem: Filmar/Assistir, Câmera/Monitor**
  - Origem: teste do autor em 2026-09-25: "celular que assiste", "celular câmera", "Para assistir"
    e a aba "Assistir" começam iguais e se confundem.
  - Escopo: a primeira abertura pergunta "Este celular vai ser usado para:" com "Filmar" (ícone de
    câmera e campo de nome) e "Assistir" (ícone de TV). Em todo o resto, os dois papéis se chamam
    **Câmera** e **Monitor**: abas "Câmera" e "Monitor", textos do app (ARB en e pt), página do
    servidor e log do QR. O celular que assiste pareia com o nome padrão "Monitor". Atualizar o
    caminho principal do `AGENTS.md` e o glossário do `SPECS.md` 1.
  - Aceite: widget test da primeira abertura com "Filmar" e "Assistir"; teste do log e da página do
    servidor falando em "Assistir"/"Monitor".
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (95 no
  > servidor, 148 no app). Mudaram só textos visíveis: ARB en/pt (aba "Monitor", primeira
  > abertura "Este celular vai ser usado para:" com "Filmar"/"Film" e "Assistir"/"Watch", nome
  > padrão "Monitor"), página do servidor (QR: "escolha Assistir... vira um Monitor"; painel com o
  > tipo "Monitor") e log do QR (em inglês, como todo log: "No Monitor paired yet... choose Watch").
  > Identificadores e protocolo continuam `Viewer`/`Owner`. O `RecamApiFactory` ganhou o
  > `LogCapture` (provedor de log escrito à mão) para o teste ler o log. `AGENTS.md` (caminho
  > principal), `SPECS.md` 1 (glossário) e `README.md` atualizados. Testes:
  > `Setup_WithoutOwner_LogsQrThatSpeaksOfWatchAndMonitor`,
  > `SetupPage_WithoutOwner_SpeaksOfWatchAndMonitor`, `FirstRunScreen` em pt e en.

- [x] **1.12.13 O "+" do Monitor abre direto o QR da câmera**
  - Origem: teste do autor em 2026-09-25: a escolha "Outra câmera" / "Outro celular para assistir"
    confunde, e adicionar câmera é o caso comum.
  - Escopo: no Monitor, o "+" e o "Adicionar câmera" da lista vazia mostram direto o QR de câmera,
    sem seletor. "Adicionar monitor" vai para o menu ⋮ do app, só com o Monitor pareado, e mostra o
    QR de monitor.
  - Aceite: widget tests do "+" sem seletor e do menu ⋮ abrindo o QR de monitor.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (95 no
  > servidor, 151 no app). A `AddDeviceScreen` perdeu o seletor e recebe o papel pronto; o título é
  > "Adicionar câmera" ou "Adicionar monitor". O "+" da lista e o "Adicionar câmera" da lista
  > vazia abrem o QR de câmera. O menu ⋮ do `HomeShell` ganhou "Adicionar monitor", que só aparece
  > com o Monitor pareado e abre o QR de monitor com a sessão do Monitor. Testes: "+" sem seletor,
  > menu abrindo o QR de monitor, menu sem Monitor sem a opção, e a tela nos dois papéis.

- [x] **1.12.14 A tela do QR fecha sozinha quando o aparelho pareia**
  - Origem: teste do autor em 2026-09-25: depois de parear a câmera, o Monitor continuava no QR.
  - Escopo: `POST /api/pairing-tokens` devolve também o `id` do token. `GET /api/pairing-tokens/{id}`
    (Owner, Viewer) devolve `{ used }` só para o aparelho que criou o token; para os outros, 404. A
    regra fica em `PairingToken`. No app, a tela do QR consulta a cada 2 s; quando o token foi
    usado, fecha e a lista de câmeras recarrega.
  - Aceite: `GetPairingToken_AfterPair_ReturnsUsed`, `GetPairingToken_FromAnotherDevice_Returns404`
    e o teste do controller avisando que o aparelho pareou.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (100 no
  > servidor, 155 no app). A regra é `PairingToken.UsageFor(deviceId)`: devolve `Result<bool>` para
  > quem criou e `pairing.token_not_found` para qualquer outro; o endpoint também devolve esse
  > mesmo 404 para id inexistente, então um id não revela se o token existe. Revisão no `SPECS.md`
  > 12. No app, o `AddDeviceController` consulta a cada 2 s (sem sobrepor consultas) e passa para
  > `AddDevicePaired`; a tela fecha com `true`, e a lista de câmeras recarrega quando foi o "+" que
  > a abriu. Uma consulta que falha só espera a próxima. Extras: `GetPairingToken_Unknown_Returns404`,
  > testes de domínio do `UsageFor`, `pairingTokenUsed` no `HttpApiClient`, controller parando de
  > consultar depois de pareado e o fluxo no `app_test` (QR fecha e a lista recarrega).

- [x] **1.12.15 "Reiniciar o app" tira o celular do servidor**
  - Origem: teste do autor em 2026-09-25: depois de reiniciar os dois celulares para trocar os
    papéis, o servidor continuou com os dois, e ninguém mais conseguia parear.
  - Escopo: `DELETE /api/me` (qualquer aparelho) revoga o próprio aparelho (`Device.Revoke`). "Reiniciar
    o app" chama isso para cada aba pareada antes de apagar os dados; sem resposta do servidor, apaga
    mesmo assim. Quando não sobra nenhum Monitor ativo (dono ou visualizador), o servidor volta a gerar
    o QR do primeiro celular, e a página do servidor volta a mostrá-lo. Uma câmera removida some da
    lista dos Monitores na próxima atualização.
  - Aceite: `DeleteMe_RevokesTheDevice`, `SetupPage_AfterLastMonitorLeaves_ShowsQrAgain` e o teste do
    `AppReset` avisando o servidor.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (107 no
  > servidor, 159 no app). `Device.Revoke(now)` guarda a primeira hora (`RevokedAt ??= now`), e
  > `Device.IsMonitor` (dono ou visualizador) passou a ser usado também no `IssuePairingToken`.
  > `DELETE /api/me` carrega o próprio aparelho, revoga e salva (204). O `OwnerSetup` só considera o
  > servidor configurado enquanto houver Monitor ativo; sem nenhum, volta a gerar o QR do primeiro
  > celular, que vira o novo dono. Câmeras revogadas já saíam de `GET /api/cameras` e do painel. No
  > app, `AppReset` chama `ApiClient.leave` para as abas pareadas em paralelo e apaga os dados mesmo
  > se o servidor não responder (401 conta como já removido); o `HomeShell` mostra um indicador
  > enquanto isso. O texto da confirmação diz que o celular sai do servidor. Revisão no `SPECS.md`
  > 12. Extras: `Cameras_AfterCameraLeaves_DoesNotListIt`, `DeleteMe_WithoutCredential_Returns401`,
  > `SetupPage_WhenOwnerLeavesButViewerStays_KeepsPanel`, `DeviceRevokeTests`, `HttpApiClient.leave`.

- [x] **1.12.16 Página do servidor com passo a passo e visual cuidado**
  - Origem: teste do autor em 2026-09-25: a página é feia e não diz o que fazer.
  - Escopo: um idioma por vez, escolhido pelo `Accept-Language` (pt ou en), no lugar dos dois lado a
    lado. Sem Monitor: passo a passo numerado ("1. Pegue o celular que vai assistir...") ao lado do QR.
    Com Monitor: cartões separados para Câmeras e Monitores (online, transmitindo, quantos assistem,
    bateria) e o passo a passo curto de como adicionar uma câmera. CSS próprio no HTML, com tema claro
    e escuro, legível no celular. Continua sem script e só leitura.
  - Aceite: testes da página em pt e en, com e sem Monitor.
  > Validação (2026-09-25): código escrito e página aberta no Chromium (headless, via Playwright)
  > contra o servidor rodando localmente: QR em pt no tema claro a 1100 px, em en no tema escuro a
  > 390 px, painel com duas câmeras e um Monitor nos dois temas. Com o servidor de pé, também foi
  > percorrido o 1.12.15 por `curl`: `DELETE /api/me` do único Monitor devolveu 204, a credencial
  > passou a dar 401 e a página voltou ao QR. Não percorrido nos celulares. Gate verde (114 no
  > servidor, 159 no app). Os textos ficam em `SetupTexts` (um registro por idioma), e
  > `SetupTexts.For` é uma função pura que escolhe pelo `Accept-Language`. A página escapa texto
  > com um `HtmlEncoder` que libera todo o Unicode (acentos legíveis no HTML; o que é marcação
  > continua escapado). Revisão no `SPECS.md` 12. Testes: página em pt e en sem Monitor e com
  > Monitor, cartão de câmera transmitindo, ausência de `<script>` e `onclick`, e `SetupTextsTests`
  > com cinco cabeçalhos.

- [x] **1.12.17 Primeira abertura: Assistir em cima e texto do Filmar mais claro**
  - Origem: teste do autor em 2026-09-25.
  - Escopo: na primeira abertura, a seção "Assistir" vem em cima e a seção "Filmar" embaixo. A dica
    do Filmar passa a ser "Depois leia o QR code do Adicionar câmera, no celular que for assistir."
    (en: "Then scan the QR code from Add camera on the phone that will watch.").
  - Aceite: widget test da primeira abertura conferindo a ordem das duas seções e o texto novo.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (114 no
  > servidor, 161 no app). Na `FirstRunScreen`, o cartão "Assistir" veio para cima e o "Filmar" (com
  > o campo de nome) para baixo. A dica do Filmar passou a ser "Depois leia o QR code do Adicionar
  > câmera, no celular que for assistir." (en: "Then scan the QR code from Add camera on the phone
  > that will watch."). Testes: ordem das seções pela posição na tela e o texto novo em pt e en.

- [x] **1.12.18 Miniatura opcional da própria imagem no modo câmera**
  - Origem: pedido do autor no teste de 2026-09-25.
  - Escopo: no modo câmera, um botão "Ver imagem" abre, embaixo do status, uma miniatura do que a
    câmera está filmando; tocar de novo ("Esconder imagem") fecha. Fechada por padrão, e fecha ao
    sair do modo câmera. Com alguém assistindo, a miniatura mostra a mesma trilha que está sendo
    enviada. Sem ninguém assistindo, ela abre a câmera só para a miniatura, sem transmitir, e a
    solta ao fechar. Nada é gravado nem guardado no aparelho. A lanterna continua funcionando com a
    miniatura aberta. Textos no ARB en e pt.
  - Aceite: testes do controller (abrir sem transmitir, abrir transmitindo reaproveita a trilha,
    fechar solta a câmera quando ninguém assiste, `StartPublishing` com a miniatura aberta) e widget
    test do botão mostrando e escondendo a miniatura.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (114 no
  > servidor, 170 no app). A câmera virou um recurso compartilhado: `CameraCapture` (em
  > `core/media`) abre a câmera traseira e entrega um `CameraFeed` opaco, que só sabe se mostrar
  > como miniatura (`RTCVideoView` com renderer próprio, criado e descartado com o widget). O
  > `CameraModeController` segura a câmera enquanto a transmissão ou a miniatura a usam e solta
  > quando nenhuma usa; o `WhipPublisher` deixou de abrir a câmera e passou a receber o feed em
  > `start(feed)`. Por isso a miniatura aberta durante a transmissão mostra a mesma trilha enviada,
  > e um `StartPublishing` com a miniatura aberta publica essa mesma câmera. Ao parar de
  > transmitir com a miniatura aberta, a lanterna é desligada explicitamente, porque a câmera
  > continua aberta. A tela ganhou "Ver imagem"/"Esconder imagem" e a miniatura 16:9 abaixo do
  > status; a tela agora rola, para caber em celular pequeno. Nada é gravado. Testes do controller
  > (abrir sem transmitir, reaproveitar a trilha, fechar solta a câmera, fechar transmitindo mantém,
  > `StartPublishing` com a miniatura aberta, parar com lanterna, sair do modo câmera, câmera
  > indisponível) e widget test do botão.

- [ ] **1.14 [aparelho] Validar o caminho principal completo**
  - Origem: definição do MVP.
  - Escopo: servidor no PC. A10 ou 7A como câmera, emulador Android no PC como visualizador
    (revisado em 2026-09-25; antes era um segundo celular). Percorrer os passos 1 a 7 do caminho
    principal em `AGENTS.md`.
  > Revisão (2026-09-25): sem emulador. Servidor no PC, Samsung A10 e Redmi 6A, trocando os papéis
  > entre eles no meio do teste.
  - Aceite: os 7 passos funcionam. Anotar o atraso percebido, a temperatura do celular câmera
    depois de 30 min assistindo e qualquer falha, que vira bullet novo.
  > Bloqueado (2026-09-25): aguardando aparelho. Precisa do autor com o Samsung A10 e o Redmi 6A e o
  > servidor no PC; o agente não tem celular nem emulador. O roteiro de teste está na conversa de
  > 2026-09-25.

## Fase 2: câmera que aguenta ficar ligada

- [ ] **2.1 Aparelho sem H.264 em hardware** (substituído pelo 3.2 em 2026-09-25; não executar)
  - Origem: `SPECS.md` 11.
  - Escopo: antes de iniciar o modo câmera, verificar se o `flutter_webrtc` oferece H.264 para
    envio. Se não oferecer, mostrar mensagem clara (ARB) e não entrar no modo câmera.
  - Aceite: teste do controller com fake retornando "sem H.264".

- [x] **2.2 Telemetria de temperatura**
  - Origem: `SPECS.md` 11.
  - Escopo: method channel no Android lendo `BatteryManager.EXTRA_TEMPERATURE`. Campo
    `temperatureC` na telemetria, no `Device`, no `CameraStatusDto` e na lista de câmeras.
    Migração nova.
  - Aceite: testes de servidor e do controller com fake.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho; o `MainActivity.kt` não foi
  > compilado aqui (sem Android SDK). Gate verde (119 no servidor, 173 no app). Canal
  > `io.recam.app/device`, método `batteryTemperature`, lê `EXTRA_TEMPERATURE` (décimos de °C) do
  > broadcast fixo da bateria, sem registrar receiver. Servidor: `Device.TemperatureC` (`double?`,
  > migração `CameraTemperature`), validado na entidade entre -40 e 120 °C
  > (`device.invalid_temperature`); `CameraStatus` ganhou `temperatureC`. Decisão fora do bullet:
  > `ReportTelemetry` passou a receber um objeto `{ batteryLevel, isCharging, temperatureC }`
  > (`TelemetryReport`) no lugar de dois argumentos soltos, porque o cliente SignalR do Dart só
  > aceita argumentos não nulos e a temperatura pode faltar. Revisão no `SPECS.md` 12. No app, a
  > leitura compara a temperatura em graus inteiros, para uma oscilação de décimos não mandar
  > relatório a cada 15 s; a lista mostra "NN °C" ao lado da bateria. Testes: domínio (guarda, recusa
  > temperatura impossível, aceita ausência), hub até a lista, controller (manda quando muda um grau,
  > não manda por décimos) e a lista aplicando `CameraStatusChanged`.

- [x] **2.3 Redução automática de qualidade por calor**
  - Origem: `SPECS.md` 11.
  - Escopo: com temperatura ≥ 42 °C, a câmera passa para 854x480 a 10 fps e 400 kbps. Volta a
    720p abaixo de 38 °C.
  - Aceite: teste da regra de histerese como função pura.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (119 no
  > servidor, 182 no app). A regra é `VideoQuality.forTemperature(atual, temperatura)`, função pura
  > em `core/media/video_quality.dart`: ≥ 42 °C reduz, < 38 °C volta ao cheio, entre os dois mantém,
  > sem leitura não muda. O `CameraModeController` reavalia a cada leitura da bateria (a cada 15 s)
  > e manda a mudança ao `WebRtcPublisher.setQuality`, que troca os parâmetros do sender sem reabrir
  > a câmera nem renegociar (`maxBitrate` 400 kbps, `maxFramerate` 10, `scaleResolutionDownBy` 1,5)
  > e guarda a qualidade para a próxima transmissão. Com escala 1,5 sobre a captura de 1280x720, o
  > libwebrtc manda 853x480, não 854x480 exatos; para 854 seria preciso reabrir a câmera com outra
  > resolução, o que derrubaria a transmissão. A tela da câmera avisa "O celular esquentou: enviando
  > vídeo em qualidade menor até esfriar." Testes: histerese como função pura (limites 42 e 38,
  > faixa do meio nos dois sentidos, sem leitura) e o controller reduzindo e voltando.

- [x] **2.4 Tela guiada de otimização de bateria**
  - Origem: `SPECS.md` 11.
  - Escopo: ao iniciar o modo câmera pela primeira vez, verificar
    `isIgnoringBatteryOptimizations`. Se não estiver liberado, mostrar passos por fabricante
    (Samsung, Xiaomi, genérico) com botão que abre a tela de configuração certa.
  - Aceite: teste do controller escolhendo as instruções pelo fabricante.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho; o `MainActivity.kt` não foi
  > compilado aqui. Gate verde (119 no servidor, 188 no app). `BatteryOptimization` em `core/device`
  > usa o `permission_handler` (`ignoreBatteryOptimizations`, que já estava nas dependências) e o
  > canal `io.recam.app/device`, que ganhou `manufacturer` (`Build.MANUFACTURER`). O manifesto pede
  > `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`. O `BatteryGuideController.forManufacturer` escolhe
  > Samsung, Xiaomi (também "Redmi" e "POCO", que às vezes vêm como fabricante) ou genérico. Decisão:
  > o guia aparece antes do modo câmera sempre que a otimização ainda não estiver liberada, não só
  > na primeira vez, porque o Android não guarda "já mostrei" e liberar é o que importa; depois de
  > liberado, nunca mais aparece. A tela tem os passos, "Liberar" (o diálogo do próprio Android),
  > "Abrir configurações do app" (Samsung e Xiaomi) e "Continuar para o modo câmera". Testes do
  > controller por fabricante e widget test do guia antes do modo câmera.

- [ ] **2.5 [aparelho] Validar 24 horas ligado**
  - Origem: robustez.
  - Escopo: A10 e 7A em modo câmera por 24 h, na tomada, com sessões de visualização
    espalhadas.
  - Aceite: nenhum dos dois cai sem reconectar. Anotar a temperatura máxima e as falhas.
  > Bloqueado (2026-09-25): aguardando aparelho. São 24 h com o Samsung A10 e o Redmi 6A em modo
  > câmera, na tomada; o agente não tem os celulares.

## Fase 3: gravação

Combinado com o autor em 2026-09-25: a gravação entra depois da Fase 2, porque gravar obriga a
câmera a transmitir o tempo todo (calor e bateria). Desenho:

- O MediaMTX grava; o .NET só decide o que gravar, controla o espaço e serve os arquivos. O app no
  modo câmera continua sem gravar nada no aparelho.
- Gravação ligada por câmera ("Gravar sempre"). Câmera com gravação ligada transmite o tempo todo,
  mesmo sem ninguém assistindo.
- Espaço total para gravações escolhido numa barrinha no Monitor; quando enche, o mais antigo é
  apagado.
- Linha do tempo simples no Monitor: dias, horas com trechos gravados, tocar para assistir.

- [ ] **3.1 Gravação no MediaMTX, volume e desenho no SPECS**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: `SPECS.md`: a gravação sai de "Fora do escopo", entra no 1.1 e ganha uma seção
    "Gravação" com o desenho desta fase (revisão datada no 12). No `mediamtx.yml`, um segundo grupo
    de paths, `~^rec-[0-9a-f]{32}$`, com `record: yes`, `recordFormat: fmp4`, segmentos de 60 s em
    `/recordings/%path/%Y-%m-%d_%H-%M-%S-%f` e `recordDeleteAfter: 0` (quem apaga é o servidor). Os
    paths `cam-` seguem sem gravar. Volume nomeado `recam-recordings` montado no MediaMTX e no
    servidor, nos dois composes. Os arquivos criados pelo MediaMTX precisam poder ser apagados pelo
    servidor, que roda sem root: rodar o MediaMTX com o mesmo usuário do servidor, ou resolver de
    outro jeito e registrar no SPECS. Conferir se o MediaMTX grava VP8 em fMP4 e registrar o
    resultado no SPECS 11.
  - Aceite: teste com Testcontainers publicando H.264 por WHIP num path `rec-` e achando um segmento
    fMP4 na pasta; num path `cam-`, nenhum arquivo.

- [ ] **3.2 Gravar sempre, por câmera**
  - Origem: combinado com o autor em 2026-09-25. Substitui o 2.1.
  - Escopo: `Device.RecordingEnabled`, com migração e método da entidade para ligar e desligar (só
    câmera, só por Monitor). Hub: `SetRecording(Guid cameraId, bool enabled)` para Monitores. O
    proxy WHIP/WHEP escolhe o path do MediaMTX pelo estado: `rec-{id}` gravando, `cam-{id}` sem
    gravar. Mudar o estado com a câmera transmitindo faz ela reiniciar a transmissão. Câmera com
    gravação ligada recebe `StartPublishing` ao conectar e não recebe `StopPublishing` quando o
    último Monitor sai. `CameraStatusDto` ganha `recording`. App: no ao vivo e na lista, um
    interruptor "Gravar sempre"; no modo câmera, o status mostra "Gravando". Câmera sem H.264
    (VP8) não grava, se o 3.1 confirmar que o MediaMTX não grava VP8: o interruptor fica desligado
    com a explicação.
  - Aceite: testes de domínio, do hub (liga, reconecta, último Monitor sai e ela continua), do path
    escolhido pelo proxy e dos controllers do app.

- [ ] **3.3 Espaço para gravações e limpeza automática**
  - Origem: combinado com o autor em 2026-09-25 (a "barrinha" do SPECS 1.2).
  - Escopo: cota total em MB guardada no banco (migração), padrão 2048 MB. `GET` e `PUT
    /api/recordings/quota` (Monitores): o `GET` devolve cota, uso atual e espaço livre no disco; o
    `PUT` recusa cota maior que uso + livre. Um `BackgroundService` a cada 60 s soma os arquivos de
    `/recordings` e apaga os segmentos mais antigos, de qualquer câmera, até ficar dentro da cota;
    também apaga as gravações de câmeras removidas. App: no menu ⋮ do Monitor, "Gravações" com a
    barrinha, o uso atual e quantas horas cabem (a 700 kbps, cerca de 300 MB por hora por câmera).
    A página do servidor mostra o uso ("1,2 de 2 GB").
  - Aceite: testes da limpeza com pasta temporária e `FakeTimeProvider`, dos endpoints e do
    controller da barrinha.

- [ ] **3.4 Listar e servir as gravações**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: `GET /api/cameras/{id}/recordings?day=AAAA-MM-DD` (Monitores) devolve os trechos do dia
    (início, fim, url), montados a partir dos nomes dos arquivos, com trechos seguidos emendados. O
    segmento ainda sendo gravado fica de fora. `GET /api/recordings/{cameraId}/{segmento}` serve o
    arquivo com suporte a `Range`. Nenhum caminho vindo do cliente chega ao disco sem validação (sem
    `..`, só nomes no formato do MediaMTX). Horários em UTC no protocolo.
  - Aceite: testes de listagem (dia vazio, trechos emendados, segmento em andamento), de `Range` e
    de caminho malicioso recusado.

- [ ] **3.5 Linha do tempo no Monitor**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: dependência nova `video_player`. O player do Android não passa pelo pinning do Dart,
    então um repassador local em `lib/core/` (`HttpServer` em 127.0.0.1, porta aleatória) busca o
    arquivo no servidor pareado, com a credencial e o pinning, e entrega ao player, com `Range`.
    Nada é salvo no aparelho. Tela "Gravações" de cada câmera (botão no ao vivo e na lista): dias
    com gravação, as 24 horas do dia com os trechos gravados marcados; tocar num ponto começa ali e
    segue para o próximo segmento. Horários no fuso do celular.
  - Aceite: testes do repassador (repassa `Range`, só atende o próprio aparelho), do controller da
    linha do tempo e widget test da tela.

- [ ] **3.6 [aparelho] Validar a gravação**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: Samsung A10 e Redmi 6A. Uma câmera gravando por 2 h, com cota pequena (ex.: 300 MB) para
    ver a limpeza; linha do tempo e reprodução no Monitor.
  - Aceite: a gravação aparece na linha do tempo e toca, e o mais antigo some quando a cota enche.
    Anotar temperatura e bateria da câmera gravando.

## Fase 4: gestão de aparelhos

- [ ] **4.1 Revogar aparelhos**
  - Escopo: `GET /api/devices` e `DELETE /api/devices/{id}` (`OwnerOnly`, derruba a conexão do
    hub). Tela de aparelhos na aba Assistir.
  - Aceite: `DeleteDevice_AsOwner_RevokesAndDisconnects` e o teste do controller.
  > Revisão (2026-09-25): o app não mostra dono, e o dono pode ter saído (1.12.15). Qualquer
  > Monitor remove aparelhos (política `ViewerOrOwner`, regra em `Device`); um aparelho não remove a
  > si mesmo por aqui (isso é o "Reiniciar o app"). A tela "Aparelhos" fica no menu ⋮ do Monitor,
  > com câmeras e Monitores, e remover pede confirmação. A câmera removida some da lista dos
  > Monitores, e o modo câmera dela fecha com a mensagem de pareamento perdido. O nome do teste de
  > aceite vira `DeleteDevice_AsMonitor_RevokesAndDisconnects`.

- [ ] **4.2 Parear outro celular visualizador** (movido para o 1.12.9 em 2026-09-25; não executar)
  - Escopo: `POST /api/pairing-tokens` aceita `role: "viewer"`. Botão "Adicionar visualizador".
  - Aceite: `CreatePairingToken_ForViewer_PairsAsViewer`.

- [ ] **4.3 Resetar o dono**
  - Escopo: `docker compose exec server ./Recam.Server reset-owner` revoga o dono atual e
    volta a gerar o token do dono.
  > Revisão (2026-09-25): o comando revoga todos os Monitores (dono e visualizadores), não só o dono.
  > É a saída quando o único Monitor quebrou ou sumiu e não dá para usar "Reiniciar o app" nele. As
  > câmeras continuam pareadas, e a página do servidor volta ao QR do primeiro Monitor.
  - Aceite: `ResetOwner_WithOwner_RevokesAndCreatesSetupToken`.

## Fase 5: distribuição

- [ ] **5.1 Modo atrás de proxy reverso**
  - Escopo: `RECAM_TLS=off` faz o Kestrel servir HTTP. Respeitar `X-Forwarded-*` de proxies
    configurados. O QR sai sem `f`, e o app valida pelas CAs do sistema.
  - Aceite: testes do QR sem fingerprint e do middleware de forwarded headers.

- [ ] **5.2 CI no GitHub Actions** (repositório criado: `jpemendonca/ReCam`, privado até o lançamento)
  - Escopo: workflow rodando `scripts/gate.sh` em pull request e push na `main`, com relatório
    de cobertura do servidor (`coverlet`). Selos de build e cobertura no `README.md`.
  - Aceite: workflow verde num PR e selos aparecendo no README.

- [ ] **5.2.1 Observabilidade com OpenTelemetry**
  - Origem: vitrine de portfólio.
  - Escopo: pacotes `OpenTelemetry.Extensions.Hosting` e instrumentações de ASP.NET Core e
    HttpClient. Métricas próprias: câmeras online, câmeras publicando, visualizações ativas.
    Exportação OTLP ligada só com `OTEL_EXPORTER_OTLP_ENDPOINT` definida (chave vazia no
    `.env.example`). Serviço opcional do Aspire Dashboard num `deploy/compose.observability.yaml`.
  - Aceite: teste das métricas com `MeterListener`. Com o compose de observabilidade, o painel
    mostra traces de `/api/cameras` e a métrica de câmeras online.

- [ ] **5.2.2 ADRs**
  - Origem: vitrine de portfólio.
  - Escopo: `docs/adr/`, um arquivo por decisão do log do `SPECS.md` 12 (contexto, decisão,
    consequências), em português. O `SPECS.md` 12 passa a apontar para os ADRs.
  - Aceite: todo item do log tem ADR correspondente.

- [ ] **5.2.3 [aparelho] Vitrine do README**
  - Origem: vitrine de portfólio.
  - Escopo: GIF curto do caminho principal (parear, ver ao vivo, lanterna) e diagrama da
    arquitetura no `README.md`.
  - Aceite: README mostra o GIF gravado nos aparelhos reais.

- [ ] **5.3 Imagem publicada no GHCR e instalação em um comando**
  - Escopo: workflow que publica `ghcr.io/<dono>/recam-server` em tag `v*`. Os composes usam a
    imagem publicada. O README ensina a instalar baixando só a pasta `deploy/`.
  - Aceite: instalação do zero numa máquina Linux seguindo só o README.

- [ ] **5.4 APK de release assinado** (depende do usuário gerar o keystore e cadastrar os secrets)
  - Escopo: build de release com `--split-per-abi` (armeabi-v7a e arm64-v8a) publicada no
    GitHub Releases.
  - Aceite: APK de release instalado no A10 e no 7A.

- [ ] **5.5 Contribuição e CLA** (depende do usuário escolher o texto do CLA e instalar o CLA Assistant)
  - Escopo: `CONTRIBUTING.md` (como rodar, gate, Conventional Commits, CLA obrigatório) e o
    documento do CLA.
  - Aceite: antes do primeiro pull request externo, o CLA Assistant pede o aceite num PR de
    teste.

---

## Fora da fila (anotado, não executar)

Itens que dependem de decisão futura. O loop para antes daqui.

- Acesso fora da rede local.
- Detecção de movimento e notificações.
- iOS.
- Fallback por TCP ou HLS.
- Publicação nas lojas de apps de servidor caseiro (Umbrel, CasaOS, Unraid, Synology).
