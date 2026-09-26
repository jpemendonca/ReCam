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

A fila segue a ordem do arquivo, não o número da fase. Desde 2026-09-25 as Fases 6 (Monitor no
navegador) e 7 (clarear e movimento) ficam antes da Fase 5 (distribuição), porque mudam o primeiro
uso que a distribuição vai ensinar.

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

- [ ] **1.14 [aparelho] Validar o caminho principal completo** (substituído pelo 6.10 em 2026-09-25; não executar)
  - Origem: definição do MVP.
  - Escopo: servidor no PC. A10 ou 6A como câmera, emulador Android no PC como visualizador
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
  - Escopo: A10 e 6A em modo câmera por 24 h, na tomada, com sessões de visualização
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

- [x] **3.1 Gravação no MediaMTX, volume e desenho no SPECS**
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
  > Validação (2026-09-25): código escrito e caminho de servidor percorrido; nada no celular. Gate
  > verde (120 no servidor, 188 no app). `deploy/mediamtx.yml` ganhou o grupo
  > `~^rec-[0-9a-f]{32}$` com `record: yes`, fMP4, segmentos de 60 s em
  > `/recordings/%path/%Y-%m-%d_%H-%M-%S-%f` e `recordDeleteAfter: 0s`; os paths `cam-` seguem sem
  > gravar. Volume `recam-recordings` nos dois composes, em `/recordings` no MediaMTX e no servidor.
  > Permissões: o MediaMTX roda com `user: "1654:1654"` (o `APP_UID` das imagens .NET), o Dockerfile
  > cria `/recordings` com esse dono e o MediaMTX depende do servidor, para o volume nascer com a
  > permissão certa. Conferido com o mesmo arranjo do compose (imagem mínima com as linhas do
  > Dockerfile, porque o `dotnet restore` dentro do build não passa pelo proxy deste ambiente):
  > volume com dono `app`, segmento gravado como 1654, e o servidor apagou o arquivo e a pasta. VP8:
  > publicando VP8 num path `rec-` (por RTSP, só no experimento, porque o muxer WHIP do FFmpeg só
  > aceita H.264), o MediaMTX 1.21.1 registrou "no supported tracks found, skipping recording": não
  > grava VP8 (registrado no SPECS 11). Teste `Publish_ToRecPath_WritesFmp4Segment`, com MediaMTX e
  > FFmpeg reais via Testcontainers: H.264 por WHIP num `rec-` gera um `.mp4` começando com `ftyp`;
  > num `cam-`, nenhum arquivo. Dependência de teste: imagem `linuxserver/ffmpeg:9.0-cli-ls83` (o
  > publicador H.264 por WHIP que o aceite pede). No teste o MediaMTX anuncia só a `eth0`, porque o
  > FFmpeg tenta só o primeiro candidato ICE.

- [x] **3.2 Gravar sempre, por câmera**
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
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (136 no
  > servidor, 195 no app). Domínio: `Device.SetRecording(requester, enabled)` (só Monitor ativo, só
  > câmera, ligar exige H.264), `Device.ReportVideoCodecs`, `CanRecord`; migração `CameraRecording`.
  > Hub: `SetRecording` salva, avisa a câmera com `RecordingChanged` e chama
  > `WatchLeases.RecordingChangedAsync`, que reinicia uma câmera transmitindo (para trocar de path)
  > ou liga uma parada; o fim da carência de 30 s consulta o banco e não para uma câmera que grava;
  > a câmera que conecta com gravação ligada recebe `StartPublishing`. Proxy: o path sai do estado
  > da câmera no `POST`, e a sessão devolvida leva o prefixo `cam-`/`rec-` (tipo `MediaSession`), que
  > decide o path do `PATCH`/`DELETE`. VP8: como o 3.1 confirmou que o MediaMTX não grava VP8, a
  > câmera informa na telemetria se o libwebrtc oferece H.264 (`WebRtcPublisher.canSendH264`), e o
  > interruptor aparece desligado com "Não grava: este celular não tem H.264". App: `RecordingSwitch`
  > ("Gravar sempre") na lista, embaixo do status de cada câmera, e na barra do ao vivo; o modo
  > câmera mostra "Gravando". Revisão no `SPECS.md` 12. Testes: domínio (Monitor liga e desliga,
  > câmera não pode, só câmera grava, VP8 recusado, perder H.264 desliga), hub (ligar faz publicar,
  > trocar transmitindo reinicia, último Monitor sai e ela continua, reconecta e publica, câmera não
  > liga, VP8 recusado), path do proxy com um MediaMTX falso (`cam-` sem gravação, `rec-` com
  > gravação para WHIP e WHEP, `DELETE` segue a sessão, sessão desconhecida dá 404) e, no app, o
  > controller da câmera, o da lista e o widget do interruptor.

- [x] **3.3 Espaço para gravações e limpeza automática**
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
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (151 no
  > servidor, 204 no app). Domínio: `RecordingQuota` (linha única, migração `RecordingQuota`,
  > padrão 2048 MB, mínimo 100 MB) com `ChangeTo(mb, usado, livre)` e `PlanCleanup(segmentos,
  > câmeras ativas)`, função pura: apaga tudo de câmera removida e depois os mais antigos, de
  > qualquer câmera, até caber. Decisão: o segmento mais novo de cada câmera nunca é apagado, porque
  > o MediaMTX pode estar escrevendo nele (apagar um arquivo aberto não libera o espaço no Linux);
  > com uma cota muito pequena, o uso pode passar dela por até um segmento por câmera.
  > `RecordingStore` (Infrastructure) lê só pastas `rec-<32 hex>` e arquivos no formato do MediaMTX,
  > mede o disco (`DriveInfo`) e apaga; o resto da pasta é ignorado. `RecordingCleanupWorker` roda a
  > cada 60 s com o `TimeProvider`. Endpoints `GET`/`PUT /api/recordings/quota` (Monitores).
  > `RecordingQuotas.LoadAsync` ficou em `Infrastructure/Recordings`, porque a página do servidor
  > (`Features.Setup`) também lê a cota para mostrar "1,2 de 2 GB em uso". App: "Gravações" no menu
  > ⋮ (com o Monitor pareado), com a barrinha entre 100 MB e uso + livre, o uso atual e quantas
  > horas de uma câmera cabem (300 MB por hora). Revisão no `SPECS.md` 12. Testes: plano de limpeza
  > (dentro da cota, mais antigos entre câmeras, mais novo preservado, câmera removida), cota maior
  > que o disco, leitura da pasta (nomes, UTC, arquivos estranhos), endpoints (GET, PUT, maior que
  > o disco, abaixo do mínimo, câmera 403), worker com pasta temporária e `FakeTimeProvider`
  > (arquivos esparsos de 60 MB), página nos dois idiomas e, no app, controller, cliente HTTP e menu.

- [x] **3.4 Listar e servir as gravações**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: `GET /api/cameras/{id}/recordings?day=AAAA-MM-DD` (Monitores) devolve os trechos do dia
    (início, fim, url), montados a partir dos nomes dos arquivos, com trechos seguidos emendados. O
    segmento ainda sendo gravado fica de fora. `GET /api/recordings/{cameraId}/{segmento}` serve o
    arquivo com suporte a `Range`. Nenhum caminho vindo do cliente chega ao disco sem validação (sem
    `..`, só nomes no formato do MediaMTX). Horários em UTC no protocolo.
  - Aceite: testes de listagem (dia vazio, trechos emendados, segmento em andamento), de `Range` e
    de caminho malicioso recusado.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (167 no
  > servidor, 204 no app). A regra é `RecordingTimeline` (Domain, função pura): cada segmento dura
  > 60 s ou até o próximo começar, segmentos com folga de até 1 s são emendados num trecho, o dia é
  > em UTC e o segmento mais novo sai quando `newestInProgress` (a câmera tem a gravação ligada e
  > está transmitindo). Decisão fora do bullet: cada trecho traz a lista de segmentos, cada um com
  > sua url, porque o player toca arquivo por arquivo; e entrou `GET
  > /api/cameras/{id}/recording-days` para o 3.5 saber quais dias têm gravação sem pedir dia a dia.
  > O arquivo sai por `TypedResults.PhysicalFile(..., enableRangeProcessing: true)`. O caminho vem
  > de `RecordingStore.PathOf`, que só aceita o formato de nome do MediaMTX, então `..`, `%2E%2E`,
  > nomes de outros arquivos ou extensões a mais dão 404 sem tocar no disco. Revisão no `SPECS.md`
  > 12. Testes: linha do tempo (dia vazio, emenda e intervalo, segmento cortado, em andamento, outros
  > dias, dias com gravação) e endpoints (dia vazio, trechos emendados com url, em andamento com a
  > câmera gravando pelo hub, dia inválido, `Range` 206 com os bytes certos, quatro nomes maliciosos,
  > câmera 403).

- [x] **3.5 Linha do tempo no Monitor**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: dependência nova `video_player`. O player do Android não passa pelo pinning do Dart,
    então um repassador local em `lib/core/` (`HttpServer` em 127.0.0.1, porta aleatória) busca o
    arquivo no servidor pareado, com a credencial e o pinning, e entrega ao player, com `Range`.
    Nada é salvo no aparelho. Tela "Gravações" de cada câmera (botão no ao vivo e na lista): dias
    com gravação, as 24 horas do dia com os trechos gravados marcados; tocar num ponto começa ali e
    segue para o próximo segmento. Horários no fuso do celular.
  - Aceite: testes do repassador (repassa `Range`, só atende o próprio aparelho), do controller da
    linha do tempo e widget test da tela.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho; o APK não foi compilado
  > aqui (sem Android SDK). Gate verde (167 no servidor, 216 no app). Dependência nova, a pedida:
  > `video_player` `^2.14.0`. `RecordingRelay` (`core/media`): `HttpServer` em 127.0.0.1, porta
  > aleatória, sob um caminho secreto aleatório (outro app do mesmo celular não consegue usar a
  > credencial por ele); só aceita `GET` de loopback em `/api/recordings/...`; busca no servidor
  > pareado com o `HttpClient` do `dart:io` (logo com o pinning do `HttpOverrides`), a credencial e
  > o `Range`, e devolve em streaming, sem gravar nada. `RecordingPlayer` é a interface sobre o
  > `video_player` (o plugin fica em `core`). O manifesto ganhou `network_security_config.xml`
  > liberando HTTP só para 127.0.0.1, porque o Android 9+ bloqueia HTTP sem TLS. O
  > `RecordingTimelineController` converte os dias UTC do servidor para o calendário do celular,
  > busca os um ou dois dias UTC que cobrem o dia local, põe os segmentos no relógio local, toca a
  > partir do ponto tocado (num intervalo, do próximo trecho) e emenda o próximo segmento ao fim de
  > cada um. Tela "Gravações · câmera": player 16:9, chips dos dias, barra das 24 horas desenhada
  > com os trechos marcados e o segmento tocando em destaque. Botão de gravações no ao vivo e em
  > cada câmera da lista. Testes: repassador (repassa `Range` e credencial, recusa sem o segredo,
  > recusa outros caminhos do servidor, escuta só no loopback), controller (dias no fuso de
  > Brasília, dia local em dois dias UTC, tocar no meio de um segmento, tocar num intervalo, emendar
  > o próximo, liberar player e repassador) e widget test da tela (tocar nas horas toca; sem
  > gravação explica como começar).

- [ ] **3.6 [aparelho] Validar a gravação**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: Samsung A10 e Redmi 6A. Uma câmera gravando por 2 h, com cota pequena (ex.: 300 MB) para
    ver a limpeza; linha do tempo e reprodução no Monitor.
  - Aceite: a gravação aparece na linha do tempo e toca, e o mais antigo some quando a cota enche.
    Anotar temperatura e bateria da câmera gravando.
  > Bloqueado (2026-09-25): aguardando aparelho. São 2 h de gravação com o Samsung A10 e o Redmi 6A e
  > o servidor no PC; o agente não tem os celulares.

## Fase 4: gestão de aparelhos

- [x] **4.1 Revogar aparelhos**
  - Escopo: `GET /api/devices` e `DELETE /api/devices/{id}` (`OwnerOnly`, derruba a conexão do
    hub). Tela de aparelhos na aba Assistir.
  - Aceite: `DeleteDevice_AsOwner_RevokesAndDisconnects` e o teste do controller.
  > Revisão (2026-09-25): o app não mostra dono, e o dono pode ter saído (1.12.15). Qualquer
  > Monitor remove aparelhos (política `ViewerOrOwner`, regra em `Device`); um aparelho não remove a
  > si mesmo por aqui (isso é o "Reiniciar o app"). A tela "Aparelhos" fica no menu ⋮ do Monitor,
  > com câmeras e Monitores, e remover pede confirmação. A câmera removida some da lista dos
  > Monitores, e o modo câmera dela fecha com a mensagem de pareamento perdido. O nome do teste de
  > aceite vira `DeleteDevice_AsMonitor_RevokesAndDisconnects`.
  > Validação (2026-09-25): só código escrito. Não percorrido no aparelho. Gate verde (177 no
  > servidor, 222 no app). Segue a revisão do bullet: qualquer Monitor remove (política
  > `ViewerOrOwner`), a regra é `Device.RevokeBy(requester, now)` (só Monitor ativo, nunca a si
  > mesmo, já removido é "não encontrado"), e a tela "Aparelhos" fica no menu ⋮ do Monitor, com
  > câmeras e Monitores separados, "Este celular" sem o botão de remover, e confirmação. Para
  > derrubar a conexão, a feature `Realtime` passou a guardar as conexões de cada aparelho
  > (`DeviceConnections`, `HubCallerContext.Abort`) e implementa `IDeviceRemovals`, interface em
  > `Infrastructure/Realtime`: assim o endpoint (feature `Devices`) aborta as conexões e avisa os
  > Monitores com `CameraRemoved` sem depender de `Realtime`. O `DELETE /api/me` do 1.12.15 usa o
  > mesmo caminho. No app, a lista de câmeras tira a câmera ao receber `CameraRemoved`; a câmera
  > removida é derrubada, reconecta, recebe 401 e o modo câmera fecha pelo caminho do 1.12.3.
  > Revisão no `SPECS.md` 12. Testes: `DeleteDevice_AsMonitor_RevokesAndDisconnects` (204, a
  > conexão real da câmera fecha, o Monitor recebe `CameraRemoved`, a credencial dá 401, a lista fica
  > vazia), listagem com presença, remover a si mesmo 409, desconhecido 404, câmera 403, domínio do
  > `RevokeBy`, e no app o controller dos aparelhos, a lista tirando a câmera e o fluxo pelo menu
  > com confirmação.

- [ ] **4.2 Parear outro celular visualizador** (movido para o 1.12.9 em 2026-09-25; não executar)
  - Escopo: `POST /api/pairing-tokens` aceita `role: "viewer"`. Botão "Adicionar visualizador".
  - Aceite: `CreatePairingToken_ForViewer_PairsAsViewer`.

- [x] **4.3 Resetar o dono**
  - Escopo: `docker compose exec server ./Recam.Server reset-owner` revoga o dono atual e
    volta a gerar o token do dono.
  > Revisão (2026-09-25): o comando revoga todos os Monitores (dono e visualizadores), não só o dono.
  > É a saída quando o único Monitor quebrou ou sumiu e não dá para usar "Reiniciar o app" nele. As
  > câmeras continuam pareadas, e a página do servidor volta ao QR do primeiro Monitor.
  - Aceite: `ResetOwner_WithOwner_RevokesAndCreatesSetupToken`.
  > Validação (2026-09-25): código escrito e comando percorrido fora do Docker; nada no celular. Gate
  > verde (179 no servidor, 222 no app). Segue a revisão do bullet: revoga todos os Monitores ativos
  > (dono e visualizadores) e mantém as câmeras. `Program.cs` despacha `reset-owner` antes de montar
  > o servidor, como o `healthcheck`. O `ResetOwnerCommand` abre o banco por
  > `PersistenceExtensions.CreateDbContext` (sem DI, para não usar `GetRequiredService` fora do
  > `Program`), aplica as migrações, revoga cada Monitor pela entidade (`Device.Revoke`) e imprime o
  > que fazer. Quem gera o QR novo é o servidor em execução: sem Monitor ativo, o `OwnerSetup` emite
  > o token do dono no log e em `/setup` em até 30 s. Conferido: servidor rodando, um dono pareado, e
  > `./Recam.Server reset-owner` (o executável que a publicação gera) imprimiu "Removed 1
  > Monitor(s)", saiu com 0 e a página voltou a mostrar o QR. Limitação: as conexões do hub dos
  > Monitores revogados vivem no processo do servidor e só caem na próxima reconexão, que recebe 401.
  > Testes: `ResetOwner_WithOwner_RevokesAndCreatesSetupToken` (dono e visualizador revogados, câmera
  > mantida, `OwnerSetup` volta a `Pending` e a página mostra o QR) e servidor sem Monitor.

## Fase 6: Monitor no navegador

Desenho em `SPECS.md` 2.5 e no caminho principal do `AGENTS.md`, decidido pelo autor em 2026-09-25
(ADRs 0038 a 0040). O navegador vira o primeiro Monitor com o código do log, e o primeiro celular é
sempre uma câmera. Cada bullet deixa o servidor e o app funcionando; o caminho novo só fecha no 6.8.

- [x] **6.1 Esqueleto do Monitor web**
  - Origem: ADR 0039.
  - Escopo: projeto `server/src/Recam.Web` (Blazor WebAssembly) e `server/tests/Recam.Web.Tests`
    (bUnit), na `Recam.slnx`. O `Recam.Server` serve o `Recam.Web` na raiz (feature `Web`), com
    `Content-Security-Policy` restrita à própria origem, `X-Content-Type-Options` e
    `Referrer-Policy`. Idioma `en`/`pt` por `.resx` e
    `IStringLocalizer`, escolhido pelo navegador. Layout com tema claro e escuro e a página
    inicial "Este navegador ainda não é um Monitor", sem ações. O Dockerfile publica os dois.
    Dependências novas pedidas por este bullet: `Microsoft.AspNetCore.Components.WebAssembly`,
    `Microsoft.AspNetCore.Components.WebAssembly.Server` e `bunit`.
  - Fora: entrar, câmeras, vídeo. O `/setup` continua como está até o 6.2.
  - Aceite: teste de integração (a raiz entrega o app com a CSP) e teste
    bUnit da página inicial em `en` e `pt`. Gate rodando os dois projetos de teste.
  > Validação (2026-09-26): código escrito e percorrido no navegador do PC (Chromium headless),
  > sem celular. `Recam.Web` (Blazor WebAssembly) com layout, página inicial, "Página não
  > encontrada", tema claro e escuro e textos em `Strings.resx`/`Strings.pt.resx`; o idioma vem do
  > navegador. O `Recam.Server` serve o app na raiz pela feature `Web` (`MapStaticAssets` e o
  > `index.html` para as rotas do cliente, nunca para `/api`, `/hubs`, `/whip`, `/whep`, `/setup` e
  > `/health`), com CSP sem `unsafe-inline` (`script-src 'self' 'wasm-unsafe-eval'`), `nosniff` e
  > `no-referrer`; o `/setup` fica fora da CSP até o 6.2 tirá-lo. O `index.html` usa
  > `_framework/blazor.webassembly.js` sem fingerprint e sem import map inline: com o
  > `OverrideHtmlAssetPlaceholders`, o publish deixava o placeholder cru e o app não subia em
  > produção. Dependências: `Microsoft.AspNetCore.Components.WebAssembly`,
  > `...WebAssembly.Server`, `Microsoft.Extensions.Localization` (para o `IStringLocalizer`) e
  > `bunit`. Observado: em Development e no publish Release rodando como Production, o Chromium
  > abriu a página em pt e em en sem erro de CSP no console; rota desconhecida do cliente mostrou
  > "Page not found"; `/api/...` desconhecido deu 404 em JSON. O download comprimido do app é de
  > 2,8 MB. O Dockerfile copia o projeto novo, mas a imagem não foi construída aqui. Gate verde
  > (197 testes no servidor e no web, 223 no app).

- [x] **6.2 Primeira abertura: o navegador vira Monitor**
  - Origem: ADR 0038.
  - Escopo:
    - Servidor: o código de primeira abertura (`SPECS.md` 6) substitui o token do dono. Regra no
      domínio (gerar, conferir em tempo constante, contar erros, trocar depois de 5). Impresso no
      log com o endereço enquanto não há Monitor ativo; o QR e o token do dono saem do log, a página
      `/setup` sai (`GET /setup` redireciona para `/`), e a exceção de log do `AGENTS.md` passa a
      valer para o código.
      `POST /api/web/first-open` (só rede local, só sem Monitor, limite por IP) cria o `Owner`
      com o nome "Navegador · <navegador> no <sistema>" e grava o cookie `recam_device`.
      `DeviceAuthenticationHandler` aceita o cookie. Método que muda estado com cookie exige
      `X-Recam-Web: 1`. `POST /api/web/sign-out` revoga e apaga o cookie. O `reset-owner` passa a
      imprimir o código novo.
    - Navegador: tela do código com "Lembrar neste computador"; depois de entrar, uma tela
      provisória "Você é o Monitor" com **Sair**. `IRecamApi` com `HttpClient` e o cabeçalho.
  - Fora: o QR de câmera (6.4).
  - Aceite: testes do domínio do código; testes de integração (código certo cria o `Owner` e o
    cookie, código errado 5 vezes troca o código, com Monitor ativo recusa, fora da rede local
    recusa, cookie sem `X-Recam-Web` num `POST` recusa, sair revoga, o log mostra o código e
    nunca a credencial, `/setup` redireciona); bUnit da tela do código.
  > Validação (2026-09-26): código escrito e percorrido no navegador do PC (Chromium headless contra
  > o servidor rodando), sem celular. Servidor: `FirstOpenCode` no domínio (8 caracteres sem letras
  > parecidas, `XXXX-XXXX`, conferido em tempo constante sem ligar para caixa, espaço e traço,
  > gasto depois de 5 erros; com Monitor ativo recusa sem contar erro) e `Device.CreateFirstMonitor`.
  > O serviço `FirstOpen` substituiu o `OwnerSetup`: guarda o código só em memória, imprime no log
  > enquanto não há Monitor (novo a cada start, depois de 5 erros e depois que o último Monitor
  > sai) e cria o dono. Rotas `GET`/`POST /api/web/first-open` (o `GET` diz só se ainda está aberto)
  > e `POST /api/web/sign-out`; o nome do aparelho sai do `User-Agent` e do idioma
  > ("Navegador · Chrome no Windows"). Cookie `recam_device` (`HttpOnly`, `Secure`,
  > `SameSite=Strict`, 180 dias com "Lembrar neste computador", sessão sem) lido pelo
  > `DeviceAuthenticationHandler`, que recusa `POST`/`PUT`/`PATCH`/`DELETE` pelo cookie sem
  > `X-Recam-Web: 1`. Saíram o token e o QR do dono, a página e o painel do `/setup` (agora
  > redireciona para `/`) e, com eles, o QRCoder do servidor, que só o QR do log usava (volta no
  > 6.4, no `Recam.Web`). O `reset-owner` não imprime o código, ao contrário do que o bullet
  > pedia: o código só existe na memória do servidor em execução, e o comando roda em outro
  > processo; ele passou a dizer para olhar o log e abrir o endereço no navegador. Navegador:
  > `IRecamApi`/`HttpRecamApi` (manda `X-Recam-Web: 1` sempre), `StartController` e a página
  > inicial com o código, "Lembrar neste computador" (marcado por padrão), erros, "já tem um
  > Monitor", "sem conexão" e a tela provisória "Este navegador é o Monitor" com **Sair**.
  > Observado no Chromium: código errado mostrou o erro; o código certo, digitado em minúsculas,
  > virou Monitor com o cookie como descrito; recarregar manteve; um segundo navegador viu "This
  > server already has a Monitor"; **Sair** apagou o cookie e voltou ao formulário. Testes: domínio
  > do código, nome do navegador, rotas (cookie e atributos, 180 dias, `/api/me` pelo cookie,
  > credencial fora do log, 5 erros trocam o código, com Monitor 409, IP público 403,
  > `X-Forwarded-For` sem proxy confiável 403, proxy confiável usa o IP real, cookie sem cabeçalho
  > 401, sair revoga, `/setup` redireciona), `reset-owner` e bUnit/controller da página. Gate
  > verde (210 testes no servidor e no web, 223 no app). O app ainda fala do QR do servidor na
  > aba Monitor; muda no 6.8.

- [x] **6.3 Câmeras e vídeo ao vivo no navegador**
  - Origem: caminho principal, passos 4 e 5.
  - Escopo: lista de câmeras (online, transmitindo ou parada, bateria, temperatura, gravando) ao
    vivo pelo hub, com o cliente SignalR para .NET no navegador (dependência nova pedida por este
    bullet: `Microsoft.AspNetCore.SignalR.Client` no `Recam.Web`). Tela do vídeo ao vivo com o
    módulo `wwwroot/js/whep.js` próprio (só recepção), lease pelo hub, lanterna e a chave
    **Gravar sempre**. O servidor aceita o cookie no hub e no WHEP.
  - Aceite: testes dos controllers (lista reage ao hub, abrir e fechar o vídeo abre e fecha o lease,
    lanterna com e sem vídeo), bUnit das telas e teste de integração do hub e do `POST /whep` com o
    cookie.
  > Validação (2026-09-26): código escrito e percorrido no navegador do PC com vídeo de verdade, sem
  > celular. `Recam.Web`: `IDeviceHub`/`SignalRDeviceHub` (cliente SignalR .NET no navegador, cookie
  > do navegador, `X-Recam-Web` nos pedidos HTTP, reconexão sem fim com 1, 2, 4, 8, 16 e 30 s),
  > `CameraListController` (lista pela API, atualizada pelo hub, recarregada a cada reconexão, igual
  > ao app), `LiveController` (lease pelo hub, até 20 tentativas de WHEP enquanto a câmera abre,
  > reconecta sozinho quando o vídeo cai, lanterna pelo que a câmera informa) e o módulo próprio
  > `wwwroot/js/whep.js` (só recepção, oferta com todos os candidatos, `DELETE` da sessão ao sair).
  > Telas: lista com estado, bateria, temperatura, selo "Gravando" e a chave **Gravar sempre** (ou
  > o aviso de sem H.264), status do hub e **Sair**; página `/cameras/{id}` com o vídeo, **Ligar
  > lanterna**/**Desligar lanterna** e **Gravar sempre**. O servidor já aceitava o cookie no hub e no
  > WHEP; ganhou os testes: hub com cookie e cabeçalho, sem cabeçalho recusa, e WHEP com cookie
  > chega ao MediaMTX sem o cookie e sem o `X-Recam-Web`. Observado: MediaMTX 1.21.1 real, um
  > Chromium como câmera falsa publicando VP8 por WHIP com a credencial de câmera (o Chromium daqui
  > não tem H.264) e outro Chromium como Monitor web: a lista mostrou "Conectado" e a câmera; a
  > página da câmera tocou o vídeo (480x270, tempo avançando) pelo proxy; voltar para a lista fechou
  > a sessão no MediaMTX. A lanterna e a lista mudando ao vivo foram cobertas por testes (a câmera
  > falsa não entra no hub). Uma câmera recém-pareada só aparece quando ela conecta ou a lista
  > recarrega, como no app; o 6.4 recarrega ao fechar o QR. Gate verde (232 testes no servidor e no
  > web, 223 no app).

- [x] **6.4 Adicionar câmera no navegador e primeiro uso guiado**
  - Origem: caminho principal, passos 2 e 3.
  - Escopo: **Adicionar câmera** no navegador, com o QR em SVG pelo QRCoder (a mesma dependência
    do servidor, agora também no `Recam.Web`), o tempo que falta e a espera pelo uso do token
    (`GET /api/pairing-tokens/{id}`). Sem nenhuma câmera, a tela inicial é esse QR com o passo a
    passo (baixar o app, abrir, tocar em Ler QR code, ler, dar um nome). Quando a primeira câmera
    pareia, o vídeo ao vivo dela abre sozinho. O QR sai com `r=camera`.
  - Aceite: testes do controller (espera, expiração, abrir o vídeo da primeira câmera) e bUnit do
    passo a passo em `en` e `pt`.
  > Validação (2026-09-26): código escrito e percorrido no navegador do PC, sem celular (a leitura do
  > QR pelo app fica para o 6.10). `AddDeviceController` (mesmo comportamento do app: cria o token,
  > conta o tempo, troca o QR quando expira, pergunta a cada 2 s se foi usado) e o componente
  > `AddDevicePanel`, usado na página `/add-camera` (botão **Adicionar câmera** na lista) e, sem
  > nenhuma câmera, como primeiro uso guiado na página inicial ("Adicione sua primeira câmera", quatro
  > passos, QR, "Expira em", código em texto para o **Colar código**). O QR é desenhado em SVG a
  > partir da matriz do QRCoder, só com atributos, porque a CSP bloqueia `style` inline; o tempo que
  > falta é medido pelo relógio do servidor (cabeçalho `Date`, menos 1 s). Quando a primeira câmera
  > pareia, a lista recarrega e o vídeo dela abre sozinho; nas seguintes, volta para a lista.
  > Observado no Chromium: passo a passo em pt, "Expira em 9:59", QR com `r=camera`; um "celular"
  > pareou pela API com o token do QR e a página foi sozinha para `/cameras/{id}`; pelo **Adicionar
  > câmera**, a segunda câmera pareou e a lista mostrou as duas. O QR da captura de tela foi lido com
  > o OpenCV e deu o `recam://pair` certo. Testes: controller (QR, contagem, troca ao expirar,
  > pareado, sem servidor), SVG sem `style` e bUnit do primeiro uso (abre o vídeo sozinho) e da
  > página de adicionar. Gate verde (240 testes no servidor e no web, 223 no app).

- [x] **6.5 Gravações no navegador**
  - Origem: Monitor completo (ADR 0038).
  - Escopo: dias, barra das 24 h e reprodução com `<video>` direto nos segmentos (com `Range` e
    o cookie), passando para o próximo sozinho; **Espaço para gravações** com a mesma regra do
    app. Usa as rotas da Fase 3, sem mudar o servidor.
  - Aceite: testes do controller (dias locais a partir dos UTC, tocar a partir de um ponto,
    próximo segmento, cota) e bUnit da barra.
  > Validação (2026-09-26): código escrito e percorrido no navegador do PC com arquivos gravados de
  > verdade, sem celular. `TimelineController` (dias locais a partir dos dias UTC, dois dias UTC por
  > dia local, tocar a partir do ponto clicado ou do próximo arquivo, passar para o próximo sozinho)
  > e `QuotaController` (mínimo de 100 MB, teto no que o disco permite, 300 MB por hora). Página
  > `/cameras/{id}/recordings` com os dias, a barra das 24 h em SVG (só atributos, por causa da
  > CSP; 144 faixas de 10 min clicáveis) e um `<video>` que toca o segmento direto do servidor com
  > o cookie e `#t=segundos` para começar no ponto, sem JavaScript nem relay; página `/recordings`
  > com o espaço. Links "Gravações" no cartão da câmera e no vídeo ao vivo, "Espaço das gravações"
  > na lista. Diferença do app, de propósito: ao abrir, a linha do tempo pula e tira da lista os
  > dias locais que um dia UTC só toca pela borda, sem nada gravado (no fuso de São Paulo, o dia
  > UTC de hoje cria "hoje" vazio quando tudo foi gravado ontem à noite). O app tem o mesmo
  > defeito; virou o bullet 6.8.1. Observado no Chromium com fuso de São Paulo e dois segmentos
  > VP9 em fMP4 gerados pelo FFmpeg (o Chromium daqui não toca H.264): a lista de dias mostrou "25
  > de setembro", clicar no trecho tocou a partir das 21:31, e o segundo arquivo começou sozinho
  > quando o primeiro acabou; a página de espaço mostrou "Em uso: 0,0 de 2,0 GB." e salvou 1,0 GB.
  > Testes: controllers (fuso, dois dias UTC, ponto de início, próximo, cota, falhas) e bUnit das
  > duas páginas. Gate verde (252 testes no servidor e no web, 223 no app).

- [x] **6.6 Aparelhos e Adicionar Monitor no navegador**
  - Origem: Monitor completo (ADR 0038).
  - Escopo: **Aparelhos** (câmeras e Monitores, "Este navegador", remover com confirmação) e
    **Adicionar Monitor**, com o QR `r=viewer` para um celular virar Monitor. Usa as rotas da
    Fase 4.
  - Aceite: testes dos controllers e bUnit das duas telas.
  > Validação (2026-09-26): código escrito e percorrido no navegador do PC, sem celular.
  > `DevicesController` e a página `/devices`: câmeras e Monitores, "Este navegador" sem botão de
  > remover (para isso existe **Sair**), e remover com confirmação na própria linha ("Remover
  > Porta?", com o texto do app), sem `confirm()` do navegador. **Adicionar Monitor** (`/add-monitor`)
  > reusa o painel do 6.4 com o QR `r=viewer` e passos de Monitor, e volta para os aparelhos quando
  > o celular pareia. A lista de câmeras ganhou botões demais no cabeçalho, então a navegação
  > (Câmeras, Gravações, Aparelhos) e o **Sair** foram para a barra do topo, que só aparece quando o
  > navegador é Monitor; o `StartController` virou um só por app, e **Sair** recarrega o app para
  > não sobrar nada na memória. Observado no Chromium: câmera e Monitor pareados pelos QRs em texto
  > do navegador, a página de aparelhos listou os três com "Este navegador" no lugar certo, remover
  > a câmera pediu confirmação e a tirou dos aparelhos e da lista de câmeras. Visto também: sem o
  > MediaMTX rodando, o proxy WHEP responde 500 (exceção de conexão recusada), coisa de antes desta
  > fase. Testes: bUnit dos aparelhos (marca deste navegador, remover confirmado e cancelado), do
  > Adicionar Monitor e da barra do topo. Gate verde (259 testes no servidor e no web, 223 no app).

- [x] **6.7 Conectar navegador pelo celular**
  - Origem: ADR 0038 (segundo navegador, cookie apagado).
  - Escopo: servidor com a entidade `BrowserLink` e as rotas `POST /api/browser-links`,
    `/approve` e `/claim` (`SPECS.md` 3 e 5.5): o QR leva o segredo de aprovação, o navegador guarda
    o de resgate, uso único, 10 minutos; o aparelho criado é `Viewer` com o nome do navegador.
    Navegador: sem cookie e com Monitor existente, a raiz mostra o QR de "Conectar navegador" e
    espera. App: item **Conectar navegador** no menu do Monitor, que lê o QR e aprova.
  - Aceite: testes do domínio e de integração (aprovar só Monitor, resgate só com o segredo certo,
    expirado, usado duas vezes), teste do controller no app e bUnit da espera.
  > Validação (2026-09-26): código escrito; servidor e navegador percorridos no PC, a parte do app
  > só em testes (sem celular). Servidor: entidade `BrowserLink` (dois segredos: o de aprovação vai
  > no QR, o de resgate fica no navegador; 10 minutos; aprovar só por Monitor ativo; resgate uma
  > vez, que cria um `Viewer` com o nome do navegador), migração `BrowserLinks`, rotas
  > `POST /api/browser-links` (com o mesmo limite por IP do código), `/approve` e `/claim` (que grava o
  > cookie). Link desconhecido, vencido, usado ou com segredo errado responde 404 igual. QR
  > `recam://connect-browser?v=1&l=<id>&s=<segredo>` (`SPECS.md` 5.2). Navegador: `ConnectController`
  > e o painel no lugar de "já tem um Monitor", com o QR, a contagem, "Lembrar neste computador" e
  > a espera de 2 em 2 s; quando o celular aprova, o navegador vira Monitor sem recarregar. App:
  > `BrowserLinkCode` (parse), `approveBrowserLink` na API, `ConnectBrowserController`, a tela
  > `ConnectBrowserScreen` (instruções, **Ler QR code**, mensagens de pronto, QR errado, vencido e sem
  > conexão) e o item **Conectar navegador** no menu do Monitor, textos nos ARB. Junto: o layout do
  > navegador passou a abrir o hub assim que ele é Monitor, em qualquer página (antes só a lista
  > abria, e o navegador aparecia offline em Aparelhos depois de recarregar ali). Observado no
  > Chromium: o primeiro navegador virou Monitor e pareou um "celular" Monitor pela API; um segundo
  > navegador mostrou "Este servidor já tem um Monitor" com o QR; o QR foi lido da captura de tela
  > com o OpenCV, como uma câmera leria; o "celular" aprovou com a credencial dele (204) e o segundo
  > navegador virou Monitor sozinho, com cookie persistente, e apareceu em Aparelhos. Testes: domínio
  > e rotas do link no servidor, controller e página no navegador, parse, controller e tela no app.
  > Gate verde (273 testes no servidor e no web, 230 no app).

- [x] **6.8 App abre em "Ler QR code"**
  - Origem: ADR 0040.
  - Escopo: a primeira tela do app troca "Filmar"/"Assistir" por **Ler QR code** e **Colar
    código**. O `r` passa a ser obrigatório no parse (`camera` ou `viewer`). QR de câmera pede o
    nome e entra no modo câmera; QR de Monitor pareia com "Monitor". Textos do app que falam do
    `/setup` e do QR do servidor passam a falar do navegador. ARB em `en` e `pt`.
  - Aceite: testes do parse (sem `r`, `r=owner`), do roteamento pelo papel e widget test da
    primeira tela.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho (fica para o 6.10). A
  > primeira tela do app virou "Boas-vindas ao ReCam", com um texto dizendo para abrir o endereço
  > do servidor no navegador e escolher **Adicionar câmera** ou **Adicionar Monitor**, e um único
  > botão **Ler QR code** (o **Colar código** continua dentro do leitor). QR de câmera abre o passo
  > "Dê um nome a esta câmera", com o nome padrão "Câmera" e **Confirmar**, e depois entra no modo
  > câmera; QR de Monitor pareia na hora com o nome "Monitor"; QR que não é do ReCam mostra "Este não
  > é um QR code de pareamento do ReCam." O `QrPayload` passou a exigir `r` com `camera` ou `viewer`
  > (erro `invalidRole`; `owner` não vale mais). Textos revistos em `en` e `pt`: "Filmar"/"Assistir"
  > e as dicas antigas saíram; "não pareado" da aba Monitor, "não pareado" da aba Câmera e as
  > instruções de Adicionar câmera e de Adicionar Monitor falam do navegador e do **Ler QR code**;
  > "Adicionar monitor" virou "Adicionar Monitor", como no glossário; "Recam" virou "ReCam" no erro
  > de QR. Testes: parse (sem `r`, `r=owner`), primeira tela (só um botão, câmera pede nome, Monitor
  > pareia direto, código errado, nome vazio) e o app inteiro no primeiro uso. Gate verde (273 testes
  > no servidor e no web, 232 no app).

- [x] **6.8.1 App: linha do tempo abre num dia com gravação**
  - Origem: achado no 6.5 (2026-09-26).
  - Escopo: um dia UTC cobre partes de dois dias locais. Quando tudo foi gravado de noite (no fuso
    de São Paulo, depois das 21h), a lista de dias do app mostra também "hoje", vazio, e abre nele.
    Ao abrir, pular os dias vazios e tirá-los da lista, como o navegador faz.
  - Aceite: teste do controller com gravação às 00:30 UTC e fuso -3: abre no dia anterior e a lista
    não tem o dia vazio.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho. Ao carregar, o
  > `RecordingTimelineController` abre o dia local mais novo que tem gravação e tira da lista os dias
  > vazios que vêm antes dele, como o navegador faz desde o 6.5; sem nenhuma gravação, a lista fica
  > vazia e a tela mostra "Nenhuma gravação ainda". Testes: gravação às 00:30 UTC com fuso -3 abre
  > o dia 25 às 21:30 sem o dia 26 vazio; sem gravação, nenhum dia. Gate verde (273 no servidor e
  > no web, 234 no app).

- [x] **6.9 README e mensagens do servidor**
  - Origem: caminho principal novo.
  - Escopo: README com a instalação do novo jeito (aceitar o certificado uma vez, o código do
    log), mensagem do log e do `reset-owner` revisadas, e o `SPECS.md` 5.5 e 5.6 conferidos com o
    que ficou.
  - Aceite: o passo a passo do README bate com o caminho principal do `AGENTS.md`.
  > Validação (2026-09-26): só documentação, conferida contra o código. O README deixou de dizer que
  > nada funciona e ensina o caminho novo: subir o compose (Linux ou Docker Desktop com
  > `RECAM_HOST`), ler o código em `docker compose logs server`, abrir `https://<ip>:8443`, aceitar o
  > aviso do certificado uma vez, digitar o código, ler o QR com o app, e depois **Adicionar
  > câmera**, **Adicionar Monitor**, **Conectar navegador** e o `reset-owner`; bate com os passos do
  > caminho principal do `AGENTS.md`. As mensagens do servidor já tinham sido revistas no 6.2 (a
  > linha do código no log e a saída do `reset-owner`); nenhuma outra fala de `/setup` ou do QR do
  > dono. `SPECS.md` 5.5 e 5.6 foram reescritos com as rotas e os métodos do hub que existem hoje
  > (conferidos com os `Map*` do servidor e o `IDeviceClient`), no lugar das tabelas da fase 1.

- [ ] **6.10 [aparelho] Validar o novo caminho principal**
  - Origem: substitui o 1.14.
  - Escopo: servidor no PC, navegador no PC, Samsung A10 como câmera. Percorrer os passos 1 a 5 do
    caminho principal do `AGENTS.md`. Depois o Redmi 6A pelo **Adicionar Monitor**, e o
    **Conectar navegador** num segundo navegador.
  - Aceite: os passos funcionam. Anotar o atraso percebido no navegador, a temperatura da câmera
    depois de 30 min e qualquer falha, que vira bullet novo.
  > Bloqueado (2026-09-26): aguardando aparelho. Precisa do autor com o PC, o Samsung A10 e o Redmi
  > 6A; o agente percorreu no Chromium tudo o que não depende de celular (código, lista, vídeo ao
  > vivo com câmera falsa, adicionar câmera e Monitor, gravações, aparelhos, conectar navegador).

## Fase 7: ver melhor e achar movimento

Decidido com o autor em 2026-09-25, logo depois da Fase 6. Vale no app e no navegador.

- [x] **7.1 Clarear a imagem no Monitor**
  - Origem: pedido do autor em 2026-09-25 (ver melhor com pouca luz).
  - Escopo: botão "Clarear" no vídeo ao vivo e no player da linha do tempo, no app e no
    navegador (no navegador, filtro CSS no `<video>`). Liga um filtro de
    brilho e contraste só na tela de quem assiste (`ColorFiltered` com matriz de cor), com dois
    controles deslizantes e um "Voltar ao normal". Não muda a câmera, o envio nem a gravação; cada
    Monitor tem o seu. O ajuste fica guardado por câmera no próprio Monitor. Textos nos ARB e nos
    `.resx`.
    Sem servidor e sem dependência nova.
  - Aceite: teste do controller (ligar, ajustar, voltar ao normal, lembrar por câmera) e widget
    test do filtro aplicado no vídeo ao vivo, no app e no navegador (bUnit).
  > Validação (2026-09-26): código escrito; no navegador, percorrido no Chromium com vídeo de
  > verdade; no app, só código e testes (sem celular). Brilho de 100% a 300% e contraste de 50% a
  > 200%, só na tela de quem assiste, guardados por câmera no próprio Monitor, com **Voltar ao
  > normal**. App: `ImageAdjustment` (matriz de cor: contraste em volta do cinza médio, depois
  > brilho), `AdjustmentStore` no `flutter_secure_storage` que o app já usa (sem dependência nova),
  > `BrightenController`, botão **Clarear** na barra do vídeo ao vivo e da linha do tempo, painel
  > com os dois controles e o vídeo embrulhado num `ColorFiltered`. Navegador: o filtro CSS vai
  > pelo CSSOM a partir do módulo `wwwroot/js/brighten.js`, porque a CSP bloqueia `style` inline, e o
  > ajuste fica no `localStorage`; `BrightenController` e o componente `BrightenControls` no vídeo
  > ao vivo e no player das gravações (reaplicado a cada arquivo). Observado no Chromium: câmera
  > falsa publicando no MediaMTX real, vídeo ao vivo tocando; **Clarear** abriu os controles, e com
  > 200% e 130% o `<video>` ficou com `brightness(2) contrast(1.3)` sem erro de CSP; depois de
  > recarregar a página o filtro voltou sozinho. Testes: controller, codificação e widget/bUnit
  > (abre, filtra, volta ao normal, guarda por câmera) no app e no navegador. Gate verde (276 no
  > servidor e no web, 240 no app).

- [ ] **7.2 Detectar movimento nas gravações**
  - Origem: pedido do autor em 2026-09-25 (achar os momentos importantes sem assistir horas).
  - Escopo: só câmeras com "Gravar sempre". Desenho, sem o .NET processar vídeo:
    - Serviço `motion` novo nos dois composes, com a imagem FFmpeg que os testes já usam
      (`linuxserver/ffmpeg`, mesma tag exata), montando `recam-recordings`. Um script em loop
      pega cada segmento já fechado (nunca o mais novo de cada câmera, a mesma regra da limpeza),
      roda o FFmpeg a 2 quadros por segundo em 160 px de largura com a nota de mudança de cena
      (`scene`) e grava ao lado do `.mp4` um arquivo pequeno com a nota de cada quadro.
    - O servidor lê essas notas e monta os eventos (início, fim, pico) com a sensibilidade da
      câmera (baixa, média, alta; padrão média), emendando mudanças a menos de 10 s. Mudar a
      sensibilidade vale também para o que já foi gravado, sem reprocessar. Os segundos logo
      depois de ligar ou desligar a lanterna são ignorados.
    - `GET /api/cameras/{id}/motion?day=` (Monitores) e a sensibilidade no `Device`, trocada por
      um Monitor. A limpeza da cota apaga as notas junto com o segmento.
    - Atraso esperado: o evento aparece quando o segmento de 60 s fecha, não na hora.
  - Fora: pessoas e carros com IA, zonas e notificação.
  - Aceite: teste do domínio (notas para eventos em cada sensibilidade, lanterna ignorada),
    teste do endpoint e teste com Testcontainers rodando o script do `motion` num segmento gravado
    com movimento e noutro parado. Revisão do `SPECS.md` (seções 2.4 e 7) e ADR.

- [ ] **7.3 Movimento na linha do tempo**
  - Origem: continuação do 7.2.
  - Escopo: na tela de gravações, os eventos de movimento aparecem destacados na barra das 24 h,
    com botões "Movimento anterior" e "Próximo movimento" que tocam a partir de alguns segundos
    antes do evento. Um filtro "Só movimento" e a escolha da sensibilidade da câmera na mesma
    tela, no app e no navegador. Textos nos ARB e nos `.resx`.
  - Aceite: testes dos controllers (navegar entre eventos, filtro, trocar sensibilidade) e testes
    das marcas na barra (widget test no app, bUnit no navegador).

## Fase 5: distribuição

- [x] **5.1 Modo atrás de proxy reverso**
  - Escopo: `RECAM_TLS=off` faz o Kestrel servir HTTP. Respeitar `X-Forwarded-*` de proxies
    configurados. O QR sai sem `f`, e o app valida pelas CAs do sistema.
  - Aceite: testes do QR sem fingerprint e do middleware de forwarded headers.
  > Validação (2026-09-25): só código escrito. Não percorrido atrás de um proxy de verdade. Gate verde
  > (187 no servidor, 223 no app; o servidor rodou 5 vezes seguidas sem falha). `ServerSettings`
  > ganhou `RECAM_TLS` (on/off; outro valor para o servidor na partida) e `RECAM_TRUSTED_PROXIES`
  > (IPs ou redes). Com TLS desligado, o Kestrel escuta HTTP na 8443, nenhum certificado é criado, e
  > um `CertificatePin(null)` substitui o `ServerCertificate` onde o QR é montado (`OwnerSetup` e
  > pareamento), então o `f` some; o `healthcheck` também passa a usar `http`. O `ForwardedHeaders`
  > (For, Proto, Host) só entra com proxies configurados e limpa as redes que o ASP.NET confia por
  > padrão. Chaves novas no `.env.example` e nos dois composes. No app nada mudou: QR sem `f` já não
  > pinava e ficava com as CAs do sistema; ganhou um teste disso. Revisão no `SPECS.md` 12. Testes:
  > QR do primeiro Monitor e de câmera sem `f` com TLS desligado, com `f` no padrão, `/setup` atrás
  > de proxy confiável (cliente local passa, cliente externo não), proxy não configurado recusado,
  > valor inválido em `RECAM_TRUSTED_PROXIES`. Observado uma vez, antes deste bullet ficar pronto,
  > uma falha isolada ao abrir o SQLite no teste da lanterna; não se repetiu em 8 rodadas.

- [ ] **5.2 CI no GitHub Actions** (repositório criado: `jpemendonca/ReCam`, privado até o lançamento)
  - Escopo: workflow rodando `scripts/gate.sh` em pull request e push na `main`, com relatório
    de cobertura do servidor (`coverlet`). Selos de build e cobertura no `README.md`.
  - Aceite: workflow verde num PR e selos aparecendo no README.
  > Em andamento (2026-09-25): existem `.github/workflows/ci.yml` (gate em pull request e em push na
  > `main`, com .NET pelo `global.json`, Flutter 3.47.4 e o Docker do runner para os Testcontainers;
  > depois a cobertura do servidor com `coverlet.MTP` 10.0.1, só do assembly `Recam.Server`, sem as
  > migrações, em Cobertura, com o percentual no resumo do job), `scripts/coverage-badge.sh` (gera o
  > SVG do selo a partir do Cobertura, sem dependência nova) e um job que, em push na `main`, publica
  > o selo sozinho no branch `badges`. Selos de build e de cobertura no topo do `README.md`.
  > Localmente: 94% das linhas do servidor. Falta ver o workflow verde no GitHub.
  > Bloqueado (2026-09-25): o workflow rodou verde no push da `main` (run 36186027715: gate em 5 min,
  > cobertura de 94%, e o job `badge` publicou o `coverage.svg` no branch `badges`). Falta a metade
  > do aceite que não dá para fazer daqui: um PR verde (o autor pediu trabalho direto na `main`,
  > sem PR) e ver os selos renderizados no README. O selo de cobertura passou a apontar para
  > `github.com/<dono>/ReCam/raw/badges/coverage.svg`, que redireciona com a sessão de quem tem
  > acesso enquanto o repositório for privado. Para fechar: abrir qualquer PR, conferir o check
  > `CI / gate` verde e os dois selos na página do repositório.

- [x] **5.2.1 Observabilidade com OpenTelemetry**
  - Origem: vitrine de portfólio.
  - Escopo: pacotes `OpenTelemetry.Extensions.Hosting` e instrumentações de ASP.NET Core e
    HttpClient. Métricas próprias: câmeras online, câmeras publicando, visualizações ativas.
    Exportação OTLP ligada só com `OTEL_EXPORTER_OTLP_ENDPOINT` definida (chave vazia no
    `.env.example`). Serviço opcional do Aspire Dashboard num `deploy/compose.observability.yaml`.
  - Aceite: teste das métricas com `MeterListener`. Com o compose de observabilidade, o painel
    mostra traces de `/api/cameras` e a métrica de câmeras online.
  > Validação (2026-09-25): código escrito e caminho percorrido no PC, sem celular.
  > `Infrastructure/Observability` liga traces e métricas de ASP.NET Core e HttpClient (o proxy
  > WHIP/WHEP) e o meter `Recam.Server` com três medidores observáveis lidos do `DevicePresence`:
  > `recam.cameras.online`, `recam.cameras.publishing`, `recam.views.active`. O `DevicePresence`
  > passou a separar as câmeras dos Monitores online. OTLP só com `OTEL_EXPORTER_OTLP_ENDPOINT`
  > (chave vazia no `.env.example`, repassada pelos dois composes). `deploy/compose.observability.yaml`
  > soma o Aspire Dashboard 9.5.2 (tag exata mais nova no MCR), painel e OTLP presos ao loopback.
  > Testes: `RecamMetricsTests` (com `MeterListener`, filtrando o meter pelo `IMeterFactory` do
  > servidor do teste: câmera online, publicando e assistida dá 1/1/1, uma câmera offline pareada não
  > conta, e tudo volta a 0 quando a câmera cai) e o contador no `DevicePresenceTests`. Observado:
  > com o dashboard em container e o servidor rodando com a variável, o painel listou os traces de
  > `GET /api/cameras` e `/hubs/devices`, e o gráfico de `recam.cameras.online` subiu de 0 para 1
  > quando uma câmera pareada pela API abriu a conexão do hub. O compose completo não foi subido
  > aqui (a imagem do servidor não compila neste ambiente); o `docker compose config` com os dois
  > arquivos resolve o endpoint para `http://aspire-dashboard:18889`. Gate verde (190 testes no
  > servidor, 223 no app).

- [x] **5.2.2 ADRs**
  - Origem: vitrine de portfólio.
  - Escopo: `docs/adr/`, um arquivo por decisão do log do `SPECS.md` 12 (contexto, decisão,
    consequências), em português. O `SPECS.md` 12 passa a apontar para os ADRs.
  - Aceite: todo item do log tem ADR correspondente.
  > Validação (2026-09-25): só documentação. `docs/adr/` tem 36 ADRs, um por item do log: as 16
  > decisões iniciais (0001 a 0016) e as 20 revisões datadas, na ordem em que aparecem (0017 a 0036,
  > a última sendo a do OpenTelemetry do 5.2.1). Cada um tem data, origem, contexto, decisão e
  > consequências, e os que foram mudados depois apontam para o ADR que os mudou (a transmissão sob
  > demanda aponta para "Gravar sempre", por exemplo). `docs/adr/README.md` é o índice. O `SPECS.md` 12
  > abre dizendo onde estão e como a numeração segue o log, e a regra de revisão passou a pedir o
  > ADR seguinte. O mapa do `AGENTS.md` ganhou a pasta. Conferido: 16 itens `- **` mais 20
  > `> Revisão` no log, 36 arquivos numerados. Gate verde.

- [ ] **5.2.3 [aparelho] Vitrine do README**
  - Origem: vitrine de portfólio.
  - Escopo: GIF curto do caminho principal (código no navegador, ler o QR com o celular, vídeo
    ao vivo no navegador, lanterna) e diagrama da arquitetura no `README.md`. Revisado em
    2026-09-25 para o caminho novo da Fase 6.
  - Aceite: README mostra o GIF gravado nos aparelhos reais.

- [ ] **5.3 Imagem publicada no GHCR e instalação em um comando**
  - Escopo: workflow que publica `ghcr.io/<dono>/recam-server` em tag `v*`. Os composes usam a
    imagem publicada. O README ensina a instalar baixando só a pasta `deploy/`.
  - Aceite: instalação do zero numa máquina Linux seguindo só o README.

- [ ] **5.4 APK de release assinado** (depende do usuário gerar o keystore e cadastrar os secrets)
  - Escopo: build de release com `--split-per-abi` (armeabi-v7a e arm64-v8a) publicada no
    GitHub Releases.
  - Aceite: APK de release instalado no A10 e no 6A.

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
- Detecção de pessoas e carros com IA, marcada na linha do tempo (conversa de 2026-09-25).
  Opcional na instalação, num compose à parte como o de observabilidade, porque pesa no PC:
  quem não quiser não sobe. Rodaria no PC (nunca no celular), num container próprio com um modelo
  pequeno (via ONNX Runtime, possivelmente em .NET), só nos momentos que o 7.2 já marcou como
  movimento. Precisa revisar a regra "o .NET não processa vídeo" para valer só no servidor
  principal. Zonas (ignorar rua e calçada) entram junto ou logo depois.
- Modo noturno (conversa de 2026-09-25): botão no Monitor que manda a câmera aceitar de 5 a 15
  quadros por segundo em vez de 15 fixos, para expor por mais tempo no escuro. Vale para todos os
  Monitores e para a gravação, como a lanterna. Antes do botão, testar no A10 e no 6A se o
  `flutter_webrtc` repassa a faixa de quadros para a câmera; se não repassar, exige captura nativa.
