# Recam: roadmap

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
  que o autor precisa saber defender em entrevista.
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
  > (o 3.2 libera `viewer`); o app conta o tempo restante a partir da hora do servidor, lida do
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

- [ ] **1.6.4 Link `recam://pair` abre o app e pareia na aba certa**
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

- [ ] **1.6.5 Script de desenvolvimento em um comando**
  - Origem: pedido do autor (2026-09-25): não ficar rodando comando e copiando código.
  - Escopo: `scripts/dev.ps1`. Sobe o servidor (`-Reset` zera os dados antes), espera o código
    do dono no log, liga o emulador se nenhum estiver rodando, compila e instala o APK debug no
    emulador e em todo celular conectado por USB, e manda o link do dono para o emulador com
    `adb`. No fim, imprime em PT-BR o que fazer no celular câmera. Documentar no `README.md`.
  - Aceite: `./scripts/dev.ps1 -Reset` numa máquina com o emulador criado termina com o
    emulador pareado como dono, sem nenhum outro comando.

- [ ] **1.6.2 [aparelho] Validar pareamento com emulador como dono e celular como câmera**
  - Origem: nova forma de teste (1.6.1).
  - Escopo: `./scripts/dev.ps1 -Reset` (1.6.5) deixa o emulador pareado como dono. O emulador abre
    "Adicionar câmera". O A10 lê esse QR na tela do PC pela aba Câmera.
  - Aceite: o emulador mostra "pareado" como dono e o A10 mostra "pareado" como câmera.

- [ ] **1.7 [junto] Servidor: hub, presença e telemetria**
  - Origem: caminho principal, passo 5.
  - Escopo: `Features/Realtime/DeviceHub.cs` em `/hubs/devices`, autenticado por
    `access_token`. Presença em memória (online e offline, `LastSeenAt`). `ReportTelemetry`
    grava no `Device` e emite `CameraStatusChanged`. `Features/Devices/`: `GET /api/cameras`.
  - Aceite: testes com cliente SignalR real contra a `WebApplicationFactory`:
    `ReportTelemetry_FromCamera_NotifiesViewers`, `ReportTelemetry_FromViewer_IsRejected`,
    `Cameras_AfterCameraDisconnects_ShowsOffline`.

- [ ] **1.8 App: modo câmera ocioso**
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

- [ ] **1.9 App: lista de câmeras na aba Assistir**
  - Origem: caminho principal, passo 5.
  - Escopo: `viewer/`: lista via `GET /api/cameras`, atualizada por `CameraStatusChanged`.
    Cada item mostra nome, online ou offline e bateria com o ícone de carregando. Lista vazia
    com o botão "Adicionar câmera".
  - Aceite: teste `CameraListController` (aplica `CameraStatusChanged` sobre a lista inicial,
    com fakes).

- [ ] **1.10 [junto] Servidor: proxy WHIP/WHEP e transmissão sob demanda**
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

- [ ] **1.11 [opus] App: câmera transmite por WHIP**
  - Origem: caminho principal, passo 6.
  - Escopo: dependência `flutter_webrtc`. Interface `WebRtcPublisher` em `core/media/` e a
    implementação com WHIP (POST do offer, PATCH de ICE, DELETE ao parar). Captura conforme
    `SPECS.md` 2.3: 1280x720, 15 fps, H.264 preferido, sem áudio, 700 kbps. `StartPublishing`
    abre, `StopPublishing` fecha e libera a câmera. `ReportPublishing` nos dois casos.
  - Aceite: teste `CameraModeController` (com `WebRtcPublisher` fake, `StartPublishing` inicia e
    reporta, `StopPublishing` para e reporta).

- [ ] **1.12 [opus] App: vídeo ao vivo na aba Assistir**
  - Origem: caminho principal, passo 6.
  - Escopo: interface `WebRtcViewer` em `core/media/` e a implementação com WHEP. Tela de vídeo
    ao vivo com `RTCVideoView`. Chama `WatchCamera` ao abrir e `UnwatchCamera` ao sair. Tenta o
    WHEP a cada 1 s, até 10 vezes, enquanto a câmera não reporta que está publicando. Mostra
    "conectando" e erro com botão de tentar de novo.
  - Aceite: teste `LiveViewController` (com fakes: abre lease, espera publicação, conecta e
    fecha o lease no dispose).

- [ ] **1.13 Lanterna**
  - Origem: caminho principal, passo 7.
  - Escopo: hub: `SetTorch` e `ReportTorch` conforme `SPECS.md` 5.6. App câmera: aplica com
    `Helper.setTorch` na trilha de vídeo ativa e reporta. App visualizador: botão de lanterna na
    tela ao vivo, refletindo `TorchChanged`.
  - Aceite: testes `SetTorch_WhenCameraNotPublishing_ReturnsError`,
    `SetTorch_WhenPublishing_ForwardsToCamera` e o controller da câmera aplicando a lanterna
    via fake.

- [ ] **1.14 [aparelho] Validar o caminho principal completo**
  - Origem: definição do MVP.
  - Escopo: servidor no PC. A10 ou 7A como câmera, emulador Android no PC como visualizador
    (revisado em 2026-09-25; antes era um segundo celular). Percorrer os passos 1 a 7 do caminho
    principal em `AGENTS.md`.
  - Aceite: os 7 passos funcionam. Anotar o atraso percebido, a temperatura do celular câmera
    depois de 30 min assistindo e qualquer falha, que vira bullet novo.

## Fase 2: câmera que aguenta ficar ligada

- [ ] **2.1 Aparelho sem H.264 em hardware**
  - Origem: `SPECS.md` 11.
  - Escopo: antes de iniciar o modo câmera, verificar se o `flutter_webrtc` oferece H.264 para
    envio. Se não oferecer, mostrar mensagem clara (ARB) e não entrar no modo câmera.
  - Aceite: teste do controller com fake retornando "sem H.264".

- [ ] **2.2 Telemetria de temperatura**
  - Origem: `SPECS.md` 11.
  - Escopo: method channel no Android lendo `BatteryManager.EXTRA_TEMPERATURE`. Campo
    `temperatureC` na telemetria, no `Device`, no `CameraStatusDto` e na lista de câmeras.
    Migração nova.
  - Aceite: testes de servidor e do controller com fake.

- [ ] **2.3 Redução automática de qualidade por calor**
  - Origem: `SPECS.md` 11.
  - Escopo: com temperatura ≥ 42 °C, a câmera passa para 854x480 a 10 fps e 400 kbps. Volta a
    720p abaixo de 38 °C.
  - Aceite: teste da regra de histerese como função pura.

- [ ] **2.4 Tela guiada de otimização de bateria**
  - Origem: `SPECS.md` 11.
  - Escopo: ao iniciar o modo câmera pela primeira vez, verificar
    `isIgnoringBatteryOptimizations`. Se não estiver liberado, mostrar passos por fabricante
    (Samsung, Xiaomi, genérico) com botão que abre a tela de configuração certa.
  - Aceite: teste do controller escolhendo as instruções pelo fabricante.

- [ ] **2.5 [aparelho] Validar 24 horas ligado**
  - Origem: robustez.
  - Escopo: A10 e 7A em modo câmera por 24 h, na tomada, com sessões de visualização
    espalhadas.
  - Aceite: nenhum dos dois cai sem reconectar. Anotar a temperatura máxima e as falhas.

## Fase 3: gestão de aparelhos

- [ ] **3.1 Revogar aparelhos**
  - Escopo: `GET /api/devices` e `DELETE /api/devices/{id}` (`OwnerOnly`, derruba a conexão do
    hub). Tela de aparelhos na aba Assistir.
  - Aceite: `DeleteDevice_AsOwner_RevokesAndDisconnects` e o teste do controller.

- [ ] **3.2 Parear outro celular visualizador**
  - Escopo: `POST /api/pairing-tokens` aceita `role: "viewer"`. Botão "Adicionar visualizador".
  - Aceite: `CreatePairingToken_ForViewer_PairsAsViewer`.

- [ ] **3.3 Resetar o dono**
  - Escopo: `docker compose exec server ./Recam.Server reset-owner` revoga o dono atual e
    volta a gerar o token do dono.
  - Aceite: `ResetOwner_WithOwner_RevokesAndCreatesSetupToken`.

## Fase 4: distribuição

- [ ] **4.1 Modo atrás de proxy reverso**
  - Escopo: `RECAM_TLS=off` faz o Kestrel servir HTTP. Respeitar `X-Forwarded-*` de proxies
    configurados. O QR sai sem `f`, e o app valida pelas CAs do sistema.
  - Aceite: testes do QR sem fingerprint e do middleware de forwarded headers.

- [ ] **4.2 CI no GitHub Actions** (depende do usuário criar o repositório no GitHub)
  - Escopo: workflow rodando `scripts/gate.sh` em pull request e push na `main`, com relatório
    de cobertura do servidor (`coverlet`). Selos de build e cobertura no `README.md`.
  - Aceite: workflow verde num PR e selos aparecendo no README.

- [ ] **4.2.1 Observabilidade com OpenTelemetry**
  - Origem: vitrine de portfólio.
  - Escopo: pacotes `OpenTelemetry.Extensions.Hosting` e instrumentações de ASP.NET Core e
    HttpClient. Métricas próprias: câmeras online, câmeras publicando, visualizações ativas.
    Exportação OTLP ligada só com `OTEL_EXPORTER_OTLP_ENDPOINT` definida (chave vazia no
    `.env.example`). Serviço opcional do Aspire Dashboard num `deploy/compose.observability.yaml`.
  - Aceite: teste das métricas com `MeterListener`. Com o compose de observabilidade, o painel
    mostra traces de `/api/cameras` e a métrica de câmeras online.

- [ ] **4.2.2 ADRs**
  - Origem: vitrine de portfólio.
  - Escopo: `docs/adr/`, um arquivo por decisão do log do `SPECS.md` 12 (contexto, decisão,
    consequências), em português. O `SPECS.md` 12 passa a apontar para os ADRs.
  - Aceite: todo item do log tem ADR correspondente.

- [ ] **4.2.3 [aparelho] Vitrine do README**
  - Origem: vitrine de portfólio.
  - Escopo: GIF curto do caminho principal (parear, ver ao vivo, lanterna) e diagrama da
    arquitetura no `README.md`.
  - Aceite: README mostra o GIF gravado nos aparelhos reais.

- [ ] **4.3 Imagem publicada no GHCR e instalação em um comando**
  - Escopo: workflow que publica `ghcr.io/<dono>/recam-server` em tag `v*`. Os composes usam a
    imagem publicada. O README ensina a instalar baixando só a pasta `deploy/`.
  - Aceite: instalação do zero numa máquina Linux seguindo só o README.

- [ ] **4.4 APK de release assinado** (depende do usuário gerar o keystore e cadastrar os secrets)
  - Escopo: build de release com `--split-per-abi` (armeabi-v7a e arm64-v8a) publicada no
    GitHub Releases.
  - Aceite: APK de release instalado no A10 e no 7A.

- [ ] **4.5 Contribuição e CLA** (depende do usuário escolher o texto do CLA e instalar o CLA Assistant)
  - Escopo: `CONTRIBUTING.md` (como rodar, gate, Conventional Commits, CLA obrigatório) e o
    documento do CLA.
  - Aceite: antes do primeiro pull request externo, o CLA Assistant pede o aceite num PR de
    teste.

---

## Fora da fila (anotado, não executar)

Itens que dependem de decisão futura. O loop para antes daqui.

- Gravação no servidor com cota de disco por barrinha, apagando o mais antigo.
- Acesso fora da rede local.
- Detecção de movimento e notificações.
- iOS.
- Fallback por TCP ou HLS.
- Publicação nas lojas de apps de servidor caseiro (Umbrel, CasaOS, Unraid, Synology).
