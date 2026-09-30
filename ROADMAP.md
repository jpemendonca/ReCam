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
  do servidor, que o autor quis entender e decidir de perto. Desde 2026-09-25 o autor não acompanha mais:
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
A Fase 11 (Monitor no iPhone pelo navegador), anotada em 2026-09-27, também fica antes da Fase 5.
Na branch `claude/person-activity-detection-4pq7pg`, aberta em 2026-09-28, a fila começa na
Fase 12 (detecção de pessoas). Ela pode não ir para a `main`.

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

- [x] **2.5 [aparelho] Validar 24 horas ligado**
  - Origem: robustez.
  - Escopo: A10 e 6A em modo câmera por 24 h, na tomada, com sessões de visualização
    espalhadas.
  - Aceite: nenhum dos dois cai sem reconectar. Anotar a temperatura máxima e as falhas.
  > Bloqueado (2026-09-25): aguardando aparelho. São 24 h com o Samsung A10 e o Redmi 6A em modo
  > câmera, na tomada; o agente não tem os celulares.
  > Validação (2026-09-27): o autor percorreu no Samsung A10, no Redmi 6A e no navegador, com o
  > servidor na VPS, cerca de 16 horas seguidas (ao vivo, som, lanterna, gravação, linha do tempo,
  > movimento, bateria e temperatura). Tudo funcionando.

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

- [x] **3.6 [aparelho] Validar a gravação**
  - Origem: combinado com o autor em 2026-09-25.
  - Escopo: Samsung A10 e Redmi 6A. Uma câmera gravando por 2 h, com cota pequena (ex.: 300 MB) para
    ver a limpeza; linha do tempo e reprodução no Monitor.
  - Aceite: a gravação aparece na linha do tempo e toca, e o mais antigo some quando a cota enche.
    Anotar temperatura e bateria da câmera gravando.
  > Bloqueado (2026-09-25): aguardando aparelho. São 2 h de gravação com o Samsung A10 e o Redmi 6A e
  > o servidor no PC; o agente não tem os celulares.
  > Validação (2026-09-27): o autor percorreu no Samsung A10, no Redmi 6A e no navegador, com o
  > servidor na VPS, cerca de 16 horas seguidas (ao vivo, som, lanterna, gravação, linha do tempo,
  > movimento, bateria e temperatura). Tudo funcionando.

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

- [x] **6.10 [aparelho] Validar o novo caminho principal**
  - Origem: substitui o 1.14.
  - Escopo: servidor no PC, navegador no PC, Samsung A10 como câmera. Percorrer os passos 1 a 5 do
    caminho principal do `AGENTS.md`. Depois o Redmi 6A pelo **Adicionar Monitor**, e o
    **Conectar navegador** num segundo navegador.
  - Aceite: os passos funcionam. Anotar o atraso percebido no navegador, a temperatura da câmera
    depois de 30 min e qualquer falha, que vira bullet novo.
  > Bloqueado (2026-09-26): aguardando aparelho. Precisa do autor com o PC, o Samsung A10 e o Redmi
  > 6A; o agente percorreu no Chromium tudo o que não depende de celular (código, lista, vídeo ao
  > vivo com câmera falsa, adicionar câmera e Monitor, gravações, aparelhos, conectar navegador).
  > Validação (2026-09-27): o autor percorreu no Samsung A10, no Redmi 6A e no navegador, com o
  > servidor na VPS, cerca de 16 horas seguidas (ao vivo, som, lanterna, gravação, linha do tempo,
  > movimento, bateria e temperatura). Tudo funcionando.

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

- [x] **7.2 Detectar movimento nas gravações**
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
  > Validação (2026-09-26): código escrito; o script rodou de verdade no container do FFmpeg, sem
  > celular. Mudança no desenho, registrada no `SPECS.md` 12 e no ADR 0041: a nota `scene` do
  > FFmpeg não serve (medida em cenas de teste, é feita para corte de cena e deu o mesmo para uma
  > forma andando e para a cena parada). O `deploy/motion.sh` mede a fração da imagem que mudou
  > mais de 30 de 255 níveis entre meios segundos (ruído do sensor: 0; forma do tamanho de uma
  > pessoa andando: cerca de 5%). Serviço `motion` nos dois composes (mesma imagem e tag dos testes,
  > usuário 1654, sem rede, script só leitura). Servidor: `MotionEvents` no domínio (baixa 3%,
  > média 1%, alta 0,3%, emenda a menos de 10 s, ignora 5 s depois de a lanterna mudar),
  > `Device.MotionSensitivity` com `SetMotionSensitivity` (migração `CameraMotion`, câmeras antigas
  > em média), trocas de lanterna guardadas em memória no `DevicePresence` a partir do `ReportTorch`,
  > `RecordingStore.ReadMotion`, a limpeza apagando as notas com o segmento e as órfãs, e as rotas
  > `GET /api/cameras/{id}/motion?day=` e `PUT /api/cameras/{id}/motion-sensitivity`. Testes:
  > domínio, store, rotas (eventos do dia, sensibilidade vale para trás, lanterna ignorada, só
  > Monitor muda, valor obrigatório) e Testcontainers rodando o `motion.sh --once` sobre um segmento
  > parado com ruído e um com a caminhada: o parado não deu evento nem em alta; a caminhada deu um
  > evento de 5 s a 12 s. O compose com o serviço novo foi validado com `docker compose config`, mas
  > não subido (a imagem do servidor não compila neste ambiente). Gate verde (288 no servidor e no
  > web, 240 no app).

- [x] **7.3 Movimento na linha do tempo**
  - Origem: continuação do 7.2.
  - Escopo: na tela de gravações, os eventos de movimento aparecem destacados na barra das 24 h,
    com botões "Movimento anterior" e "Próximo movimento" que tocam a partir de alguns segundos
    antes do evento. Um filtro "Só movimento" e a escolha da sensibilidade da câmera na mesma
    tela, no app e no navegador. Textos nos ARB e nos `.resx`.
  - Aceite: testes dos controllers (navegar entre eventos, filtro, trocar sensibilidade) e testes
    das marcas na barra (widget test no app, bUnit no navegador).
  > Validação (2026-09-26): código escrito e o caminho percorrido no navegador; no app, só testes.
  > Navegador: `TimelineController` carrega os eventos dos mesmos dias UTC da linha do tempo e os
  > põe no relógio do computador; marcas laranja na base da barra (a barra inteira com "Só
  > movimento", que também esconde os trechos gravados e, no fim de um arquivo, segue para o
  > próximo movimento, ou para o arquivo seguinte se o movimento continua nele); "Movimento
  > anterior" e "Próximo movimento" tocam a partir de 5 s antes; seletor de sensibilidade que salva
  > e recarrega os eventos. App: o mesmo no `RecordingTimelineController` (`playingFrom`,
  > `motion`, `onlyMotion`, `setSensitivity`), `ApiClient.motion`/`setMotionSensitivity`, marcas
  > sobre a barra, botões, chave "Só movimento" e `SegmentedButton` de sensibilidade; textos em ARB
  > e resx, en e pt. SPECS 2.4 descreve a tela. Testes: 5 de controller e 2 bUnit no navegador; 5
  > de controller e 1 widget test no app. Observado no Chromium contra o servidor de dev: dois
  > segmentos VP9 com a cena da pessoa andando, pontuados pelo `deploy/motion.sh --once` na imagem
  > real, viraram duas marcas na barra e "Movimentos neste dia: 2"; "Próximo movimento" tocou o
  > primeiro (22:38, `#t=0`, vídeo 640 px andando) e depois o segundo (22:48); "Movimento
  > anterior" voltou ao primeiro; "Só movimento" tirou os trechos gravados e as marcas ocuparam a
  > barra; a sensibilidade trocada para Alta continuou Alta depois de recarregar a página. Falta
  > ver o app num celular (junto com o 6.10).

## Fase 8: ajustes do teste de 2026-09-26

Pedidos do autor depois de testar as fases 3, 6 e 7 no PC, no Samsung A10 e no Redmi 6A. Valem no
app e no navegador quando o bullet não disser outra coisa.

- [x] **8.1 Um papel por celular**
  - Origem: teste do autor em 2026-09-26: o Redmi 6A, pareado como câmera e depois como Monitor,
    aparecia na própria lista como câmera offline, e abrir essa câmera ficava carregando.
  - Escopo: o celular é Câmera ou Monitor, nunca os dois. A barra de abas some; o app mostra só a
    tela do papel dele. Ler um QR do outro papel pergunta "Este celular é uma câmera. Virar
    Monitor?" (e o contrário); confirmando, o app sai do servidor no papel antigo (`DELETE
    /api/me`) e pareia no novo. "Reiniciar o app" continua no menu ⋮ dos dois papéis e volta à
    tela inicial zerada. Revisão no `SPECS.md` 12 (a decisão "um app com duas abas" muda).
  - Aceite: widget tests do app pareado como Câmera e como Monitor sem barra de abas, da troca de
    papel confirmada (sai do servidor e pareia) e cancelada (nada muda).
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho. O `HomeShell` não tem
  > mais barra de abas: mostra a tela da Câmera, a do Monitor ou a primeira abertura, e o título diz
  > o papel ("ReCam · Câmera" / "ReCam · Monitor"). As duas telas continuam montadas por baixo (fora
  > do palco), para a Câmera ver o pareamento terminar e abrir o modo câmera. Trocar de papel: o
  > menu ⋮ dos dois papéis ganhou "Ler QR code"; um QR do outro papel (lido ali ou aberto como link)
  > pergunta "Este celular é uma câmera. Virar Monitor?" (ou o contrário). Confirmando, o app sai do
  > servidor (`DELETE /api/me`, o mesmo do "Reiniciar o app") e abre a primeira abertura já com o
  > código lido: QR de câmera pede o nome, QR de Monitor pareia direto. `PairingRouter.conflictWith`
  > decide se há troca. Celular que já estava pareado nos dois papéis (antes deste bullet, como o
  > Redmi 6A no teste) fica como Monitor ao abrir o app: a câmera sai do servidor e é esquecida
  > (`AppReset.keepOneRole`). Saíram as telas "não pareado" das duas abas, `checkName` e as chaves
  > `cameraNotPaired`/`watchNotPaired`, que ficaram sem chamador. Testes: Monitor e Câmera sem barra
  > de abas, pareado nos dois papéis fica Monitor, troca confirmada nos dois sentidos, troca
  > cancelada, e `conflictWith`.

- [x] **8.2 Abas do Monitor: Câmeras, Gravações, Aparelhos e Configurações**
  - Origem: teste do autor em 2026-09-26.
  - Escopo: no app (Monitor) e no navegador, quatro abas. Câmeras fica como está. Aparelhos fica
    como está. Configurações, por enquanto, só com o espaço de gravação (a tela que hoje se chama
    "Gravações" no menu ⋮), com o texto deixando claro que o valor é o total de todas as câmeras
    e que o espaço fica no servidor, não no celular, e o aviso "com N câmeras gravando, cabem
    cerca de X horas". Gravações é o 8.4. "Adicionar Monitor", "Conectar navegador" e "Reiniciar o
    app" ficam no menu ⋮ do app.
  - Aceite: widget tests das abas no app e testes bUnit (ou o que o `Recam.Web.Tests` já usa) das
    abas no navegador; teste do texto de horas com 1 e com 3 câmeras gravando.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho nem aberto no navegador.
  > App: o Monitor ganhou barra embaixo com Câmeras, Gravações, Aparelhos e Configurações, e a tela
  > de câmeras segura a única conexão com o hub para as quatro abas. Gravações lista as câmeras (com
  > "Gravando" em quem grava) e abre a linha do tempo de cada uma; o 8.4 põe a linha do tempo dentro
  > da própria aba. Aparelhos é a antiga tela do menu, agora sem barra própria (`DevicesView`).
  > Configurações é a antiga tela "Gravações" do menu (`SettingsView`), com o título "Espaço para
  > gravações", a frase "Este é o total de todas as câmeras juntas. As gravações ficam no servidor,
  > não nos celulares." e as horas divididas pelas câmeras que gravam ("Com 2 câmeras gravando,
  > cabem cerca de 3,4 horas"; sem nenhuma gravando, o texto de uma câmera). O menu ⋮ ficou com
  > Adicionar Monitor, Conectar navegador, Ler QR code e Reiniciar o app. Navegador: a página do
  > espaço virou `/settings` ("Configurações", mesma frase e as mesmas horas), e `/recordings` virou
  > a aba Gravações, com um cartão por câmera que abre a linha do tempo. `HoursFor` (app e
  > navegador) recebe o número de câmeras gravando. Testes: abas do Monitor no app, aba Gravações
  > abrindo a linha do tempo, Configurações com a frase do servidor, Aparelhos removendo; no
  > navegador, a barra com Configurações, as horas com 2 câmeras e a lista da aba Gravações.

- [x] **8.3 Espaço de gravação pedido depois da primeira câmera**
  - Origem: teste do autor em 2026-09-26.
  - Escopo: logo depois que a primeira câmera pareia, o Monitor que mostrou o QR (app ou navegador)
    pergunta quanto espaço as gravações podem usar, com 2 GB marcado e o máximo igual ao espaço
    livre no servidor, e o mesmo texto do 8.2 (total, no servidor). Pular mantém 2 GB. Não pergunta
    de novo nas câmeras seguintes.
  - Aceite: testes do fluxo pedindo na primeira câmera e não pedindo na segunda.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho nem aberto no navegador.
  > "Primeira câmera" é a lista vazia antes de adicionar: o Monitor que mostrou o QR sabe disso sem
  > mudar o servidor. Navegador: quando a primeira câmera pareia, a página vai para
  > `/first-space/{câmera}` ("Sua primeira câmera está pronta"), com a mesma barrinha das
  > Configurações (virou o componente `SpaceEditor`, usado pelas duas páginas), 2 GB marcados e o
  > máximo igual ao livre no servidor; "Salvar e continuar" salva e abre o ao vivo da câmera, "Pular
  > (fica em 2 GB)" abre o ao vivo sem salvar. App: depois do QR da primeira câmera fechar, abre a
  > mesma tela (o `SettingsView` com `onDone`), com os mesmos dois botões. Câmeras seguintes não
  > perguntam. Testes: no navegador, a primeira câmera leva ao `/first-space` e salvar abre o ao
  > vivo com a cota salva; no app, a primeira câmera pergunta e salva 2048, a segunda não pergunta.

- [x] **8.4 Aba Gravações**
  - Origem: teste do autor em 2026-09-26: "quero de fato poder ver minhas gravações".
  - Escopo: a aba lista as câmeras com gravação e, ao escolher uma, mostra a linha do tempo dela
    (a mesma do 8.5). O acesso de hoje, pela câmera, continua.
  - Aceite: testes da aba no app e no navegador escolhendo uma câmera e abrindo a linha do tempo.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho nem aberto no navegador. A
  > aba Gravações mostra no topo a escolha da câmera e, embaixo, a linha do tempo dela, a mesma de
  > quando se toca na câmera. Abre na primeira câmera que grava (é a que tem o que ver); na lista,
  > quem grava aparece com "· Gravando". App: o corpo da tela da linha do tempo virou
  > `RecordingsTimelinePane`, usado pela tela própria (com título e "Clarear" na barra) e pela aba
  > (com a escolha da câmera ao lado do "Clarear"). Navegador: o corpo da página virou o componente
  > `TimelineView`, usado pela página `/cameras/{id}/recordings` e pela aba `/recordings`, que troca
  > de câmera por um `select`. Testes: no app, a aba abre na câmera que grava e troca para outra; no
  > navegador, a aba carrega a câmera que grava e depois a escolhida; os testes antigos da linha do
  > tempo seguem valendo.

- [x] **8.5 Linha do tempo com zoom**
  - Origem: teste do autor em 2026-09-26: difícil tocar no momento certo e ver onde teve movimento.
  - Escopo: no app e no navegador, janela visível de 1 min, 15 min, 1 h ou 3 h (abre em 1 h),
    arrastar para os lados para andar no tempo, a hora aparecendo embaixo do dedo ou do mouse
    antes de soltar, marcas de movimento bem visíveis na faixa. No topo, o espaço usado ("Em uso
    1,2 de 2 GB · cabem cerca de 6 h").
  - Aceite: testes do cálculo de posição e hora para cada zoom (função pura), do arrastar e dos
    widgets nos dois lados.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho nem aberto no navegador. A
  > barra deixou de mostrar o dia inteiro: mostra uma janela de 1 min, 15 min, 1 h (padrão) ou 3 h,
  > escolhida em botões acima dela, com "‹ Antes" e "Depois ›" e o intervalo escrito no meio ("14:00
  > – 15:00"; com 1 min, com segundos). A janela abre no que está tocando ou, sem nada tocando, na
  > última gravação do dia, e acompanha a reprodução quando ela pula para fora (próximo movimento,
  > próximo arquivo). Marcas de movimento têm largura mínima para não sumirem. No topo, "Em uso: 0,3
  > de 2 GB · cabem cerca de 6,8 h" (as horas divididas pelas câmeras que gravam). A conta de
  > janela, posição e hora é uma função pura nos dois lados (`TimelineWindow` e `TimelineZoom`, em
  > Dart e em C#), testada por zoom. App: arrastar para os lados anda no tempo, tocar toca, e
  > segurar mostra a hora sob o dedo e toca ao soltar. Navegador, sem JavaScript: a barra tem 60
  > fatias clicáveis em qualquer zoom (1 s a 3 min cada), passar o mouse mostra a hora embaixo da
  > barra (e no balão do navegador), clicar toca, e apertar numa fatia e soltar em outra arrasta.
  > Testes: `TimelineWindow` nos dois lados (centro, borda do dia, posição por zoom, arrastar, zoom
  > mantendo o centro, passo, marcas), e na tela: zoom, setas, arrastar, hora sob o dedo/mouse,
  > espaço em uso e as posições novas das marcas.

- [x] **8.6 Player do app com pausar e avançar**
  - Origem: teste do autor em 2026-09-26: no app só dá para assistir, no navegador dá para pausar e
    avançar.
  - Escopo: no player de gravação do app, pausar, continuar, barra para avançar e voltar dentro do
    trecho, seguindo para o próximo segmento como hoje.
  - Aceite: widget test dos controles com o player falso.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho. Embaixo do vídeo da
  > gravação, no app, ficaram "Voltar 10 segundos", pausar/continuar, "Avançar 10 segundos", uma
  > barra para andar dentro do arquivo que toca e o tempo ("0:12 / 1:00"). Arrastar a barra só move
  > o vídeo ao soltar. Ao chegar no fim do arquivo, segue para o próximo, como antes. O
  > `RecordingPlayer` (em `lib/core/media/`) ganhou `position` (um `ValueListenable` com posição,
  > duração e se toca), `pause`, `resume` e `seekTo`, implementados sobre o `VideoPlayerController`.
  > Teste: pausar troca o ícone e avançar pede +10 s ao player.

- [x] **8.7 Correção: a tela pula para o topo no "Próximo movimento" do navegador**
  - Origem: teste do autor em 2026-09-26.
  - Escopo: no navegador, "Movimento anterior" e "Próximo movimento" trocam o vídeo sem redesenhar a
    página inteira e sem mudar a rolagem.
  - Aceite: teste que confirma que só o player e a posição mudam (sem navegação), e conferência no
    Chromium.
  > Validação (2026-09-26): só código escrito. Não conferido no Chromium: falta uma câmera gravando
  > para ter o que tocar. Causa provável, pelo código: cada arquivo novo trocava o `<video>` inteiro
  > (`@key` pelo endereço), e o vídeo novo nascia com altura zero até carregar; a página encolhia e
  > o navegador voltava para cima. Agora o mesmo `<video>` recebe o arquivo novo (sem `@key`) e tem
  > caixa fixa (`aspect-ratio: 16 / 9`, `object-fit: contain`), então a altura não muda enquanto
  > carrega. O bUnit remonta o HTML a cada renderização e não consegue provar que o elemento é o
  > mesmo; o teste confere o que dá para ver: dois "Próximo movimento" seguidos trocam o arquivo de
  > um só vídeo, sem navegar.

- [x] **8.8 Cards de câmera no navegador**
  - Origem: teste do autor em 2026-09-26.
  - Escopo: cards maiores, o card inteiro clicável para abrir o ao vivo, com efeito ao passar o
    mouse (leve elevação e cursor de clique). Bolinha verde quando a câmera transmite imagem,
    vermelha quando não transmite (parada ou offline). Símbolo de gravação quando "Gravar sempre"
    está ligado e de lanterna acesa (8.10). Embaixo, "Ver imagem" abre uma miniatura ao vivo,
    desligada por padrão, que fecha sozinha ao sair da tela ou depois de 2 minutos.
  - Aceite: testes do card (cores, símbolos, miniatura abrindo e fechando no tempo) e conferência
    no Chromium.
  > Validação (2026-09-26): só código escrito. Não conferido no Chromium: o navegador de teste daqui
  > não vira Monitor sem o autor aprovar pelo celular ("Conectar navegador"). Cards maiores (mínimo
  > de 320 px por coluna, mais espaço interno) e clicáveis inteiros: clicar em qualquer parte abre o
  > ao vivo, com o card subindo um pouco, borda na cor de destaque e sombra ao passar o mouse; os
  > botões e links dentro dele continuam fazendo só o que fazem. A bolinha ficou verde quando a
  > câmera manda imagem e vermelha quando não manda (parada ou offline), com a dica "Sem imagem
  > agora"; o selo "Gravando" ganhou um ponto vermelho. "Ver imagem" abre embaixo uma miniatura ao
  > vivo (componente `CameraPreview`, com o mesmo `LiveController` do ao vivo, então a câmera começa
  > a mandar vídeo e o lease é devolvido ao fechar); ela fecha ao clicar em "Esconder imagem", ao
  > sair da página e sozinha depois de 2 minutos. O ícone de lanterna acesa no card fica para o
  > 8.10, que é quem passa a guardar esse estado. Testes: cores da bolinha, clique no card abrindo o
  > ao vivo, miniatura que abre, assiste e fecha sozinha devolvendo o lease.

- [x] **8.9 Código de primeira abertura fácil de achar**
  - Origem: teste do autor em 2026-09-26: o código só aparecia no log.
  - Escopo: quem sobe o container à mão já vê, sem procurar, o endereço e o código juntos ("Abra
    https://<ip>:8443 e digite o código XXXX-XXXX"), em destaque no início do log. Um comando curto
    para mostrar de novo (ex.: `docker compose exec server ./Recam.Server code`). O README ensina
    os dois.
  - Aceite: teste do comando e do texto do log.
  > Validação (2026-09-26): só código escrito. Não rodado num container. Ao subir sem Monitor, o log
  > mostra um bloco emoldurado ("==================== ReCam ====================") com o endereço
  > para abrir, o código e como ver de novo; aparece direto em `docker compose up` sem `-d` e no
  > começo de `docker compose logs server`. O comando novo `docker compose exec server
  > ./Recam.Server code` mostra o mesmo bloco a qualquer momento: como roda em outro processo, o
  > servidor passou a guardar o código em `/data/first-open-code` enquanto não há Monitor e a apagar
  > o arquivo quando um navegador vira Monitor. Com Monitor, o comando diz que não há código e
  > aponta o `reset-owner`. README (passo 2) e a exceção de segredo do `AGENTS.md` atualizados;
  > revisão no SPECS 12. Com `docker compose up -d` o terminal continua sem mostrar nada: quem sobe
  > assim roda o comando `code`, como o README ensina. Testes: o comando mostra o endereço e o mesmo
  > código do log (que sai emoldurado), e depois de um navegador virar Monitor o arquivo some e o
  > comando diz que já há Monitor.

- [x] **8.10 Correção: estado da lanterna**
  - Origem: teste do autor em 2026-09-26: com a lanterna já acesa, o navegador mostrava "Ligar
    lanterna", e o primeiro clique não fazia nada.
  - Escopo: o servidor guarda em memória o estado que a câmera informa (`ReportTorch`) e o manda
    no status da câmera (`torchOn`); zera quando a câmera para de transmitir ou cai. O botão do ao
    vivo abre no estado certo, no app e no navegador, e o card mostra o ícone (8.8).
  - Aceite: testes do hub (estado guardado, enviado, zerado) e dos controllers.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho nem no navegador. O
  > servidor passou a lembrar a lanterna que a câmera informa (`DevicePresence.SetTorch`, em
  > memória), apagando quando a câmera para de transmitir (a lanterna é da trilha de vídeo e desliga
  > junto) ou cai. O estado vai no status da câmera (`torchOn` em `GET /api/cameras` e em
  > `CameraStatusChanged`), e o `ReportTorch` agora manda também o status, para as listas ficarem
  > certas sem recarregar. Navegador: o ao vivo abre com o botão no estado certo ("Desligar
  > lanterna" se já estiver acesa), e o card mostra 🔦 enquanto está acesa (seguindo o
  > `TorchChanged`). App: o ao vivo abre com o estado da lista, e a linha da câmera mostra o ícone
  > de lanterna. Tabela do SPECS 5.5 e 5.6 atualizadas. Testes: servidor guarda e esquece ao parar
  > de transmitir e ao cair; navegador abre o ao vivo com "Desligar lanterna" e o card mostra e tira
  > o ícone; app abre o ao vivo com a lanterna acesa a partir do status.

## Fase 9: áudio

Decidido com o autor em 2026-09-26: gravar e ouvir ao vivo, como o Alfred. Revisa a regra "nada de
áudio no MVP" do `AGENTS.md` e do `SPECS.md`.

- [x] **9.1 A câmera manda áudio, e a gravação guarda o som**
  - Origem: pedido do autor em 2026-09-26.
  - Escopo: a câmera publica uma trilha de áudio (Opus, mono) junto com o vídeo, sempre ligada. O
    MediaMTX grava o som no mesmo arquivo. Conferir que a gravação em fMP4 aceita Opus e registrar
    no `SPECS.md`. Permissão de microfone pedida junto com a da câmera.
  - Aceite: teste com Testcontainers achando a trilha de áudio no segmento gravado; teste do
    publisher pedindo áudio.
  > Validação (2026-09-26): código escrito; a gravação do som foi comprovada com o MediaMTX real,
  > mas não percorrida no aparelho. A câmera abre o microfone junto com a câmera (`getUserMedia` com
  > áudio) e publica uma trilha de áudio SendOnly; o libwebrtc usa Opus. Se o microfone for negado,
  > a câmera abre sem áudio e manda só vídeo. A permissão do microfone é pedida junto com a da
  > câmera, antes do serviço em primeiro plano, que passa a ser `camera|microphone` quando ela é
  > dada (o Android 14 recusa o tipo microfone sem a permissão). Manifesto: `RECORD_AUDIO` e
  > `FOREGROUND_SERVICE_MICROPHONE`. O MediaMTX grava o Opus no mesmo fMP4 do vídeo, sem mudar o
  > `mediamtx.yml`. Revisão no SPECS 2.3. Testes: Testcontainers publica H.264 com Opus por WHIP
  > (FFmpeg) e acha as duas trilhas (`avc1` e `Opus`) no segmento gravado; as restrições de captura
  > pedem o microfone e a câmera traseira. O publisher em si fala com o plugin nativo e não tem
  > teste.

- [x] **9.2 Ouvir ao vivo e nas gravações**
  - Origem: pedido do autor em 2026-09-26.
  - Escopo: no ao vivo e no player de gravação, no app e no navegador, o som toca, com um botão
    para tirar o som. O navegador começa sem som até o primeiro clique (regra dos navegadores).
  - Aceite: testes dos controllers e dos widgets do botão de som.
  > Validação (2026-09-26): só código escrito. Não percorrido no aparelho nem no navegador. Ao vivo:
  > o app e o navegador passam a pedir áudio ao MediaMTX (linha de áudio só de recebimento na oferta
  > WHEP; câmera sem microfone recusa a linha e o vídeo segue sozinho). No app o som toca direto, e
  > um botão de alto-falante na barra do ao vivo tira e devolve o som (desliga a trilha de áudio
  > recebida). No navegador o ao vivo começa sem som, porque o navegador só toca sozinho vídeo mudo;
  > o botão "Ativar som" / "Tirar som" muda a propriedade `muted` do `<video>` por JavaScript
  > (`whep.js`). Gravações: no app, o player ganhou o botão de som, que vale também para os arquivos
  > seguintes (`setVolume`); no navegador, o `<video>` da gravação perdeu o `muted`: ela começa num
  > clique, então o navegador deixa tocar com som, e o controle nativo do player tira o som. A
  > miniatura dos cards continua muda. O botão da lanterna do ao vivo no navegador ganhou a classe
  > `torch` para não ser confundido com o de som nos testes. Testes: controller do ao vivo no app
  > silencia e devolve o som, botão de som do player de gravação, e no navegador o ao vivo começa
  > com "Ativar som" e o botão desliga o mudo.

- [x] **9.3 [aparelho] Validar as fases 8 e 9**
  - Origem: pedidos do autor em 2026-09-26.
  - Escopo: PC, Samsung A10 e Redmi 6A, percorrendo cada bullet das fases 8 e 9.
  - Aceite: tudo funciona; qualquer falha vira bullet novo.
  > Bloqueado (2026-09-26): aguardando aparelho. Precisa do autor com o PC, o Samsung A10 e o
  > Redmi 6A. Nada das fases 8 e 9 foi percorrido no aparelho; o que dá para rodar sem celular
  > está nos testes e no gate.
  > Validação (2026-09-27): o autor percorreu no Samsung A10, no Redmi 6A e no navegador, com o
  > servidor na VPS, cerca de 16 horas seguidas (ao vivo, som, lanterna, gravação, linha do tempo,
  > movimento, bateria e temperatura). Tudo funcionando.

- [x] **9.4 A aba Gravações do navegador quebra ao abrir**
  - Origem: teste do autor em 2026-09-26 (9.3): a barra vermelha de erro do Blazor aparece ao
    abrir Gravações.
  - Causa: a lista de dias chega antes de o dia ser escolhido, e a linha do tempo lê o dia
    escolhido ainda vazio.
  - Aceite: enquanto o dia não foi escolhido, a página mostra o carregando; teste bUnit com a API
    respondendo devagar.
  > Validação (2026-09-26): o console mostrou `InvalidOperationException` em
  > `TimelineView.Window`, lendo o dia escolhido ainda vazio. A linha do tempo mostra o carregando
  > até o dia ser escolhido. O teste bUnit novo segura a lista de dias e os trechos, falha sem a
  > correção e passa com ela. Só código e teste; falta o autor conferir no navegador.

- [x] **9.5 O botão de som do ao vivo lembra a escolha**
  - Origem: teste do autor em 2026-09-26 (9.3): no navegador, ativar o som, sair e voltar para a
    câmera mostra "Ativar som" de novo. É o mesmo problema que a lanterna tinha antes da 8.10.
  - Escopo: o ao vivo abre com o som no estado que a pessoa deixou, e o botão mostra a ação
    certa. Conferir o app também.
  - Cuidado: o navegador só toca som depois de um clique na página. Se ele bloquear, o ao vivo
    fica sem som e o botão mostra "Ativar som", sem mentir.
  - Aceite: testes bUnit e de widget de sair e voltar com o som ligado.
  > Validação (2026-09-26): navegador: `SoundChoice` guarda a escolha enquanto a aba está aberta,
  > e o ao vivo reabre com ela. O `whep.js` só tira o mudo depois de algum clique na página e
  > devolve o estado real, então o botão não mente. A miniatura dos cards continua muda. App: a
  > lista de câmeras guarda a escolha, e o próximo ao vivo abre igual. Testes bUnit (sair e
  > voltar com som, navegador bloqueando) e de controller no app. Só código e teste; falta
  > conferir no navegador e no Redmi 6A.

- [x] **9.6 Relógio por cima do vídeo**
  - Origem: teste do autor em 2026-09-26 (9.3): na gravação, o player do navegador parece parar
    ou reiniciar a cada arquivo de 1 minuto, mesmo emendando. Uma hora na imagem ajuda a se
    guiar.
  - Escopo: um relógio discreto num canto do vídeo, desenhado na tela de quem assiste, no app e
    no navegador. No ao vivo, a hora atual. Na gravação, a hora do trecho somada à posição do
    vídeo, andando junto e seguindo ao trocar de arquivo. Nada muda no vídeo gravado nem na
    câmera.
  - Aceite: testes bUnit e de widget com a hora da gravação avançando e passando de um arquivo
    para o seguinte.
  > Validação (2026-09-26): hora no canto superior direito, com fundo escuro. App: `LiveClock`
  > no ao vivo e `VideoClock` na gravação, somando o início do arquivo à posição do player.
  > Navegador: `LiveClock.razor` no ao vivo; na gravação, o `clock.js` atualiza a hora no
  > `timeupdate` do vídeo e recomeça a cada arquivo. Testes de widget e bUnit cobrem a hora
  > andando e a troca de arquivo. Só código e teste; falta conferir no navegador e no Redmi 6A.

- [x] **9.7 Traço na linha do tempo acompanhando o vídeo**
  - Origem: pedido do autor em 2026-09-26, testando as gravações.
  - Escopo: um traço vertical fino na barra da linha do tempo, na hora que o vídeo está tocando
    (início do arquivo mais a posição do player), no app e no navegador. Anda com o vídeo, segue
    para o próximo arquivo e leva a janela junto quando sai dela. Tocar ou clicar em outro ponto
    da barra continua tocando dali, e o traço pula para lá. No navegador, o `clock.js` que já
    acompanha o `timeupdate` move o traço, sem ida ao .NET a cada quadro.
  - Aceite: testes de widget e bUnit do traço andando, trocando de arquivo e movendo a janela.
  > Validação (2026-09-26): app: traço vermelho na barra, na hora do vídeo; a janela vai junto
  > quando a reprodução pula para fora dela ou o vídeo passa da borda, mas não enquanto a pessoa
  > olha outro trecho. Corrigido de passagem: tocar recriava a barra e voltava o zoom para 1 h (a
  > área do player surge acima dela); a barra ganhou chave fixa. Navegador: `<line>` no SVG movido
  > pelo `clock.js`, que só avisa o .NET quando o vídeo sai da janela. Testes: widget (traço
  > andando, janela seguindo, zoom mantido), `TimelineWindow.contains` e bUnit (janela informada
  > ao script, janela seguindo o vídeo). Só código e teste; o JS nunca rodou num navegador de
  > verdade.

- [x] **9.8 Escolher o idioma nas Configurações**
  - Origem: pedido do autor em 2026-09-26. Hoje o app segue o idioma do celular e o navegador segue
    o do navegador, sem opção de trocar.
  - Escopo: na aba Configurações do app e do navegador, escolher entre "Idioma do aparelho",
    "Português" e "English". A escolha fica só naquele aparelho (no app, no armazenamento local;
    no navegador, no `localStorage`) e vale na hora, sem reinstalar.
  - Aceite: testes de widget e bUnit trocando o idioma e vendo o texto mudar.
  > Validação (2026-09-26): app: seletor "Do celular / Português / English" no fim da aba
  > Configurações (não aparece na tela do primeiro espaço), guardado no armazenamento seguro e
  > lido antes do primeiro quadro; o `MaterialApp` troca o `locale` na hora. Navegador: seletor na
  > página Configurações; a escolha fica no `localStorage`, o `Program.cs` aplica a cultura antes de
  > subir, e trocar recarrega a página (o Blazor só define a cultura uma vez). Foi preciso ligar
  > `BlazorWebAssemblyLoadAllGlobalizationData`: sem ele o inglês quebrava a página com o navegador
  > em português. Percorrido num navegador de verdade (cópia temporária do servidor sem TLS):
  > inglês, português e sem escolha mostram a página certa. Testes de widget, de app e bUnit.
  > Falta conferir no Redmi 6A.

- [x] **9.9 Câmera nova já grava**
  - Origem: pedido do autor em 2026-09-26. Hoje a câmera pareada só transmite ao vivo, e a tela do
    espaço (8.3) pergunta o espaço de uma gravação que ainda não existe.
  - Escopo: o servidor cria toda câmera nova com a gravação ligada. Câmeras já pareadas ficam como
    estão. A tela do espaço, no app e no navegador, passa a avisar que a câmera já está gravando
    e que, quando o espaço enche, as gravações mais antigas são apagadas; "Pular" continua usando
    o espaço padrão. Revisar o `SPECS.md` (2.4).
  - Aceite: teste de integração pareando uma câmera e vendo a gravação ligada; testes de widget e
    bUnit do novo texto da tela do espaço.
  > Validação (2026-09-26): `Device.Pair` cria câmera com `RecordingEnabled` ligado; o
  > `ReportVideoCodecs` sem H.264 continua desligando. Testes antigos que falavam de câmera só ao
  > vivo (caminho `cam-`, lease que para a câmera, WHEP com cookie) passaram a desligar a gravação
  > no arrange, com o helper `RecamApiFactory.SetRecordingAsync`. Novos: domínio (câmera grava,
  > Monitor não), integração (câmera pareada pela API aparece gravando na lista), bUnit e widget do
  > texto novo da tela do espaço (no app a tela não tinha teste; ganhou, junto com o seletor de
  > idioma só na aba). `SPECS.md` revisado. Só código e teste.

- [x] **9.10 Chave de gravação visível, com o estado real**
  - Origem: teste do autor em 2026-09-26: a opção de gravar fica escondida dentro da câmera, numa
    caixa que não diz se a gravação começou.
  - Escopo: uma chave "Gravar" à vista no card da câmera (navegador) e na linha da câmera (app),
    e também na barra do ao vivo nos dois. Ao lado, o estado que o servidor confirma, não o que
    foi pedido: "Gravando" (ponto vermelho, arquivos chegando), "Começando…" (ligada, primeiro
    arquivo ainda não chegou) ou "Não está gravando", com o motivo (câmera offline, sem espaço,
    aparelho sem H.264). Se ligar e não começar em cerca de 30 s, um aviso diz por quê. Se o
    servidor ainda não souber distinguir esses estados, este bullet inclui essa parte.
  - Aceite: testes de widget e bUnit de cada estado e da troca de "Começando…" para "Gravando";
    teste de integração do estado vindo do servidor.
  > Validação (2026-09-26): servidor: `RecordingState` e a regra pura `RecordingStates.Of` no
  > domínio; o `RecordingStateTracker` (Infrastructure) lê a presença e a hora do último arquivo
  > gravado da câmera, e lembra desde quando ela espera o primeiro arquivo; o
  > `RecordingStateWorker` (Realtime) confere a cada 5 s e avisa os Monitores só quando o estado
  > muda. O "aviso depois de 30 s" é o estado "Não está gravando: o vídeo não chega ao servidor".
  > Enums no hub passaram a ir como texto. Navegador: a caixa virou um switch com o estado ao
  > lado (ponto pulsando ao gravar, amarelo começando, contorno vermelho com o motivo), no card
  > e no ao vivo; o selo "Gravando" do card só aparece gravando de verdade. App: o mesmo estado
  > ao lado do switch na linha da câmera e no ao vivo. Logo depois de ligar, antes do servidor
  > olhar o disco, os dois mostram "Começando…". Testes: domínio (cada estado), tracker (começando,
  > parada, gravando com o tempo), hub (viewer ouve "gravando" quando o arquivo chega), bUnit e
  > widget (cada estado e a troca). Só código e teste; o visual do switch nunca foi visto num
  > navegador de verdade.

- [x] **9.12 Aparelho preso como online quando a conexão cai durante a entrada**
  - Origem: achado em 2026-09-26, investigando a falha intermitente do gate (teste "A camera that
    disconnects shows as offline to viewers").
  - Causa: o `OnConnectedAsync` do hub usava o token da conexão. Se ela caía no meio da entrada, a
    entrada era cancelada, e o SignalR não chama o `OnDisconnectedAsync` de uma entrada que falhou:
    o aparelho ficava online no servidor até ele reiniciar. A 9.9 alongou a entrada da câmera
    (ela já começa gravando) e aumentou a janela.
  - Aceite: a entrada termina mesmo com a conexão caindo; teste de câmera que cai logo ao
    conectar terminando offline; suíte do servidor sem a falha intermitente.
  > Validação (2026-09-26): a entrada usa `CancellationToken.None` nos passos curtos (grupo,
  > presença, leitura da gravação). Teste novo. A suíte inteira do servidor rodou 4 vezes seguidas
  > sem falha. Entrou no mesmo commit da 9.10, porque o gate dela dependia disso.

- [x] **9.11 Ajustes de bateria conferidos sozinhos**
  - Origem: teste do autor em 2026-09-26: no Samsung A10 a tela de bateria sumiu depois dos
    ajustes, mas no Redmi 6A aparece toda vez sem dizer o que falta. Os ajustes são
    recomendações, não obrigação, e a pessoa não marca nada.
  - Escopo: a tela lista cada ajuste. Os que o app consegue ler (otimização de bateria, uso em
    segundo plano restrito a partir do Android 9, permissão de notificação a partir do Android 13)
    aparecem com um check automático ou com um botão "Ajustar", que abre a tela certa do
    Android, e o check atualiza ao voltar para o app. Os que nenhum app consegue ler (início
    automático da MIUI, apps em suspensão da Samsung, travar nos recentes) aparecem só como dica
    em texto, separados pelo fabricante como hoje. A tela nunca bloqueia: sempre tem
    "Continuar". Aparece sozinha só quando um ajuste que o app lê está faltando, e também pelo
    menu ⋮ da câmera.
  - Aceite: testes de widget com cada ajuste certo e faltando, e do check mudando ao voltar para o
    app; testes do controller decidindo quando a tela aparece sozinha.
  > Validação (2026-09-26): o canal `io.recam.app/device` ganhou `isBackgroundRestricted`
  > (`ActivityManager`) e `areNotificationsEnabled` (`NotificationManager`); a otimização de
  > bateria continua pelo `permission_handler`. O controller devolve o fabricante e os ajustes
  > faltando; a tela mostra cada um com check ou "Ajustar" (abre a tela certa do Android), confere
  > de novo ao voltar para o app, e lista as dicas que nenhum app lê só em texto. Nunca bloqueia.
  > Aparece antes do modo câmera só se falta algum ajuste lido, e sempre pelo novo "Ajustes de
  > bateria" no menu ⋮ da câmera. Os passos antigos (Liberar, Abrir configurações) saíram. Testes
  > de controller, de tela (checks, "Ajustar", conferência ao voltar, dicas sem caixa) e do menu.
  > O Kotlin compilou no `flutter build apk`. Não instalado: os celulares estavam desconectados.

- [x] **9.13 Gravações cifradas no disco do servidor**
  - Origem: pedido do autor em 2026-09-27. Quem abre a pasta de gravações (Explorer, cópia do
    volume) não deve conseguir assistir. Proteção leve, para desanimar o curioso: a chave fica no
    próprio servidor, então quem tem root e paciência ainda decifra.
  - Escopo: o servidor gera uma chave AES aleatória no primeiro uso e guarda em `/data`. Cada
    arquivo de 1 minuto é cifrado com AES-CTR assim que fecha e depois de a análise de movimento
    ler o arquivo aberto; o `.motion` continua em texto. O tamanho praticamente não muda (só um
    cabeçalho de poucos bytes com o número único do arquivo). Ao tocar uma gravação no app ou no
    navegador, o servidor decifra enquanto envia, aceitando pedido de trecho (Range), para
    pular no vídeo continuar funcionando. Arquivos já gravados antes deste bullet são cifrados
    também. A cota, a limpeza e a linha do tempo seguem iguais. O arquivo que está sendo gravado
    no momento fica aberto até fechar. A chave nunca aparece em log. Revisar o `SPECS.md` (2.4 e
    segredos) e a regra "o .NET não processa vídeo" do `AGENTS.md`: cifrar bytes não é processar
    vídeo, mas vale deixar escrito.
  - Aceite: testes de cifrar e decifrar ida e volta, de pedido de trecho no meio do arquivo, de
    arquivo fechado sendo cifrado só depois do `.motion` existir, e de o arquivo cifrado não ter
    mais a assinatura de MP4 (`ftyp`); teste com o MediaMTX real gravando e o app tocando pelo
    servidor.
  > Validação (2026-09-27): `RecordingCipher` (AES-CTR montado com AES-ECB sobre os blocos do
  > contador, porque o .NET não tem CTR pronto) e `RecordingCipherWorker`, a cada 20 s. A troca é
  > atômica (arquivo temporário e `Move`) e mantém a hora do arquivo, que o estado de gravação da
  > 9.10 lê. O endpoint decifra num stream com `Seek`, então o Range continua funcionando. Testes:
  > ida e volta, leitura no meio (inclusive cruzando blocos), cifrar duas vezes não muda nada,
  > arquivo aberto lido como está, o worker esperando o `.motion` ou 30 min, o endpoint servindo um
  > trecho decifrado, e um MP4 real do FFmpeg (H.264 + Opus) que o `ffprobe` não lê cifrado e lê de
  > novo decifrado. Não ficou o teste com o MediaMTX gravando de ponta a ponta: o do FFmpeg cobre
  > o mesmo formato de arquivo. Só código e teste.

- [x] **9.14 Versão automática no app e no navegador**
  - Origem: pedido do autor em 2026-09-27. Saber qual build está rodando em cada aparelho, sem
    numerar versão à mão (nada de 1.0, 1.1).
  - Escopo: a versão sai do Git na hora do build: data do commit e hash curto, no formato
    `2026.09.27+4b4d231` (com `-dirty` quando o build tem mudanças sem commit). O servidor e o
    Monitor web recebem a mesma versão pelo MSBuild; na imagem Docker, que não tem a pasta
    `.git`, ela entra como argumento de build. O app recebe pelo `--build-name` e
    `--build-number` do Flutter. O app mostra a própria versão e a do servidor na aba
    Configurações (e no modo câmera, num lugar discreto); o navegador mostra a dele e a do
    servidor na página Configurações. `GET /health` passa a devolver a versão. Sem versão do Git
    (build fora do repositório), aparece "dev".
  - Aceite: teste do script que monta a versão (com e sem mudanças pendentes); testes de widget
    e bUnit mostrando as versões; teste de integração do `/health` com a versão.
  > Validação (2026-09-27): `scripts/version.sh` e `version_test.sh` (repositório descartável:
  > fora do Git dá `dev`, limpo dá data+hash, com arquivo pendente ganha `-dirty`); o gate roda o
  > teste. Servidor e Monitor web leem a `InformationalVersion`, que o `Directory.Build.props` tira
  > do `RECAM_VERSION` (argumento de build no Dockerfile e nos composes). O app lê
  > `String.fromEnvironment('RECAM_VERSION')`, sem dependência nova. Scripts de build:
  > `build-server.sh` e `build-apk.sh`. O app mostra "Este celular" e "Servidor" no fim de
  > Configurações e a própria versão em texto pequeno no modo câmera; o navegador mostra "Este
  > Monitor" e "Servidor" na página Configurações. Decisão: os builds de desenvolvimento (gate,
  > IDE) ficam `dev`, porque rodar o Git dentro do MSBuild no Windows esbarra no `%` do `cmd`.
  > Testes: script, `/health`, bUnit e widget. Percorrido no deploy desta mesma data.

- [x] **9.15 Tela Aparelhos atualizando sozinha, com Monitores online**
  - Origem: pergunta do autor em 2026-09-27. A lista de câmeras atualiza pelo hub, mas a tela
    Aparelhos (app e navegador) só carrega ao abrir, e os Monitores não mostram se estão online.
  - Escopo: o servidor avisa os Monitores pelo hub quando um aparelho entra, sai, é removido ou
    fica online/offline (câmeras e Monitores). A tela Aparelhos ouve esses avisos e se atualiza
    sozinha, como a lista de câmeras. Cada Monitor mostra online agora ou, se não, quando foi
    visto pela última vez. Revisar o `SPECS.md` (hub e `GET /api/devices`).
  - Aceite: teste de integração do aviso chegando pelo hub quando um Monitor conecta, cai e é
    removido; testes de widget e bUnit da tela mudando sem reabrir.
  > Validação (2026-09-27): servidor: `DevicesChanged()` no hub, mandado pela entrada e saída no
  > hub (qualquer papel), pelo pareamento, pelo "Conectar navegador" e pela remoção; o aviso passa
  > por `IDeviceListChanges` (Infrastructure), para as features não se referenciarem. `GET
  > /api/devices` ganhou `lastSeenAt`. Navegador: o controller ouve o hub e recarrega em silêncio,
  > sem piscar a lista; a página solta o aviso ao fechar. App: a lista de câmeras repassa o aviso
  > por um `ValueNotifier`, e a aba Aparelhos recarrega. Offline mostra "visto por último em".
  > Testes: hub (pareia, conecta, cai, removido), lista com `lastSeenAt`, bUnit e widget da tela
  > mudando sem reabrir, controller do app. Só código e teste.

- [x] **9.17 Título das dicas de bateria sem o parêntese**
  - Origem: pedido do autor em 2026-09-27: na tela "Manter a câmera ligada", o título "Também
    ajuda (o app não consegue conferir):" fica só "Também ajuda:".
  > Validação (2026-09-27): texto trocado em `pt` e `en`. Testes do app verdes.

- [x] **9.16 Investigar a falha intermitente do teste do hub do navegador**
  - Origem: em 2026-09-27 o teste "With its cookie and the web header, the browser opens the hub
    and watches a camera" falhou uma vez na suíte completa e passou nas rodadas seguintes, com
    erros de conexão do SQLite no log. Suspeita: escritas simultâneas no banco sob carga.
  - Escopo: rodar a suíte completa várias vezes guardando o erro exato. Se não repetir, fechar sem
    mudança. Se repetir, achar a causa e corrigir, com teste.
  - Aceite: causa descrita na validação, ou registro de que não repetiu.
  > Validação (2026-09-27): a suíte completa do servidor rodou 10 vezes seguidas, sem nenhuma
  > falha. Fechado sem mudança, como combinado com o autor. Se voltar, reabrir guardando o log.

## Fase 10: servidor numa VPS

Decidido com o autor em 2026-09-26: rodar o servidor numa VPS, com as câmeras em casa e quem
assiste em qualquer lugar. Certificado, tráfego e limite da VPS são responsabilidade de quem roda o
servidor; o projeto só precisa funcionar nesse cenário e explicar como montar.

- [x] **10.1 Código de primeira abertura aceito de qualquer rede**
  - Origem: decisão do autor em 2026-09-26. O código já basta: 8 caracteres entre 31, morre após
    5 erros e só existe até o primeiro Monitor. A regra "só da rede local" impede usar numa VPS.
  - Escopo: `POST /api/web/first-open` deixa de exigir IP da rede local. Mantém o limite de 5 por
    minuto por IP e os 5 erros por código. Revisar `SPECS.md` (5.5, 6 e o log de decisões) e a
    regra de token no `AGENTS.md`.
  - Aceite: teste de integração com IP público virando Monitor com o código certo, e o limite de
    tentativas continuando a valer.
  > Validação (2026-09-26): saiu a checagem de rede local, o erro `setup.not_local` e o texto
  > "CodeNotLocal" do navegador. O log agora diz só "On a computer". Testes novos: IP público vira
  > Monitor com o código; o sexto erro no minuto recebe 429; atrás de proxy confiável cada IP real
  > tem o próprio limite, e de proxy desconhecido todos dividem um. Só código e teste. Achado para
  > a 10.2: o suporte a proxy já existe em boa parte (`RECAM_TRUSTED_PROXIES`, TLS desligável e QR
  > sem fingerprint); falta conferir o app e escrever o guia.

- [x] **10.2 Servidor atrás de um proxy com certificado de verdade**
  - Origem: VPS do autor, em 2026-09-26. Quem expõe na internet coloca um proxy (Caddy, Nginx
    Proxy Manager) com certificado do Let's Encrypt na frente.
  - Escopo: o servidor lê o IP real de `X-Forwarded-For` só quando vem de um proxy configurado
    (senão o limite por IP vira um limite para todo mundo). O QR usa o endereço do proxy
    (`RECAM_PUBLIC_URLS`). O app pareia com um servidor de certificado válido, sem depender do
    fingerprint. O SignalR e o WHIP/WHEP passam pelo proxy. O vídeo continua direto na
    `8189/udp`, fora do proxy.
  - Fora do escopo: pôr o Caddy no compose. O exemplo de configuração fica num guia à parte,
    em `docs/`, que o README só cita.
  - Aceite: teste do IP real atrás do proxy; teste do app aceitando certificado válido sem `f`.
  > Validação (2026-09-26): o código já existia desde o 5.1: `RECAM_TLS=off`, `RECAM_TRUSTED_PROXIES`
  > e QR sem fingerprint. O IP real atrás do proxy é coberto pelo teste novo da 10.1 (limite por
  > cliente real). O app sem `f` usa as CAs do sistema, e o `PinnedHttpOverrides` só entra quando o
  > certificado falha na validação padrão (teste de pareamento sem fingerprint). Novo:
  > `docs/reverse-proxy.md`, com Caddyfile, `.env`, firewall (fechar a 8443 para fora) e o aviso de
  > parear de novo os celulares. O README cita o guia. Só documentação e testes existentes; nunca
  > percorrido com um proxy de verdade.

- [x] **10.3 Guia de instalação numa VPS**
  - Escopo: no quick start do README (inglês), começar rápido e com poucos comandos, em casa ou
    numa VPS. Sem proxy: portas `8443/tcp` e `8189/udp` abertas, `RECAM_HOST` com o IP público
    (que também vai para o `webrtcAdditionalHosts` do MediaMTX), e o código de primeira abertura
    (`./Recam.Server code`). O navegador mostra o aviso de certificado, e o guia diz que é
    esperado. Tirar o aviso com um proxy fica no guia da 10.2, que o README só cita.
  - Aceite: o autor segue o guia do zero na VPS dele sem precisar de outra ajuda.
  > Em andamento (2026-09-26): o README ganhou o caminho da VPS no passo 1 (`git clone`, `.env` com
  > `RECAM_HOST` = IP público, `docker compose up -d`, portas também no painel do provedor) e diz
  > que o aviso do certificado é esperado, apontando o guia de proxy. O `compose.yaml` agora
  > manda o `RECAM_HOST` para o `webrtcAdditionalHosts` do MediaMTX (conferido com
  > `docker compose config`; vazio, o MediaMTX sobe normal). `.env.example` e `SPECS.md` 5.1
  > revisados. Falta o aceite: o autor seguir o guia do zero na VPS, junto com a 10.4.
  > Validação (2026-09-27): o autor percorreu no Samsung A10, no Redmi 6A e no navegador, com o
  > servidor na VPS, cerca de 16 horas seguidas (ao vivo, som, lanterna, gravação, linha do tempo,
  > movimento, bateria e temperatura). Tudo funcionando.

- [x] **10.4 [aparelho] Validar na VPS**
  - Escopo: servidor na VPS do autor. Samsung A10 em casa, no Wi-Fi, como câmera. Redmi 6A no
    4G assistindo. Navegador fora da rede de casa virando Monitor com o código.
  - Aceite: ao vivo com som, lanterna, gravação e linha do tempo funcionando. Anotar o atraso e
    qualquer falha como bullet novo.
  > Validação (2026-09-27): o autor percorreu no Samsung A10, no Redmi 6A e no navegador, com o
  > servidor na VPS, cerca de 16 horas seguidas (ao vivo, som, lanterna, gravação, linha do tempo,
  > movimento, bateria e temperatura). Tudo funcionando.

## Fase 11: Monitor no iPhone pelo navegador

Decidido com o autor em 2026-09-27. Não existe app de iPhone, e o autor quer usar o iPhone só
para assistir. O caminho é o navegador do iPhone virar Monitor. Só um Monitor que já existe pode
criar o convite, então nada disso funciona antes da configuração inicial no PC. Assistir de fora
de casa depende de o servidor ser alcançável de fora (VPS da Fase 10, porta aberta ou Tailscale),
o que continua por conta de quem roda o servidor.

- [x] **11.1 App: "Adicionar Monitor" numa opção só**
  - Origem: o autor achou confuso ter "Adicionar Monitor" e "Conectar navegador" lado a lado no
    app (2026-09-27). Os nomes não dizem que um mostra QR e o outro lê.
  - Escopo: sai o item "Conectar navegador" do menu. "Adicionar Monitor" pergunta "Onde vai
    assistir?", com duas escolhas: "Em outro celular" (mostra o QR de pareamento, como hoje) e
    "Num navegador" (abre a leitura do QR que o navegador mostra, o fluxo do atual "Conectar
    navegador"). O texto que o navegador mostra ao pedir aprovação passa a citar "Adicionar Monitor
    › Num navegador". Textos nos ARB (en e pt) e nos `.resx`. O protocolo não muda.
  - Aceite: widget tests das duas escolhas levando à tela certa, e do menu sem "Conectar
    navegador".
  > Validação (2026-09-27): só código escrito e testes; não percorrido no celular. O menu do
  > Monitor tem só "Adicionar Monitor", que abre a folha `AddMonitorSheet` ("Onde vai assistir?"):
  > "Em outro celular" abre a tela do QR de pareamento como antes, e "Num navegador" abre a leitura
  > do QR do navegador (a tela do antigo "Conectar navegador", agora "Monitor num navegador"). A
  > chave `connectBrowserButton` saiu dos ARB; entraram `addMonitorWhere`, `addMonitorOnPhone`,
  > `addMonitorInBrowser` e as dicas, em en e pt. O texto do navegador que pede aprovação agora diz
  > "⋮ > Adicionar Monitor > Num navegador" (resx en e pt). `SPECS.md` 2.5 e 5.2 e o README citam o
  > caminho novo. Testes: widget tests das duas escolhas (outro celular pede token de Monitor;
  > navegador abre `ConnectBrowserScreen` sem pedir token) e do menu sem "Connect browser"; bUnit da
  > página inicial com o texto novo.

- [x] **11.2 Link de Monitor para navegador, e o Monitor web no celular**
  - Origem: continuação do 11.1. No iPhone não há app para ler o QR de "Conectar navegador", e o
    navegador do PC não lê QR.
  - Escopo:
    - "Adicionar Monitor" no navegador pergunta "Onde vai assistir?", como no app: "Em outro
      celular com o app" (o QR de pareamento de hoje) e "Num navegador (iPhone, outro computador)".
    - A segunda escolha cria um convite já aprovado pelo Monitor que o gerou (um `BrowserLink`
      criado por um Monitor, rota nova no `SPECS.md` 5.5) e mostra um QR com um link `https://`,
      mais o link para copiar. A câmera do iPhone lê o QR e abre no Safari; o navegador resgata o
      convite pela rota de resgate que já existe e vira `Viewer`, com "Lembrar neste aparelho".
    - O segredo vai no fragmento (`#`), que o navegador não manda ao servidor, então não aparece
      em log de servidor nem de proxy. Vale 10 minutos e uma vez só, como os QR de pareamento.
    - Endereço do link: o primeiro de `RECAM_PUBLIC_URLS` quando existe, senão o endereço que o
      navegador do Monitor está usando. O cookie vale só para o endereço em que foi criado, então
      a tela avisa que é preciso abrir sempre pelo mesmo endereço.
    - Layout do Monitor web em tela de celular (~360 a 430 px): navegação do topo, lista de
      câmeras, vídeo ao vivo, linha do tempo, botões de movimento e de clarear cabendo sem rolagem
      lateral.
    - Revisar `SPECS.md` (1.2, 2.5, 5.5, 6 e o log de decisões, com ADR) e o README.
  - Fora do escopo: app de iPhone; o iPhone como câmera.
  - Aceite: testes de integração do convite (só Monitor cria; resgate vira `Viewer`; vencido ou
    usado é recusado; o segredo não aparece no log), bUnit das duas escolhas e da página de
    resgate, e o caminho percorrido no Chromium com tela de celular (link aberto noutro perfil
    virando Monitor, telas sem rolagem lateral).
  > Validação (2026-09-27): código escrito e caminho percorrido no Chromium (sem iPhone). Servidor:
  > `BrowserLink.Invite` cria o link já aprovado por um Monitor (câmera recebe 403, sem credencial
  > 401) e `POST /api/browser-links/invite` devolve `{ id, url, expiresAt }`, com o endereço do
  > primeiro `RECAM_PUBLIC_URLS` ou o do pedido e o `claim` no fragmento de `/connect`. Navegador:
  > "Adicionar Monitor" pergunta "Onde vai assistir?"; "Num navegador" mostra o QR do link, o link
  > para copiar (área de transferência por `IClipboard`), a validade ("Vale por 10:00, uma vez só")
  > e o aviso do endereço; a página `/connect` resgata com "Lembrar neste aparelho", recarrega na
  > raiz e tira o convite do endereço. Layout de celular: até 600 px a navegação quebra em duas
  > linhas, os títulos descem para baixo do "voltar" e os cartões têm menos margem. Achado no
  > caminho: as horas da linha do tempo e a linha que acompanha o vídeo usavam estilo inline, que a
  > CSP bloqueia (as horas ficavam todas empilhadas à esquerda, também no PC); agora são atributos
  > de SVG, com teste que não deixa `style` voltar. Testes: 5 de integração (convite vira Viewer e o
  > `claim` não aparece no log; endereço público; usado e vencido recusados; câmera e estranho
  > recusados; celular Monitor também convida), 2 de domínio, 4 do controller do convite e 8 bUnit
  > (escolha, QR e cópia, outro celular, página de resgate, convite usado, link incompleto,
  > navegador que já é Monitor, leitura do fragmento). Observado no Chromium contra o servidor de
  > dev: o PC criou o convite, um contexto novo com tela de iPhone abriu o link, tocou em "Assistir
  > neste navegador" e caiu na lista de câmeras com cookie persistente e HttpOnly; o mesmo link
  > aberto de novo nesse celular disse "Este navegador já é um Monitor", e noutro navegador disse
  > que o convite venceu ou já foi usado. A 390 px, nenhuma das oito telas (câmeras, ao vivo,
  > linha do tempo, gravações, aparelhos, configurações, adicionar câmera, adicionar Monitor) rola
  > para o lado, e o console ficou sem erro de CSP. Falta o Safari de verdade (11.3).

- [ ] **11.3 [aparelho] Validar no iPhone**
  - Escopo: iPhone do autor com Safari. Primeiro no Wi-Fi de casa, depois no 4G com o servidor
    alcançável de fora. Ler com a câmera do iPhone o QR de "Adicionar Monitor › Num navegador".
  - Aceite: o iPhone vira Monitor; ao vivo (com o botão de som), lanterna, gravações, linha do
    tempo e movimento funcionam; fechar e abrir o Safari dias depois continua logado. Anotar o
    aviso de certificado, o atraso e qualquer falha como bullet novo.
  > Bloqueado (2026-09-27): aguardando validação no aparelho. O 11.1 e o 11.2 estão prontos e
  > percorridos no Chromium com tela de iPhone; falta o Safari e a rede móvel. Roteiro: atualizar o
  > servidor na VPS; no PC, Aparelhos › Adicionar Monitor › "Num navegador"; ler o QR com a câmera
  > do iPhone; "Assistir neste navegador" com "Lembrar neste aparelho"; conferir lista, ao vivo com
  > "Ativar som", lanterna, gravações, linha do tempo, movimento e Clarear, sem rolagem lateral;
  > repetir no 4G; fechar o Safari e abrir de novo dias depois. Se o ao vivo ficar preto só no 4G,
  > anotar o resultado do teste do roteador do Android e do IPv6 da VPS (conversa de 2026-09-27).

- [x] **11.4 Menu do topo do Monitor web em tela de celular**
  - Origem: teste do autor em 2026-09-27, num celular: o menu do topo (Câmeras, Gravações,
    Aparelhos, Configurações, Sair) quebra em duas linhas e ocupa boa parte da tela.
  - Escopo: em tela estreita (até ~600 px), o topo vira uma barra compacta: nome do ReCam e um
    botão de menu que abre as seções, ou uma barra de abas fixa embaixo, como no app. "Sair" vai
    para dentro do menu ou de Configurações. Em tela larga fica como está. Sem biblioteca nova.
  - Aceite: bUnit do menu abrindo e fechando; conferido no Chromium com tela de celular
    (360 a 430 px) sem rolagem lateral e sem o topo quebrar em duas linhas.
  > Validação (2026-09-27): até 600 px, o topo fica com o nome e um botão ☰ (`aria-expanded`); ele
  > abre as seções numa lista sobre a página, com "Sair" no fim, e escolher uma seção fecha a
  > lista. Em tela larga nada muda. Teste bUnit do menu abrindo e fechando ao navegar. Percorrido
  > no Chromium a 375x812 numa cópia local do servidor: topo com 56 px numa linha só, menu abrindo
  > e fechando, `scrollWidth` igual à largura da tela (sem rolagem lateral).

- [x] **11.5 Convite de navegador fecha sozinho quando é usado**
  - Origem: teste do autor em 2026-09-27: depois que o navegador do iPhone virou Monitor pelo
    link, a tela "Adicionar Monitor › Em um navegador" do PC continuou mostrando o QR.
  - Escopo: a tela do convite percebe quando o link foi resgatado e volta sozinha para
    Aparelhos, onde o novo Monitor já aparece (9.15), com um aviso curto de que o navegador foi
    conectado. Como o "Adicionar câmera" faz com o QR de pareamento. Pode ouvir o
    `DevicesChanged` do hub e conferir o convite, ou perguntar ao servidor se o convite já foi
    usado; nada de expor o segredo. Se o convite vencer sem uso, a tela diz isso e oferece um
    novo.
  - Aceite: teste de integração da consulta de "convite usado" (só o Monitor que criou vê);
    bUnit da tela voltando para Aparelhos quando o convite é usado, e mostrando "venceu" quando
    passa o prazo.
  > Validação (2026-09-27): domínio `BrowserLink.InviteStateFor` (só quem convidou; os outros
  > recebem o mesmo "não encontrado" de um link qualquer) e a rota `GET .../invite`. No navegador,
  > o controller pergunta a cada 2 s; usado, a página vai para `devices?connected=browser`, que mostra
  > "Navegador conectado. Ele já é um Monitor."; vencido, mostra "Este link venceu sem ser usado." e
  > "Gerar outro link". Escolhi perguntar ao servidor em vez de ouvir o `DevicesChanged`: não
  > depende do hub estar conectado. Testes: integração (esperando, usado, vencido, outro Monitor
  > recebe 404 sem segredo), controller (vencido espera pedido; usado muda de estado) e bUnit (a
  > página volta sozinha; Aparelhos mostra o aviso). Só código e teste.

- [x] **11.6 Ao vivo preto no navegador do iPhone: `play()` explícito e painel de diagnóstico**
  - Origem: teste do autor em 2026-09-27: no navegador do iPhone, o ao vivo abre, o relógio anda,
    mas a imagem fica preta. No PC e no app funciona.
  - Escopo:
    - `whep.js` chama `video.play()` depois de ligar o fluxo recebido ao `<video>`, e de novo
      quando chega uma trilha nova. Se o navegador recusar, o erro vai para o diagnóstico em vez
      de sumir.
    - Um botão "Diagnóstico" embaixo do vídeo, no ao vivo do navegador, abre um painel que se
      atualiza a cada segundo com: estado da conexão e do ICE, par de candidatos em uso
      (local/remoto, UDP ou TCP), bytes e quadros de vídeo recebidos e decodificados, codec e
      perfil negociados, tamanho da imagem recebida, estado do `<video>` (pausado, `readyState`,
      tamanho) e o último erro do `play()`. Nada disso sai do navegador; é para a pessoa tirar um
      print. Textos nos `.resx`.
    - O estado "Tocando" do ao vivo passa a esperar a conexão de mídia abrir de verdade, não só a
      resposta do WHEP; se ela não abrir, mostra a falha e o botão de tentar de novo.
  - Aceite: bUnit do botão abrindo e fechando o painel e mostrando os campos que o script
    devolve; conferido no Chromium; o autor manda o print do painel no iPhone. Se o `play()`
    resolver, anotar; se não, a causa vira bullet novo a partir do print.
  > Em andamento (2026-09-27): feito o código. `whep.js` chama `video.play()` a cada trilha
  > recebida e guarda a recusa; o `start` espera a conexão de mídia abrir (até 10 s) e devolve -1
  > se não abrir, e o ao vivo mostra a falha na hora em vez de ficar preto "tocando" (`LiveStart`:
  > `Playing`, `NotYet`, `MediaFailed`). O botão "Diagnóstico" embaixo do vídeo lê o `getStats()` a
  > cada segundo: conexão e ICE, caminho (tipo e protocolo dos candidatos), bytes, quadros
  > recebidos e decodificados, codec com o perfil, tamanho da imagem, estado do `<video>` e o erro
  > do `play()`. Se a mídia nunca abriu, mostra a última leitura dessa tentativa. Testes: bUnit do
  > painel e controller falhando sem repetir; sintaxe do `whep.js` conferida no Node. Não foi
  > visto num navegador de verdade (precisa de câmera transmitindo e login). Falta: o autor
  > abrir no iPhone e mandar o print do painel.
  > Validação (2026-09-27): o autor abriu o ao vivo no iPhone depois do deploy e a imagem
  > apareceu. O `play()` explícito resolveu; o painel fica para os próximos casos.

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
  - Origem: vitrine do projeto.
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
  - Origem: vitrine do projeto.
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
  - Origem: vitrine do projeto.
  - Escopo: GIF curto do caminho principal (código no navegador, ler o QR com o celular, vídeo
    ao vivo no navegador, lanterna) e diagrama da arquitetura no `README.md`. Revisado em
    2026-09-25 para o caminho novo da Fase 6.
  - Aceite: README mostra o GIF gravado nos aparelhos reais.

- [ ] **5.3 Imagem publicada no GHCR e instalação em um comando**
  - Escopo: workflow que publica `ghcr.io/<dono>/recam-server` em tag `v*`. Os composes usam a
    imagem publicada. O README ensina a instalar baixando só a pasta `deploy/`.
  - Aceite: instalação do zero numa máquina Linux seguindo só o README.
  > Revisado (2026-09-30): o workflow é o mesmo da release do 5.4. Uma tag `v*` publica a imagem e
  > cria a release no GitHub. Primeira versão: `v0.1.0`.

- [ ] **5.4 APK de release assinado** (depende do usuário gerar o keystore e cadastrar os secrets)
  - Escopo: build de release com `--split-per-abi` (armeabi-v7a e arm64-v8a) publicada no
    GitHub Releases.
  - Aceite: APK de release instalado no A10 e no 6A.
  > Revisado (2026-09-30): sai no workflow de release por tag do 5.3, com as notas geradas pelos
  > commits e os APKs anexados. A mesma chave assina o APK do GitHub e o da Play (na Play, enviar a
  > própria chave em vez de deixar a Google gerar uma), para quem instalou por um poder atualizar pelo
  > outro. O autor gera a chave, guarda backup em dois lugares e cadastra os secrets.

- [ ] **5.5 Contribuição e CLA** (depende do usuário escolher o texto do CLA e instalar o CLA Assistant)
  - Escopo: `CONTRIBUTING.md` (como rodar, gate, Conventional Commits, CLA obrigatório) e o
    documento do CLA.
  - Aceite: antes do primeiro pull request externo, o CLA Assistant pede o aceite num PR de
    teste.

- [ ] **5.6 CI verde no GitHub**
  - Origem: autor em 2026-09-30. O job `gate` falha no GitHub com 1 teste do `Recam.Server.Tests`
    quebrando (394 de 395 passam), e o gate local passa.
  - Escopo: achar o teste pelo log do run, entender por que só falha no runner e consertar o teste
    ou o código. Sem retry nem skip.
  - Aceite: três runs seguidos verdes na `main`.
  > Em andamento (2026-09-30): pelo `gh`, os 10 últimos runs falharam sempre no mesmo teste,
  > `MotionScriptTests.MotionScript_StillAndWalking_OnlyWalkMoves`. As asserções passam; quebra no
  > `Dispose` da pasta temporária. O container do teste roda como root e cria arquivos na pasta
  > montada, que o usuário do runner não consegue apagar (no Windows, com Docker Desktop, não há
  > esse problema de dono, por isso o gate local passa). O script do container agora abre as
  > permissões do que escreveu ao sair (`trap ... EXIT`), no mesmo jeito do teste da imagem de
  > detecção. Falta: push e três runs verdes na `main`.

- [ ] **5.7 README com imagens e About do repositório**
  - Origem: autor em 2026-09-30.
  - Escopo: capturas do navegador (lista de câmeras, vídeo ao vivo, gravações com a linha do tempo,
    pessoas detectadas) e do app (ler QR, modo câmera) no `README.md`, junto com o GIF do 5.2.3.
    Texto do About e tópicos do repositório (self-hosted, dotnet, flutter, webrtc, android, camera)
    propostos para o autor colar no GitHub. Seguir a regra de textos genéricos e a skill stop-slop.
  - Aceite: README com as imagens renderizando no GitHub; About e tópicos preenchidos pelo autor.
  > Em andamento (2026-09-30): o `README.md` ganhou a imagem de abertura e a seção Screenshots,
  > apontando para seis arquivos em `docs/images/` (`monitor-live`, `monitor-cameras`,
  > `recordings-timeline`, `people-on-this-day`, `app-scan-qr`, `app-camera-mode`, todos `.png`).
  > About e tópicos entregues ao autor na conversa. Falta: o autor capturar as seis telas nos
  > aparelhos reais, colar o About no GitHub, e o commit com as imagens.
  > Revisado (2026-09-30): o autor capturou cinco telas (A10 e navegador, servidor na VPS). A de
  > pessoas detectadas ficou de fora por privacidade, e a seção mostra câmeras e gravações na grade e
  > as duas telas do celular embaixo. O vídeo ao vivo atual é provisório; o autor vai trocar a cena.
  > Falta: ver as imagens renderizando no GitHub e o About colado.

- [ ] **5.8 Ícone, splash e favicon**
  - Origem: autor em 2026-09-30. O app usa o ícone padrão do Flutter e abre com a tela branca.
  - Escopo: ícone escolhido pelo autor (seta circular em volta de um celular deitado, visto de
    costas, branco sobre `#00695C`). No app: ícone adaptativo em vetor com a camada monocromática,
    e splash com o fundo `#00695C` e o símbolo, do Android 9 ao 12+. No Monitor: favicon em SVG.
    Em `docs/brand/`: o SVG, o PNG de 512 px e o gráfico de 1024x500 da Play. Logo no topo do
    `README.md`. Sem dependência nova: os PNG saem de um navegador renderizando o SVG.
  - Aceite: no celular, o ícone aparece no launcher e a splash sai verde-azulada com o símbolo; no
    navegador, a aba mostra o ícone.
  > Em andamento (2026-09-30): `docs/brand/icon.svg` é a fonte. No app, `ic_launcher_foreground.xml`
  > em vetor (o desenho reduzido a 80% para caber na zona segura), `mipmap-anydpi-v26/ic_launcher.xml`
  > adaptativo com fundo `recam_brand` e camada monocromática; os PNG antigos do Flutter saíram
  > (minSdk 28 só usa o adaptativo). Splash: `launch_background.xml` com o fundo e o símbolo até o
  > Android 11, e `values-v31`/`values-night-v31` com a splash do sistema no 12+. Monitor:
  > `favicon.svg` ligado no `index.html`. `docs/brand/` tem o `icon-512.png` e o
  > `feature-graphic.png` (1024x500), renderizados pelo Edge. Logo no título do `README.md`.
  > Observado no emulador (Android 16): ícone no launcher e splash verde-azulada com o símbolo.
  > Falta: conferir no A10 (Android 11) e a aba do navegador.

---

- [x] **11.7 [aparelho] Teste no PC antigo com o README paralelo**
  - Origem: pedido do autor em 2026-09-28. Um PC antigo (i5 de 3ª geração, SSD) com Linux Mint
    instalado do zero faz o papel de um usuário novo.
  - Escopo: o autor segue só o `README.preview.md`, escrito como se fosse o README definitivo
    (instalação rápida, primeiro uso e problemas comuns). Ele não é a versão final: depois do
    teste, o que funcionou vai para o `README.md` de verdade e o `README.preview.md` é apagado.
    O app ainda não tem APK publicado (5.4), então no teste ele vem instalado por `adb`.
    Nesta branch (Fase 12), o `README.preview.md` instala com a detecção de pessoas ligada
    (`COMPOSE_FILE=compose.yaml:compose.detect.yaml` no `.env`), para ver também quanto ela pesa
    num processador antigo.
  - A observar: instalação do Docker no Mint, compose em rede `host` achando o IP da rede local
    sozinho, firewall, atraso na rede local, e uso de processador do FFmpeg de movimento e da
    cifra num processador antigo.
  - Aceite: o autor chega ao ao vivo seguindo só o README paralelo; cada tropeço vira bullet; o
    `README.preview.md` é apagado.
  > Validação (2026-09-29): o autor instalou no PC antigo (i5-3470, Linux Mint) seguindo o
  > `README.preview.md`, tanto da `main` quanto da branch da detecção, e tudo funcionou. O guia
  > foi para o `README.md` (instalação, primeiro uso, problemas comuns, atualizar, parar e os
  > containers), com a detecção de pessoas como passo opcional, e o `README.preview.md` foi
  > apagado.

- [x] **11.8 [aparelho] Modo câmera sem internet: avisar na hora, e com rede ruim tentar e explicar**
  - Origem: teste do autor em 2026-09-28. O Samsung A10 já pareado, em modo avião, toca em
    "Iniciar modo câmera": a tela fica em "Conectando ao servidor…" sem dizer que não há rede.
  - Escopo:
    - Antes de conectar, o app confere se o celular tem alguma rede (Wi-Fi ou dados). Sem rede,
      não entra no modo câmera: mostra na hora "Sem conexão. Ligue o Wi-Fi e tente de novo.",
      com o botão de tentar de novo.
    - Com rede, mas sem chegar ao servidor (rede ruim, servidor desligado, IP mudou), continua
      tentando por um tempo fixo (30 s) mostrando "Conectando ao servidor…". Passado esse tempo,
      troca por uma mensagem clara ("Não foi possível falar com o servidor. Confira se o celular
      está na mesma rede e se o servidor está ligado.") com "Tentar de novo" e "Voltar". A tela
      nunca sai sozinha por falta de rede.
    - A conferência de rede usa só o que o Android já sabe (`ConnectivityManager`, pelo canal
      nativo que já existe); nada de chamada a site de fora para "testar a internet".
    - Textos nos ARB, en e pt.
  - Aceite: testes de widget dos três casos (sem rede, rede sem servidor passando do prazo,
    conexão que chega dentro do prazo) e teste de que ficar sem rede não apaga o pareamento. No
    A10: modo avião mostra o aviso na hora; Wi-Fi ligado com o servidor parado mostra a mensagem
    depois de 30 s; ligar o servidor e tocar em "Tentar de novo" entra no modo câmera.
  > Em andamento (2026-09-28): feito o código. O Android responde `hasNetwork` pelo canal
  > `io.recam.app/device` (`ConnectivityManager`, rede ativa com `NET_CAPABILITY_INTERNET`; o
  > manifesto ganhou `ACCESS_NETWORK_STATE`). Sem rede, o `CameraModeController` não liga serviço,
  > tela nem hub, e a tela mostra "Sem conexão. Ligue o Wi-Fi e tente de novo." com "Tentar de
  > novo" e "Voltar". Com rede, a primeira conexão tem 30 s (`connectTimeout`); passado o prazo, a
  > tela mostra "Não foi possível falar com o servidor…", e o hub segue tentando por trás: se
  > conectar, a mensagem some sozinha. "Tentar de novo" corta a espera do backoff
  > (`HubSession.retryNow`). Queda depois de já ter conectado não tem prazo, como antes. Nada
  > disso apaga o pareamento. Testes: controller (sem rede não começa, volta com a rede, prazo
  > vencido, tentar de novo conecta na hora, conexão no prazo, queda depois de conectado), hub
  > (`retryNow`) e widget (as duas mensagens, sem pareamento perdido). Falta: conferir no A10.
  > Validação (2026-09-28): o autor testou no A10 e o aviso de sem conexão e a tentativa com
  > prazo funcionaram.

- [x] **11.9 [aparelho] Primeiro minuto de cada gravação não toca no navegador**
  - Origem: teste do autor em 2026-09-28, servidor na VPS. Na tela de gravações, o arquivo do
    primeiro minuto de uma transmissão fica preto e o play não faz nada; os seguintes tocam.
  - Causa (conferida no arquivo decifrado): no começo da transmissão o WebRTC do celular sobe a
    resolução aos poucos enquanto mede a rede (320x180, 480x270, 640x360, 960x540 e só então
    1280x720). O MediaMTX grava tudo no mesmo MP4, e o Chromium para com
    `PIPELINE_ERROR_DECODE` quando a resolução muda no meio do arquivo. Os minutos seguintes ficam
    em 1280x720 do começo ao fim e tocam. O mesmo vale para o "celular quente" (9.x), que reduz a
    resolução no meio da transmissão (`scaleResolutionDownBy`).
  - Escopo:
    - O `WhipPublisher` pede `degradationPreference: maintain-resolution` no sender: com rede
      fraca, o celular baixa quadros por segundo e qualidade, não a resolução.
    - A qualidade reduzida por calor passa a baixar só bitrate e quadros por segundo, mantendo a
      resolução.
    - Não mexer no MediaMTX nem no servidor.
  - Aceite: teste do publisher conferindo a preferência e que a qualidade reduzida não muda a
    resolução. Na VPS: o arquivo do primeiro minuto de uma transmissão nova toca no navegador, e o
    `ffprobe` dele mostra uma resolução só.
  > Em andamento (2026-09-28): feito o código. `WhipPublisher.withQuality` põe
  > `degradationPreference: maintain-resolution` e `scaleResolutionDownBy: 1` nos parâmetros do
  > sender; o publisher aplica logo depois de criar o sender (pelo `setQuality`, que engole a
  > recusa de um celular que não aceite, e aí ele transmite como antes) e a cada troca de
  > qualidade. `VideoQuality` perdeu a escala: a reduzida é 10 fps e 400 kbps em 1280x720. A
  > preferência chega ao libwebrtc pelo `setParameters` do `flutter_webrtc` no Android. Testes:
  > `withQuality` nas duas qualidades e o `VideoQuality`. Falta: transmissão nova na VPS e o
  > primeiro minuto tocando no navegador.
  > Validação (2026-09-28): o autor iniciou uma transmissão nova do A10 para a VPS, com o app da
  > branch da Fase 12, e o arquivo do primeiro minuto tocou no navegador.

- [ ] **11.10 Logs dos containers com tamanho limitado**
  - Origem: pergunta do autor em 2026-09-28. O compose não configura rotação: com o driver padrão
    `json-file`, o log de cada container cresce para sempre, e o do servidor escreve várias linhas
    a cada ao vivo aberto ou fechado. Em meses, pode encher o disco e parar as gravações.
  - Escopo:
    - Todo serviço de `compose.yaml`, `compose.bridge.yaml` e `compose.detect.yaml` ganha
      `logging` com `json-file`, `max-size: 10m` e `max-file: 3`.
    - No servidor, o `HttpClient` (as chamadas ao MediaMTX) passa a logar só `Warning` ou pior.
    - Revisão no `SPECS.md` (Deploy).
  - Aceite: `docker compose config` mostra o limite em todos os serviços; na VPS, `docker inspect`
    confirma a opção, e abrir e fechar o ao vivo não gera mais as linhas de "Start processing HTTP
    request".
- [x] **11.11 Documentar os containers da stack e desativação do motion no README**
  - Origem: pedido do autor em 2026-09-28 durante testes em máquina de entrada. (Criado como
    11.8 no GitHub; renumerado no merge porque a 11.8 local já existia.)
  - Escopo: no `README.md`, detalhar o papel de cada serviço (`server`, `mediamtx`, `motion`),
    o que roda em cada container, e que o serviço de detecção de movimento (`motion`) pode ser
    interrompido em hardware extremamente modesto para economizar recursos sem afetar o vídeo
    ao vivo, o pareamento ou a gravação contínua (embora não seja necessário, pois o consumo
    é inferior a 60 MB de RAM).
  - Aceite: seção explicativa no `README.md`.
  > Validação (2026-09-28): seção "The containers" adicionada ao `README.md` detalhando os três
  > serviços da stack e a nota sobre desativação em hardware de baixo consumo.

- [x] **11.12 README genérico, com a detecção de pessoas ligada no guia**
  - Origem: pedido do autor em 2026-09-29. O README citava a máquina e as circunstâncias do teste
    dele, e a detecção vinha como passo opcional no meio da instalação.
  - Escopo: o começo do README vira só comandos para copiar e colar, com a detecção ligada; os
    requisitos ficam genéricos; uma seção "Person detection" no fim diz o que ela custa, quando
    não vale a pena (1 núcleo ou 1 GB de RAM, disco pequeno, gente passando o dia todo) e como
    desligar. O `compose.detect.yaml` continua um arquivo à parte: o padrão vem do `.env` que o
    guia escreve.
  - Aceite: README sem aparelho, marca ou máquina do autor.
  > Validação (2026-09-29): README reescrito como descrito. Só documentação.

## Fase 12: pessoas nas gravações

Juntada à `main` em 2026-09-29, depois do teste no PC antigo; o serviço segue opcional.
Decidido com o autor em 2026-09-28, na branch `claude/person-activity-detection-4pq7pg`, que pode
não ir para a `main`. A meta final é uma linha do tempo que diz "14:02, pessoa no Quarto". Esta
fase faz só a primeira parte: saber se tinha pessoa em cada evento de movimento que o 7.2 já acha.

Decisões da conversa:
- Só detecção de pessoa, com um modelo YOLO nano. Nada de descrever a cena em texto ("mexeu nas
  gavetas") nesta fase: isso pede modelo de visão com linguagem, pesado demais para o PC alvo.
- Só nas gravações (câmeras com "Gravar sempre"), nunca no vídeo ao vivo.
- Tudo local. Nenhuma API externa, nenhuma chamada de rede do container de detecção.
- Opcional: um compose à parte, `deploy/compose.detect.yaml`, como o de observabilidade. Quem não
  sobe esse compose não baixa nada a mais, e o ReCam funciona igual a hoje.

- [x] **12.1 Container opcional que acha pessoas nos segmentos com movimento**
  - Origem: conversa com o autor em 2026-09-28.
  - Escopo:
    - Projeto novo `server/src/Recam.Detect`, um worker .NET de console na mesma solução (o gate
      já o cobre), com imagem própria que traz o FFmpeg. Dependência nova pedida por este bullet:
      `Microsoft.ML.OnnxRuntime` (CPU). O modelo é o YOLO nano em ONNX (Ultralytics, AGPL-3.0,
      compatível com o ReCam), baixado no build da imagem com SHA-256 fixo, nunca em tempo de
      execução. Versão exata do modelo e do pacote no ADR.
    - Mesmo padrão do `motion`: monta `recam-recordings`, roda em loop, pega cada segmento que já
      tem o `.motion` pronto e ainda não tem o `.people`. Só olha os segundos em que o `.motion`
      passou do limite da sensibilidade alta (0,3%), um quadro por segundo, pedido ao FFmpeg por
      pipe em RGB cru (sem biblioteca de imagem). Grava ao lado do `.mp4` um
      `<segmento>.people` com uma linha por segundo olhado: `<segundos do início>` seguido de
      zero ou mais caixas de pessoa, cada uma `<confiança> <x> <y> <largura> <altura>`, com
      confiança de 0 a 1 e coordenadas de 0 a 1 relativas ao quadro. Segundo sem pessoa fica só
      com o tempo. As caixas já saem aqui para o 12.4 não precisar reprocessar nada. Arquivo ilegível vira `.people` vazio, como no `motion`.
    - Só a classe "pessoa" do modelo. Os outros resultados são descartados.
    - Compose: serviço `detect` só no `compose.detect.yaml`, usuário 1654, `network_mode: none`,
      sem porta, limite de CPU configurável no `.env` (padrão 1 núcleo) para não roubar o
      servidor. Comentário no topo com o comando para subir, como no de observabilidade.
    - "O .NET não processa vídeo" continua valendo para o `Recam.Server`. Revisão do `SPECS.md`
      (seções 2.4 e 7, regra reescrita para dizer que vale para o servidor principal, e o
      container opcional descrito) e ADR novo. Seção curta no `README.md` dizendo como ligar e
      desligar.
  - Fora: carro, animal, zonas, notificação, vídeo ao vivo, descrição em texto, GPU.
  - Aceite: teste do pós-processamento (saída do modelo para confiança de pessoa, só a classe
    pessoa, limites, caixas em coordenadas relativas) com tensor de exemplo, e teste com Testcontainers rodando o worker sobre um
    segmento feito de uma foto com pessoa e outra sem (fotos de licença livre, CC0, guardadas nos
    testes): o `.people` do primeiro passa de 0,5 e o do segundo fica abaixo. `docker compose -f
    compose.yaml -f compose.detect.yaml config` passa.
  > Validação (2026-09-28): código escrito e cadeia percorrida no Docker deste ambiente, sem
  > celular. Mudança no modelo, registrada no `SPECS.md` 12 e no ADR 0043: no lugar do YOLO nano da
  > Ultralytics ficou o YOLOX-Tiny (Megvii, Apache-2.0, ONNX pronto na release oficial, SHA-256
  > fixo no Dockerfile), porque a Ultralytics só publica os pesos em PyTorch e exportar exigiria
  > PyTorch no build. `Recam.Detect`: `MotionSeconds` (segundos acima de 0,3%), `SegmentFrames`
  > (ffprobe e FFmpeg por pipe, um quadro por segundo em 416 px com borda cinza), `PersonDetector`
  > (ONNX Runtime 1.30.0, telemetria desligada), `YoloxDecoder` (grades 8/16/32, só pessoa, corte
  > em 0,3, sobreposição acima de 45% vira uma caixa), `PeopleFile` e `SegmentScanner` (pula
  > segmento cifrado, toca `/recordings/.detect` antes de cada segmento). Imagem sobre a mesma
  > `linuxserver/ffmpeg` do `motion` (acrescenta cerca de 260 MB), usuário 1654. Mudança fora do
  > texto do bullet, necessária para ele funcionar: a cifra das gravações (9.13) apagaria a chance
  > de ler o vídeo, então o `RecordingCipherWorker` espera também o `.people` enquanto o
  > heartbeat tem menos de 5 minutos, com o mesmo limite de 30 minutos. `compose.detect.yaml` com
  > `network_mode: none`, `RECAM_DETECT_CPUS` e `RECAM_DETECT_THREADS` no `.env.example`, seção no
  > README, `SPECS.md` 2.6 e 7, regra do `AGENTS.md` reescrita. As fotos dos testes vieram do
  > Darknet (domínio público), não de um banco CC0, porque o Wikimedia está bloqueado aqui.
  > Observado: `docker compose -f compose.yaml -f compose.detect.yaml up -d --build` subiu os quatro
  > serviços; um segmento cinza em que a foto de um homem aparece no segundo 3 ganhou `.motion`
  > (0,95 no 3,0), depois `.people` com `3 0.9148 0.2978 0.2385 0.1307 0.6587`, e só então foi
  > cifrado. Parado, o `detect` usa 0% de CPU e 83 MB. Testes: decodificador (caixa relativa, outras
  > classes, sobreposição, borda), `MotionSeconds`, `PeopleFile`, cifra esperando e não esperando o
  > detect, e Testcontainers construindo a imagem pelo Dockerfile: pessoa com 0,91, cachorro sem
  > ninguém acima de 0,5, segmento cifrado e segmento sem `.motion` pulados. Gate verde (404 no
  > servidor e no web, 299 no app).

- [x] **12.2 O servidor marca os eventos de movimento com pessoa**
  - Origem: continuação do 12.1.
  - Escopo: o domínio junta os `.people` aos eventos que o `MotionEvents` já monta. Um evento tem
    pessoa quando algum segundo dentro dele (com a mesma folga da emenda) tem confiança de 0,5 ou
    mais. Três estados por evento: com pessoa, sem pessoa, e não analisado (sem `.people`, que é o
    caso de quem não instalou o 12.1 ou do segmento ainda na fila). O
    `GET /api/cameras/{id}/motion?day=` ganha esse campo sem quebrar quem já o usa. A leitura dos
    `.people` fica no `RecordingStore`, e a limpeza da cota apaga o `.people` junto com o
    segmento, e os órfãos.
  - Fora: guardar pessoas no banco. O arquivo ao lado do segmento basta, como no 7.2.
  - Aceite: teste do domínio (os três estados, pessoa na borda do evento, evento emendado de dois
    segmentos), teste do store e teste do endpoint. Revisão do `SPECS.md` (seção 5).
  > Validação (2026-09-28): só código escrito e testes; sem celular e sem navegador. Domínio:
  > `PersonBox`, `PeopleSample`, `SegmentPeople` e `PeopleInMotion.HasPerson` (confiança 0,5, folga
  > de 1 s, segmento termina no seguinte ou em 60 s, como na linha do tempo). `RecordingStore`:
  > `ReadPeople` (null quando não há arquivo, linha quebrada ignorada), `Delete` apaga o `.people`, e
  > `DeleteOrphanMotion` virou `DeleteOrphanNotes`, que também apaga `.people` órfão. Rota:
  > `MotionEventResponse` ganhou `Person` (`bool?`), campo novo que clientes antigos ignoram.
  > Testes: domínio (com pessoa, só caixa fraca, não analisado, borda de 1 s e 2 s, evento em dois
  > segmentos), store (leitura, arquivo faltando, apagar e órfãos), ida e volta do formato com o
  > `PeopleFile` do `Recam.Detect` (o projeto de testes do servidor referencia o worker só para
  > isso) e rota (evento com pessoa e evento não analisado). `SPECS.md` 2.6 e 5. Gate verde (412
  > no servidor, no web e no detect, 299 no app).

- [x] **12.3 Pessoas na linha do tempo, no navegador e no app**
  - Origem: continuação do 12.2; é a tela que o autor pediu ("às 14h uma pessoa entrou no
    quarto").
  - Escopo: na tela de gravações, os eventos com pessoa ganham uma marca própria na barra das 24 h
    e uma lista simples do dia, uma linha por evento: hora, "Pessoa" e o nome da câmera. Tocar na
    linha toca a partir de 5 s antes, como o "Próximo movimento". Filtro "Só pessoas" ao lado do
    "Só movimento". Marca, lista e filtro só aparecem quando algum evento do dia foi analisado;
    sem o 12.1, a tela fica igual a hoje. Textos nos ARB e nos `.resx`, em `en` e `pt`.
  - Fora: frases geradas por modelo de linguagem.
  - Aceite: widget test no app e teste bUnit no navegador (marca, lista, filtro, e a tela sem
    nada novo quando nenhum evento foi analisado).
  > Validação (2026-09-28): código escrito e testes; não percorrido no celular nem no navegador.
  > Navegador: `MotionEventInfo` e `MotionMark` com `Person`; `TimelineController` com
  > `PeopleAnalyzed`, `People`, `OnlyPeople`, `ShownMotion` (o que a barra e os botões de movimento
  > usam) e `PlayMark`; o "Só pessoas" liga o mesmo comportamento do "Só movimento" com só os
  > eventos com pessoa. Na `TimelineView`, a marca de pessoa é verde (`--person`, claro e escuro), a
  > seção "Pessoas neste dia" lista "14:02 · Pessoa · Porta" e tocar na linha toca 5 s antes; sem
  > evento analisado, nada disso aparece. App: o mesmo no `RecordingTimelineController`, marca
  > `person-mark` verde na barra, `_PeopleList` com `ListTile` por pessoa e chave "Só pessoas"; o
  > `RecordingsTimelinePane` passou a receber o nome da câmera (tela própria e aba Gravações). O
  > cliente HTTP lê `person` quando é booleano. Textos novos nos `.resx` e nos ARB, em `en` e `pt`.
  > Testes: controller (só pessoas no próximo movimento e no fim do arquivo, dia não analisado,
  > tocar pela lista) e tela (marcas, lista, filtro, dia sem análise, dia analisado sem ninguém), no
  > bUnit e no widget test. Gate verde (418 no servidor, no web e no detect, 305 no app).

- [x] **12.4 Caixa ao redor da pessoa no player**
  - Origem: pergunta do autor em 2026-09-28 ("vai aparecer um retângulo verde ao redor da
    detecção?").
  - Escopo: no player das gravações, no navegador e no app, um retângulo verde desenhado por cima
    do vídeo em cada pessoa, lido das caixas do `.people`. Nada é gravado no vídeo, e o servidor
    só repassa as caixas. Como a análise é de um quadro por segundo, a posição é interpolada entre
    dois segundos seguidos, para a caixa andar em vez de pular; sem caixa no segundo seguinte, ela
    some. As caixas acompanham o tamanho do vídeo na tela (tela cheia, giro, "Clarear").
    Chave "Mostrar pessoas" no player, ligada por padrão e guardada no próprio Monitor, como o
    ajuste do 7.1. A chave só aparece quando o arquivo tocando foi analisado. O servidor entrega as
    caixas do segmento numa rota nova, só para Monitores; o formato vai no `SPECS.md` 5. Textos
    nos ARB e nos `.resx`.
  - Fora: caixa no vídeo ao vivo, mais quadros por segundo, nome ou rótulo na caixa.
  - Aceite: teste da interpolação (meio do caminho, caixa que some, duas pessoas), teste da rota
    e widget test no app e bUnit no navegador (caixas desenhadas, chave liga e desliga, sem chave
    quando o arquivo não foi analisado).
  > Validação (2026-09-28): código escrito; no navegador, percorrido no Chromium com a stack real
  > (servidor, MediaMTX, `motion` e `detect` no compose); no app, só código e testes (sem celular).
  > Servidor: `GET /api/recordings/{cameraId}/{segmento}/people`, só caixas de 0,5 ou mais, 404
  > quando não analisado. Navegador: `PersonTrack` em C# (interpolação, cada caixa segue a mais
  > próxima até 0,25 do quadro, sem par ela fica parada até o fim do segundo e some) exposto ao
  > `wwwroot/js/people.js` por `[JSInvokable]`; o script só encaixa as caixas na parte do `<video>`
  > que a imagem ocupa, a cada quadro. Chave **Mostrar pessoas** no `localStorage`
  > (`JsPeopleBoxesStore`). Mudança além do texto do bullet: o botão de tela cheia do próprio
  > `<video>` não deixa desenhar por cima, então ele ficou escondido (`controlslist="nofullscreen"`)
  > e o Monitor ganhou um botão **Tela cheia** que leva o player inteiro. App: `PersonTrack` em
  > Dart com as mesmas regras, `PeopleBoxes` (`CustomPaint` com `Ticker`, correndo o tempo pelo
  > relógio entre os avisos de posição do player), desenhado fora do filtro do **Clarear**;
  > `RecordingPlayer` ganhou `aspectRatio`; chave guardada no `SecurePeopleBoxesStore`. O app não
  > tem tela cheia, então não há o que acompanhar ali. Observado no Chromium: a câmera "Quarto"
  > pareada pela API, um segmento de 12 s em que a foto de um homem atravessa a cena ganhou
  > `.motion`, `.people` (x de 0,17 a 0,33 nos três primeiros segundos) e depois foi cifrado; a tela
  > de gravações mostrou "17:36 · Pessoa · Quarto" e a marca verde (o 12.3 funcionando);
  > tocar na linha abriu o vídeo, e aos 3,5 s havia um retângulo verde (`rgb(46, 125, 50)`) em volta
  > do homem, que um segundo depois tinha andado 56 px para a direita; desligar **Mostrar pessoas**
  > tirou a caixa e gravou `off`; nenhum erro no console (a CSP não reclamou). Para tocar no
  > Chromium do Playwright, que não tem H.264, esse segmento foi gravado em VP9; o MediaMTX grava
  > H.264. A caixa ficou alguns pixels à frente do homem andando (cerca de 0,3 s), aceitável para
  > a linha do tempo. Testes: `PersonTrack` nos dois lados, rota, controller (pessoas do arquivo
  > tocando, chave lembrada) e tela (caixas desenhadas, chave desliga, sem caixa e sem chave quando
  > não analisado, tela cheia) no bUnit e no widget test, e o cliente HTTP do app. `SPECS.md` 2.6 e
  > 5. Gate verde (427 no servidor, no web e no detect, 314 no app).

- [x] **12.5 [aparelho] Medir no PC antigo**
  - Origem: conversa com o autor em 2026-09-28 (i5 de 3ª geração, sem AVX2).
  - Escopo: subir o `compose.detect.yaml` no PC antigo, gravar um dia com movimento e pessoa, e
    anotar quanto tempo o `detect` leva por segmento, quanto de CPU e memória usa, e se o vídeo ao
    vivo continua abaixo de 1 s de atraso com ele rodando. Conferir a lista de pessoas e as caixas
    no player.
  - Aceite: nota de validação com os números medidos.
  > Bloqueado (2026-09-28): aguardando validação no aparelho (PC antigo do autor).
  > Validação (2026-09-29): medido pelo autor no i5-3470 com Linux Mint. Em 5 minutos com servidor, MediaMTX e `detect`: cerca
  > de 410 MB de RAM no total (`detect` 147 MB, estável), CPU média de 3,2% da máquina e pico de
  > 7,5%, temperatura até 66 °C. A lista de pessoas e as caixas no player funcionaram. O tempo por
  > segmento e o atraso do ao vivo não entraram no relatório.

- [ ] **12.6 Lista "Pessoas neste dia": mais recentes primeiro e altura limitada**
  - Origem: teste do autor em 2026-09-28. Num dia com muitas passagens, a lista cresce sem fim e
    empurra o player para baixo, e começa pela mais antiga.
  - Escopo:
    - No navegador, a lista vai do mais recente para o mais antigo, com altura máxima e rolagem
      própria, e funciona na largura de celular.
    - No app, conferir a mesma lista: se também começa pela mais antiga ou cresce sem limite,
      aplicar a mesma regra.
    - A ordem de "Próximo movimento" e "Movimento anterior" não muda.
  - Aceite: bUnit e teste de widget da ordem e do limite; conferido no navegador e no app com um
    dia de muitas passagens.

## Fora da fila (anotado, não executar)

Itens que dependem de decisão futura. O loop para antes daqui.

- Acesso fora da rede local.
- Gravar câmera sem H.264 (o Redmi 6A, MediaTek, cai para VP8, que o MediaMTX não grava). Caminho
  provável: converter para H.264 no servidor com FFmpeg. Bom candidato a contribuição externa
  (2026-09-26).
- Tocar um som na câmera (sirene, campainha) pelo Monitor (2026-09-26).
- Falar pela câmera: áudio do Monitor tocando no celular câmera (2026-09-26).
- Detecção de movimento e notificações.
- iOS.
- Fallback por TCP ou HLS.
- Publicação nas lojas de apps de servidor caseiro (Umbrel, CasaOS, Unraid, Synology).
- Carros e animais com IA, e zonas (ignorar rua e calçada). A detecção de pessoas virou a Fase 12,
  na branch `claude/person-activity-detection-4pq7pg` (2026-09-28).
- Descrever a cena em texto ("alguém mexeu nas gavetas") com modelo de visão com linguagem. Pesado
  demais para o PC alvo; fica para depois da Fase 12 (2026-09-28).
- Modo noturno (conversa de 2026-09-25): botão no Monitor que manda a câmera aceitar de 5 a 15
  quadros por segundo em vez de 15 fixos, para expor por mais tempo no escuro. Vale para todos os
  Monitores e para a gravação, como a lanterna. Antes do botão, testar no A10 e no 6A se o
  `flutter_webrtc` repassa a faixa de quadros para a câmera; se não repassar, exige captura nativa.
