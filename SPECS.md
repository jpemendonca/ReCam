# ReCam: especificação

## 1. Produto

ReCam reaproveita celulares Android parados como câmeras de monitoramento. Três papéis:

- **Câmera** (celular câmera): captura e transmite. Não guarda nada, não decide nada.
- **Servidor**: roda em Docker na casa do usuário ou numa VPS. Pareia aparelhos, autentica,
  repassa comandos e distribui o vídeo.
- **Monitor** (quem assiste): lista as câmeras, assiste ao vivo, manda comandos e administra. O
  primeiro Monitor é sempre o navegador do computador; celulares podem virar Monitor depois.

Um único app Flutter faz os dois papéis de celular, mas cada celular tem um papel só: **Câmera** ou
**Monitor** (revisado em 2026-09-26, bullet 8.1). O
servidor serve o Monitor no navegador (seção 2.5), com as mesmas funções do Monitor no app.

Primeiro uso (revisado em 2026-09-25, seção 12): o navegador vira o primeiro Monitor com o código
de primeira abertura do log, e o primeiro celular pareado é uma câmera, lendo o QR que o navegador
mostra. A pessoa já vê o produto funcionando no primeiro minuto. Outros celulares, como câmera ou
como Monitor, entram depois pelo navegador.

Glossário das telas (revisado em 2026-09-25):

| Onde | Texto | Papel |
|---|---|---|
| Primeira abertura do app | **Ler QR code** / Scan QR code | o QR decide: Câmera ou Monitor |
| Navegador, primeira abertura | **Código de primeira abertura** / First-time code | o navegador vira Monitor |
| Navegador e app | **Adicionar câmera** / Add camera, **Adicionar Monitor** / Add Monitor | geram o QR |
| Abas, textos do app e do navegador, log | **Câmera** / Camera e **Monitor** / Monitor | — |

No código e no protocolo, o Monitor é `Viewer` (ou `Owner`, o primeiro, que as telas não mostram)
e a Câmera é `Camera`. O celular Monitor pareia com o nome padrão "Monitor"; o navegador, com
"Navegador" seguido do navegador e do sistema (ex.: "Navegador · Chrome no Windows").

### 1.1 Dentro do escopo (MVP)

- Instalação com `docker compose up -d` em Linux. Windows via Docker Desktop/WSL como caso
  secundário.
- Primeiro Monitor no navegador, com o código de primeira abertura do log. Depois, câmeras e
  outros Monitores por QR code mostrado no navegador ou num celular Monitor.
- Monitor no navegador completo: câmeras ao vivo, lanterna, gravações, aparelhos, espaço das
  gravações, adicionar câmera e Monitor (seção 2.5). Entrou em 2026-09-25.
- Vídeo ao vivo na rede local, com atraso abaixo de 1 s.
- Transmissão sob demanda: a câmera só transmite enquanto alguém assiste.
- Lanterna liga/desliga durante o vídeo ao vivo.
- Telemetria de bateria (nível e se está carregando) e status online/offline.
- App em PT-BR e inglês.
- Android 9 (API 28) ou mais novo. Aparelhos de referência: Samsung Galaxy A10 e Xiaomi
  Redmi 6A.
- Gravação no servidor, ligada por câmera, com espaço total escolhido numa barrinha e o mais
  antigo apagado quando enche (seção 2.4). Nunca no celular câmera. Entrou em 2026-09-25.

### 1.2 Fora do escopo (anotado para depois)

- Acesso de fora da rede local (porta aberta, Tailscale, Cloudflare Tunnel em modo rede
  privada). Responsabilidade do usuário. O Cloudflare Tunnel com hostname público não transporta
  UDP, então não serve para o WebRTC.
- Notificações e detecção de pessoas e carros com IA. A detecção de movimento simples, marcada
  na linha do tempo, entrou na Fase 7 do ROADMAP em 2026-09-25.
- iOS (compilar exige macOS).
- Áudio e conversa bidirecional.
- Fallback de WebRTC por TCP e fallback por HLS.
- Contas com usuário e senha.

## 2. Arquitetura

```
[App: aba Câmera] ──HTTPS: REST, SignalR, WHIP──> [Recam.Server :8443] ──HTTP interno──> [MediaMTX :8889]
        │                                                 ▲                                     │
        └──────────── RTP/SRTP via UDP :8189 ──────────────┼────────────────────────────────────>│
                                                          │                                     │
[App: aba Assistir] ──HTTPS: REST, SignalR, WHEP──────────┤         RTP/SRTP via UDP :8189 <────┤
                                                          │                                     │
[Navegador: Recam.Web] ──HTTPS: REST, SignalR, WHEP───────┘         RTP/SRTP via UDP :8189 <────┘
```

- **Recam.Server (.NET 10, ASP.NET Core)**: plano de controle. Única porta TCP pública (8443,
  HTTPS). Serve a API REST, o hub SignalR, o Monitor no navegador (os arquivos do `Recam.Web`) e
  um proxy de sinalização para o WHIP/WHEP do MediaMTX. Não toca em RTP. Até a fase 6, servia a
  página `/setup`, que passa a redirecionar para a raiz.
- **MediaMTX (container oficial, versão fixada)**: plano de mídia. Recebe WHIP, entrega WHEP.
  A interface HTTP escuta só na rede interna (127.0.0.1 no modo host, rede do compose no modo
  bridge). Só a porta UDP 8189 fica exposta. RTSP, RTMP, HLS, SRT, API e métricas desligados.
- **SQLite** em `/data/recam.db`, via EF Core. Migrações aplicadas no startup.
- **Volume `/data`**: banco, certificado TLS. Volume nomeado `recam-data`.

### 2.1 Servidor: organização do código

Um projeto só, organizado por feature (fatias verticais):

```
server/src/Recam.Server/
  Program.cs
  Domain/                 entidades ricas, enums, erros de domínio, Result<T>
  Infrastructure/
    Hosting/              ServerSettings (RECAM_DATA_DIR, porta 8443)
    Http/                 conversão de Error para ProblemDetails
    Persistence/          RecamDbContext, Migrations/
    Auth/                 DeviceAuthenticationHandler, políticas
    Tls/                  CertificateStore
    Network/              PublicUrlResolver
    Presence/             DevicePresence (conexões ativas por aparelho, em memória)
    Realtime/             HubResult (retorno de erro esperado dos métodos do hub)
    Observability/        OpenTelemetry (traces, métricas, OTLP opcional), RecamMetrics
  Features/
    Health/               GET /health
    Setup/                código de primeira abertura, entrar e sair do navegador
    Web/                  hospeda o Recam.Web na raiz (arquivos, cabeçalhos de segurança)
    Pairing/              POST /api/pair, POST /api/pairing-tokens
    Devices/              GET /api/me, GET /api/cameras
    Realtime/             DeviceHub, presença, telemetria, leases, lanterna
    Media/                proxy WHIP/WHEP
```

Regras de dependência, fiscalizadas por teste de arquitetura (NetArchTest):
- `Domain` não depende de `Infrastructure` nem de `Features`.
- `Infrastructure` não depende de `Features`.
- `Features.X` não depende de `Features.Y`. Se duas features precisam da mesma coisa, ela vai
  para `Domain` ou `Infrastructure`.

Padrões do servidor:

- **Minimal APIs.** Cada feature expõe `MapXxxEndpoints()` e `AddXxx()`. O `Program.cs` só
  chama essas extensões.
- **Domínio rico.** A regra de negócio mora na entidade. `PairingToken.Consume(now)` decide se
  o token pode ser usado. `Device.Revoke(now)` e `Device.ReportTelemetry(...)` protegem o
  próprio estado. Setters são privados. Os endpoints orquestram (carregam, chamam o método da
  entidade, salvam) e não decidem regra.
- **`Result<T>` para erro esperado.** Métodos que podem falhar por motivo de usuário ou de
  entrada devolvem `Result<T>` ou `Result`, com um `DomainError` tipado (código, mensagem, tipo:
  `Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`). Um erro de `Validation`
  carrega todos os campos inválidos (campo → mensagens) e vira `ValidationProblem`. Os erros de cada área
  ficam numa classe estática, ex.: `PairingErrors.TokenExpired`. O tipo `Result` é escrito no
  projeto, sem biblioteca.
- **Um ponto de tradução.** `Infrastructure/Http/ResultExtensions.ToHttpResult()` converte
  `DomainError` em `ProblemDetails` com o status certo. Handler não tem try/catch.
- **Exceção só para bug.** `UseExceptionHandler` com `AddProblemDetails` registra exceções não
  tratadas e devolve 500 sem detalhe interno.
- **Token usado por duas requisições ao mesmo tempo.** `PairingToken.UsedAt` é token de
  concorrência do EF. A segunda gravação falha com exceção (500) e nenhum segundo dispositivo é
  criado. É tratado como estado excepcional, porque exige alguém com o token em mãos disputando
  com o dono dele.
- **Sem MediatR, CQRS com bancos separados, microserviços ou event sourcing.** O domínio é
  pequeno, e esses padrões só adicionariam indireção.

### 2.2 App: organização do código

```
app/lib/
  main.dart               bootstrap: HttpOverrides, localização, runApp
  app.dart                MaterialApp; o HomeShell mostra a tela do papel do celular
  core/
    network/              PinnedHttpOverrides, ApiClient, HubConnectionFactory
    pairing/              QrPayload (parse), PairingService
    storage/              CredentialStore (flutter_secure_storage)
    media/                interfaces WebRtcPublisher, WebRtcViewer e implementações
    device/               interfaces de bateria, lanterna, tela
  camera/                 aba Câmera: pareamento, modo câmera
  viewer/                 aba Monitor: lista, ao vivo, adicionar câmera e Monitor, conectar navegador
  l10n/                   app_en.arb, app_pt.arb
```

- Estado: `ChangeNotifier` + `ListenableBuilder`, do próprio Flutter. Sem pacote de gerência de
  estado.
- Dependências passadas por construtor. Sem service locator.
- Plugins de plataforma (câmera, WebRTC, bateria, lanterna) ficam atrás de interfaces em
  `core/`. Os controllers dependem das interfaces, e os testes usam fakes escritos à mão.
- Regras de import, fiscalizadas por `test/architecture_test.dart` (lê os imports dos arquivos):
  - `camera/` não importa `viewer/`, e vice-versa.
  - `core/` não importa `camera/` nem `viewer/`.

Cada aba guarda a própria credencial. Um mesmo celular pode estar pareado como câmera e como
visualizador, mas no uso normal cada celular usa uma aba só.

### 2.3 Modo câmera no celular

Para rodar num celular fraco e não ser morto pelo Android:

- Ocioso: só a conexão SignalR aberta. Nenhuma câmera aberta, nenhum encoder rodando.
- Ao receber `StartPublishing`: abre a câmera traseira a 1280x720, 15 fps, via `flutter_webrtc`.
  H.264 preferido via `setCodecPreferences`, trilha de áudio Opus quando o microfone foi
  permitido (sem ele, só vídeo; revisado em 2026-09-26, bullet 9.1), `maxBitrate` 700 kbps no
  sender. Publica por WHIP.
- Ao receber `StopPublishing`: fecha a conexão e libera a câmera.
- O vídeo nunca passa pelo Dart. Câmera, encoder de hardware e rede ficam no código nativo do
  libwebrtc.
- Sem preview local enquanto transmite. A tela mostra o status (conectado, bateria, alguém
  assistindo) e o botão de parar, com wakelock ligado. Revisado em 2026-09-25: a tela preta com
  brilho mínimo foi retirada a pedido do autor.
- Foreground service do tipo `camera` enquanto o modo câmera está ativo, para manter a
  prioridade do processo. A lógica roda no isolate principal.
- Reconexão ao servidor com backoff exponencial: 1, 2, 4, 8, 16, 30 s, e depois 30 s fixo.

### 2.4 Gravação

Desenho combinado com o autor em 2026-09-25 (Fase 3 do ROADMAP):

- **O MediaMTX grava; o .NET decide e administra.** O servidor escolhe o que gravar, controla o
  espaço e serve os arquivos. O app no modo câmera continua sem gravar nada no aparelho.
- **Dois grupos de paths no MediaMTX.** `cam-<id>` só repassa. `rec-<id>` repassa e grava em fMP4,
  com segmentos de 60 s em `/recordings/rec-<id>/AAAA-MM-DD_HH-MM-SS-ffffff.mp4` (hora UTC do
  container) e `recordDeleteAfter: 0s`: o MediaMTX nunca apaga, quem apaga é o servidor, pela cota.
  O proxy WHIP/WHEP escolhe o path pelo estado da câmera (bullet 3.2).
- **Gravação ligada por câmera ("Gravar sempre").** Câmera gravando transmite o tempo todo, mesmo
  sem ninguém assistindo.
- **Espaço total** escolhido numa barrinha no Monitor; quando enche, o segmento mais antigo sai.
- **Linha do tempo** simples no Monitor: dias, horas com trechos gravados, tocar para assistir.
- **Volume `recam-recordings`**, montado em `/recordings` no MediaMTX e no servidor, nos dois
  composes.
- **Permissões.** O servidor roda como o usuário não-root das imagens .NET (UID 1654) e precisa
  apagar o que o MediaMTX grava. Por isso o MediaMTX roda com `user: "1654:1654"`, e o Dockerfile
  do servidor cria `/recordings` com esse dono. O compose sobe o servidor primeiro
  (`depends_on`), e o Docker copia essa permissão para o volume novo. Validado em 2026-09-25 com o
  mesmo arranjo: o MediaMTX gravou como 1654 e o servidor apagou o segmento e a pasta.
- **Movimento (Fase 7).** Um serviço `motion` (imagem `linuxserver/ffmpeg`, a mesma dos testes,
  com o usuário do servidor e sem rede) roda `deploy/motion.sh`: a cada 20 s, para cada segmento
  fechado (não o mais novo de cada câmera, salvo se ninguém escreve nele há 90 s), grava ao lado
  um `<segmento>.motion` com uma linha por meio segundo, "segundos fração", onde fração é a parte
  da imagem (em 160 px de largura, tons de cinza) que mudou mais de 30 de 255 níveis em relação ao
  meio segundo anterior. O ruído do sensor fica abaixo disso. O .NET não processa vídeo: lê essas
  notas e monta os eventos com a sensibilidade da câmera (`Device.MotionSensitivity`: baixa 3%,
  média 1%, alta 0,3%; padrão média), emendando trechos a menos de 10 s e ignorando os 5 s depois
  de a câmera avisar que a lanterna mudou (guardado em memória por três dias). Mudar a
  sensibilidade vale para o que já foi gravado. A limpeza da cota apaga as notas junto com o
  segmento e as notas que ficaram sem segmento. O evento aparece quando o segmento fecha, não na
  hora. Na linha do tempo (app e navegador), os eventos ficam marcados na barra das 24 h;
  "Movimento anterior" e "Próximo movimento" tocam a partir de 5 s antes do evento; "Só movimento"
  deixa na barra só os eventos e, quando um segmento termina, segue para o próximo movimento em vez
  do próximo segmento. A sensibilidade se escolhe na mesma tela.
- **Codec.** O MediaMTX 1.21.1 grava H.264 em fMP4, mas não grava VP8: com VP8 ele registra "no
  supported tracks found, skipping recording" e só repassa (seção 11).

### 2.5 Monitor no navegador

Desenho combinado com o autor em 2026-09-25 (Fase 6 do ROADMAP):

- **Cliente do mesmo protocolo.** O `Recam.Web` é um app Blazor WebAssembly que roda no navegador e
  fala com o servidor pela mesma API REST, pelo mesmo hub SignalR e pelo mesmo WHEP que o app
  Flutter usa. Ele tem os próprios tipos do protocolo, como o app. O `Recam.Server` só serve os
  arquivos na raiz. Assim nenhuma feature do servidor precisa conhecer outra, e o navegador não
  ganha atalho que o app não tenha.
- **Projetos.** `server/src/Recam.Web` (o cliente) e `server/tests/Recam.Web.Tests` (bUnit, com
  fakes escritos à mão da API, do hub e do vídeo). O `Recam.Server` referencia o `Recam.Web` para
  servir os arquivos publicados.
- **Organização do cliente.** Componentes finos; a lógica fica em classes C# comuns (um
  controller por tela, como no app), testáveis sem navegador. Acesso a rede atrás de interfaces
  (`IRecamApi`, `IDeviceHub`, `ILiveVideo`), com as implementações reais usando `HttpClient`, o
  cliente SignalR para .NET e um módulo JavaScript próprio para o WebRTC.
- **Vídeo ao vivo.** Um módulo JavaScript próprio (`wwwroot/js/whep.js`, sem biblioteca) cria o
  `RTCPeerConnection` só de recepção, manda a oferta para `POST /whep/{cameraId}` e mostra a trilha
  num `<video>`. O Blazor chama o módulo por interop. O lease de visualização e a lanterna vão pelo
  hub, como no app.
- **Gravações.** O `<video>` toca o segmento direto de `GET /api/recordings/{cameraId}/{segmento}`,
  com `Range` e o cookie. Não há relay, porque o navegador manda o cookie sozinho.
- **QR na tela.** O QR de "Adicionar câmera" e "Adicionar Monitor" é desenhado no navegador, em
  SVG, com o QRCoder (a mesma biblioteca do servidor).
- **Idioma.** `en` e `pt`, escolhidos pelo idioma do navegador (sem `pt`, inglês), em arquivos de
  recurso (`.resx`) com `IStringLocalizer`.
- **Nada de fora.** Tudo o que o navegador baixa sai da porta 8443: sem CDN, sem fonte externa,
  sem script de terceiros. Cabeçalho `Content-Security-Policy` restrito à própria origem (com o
  mínimo que o WebAssembly exige), `X-Content-Type-Options: nosniff` e `Referrer-Policy:
  no-referrer`.
- **Primeira abertura.** Sem Monitor ativo, a raiz mostra só o campo do código de primeira
  abertura (seção 6) e a caixa "Lembrar neste computador". Com o código certo, o navegador vira o
  `Owner` e vai direto para "Adicionar câmera", com o passo a passo (baixar o app, abrir, tocar em
  Ler QR code, ler o QR). Quando a primeira câmera pareia, o vídeo ao vivo dela abre sozinho.
- **Navegador que não é Monitor, com Monitor já existente.** A raiz mostra um QR de "Conectar
  navegador". Um celular Monitor lê pelo menu **Conectar navegador** e aprova; o navegador vira
  um `Viewer` (seção 5.4). É o caminho para um segundo navegador e para quem limpou os cookies.
- **Sair.** O botão **Sair** revoga o aparelho no servidor, não só apaga o cookie.
- **Painel antigo.** O painel só leitura do `/setup` (bullets 1.12.11 e 1.12.16) sai: o Monitor
  web mostra o mesmo e mais. `GET /setup` redireciona para `/`.

## 3. Modelo de dados

```
Device
  Id              Guid (PK)
  Name            string, 1..40 caracteres
  Role            DeviceRole: Owner | Viewer | Camera
  CredentialHash  byte[32], SHA-256 do segredo
  CreatedAt       DateTimeOffset
  LastSeenAt      DateTimeOffset?
  RevokedAt       DateTimeOffset?
  BatteryLevel    int?, 0..100 (só câmera)
  IsCharging      bool? (só câmera)
  TelemetryAt     DateTimeOffset? (só câmera)

PairingToken
  Id              Guid (PK)
  TokenHash       byte[32], SHA-256 do token
  GrantsRole      DeviceRole
  CreatedAt       DateTimeOffset
  ExpiresAt       DateTimeOffset
  UsedAt          DateTimeOffset?
  CreatedByDeviceId Guid? (null para o token do dono)

BrowserLink (fase 6, "Conectar navegador")
  Id              Guid (PK)
  ClaimHash       byte[32], SHA-256 do segredo que só o navegador tem
  ApprovalHash    byte[32], SHA-256 do segredo que vai no QR
  CreatedAt       DateTimeOffset
  ExpiresAt       DateTimeOffset (10 minutos)
  ApprovedBy      Guid? (o Monitor que aprovou)
  DeviceId        Guid? (o aparelho criado para o navegador, depois de aprovado)
```

Estado que não vai para o banco, em memória no servidor: conexões SignalR online, leases de
visualização, se a câmera está publicando e o código de primeira abertura.

Hora sempre via `TimeProvider` injetado.

## 4. Dados no primeiro uso

O servidor sobe vazio. No primeiro start ele cria:
- o certificado TLS autoassinado em `/data/tls/server.pfx` (RSA 2048, validade de 10 anos);
- o banco em `/data/recam.db`;
- um token de pareamento do dono, válido por 10 minutos e renovado enquanto não existir dono.
  Revisado em 2026-09-25 (fase 6): no lugar do token do dono, um código de primeira abertura,
  só em memória, impresso no log enquanto não existe Monitor ativo (seção 6).

Nada depende de internet. Todo o caminho principal funciona numa rede local sem acesso externo.

## 5. Protocolo

### 5.1 Endereços do servidor

- `RECAM_PUBLIC_URLS` (lista separada por vírgula, ex.: `https://192.168.0.10:8443`), quando
  definida, é a lista usada no QR.
- Sem ela, e com `RECAM_HOST` definida, a lista é `https://<RECAM_HOST>:8443`.
- Sem nenhuma das duas, o servidor detecta os IPv4 das interfaces de rede ativas que não são loopback, em
  faixas privadas (10/8, 172.16/12, 192.168/16), e monta `https://<ip>:8443`. Isso só funciona
  no modo host (Linux).
- No `compose.bridge.yaml`, `RECAM_HOST` é obrigatória. Ela vai para o servidor e para o
  `webrtcAdditionalHosts` do MediaMTX.

### 5.2 QR de pareamento

URI:

```
recam://pair?v=1&t=<token>&r=<role>&f=<fingerprint>&u=<url>&u=<url>
```

- `v`: versão do formato. Hoje `1`.
- `t`: token de pareamento, 32 bytes aleatórios em base64url sem padding.
- `r`: papel que o token concede (`owner`, `viewer`, `camera`). Opcional para o app. Serve só para
  escolher a aba; quem decide é o servidor. Revisado em 2026-09-25 (fase 6): `r` passa a ser
  obrigatório e só vale `camera` ou `viewer`; o app abre em "Ler QR code" e usa o `r` para saber o
  que fazer (câmera: pedir o nome e entrar no modo câmera; Monitor: parear com o nome "Monitor").
- O app registra o esquema `recam://pair` no Android. Abrir o link abre o app na aba do papel e
  pareia, se aquela aba ainda não estiver pareada.
- `f`: SHA-256 do certificado DER do servidor, hexadecimal minúsculo, 64 caracteres. Opcional.
  Sem `f`, o app valida o certificado pelas CAs do sistema.
- `u`: URL base do servidor, percent-encoded. Uma ou mais, em ordem de preferência. O app tenta
  cada uma e fica com a primeira que responder `GET /health`.

QR de "Conectar navegador" (fase 6), mostrado por um navegador que ainda não é Monitor e lido pelo
menu **Conectar navegador** de um celular Monitor:

```
recam://connect-browser?v=1&l=<id do link, 32 hexadecimais>&s=<segredo de aprovação>
```

Não leva endereço nem fingerprint: o celular que lê já está pareado com o servidor. O segredo de
resgate, que busca a credencial, nunca sai do navegador (seção 3, `BrowserLink`).

O app rejeita o QR se `v` for diferente de `1`, se faltar `t`, se não houver `u` válida ou se
`f` estiver presente sem ter 64 caracteres hexadecimais. O parse devolve todos os erros de uma
vez.

### 5.3 TLS e pinning

- O Kestrel serve HTTPS em 8443 com o certificado de `/data/tls/server.pfx`.
- O app instala um `HttpOverrides` global. Para um host pareado com fingerprint, o
  `badCertificateCallback` aceita o certificado só se o SHA-256 do DER bater com o fingerprint
  salvo. Para qualquer outro caso, vale a validação padrão. Isso cobre `package:http`, o cliente
  SignalR e os WebSockets, porque todos usam o `HttpClient` do `dart:io`.

### 5.4 Credencial de dispositivo

- O pareamento devolve `credential = "<deviceId N-format>.<secret base64url>"`, com o segredo
  de 32 bytes aleatórios.
- O cliente manda `Authorization: Bearer <credential>` no REST, no WHIP e no WHEP. No SignalR,
  vai como `access_token` na query.
- O servidor guarda só o SHA-256 do segredo e compara em tempo constante.
- Dispositivo com `RevokedAt` preenchido recebe 401.
- **Navegador (fase 6).** A mesma credencial fica no cookie `recam_device` (`HttpOnly`, `Secure`,
  `SameSite=Strict`, `Path=/`). Com "Lembrar neste computador", o cookie dura 180 dias; sem, só até
  fechar o navegador. O `DeviceAuthenticationHandler` aceita o bearer ou o cookie. O navegador
  manda o cookie sozinho no REST, no WHEP, nos segmentos gravados e no WebSocket do SignalR.
- **Proteção contra CSRF.** Requisição autenticada pelo cookie com método que muda estado (`POST`,
  `PUT`, `PATCH`, `DELETE`) só passa com o cabeçalho `X-Recam-Web: 1`, que outro site não consegue
  mandar sem CORS (e o servidor não libera CORS). Junto com o `SameSite=Strict`.

### 5.5 REST

Estado em 2026-09-26 (conferido no bullet 6.9). As revisões do log 12 contam como cada rota chegou.
"Monitores" quer dizer `Owner` e `Viewer`; o navegador se autentica pelo cookie (seção 5.4).

| Método e rota | Quem pode | Entrada | Saída |
|---|---|---|---|
| `GET /health` | qualquer um | — | `200 "ok"` |
| `GET /` e arquivos do `Recam.Web` | qualquer um | — | o Monitor no navegador |
| `GET /setup` | qualquer um | — | redireciona para `/` |
| `GET /api/web/first-open` | qualquer um | — | `{ open }`: `true` enquanto não há Monitor ativo |
| `POST /api/web/first-open` | só da rede local, só sem Monitor ativo, limite de 5 por minuto por IP | `{ code, remember }` | `204` e o cookie; o navegador vira `Owner` |
| `POST /api/web/sign-out` | o próprio aparelho | — | `204`, revoga o aparelho e apaga o cookie |
| `POST /api/browser-links` | qualquer um, limite de 5 por minuto por IP | — | `201 { id, qrUri, claim, expiresAt }`; o `claim` fica só no navegador |
| `POST /api/browser-links/{id}/approve` | Monitores | `{ secret }` do QR | `204` |
| `POST /api/browser-links/{id}/claim` | quem tem o `claim` | `{ claim, remember }` | `204` e o cookie depois de aprovado; `409` antes |
| `POST /api/pair` | qualquer um, limite de 5 por minuto por IP | `{ token, name, expectedRoles }` | `201 { deviceId, credential, role, serverName }` |
| `POST /api/pairing-tokens` | Monitores | `{ role: "camera" \| "viewer" }` | `201 { id, qrUri, expiresAt }` |
| `GET /api/pairing-tokens/{id}` | o Monitor que criou o token | — | `{ used }`; `404` para qualquer outro |
| `GET /api/me` | qualquer aparelho | — | `{ deviceId, name, role }` |
| `DELETE /api/me` | qualquer aparelho | — | `204`, o aparelho se revoga |
| `GET /api/cameras` | Monitores | — | `[{ id, name, online, publishing, batteryLevel, isCharging, temperatureC, telemetryAt, recording, canRecord, torchOn }]` |
| `GET /api/devices` | Monitores | — | `[{ id, name, role, online }]`, câmeras primeiro |
| `DELETE /api/devices/{id}` | Monitores | — | `204`; `409` para si mesmo, `404` já removido |
| `GET /api/cameras/{id}/recording-days` | Monitores | — | `["AAAA-MM-DD"]`, dias UTC, do mais novo ao mais antigo |
| `GET /api/cameras/{id}/recordings?day=AAAA-MM-DD` | Monitores | — | `[{ start, end, segments: [{ start, end, url }] }]` |
| `GET /api/recordings/{cameraId}/{segmento}` | Monitores | `Range` | o arquivo `video/mp4` |
| `GET /api/cameras/{id}/motion?day=AAAA-MM-DD` | Monitores | — | `{ sensitivity, events: [{ start, end, peak }] }` dos segmentos que começam no dia UTC (Fase 7) |
| `PUT /api/cameras/{id}/motion-sensitivity` | Monitores | `{ sensitivity: "low" \| "medium" \| "high" }` | `204` (Fase 7) |
| `GET /api/recordings/quota` | Monitores | — | `{ quotaMb, usedBytes, freeBytes }` |
| `PUT /api/recordings/quota` | Monitores | `{ quotaMb }` | `204` |
| `POST /whip/{cameraId}` | Camera, só com `cameraId` igual ao próprio id | SDP offer | proxy para `cam-` ou `rec-{cameraId}` no MediaMTX |
| `PATCH`, `DELETE /whip/{cameraId}/{sessão}` | a mesma câmera | trickle ICE / encerrar | proxy |
| `POST /whep/{cameraId}` | Monitores | SDP offer | proxy para `cam-` ou `rec-{cameraId}` |
| `PATCH`, `DELETE /whep/{cameraId}/{sessão}` | Monitores | trickle ICE / encerrar | proxy |

- `POST /api/pair`: `serverName` é a constante `"ReCam"` por enquanto. Quem decide o papel é o `GrantsRole` do token. `expectedRoles` (obrigatório) diz que papéis a aba
  aceita; se o token for de outro papel, a resposta é 409 `pairing.wrong_role` e o token continua
  sem uso.
  Token inexistente, expirado ou já usado recebe 401 com o mesmo corpo nos três casos. `name` é
  validado (1..40 caracteres, sem controle). Erros de validação voltam juntos, em
  `ValidationProblem`.
- Proxy WHIP/WHEP: repassa corpo, `Content-Type`, `If-Match` e `ETag`. Reescreve o `Location` do
  MediaMTX para o formato `/whip/{cameraId}/{session}` ou `/whep/{cameraId}/{session}`. Não
  repassa o `Authorization` do cliente.
- Path no MediaMTX: `cam-` seguido do `deviceId` em formato `N` (32 hexadecimais).

### 5.6 SignalR: `/hubs/devices`

Estado em 2026-09-26 (conferido no bullet 6.9).

Cliente → servidor. Todos devolvem `HubResult { ok, code, message }`.

| Método | Quem chama | Efeito |
|---|---|---|
| `Heartbeat()` | qualquer aparelho | prova que a conexão funciona de ponta a ponta |
| `ReportTelemetry({ batteryLevel, isCharging, temperatureC, supportsH264 })` | Camera | grava no `Device` e avisa os Monitores |
| `ReportPublishing(bool publishing)` | Camera | atualiza o estado e avisa os Monitores |
| `ReportTorch(bool on)` | Camera | guarda o estado em memória (apagado quando a câmera para de transmitir ou cai) e avisa os Monitores com `TorchChanged` e `CameraStatusChanged` |
| `WatchCamera(Guid cameraId)` | Monitores | abre um lease. Se for o primeiro, manda `StartPublishing` à câmera |
| `UnwatchCamera(Guid cameraId)` | Monitores | fecha o lease. Se não sobrar nenhum e a câmera não gravar, manda `StopPublishing` depois de 30 s de carência |
| `SetTorch(Guid cameraId, bool on)` | Monitores | repassa à câmera. Erro `media.camera_not_publishing` se ela não estiver transmitindo |
| `SetRecording(Guid cameraId, bool enabled)` | Monitores | liga ou desliga "Gravar sempre" (`Device.SetRecording`) |

Servidor → cliente:

| Método | Para quem |
|---|---|
| `StartPublishing()` | Camera |
| `StopPublishing()` | Camera |
| `SetTorch(bool on)` | Camera |
| `WatchersChanged(int count)` | Camera |
| `RecordingChanged(bool recording)` | Camera |
| `CameraStatusChanged(CameraStatusDto)` | Monitores |
| `TorchChanged(Guid cameraId, bool on)` | Monitores |
| `CameraRemoved(Guid cameraId)` | Monitores |

- Desconexão de um visualizador fecha os leases dele.
- Quando a câmera reconecta e existe lease aberto, o servidor manda `StartPublishing` de novo.
- A câmera trata `StartPublishing` e `StopPublishing` como idempotentes: o primeiro lease pode
  coincidir com a conexão da câmera, e aí os dois caminhos mandam `StartPublishing`.
- O servidor chega no MediaMTX por `RECAM_MEDIAMTX_URL` (padrão `http://127.0.0.1:8889`; no
  `compose.bridge.yaml`, `http://mediamtx:8889`).
- A câmera manda telemetria ao conectar, a cada 60 s e quando o nível muda.
- `CameraStatusDto` tem o mesmo formato do item de `GET /api/cameras`.
- O `CameraStatus` fica em `Domain/` (`Device.ToCameraStatus`), porque as features `Devices` e
  `Realtime` usam o mesmo formato e uma feature não pode depender da outra.
- Erro esperado num método do hub volta como `HubResult` com `ok: false`. Chamar um método sem o
  papel certo é recusado pelo SignalR com `HubException` (autorização, não erro de negócio).
- Grupo `viewers`: conexões de dono e visualizador. Câmeras são alcançadas pelo id
  (`Clients.User`).

## 6. Autenticação e autorização

- Sem usuário e senha. Cada celular pareado, e cada navegador, é um `Device` com credencial
  própria. Revisado em 2026-09-25 (fase 6): o navegador entra pelo código de primeira abertura ou
  pelo "Conectar navegador"; não existe login.
- **Código de primeira abertura (fase 6).** Enquanto não há Monitor ativo, o servidor mantém em
  memória um código curto (8 caracteres de um alfabeto sem letras parecidas, mostrado como
  `XXXX-XXXX`) e o imprime no log com o endereço. É a única exceção à regra de não logar segredo.
  Ele não é credencial: só serve para `POST /api/web/first-open`, só da rede local, e só enquanto
  não há Monitor. Um código novo sai a cada start e depois de 5 tentativas erradas. Quem abrir
  primeiro na rede local com o código vira o dono; o `reset-owner` desfaz.
- O primeiro pareamento cria o `Owner`. Só existe um dono. O app não mostra esse conceito; ele
  existe para a segurança (revogar aparelhos fica na fase 4).
- Dono e visualizadores geram tokens de câmera e de visualizador. Ninguém gera token de dono.
- `DeviceAuthenticationHandler` autentica o bearer. Políticas: `OwnerOnly`, `ViewerOrOwner`,
  `CameraOnly`.
- A página `/setup` entrega o token do dono. Por isso ela só responde a conexões vindas
  diretamente de IP privado ou loopback, e recusa requisições com `X-Forwarded-For`. Quem roda
  atrás de proxy ou numa VPS usa o QR do log. Revisado (fase 6): a mesma regra de rede vale para
  `POST /api/web/first-open`; o token do dono e o QR no log deixam de existir.

## 7. Deploy

- `deploy/compose.yaml`: Linux, `network_mode: host` nos dois serviços. O MediaMTX escuta HTTP
  em `127.0.0.1:8889`. Os IPs são detectados automaticamente.
- `deploy/compose.bridge.yaml`: Docker Desktop no Windows/macOS. Rede do compose, porta
  `8443:8443/tcp` e `8189:8189/udp` publicadas. `RECAM_HOST` obrigatória no `.env`. O firewall
  do Windows precisa liberar as duas portas.
- Imagem do servidor: multi-stage (`mcr.microsoft.com/dotnet/sdk:10.0` →
  `mcr.microsoft.com/dotnet/aspnet:10.0`), usuário não-root.
- Healthcheck: o próprio binário com o argumento `healthcheck` faz `GET https://localhost:8443/health`
  aceitando o certificado local e sai com código 0 ou 1. A imagem `aspnet` não tem `curl`.
- MediaMTX: imagem `bluenviron/mediamtx` com tag exata. Nunca `latest`. Só aceita paths no
  formato `cam-<32 hexadecimais>` e `rec-<32 hexadecimais>` (gravando, seção 2.4). Roda com o
  usuário do servidor (`1654:1654`).
- Dados em volume nomeado `recam-data`. O Dockerfile cria `/data` com dono não-root, e o
  Docker copia essa permissão para o volume no primeiro uso. As gravações ficam no volume
  `recam-recordings`, em `/recordings`, pelo mesmo mecanismo.
- Serviço `motion` nos dois composes (seção 2.4): `linuxserver/ffmpeg` com tag exata, usuário
  `1654:1654`, `network_mode: none`, `deploy/motion.sh` montado só leitura e o volume
  `recam-recordings`. Não expõe porta.
- Observabilidade opcional: `deploy/compose.observability.yaml` soma ao compose escolhido o
  Aspire Dashboard (`mcr.microsoft.com/dotnet/aspire-dashboard`, tag exata), com o painel em
  `127.0.0.1:18888` e o OTLP gRPC em `127.0.0.1:4317`, os dois só no loopback. Sem ele, o servidor
  não exporta nada.

## 8. Testes

Política: meio-termo.

- **Servidor: cobertura ampla, com dependência real isolada.**
  - `WebApplicationFactory` com SQLite real num arquivo temporário por teste.
  - Testes da feature `Media` sobem o MediaMTX real via Testcontainers, com a mesma tag do
    compose.
  - `FakeTimeProvider` para tempo. Fakes escritos à mão para o resto. Sem biblioteca de mock.
  - Casos cobertos: caminho feliz e os casos ruins de segurança (token expirado, token reusado,
    dispositivo revogado, papel errado, câmera publicando no path de outra).
  - Nunca contra um servidor ou banco de desenvolvimento compartilhado.
- **Monitor web: caminho feliz.** bUnit para componentes e testes de unidade dos controllers do
  `Recam.Web`, com fakes escritos à mão de `IRecamApi`, `IDeviceHub` e `ILiveVideo`. O servidor
  testa o que protege o navegador: código de primeira abertura, cookie, `X-Recam-Web`, CSP e
  "Conectar navegador". O WebRTC no navegador só se prova abrindo o navegador de verdade.
- **App: caminho feliz.**
  - Testes unitários de controllers, parse do QR, backoff de reconexão e pinning.
  - Plugins substituídos por fakes das interfaces de `core/`.
  - Widget test do shell de abas.
- **Aparelho real:** vídeo, lanterna, foreground service e comportamento de bateria só se
  provam no A10 e no 6A. O ROADMAP tem bullets de validação no aparelho para isso.

## 9. Segredos e configuração

Esquema formal. Hoje o servidor não tem segredo configurado por humano: o certificado e o banco
são gerados em `/data`.

| Dado | Onde fica | Versionado? | Quem preenche |
|---|---|---|---|
| `RECAM_HOST`, `RECAM_PUBLIC_URLS` | `deploy/.env` | não. `deploy/.env.example` com valor vazio, sim | usuário |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `deploy/.env` (vazio: não exporta) | não. Chave vazia no `.env.example`, sim | usuário |
| Certificado TLS, banco | `deploy/data/` (volume `/data`) | não | gerado pelo servidor |
| Keystore de release do Android | fora do repositório, `app/android/key.properties` aponta para ele | não | usuário |
| Segredos de CI (registry, keystore) | GitHub Actions secrets | não | usuário |

O agente nunca escreve valor real de segredo em arquivo nenhum.

## 10. Licença e contribuição

- AGPL-3.0 para todo o repositório público.
- Toda contribuição externa exige aceite de um CLA (via CLA Assistant no GitHub), que dá ao
  mantenedor o direito de relicenciar e de publicar o app oficial nas lojas.
- `applicationId` do Android: `io.recam.app`, provisório. Precisa ser confirmado antes da
  primeira publicação em loja, porque não muda depois.

## 11. Riscos conhecidos

- **Encoder H.264 de hardware**: o libwebrtc só usa H.264 em hardware em alguns fabricantes de
  chip, e o Android não tem H.264 por software no libwebrtc. Exynos (A10) deve funcionar. O
  Redmi 6A usa MediaTek Helio A22, que pode não ter H.264 exposto ao libwebrtc: se não tiver, ele
  transmite ao vivo em VP8 e não grava (ver "Gravação de VP8"). Conferir no teste do 6A.
- **Gravação de VP8**: o MediaMTX 1.21.1 não grava VP8 em fMP4 (conferido em 2026-09-25 publicando
  VP8 num path `rec-`: "no supported tracks found, skipping recording"). Aparelho sem encoder
  H.264 transmite ao vivo em VP8, mas não grava (bullet 3.2).
- **Android matando o app**: MIUI e One UI encerram processos em segundo plano de forma
  agressiva. Mitigação: foreground service, wakelock, tela guiada de otimização de bateria
  (fase 2).
- **Aviso de certificado no navegador**: o certificado é autoassinado, então o navegador avisa
  "não seguro" na primeira abertura, e a pessoa aceita uma vez. Atrás de proxy reverso com
  certificado público (bullet 5.1), o aviso some. O README precisa ensinar esse passo.
- **Tamanho do Monitor web**: o Blazor WebAssembly baixa alguns MB na primeira abertura. Na rede
  local é rápido, e o navegador guarda em cache. Publicado com trimming e compressão.
- **Docker no Windows**: sem rede host de verdade. O IP do PC precisa ser informado em
  `RECAM_HOST`.
- **Aquecimento**: celular ligado na tomada por dias. Telemetria de temperatura e redução de
  qualidade na fase 2.

## 12. Log de decisões

Cada item deste log tem um ADR em `docs/adr/` (índice em [`docs/adr/README.md`](docs/adr/README.md)),
na ordem em que aparece aqui: as 16 decisões iniciais são os ADRs 0001 a 0016, e as revisões
datadas, de cima para baixo, os ADRs 0017 a 0041. O ADR traz contexto, decisão e consequências; o
log continua sendo o resumo.

Decisões iniciais (2026-09-24):

- **MediaMTX como plano de mídia.** Implementar WebRTC dentro do .NET (SIPSorcery) custaria meses
  em problemas de mídia. O MediaMTX fala WHIP/WHEP e grava em fMP4 quando a gravação entrar.
- **WebRTC com WHIP/WHEP.** Atraso abaixo de 1 s, controle de congestionamento embutido. RTMP
  teria de 1 a 3 s e RTSP não tem biblioteca madura em Flutter.
- **.NET como proxy da sinalização WHIP/WHEP.** Uma só porta TCP pública, um só certificado,
  autorização num lugar só. O MediaMTX fica sem porta TCP exposta e sem hook de autenticação.
- **Transmissão sob demanda.** Sem gravação no MVP, a câmera só precisa transmitir enquanto
  alguém assiste. Menos calor e menos bateria no celular fraco.
- **EF Core em vez de Dapper.** O ganho de performance do Dapper não aparece num banco deste
  tamanho, e as migrações automáticas no startup importam quando o usuário atualiza a imagem.
- **SQLite.** Nó único, sem terceiro container.
- **Pareamento sem conta.** Cada celular é um dispositivo com credencial. O dono pareia os
  demais.
- **Primeiro QR no log e em `/setup`.** Não existe dashboard web. O celular que assiste vira o dono e
  passa a gerar os QRs das câmeras.
- **Um app com duas abas.** Leveza e simplicidade, ao contrário do Alfred.
- **Só Android no MVP.** O desenvolvimento é em Windows, e iOS exige macOS. iOS também não
  permite câmera em segundo plano.
- **Android 9 como mínimo (`minSdk 28`).** Os aparelhos de referência são de 2019.
- **AGPL-3.0 com CLA.** Open source oficial, afasta quem quer fechar e revender, e o CLA
  mantém a licença ajustável no futuro.
- **Vertical slices, sem Clean Architecture em vários projetos.** O domínio é pequeno. As
  fronteiras são garantidas por teste de arquitetura, não pela quantidade de projetos.
- **Domínio rico e `Result<T>`.** Erro esperado (token expirado, nome inválido) não é exceção.
  Exceção fica para bug.
- **O projeto também é portfólio .NET do autor.** Os primeiros slices do servidor são feitos
  junto com ele para fixar o padrão. Depois, os bullets podem ir para um modelo mais barato em
  loop.
- **Gravação e acesso remoto adiados.** Gravação é a parte mais cara do projeto. Acesso remoto
  é responsabilidade do usuário.

Revisões são adicionadas abaixo, datadas, sem apagar o texto original, e cada uma ganha o ADR
seguinte em `docs/adr/`:
`> Revisão (AAAA-MM-DD): o que mudou e por quê.`

> Revisão (2026-09-24): `/data` passou de bind mount em `deploy/data/` para volume nomeado
> `recam-data`. No Linux, o Docker cria a pasta do bind mount como root, e o servidor roda como
> usuário não-root, sem permissão de escrita. O volume nomeado herda a permissão da imagem.
> Quando a gravação entrar, a escolha de pasta no host volta a ser discutida.

> Revisão (2026-09-25): H.264 é o codec preferido, não o único. O app ordena H.264 primeiro e
> deixa os outros como reserva; aparelho sem encoder H.264 (o emulador, por exemplo) publica em
> VP8, que o MediaMTX aceita. O bullet 2.1 (recusar aparelho sem H.264) deve ser revisto quando a
> gravação entrar, porque é ela que exige H.264.

> Revisão (2026-09-25): o uso ficou confuso no teste com dois celulares (não dava para inverter os
> papéis). Novo desenho, combinado com o autor: o PC serve para instalar e acompanhar; todo o resto
> é pelo celular.
> - Um só "Ler QR": o papel do QR (`r=`) decide se o celular vira câmera ou visualizador.
> - Qualquer celular que assiste gera QR de câmera e de visualizador (antes só o dono gerava, e só
>   de câmera).
> - A primeira abertura pergunta "câmera ou para assistir"; o app não mostra o conceito de dono.
> - O `/setup` vira um painel de acompanhamento só leitura na rede local. Continua não existindo
>   dashboard de gestão; ações como revogar seguem no celular do dono.

> Revisão (2026-09-25): o erro de `SetTorch` com a câmera parada tem o código
> `media.camera_not_publishing`, no mesmo formato `área.nome` dos outros erros de domínio (a tabela
> da seção 5.6 dizia `camera-not-publishing`). Câmera inexistente devolve `media.camera_not_found`.

> Revisão (2026-09-25): nova mensagem servidor → câmera, `WatchersChanged(int count)`, com quantas
> conexões de visualizador têm lease aberto nela. O servidor manda a cada mudança (lease aberto,
> fechado ou perdido por desconexão) e ao conectar a câmera. É o que o celular câmera mostra como
> "quantos assistem" (bullet 1.12.7). `UnwatchCamera` passou a esperar esse aviso antes de responder.

> Revisão (2026-09-25): com um celular já pareado, `GET /setup` deixou de ser a página "already
> configured" e virou o painel só leitura (bullet 1.12.11), na mesma regra de acesso: tabela com
> nome, tipo (câmera ou "assiste"), online, transmitindo, quantos assistem e bateria, recarregada
> sozinha a cada 5 s por `<meta http-equiv="refresh">`, sem script e sem ações. A página do QR
> também se recarrega, a cada 30 s, para virar o painel depois do primeiro pareamento. A contagem
> de quem assiste passou a ficar também no `DevicePresence` (Infrastructure), que o `WatchLeases`
> atualiza, porque `Features.Setup` não pode ler `Features.Realtime`.

> Revisão (2026-09-25): `POST /api/pairing-tokens` devolve `{ id, qrUri, expiresAt }`. Nova rota
> `GET /api/pairing-tokens/{id}` (Owner, Viewer) devolve `{ used }` só para o aparelho que criou o
> token; para qualquer outro, e para id inexistente, 404 `pairing.token_not_found`, igual nos dois
> casos. A regra é `PairingToken.UsageFor(deviceId)`. O app consulta a cada 2 s enquanto mostra o QR
> e fecha a tela quando o token foi usado (bullet 1.12.14).

> Revisão (2026-09-25): nova rota `DELETE /api/me` (qualquer aparelho, 204): o aparelho se revoga
> (`Device.Revoke`, que guarda a primeira hora se chamado de novo). "Reiniciar o app" chama essa rota
> para cada aba pareada antes de apagar os dados; sem resposta, apaga mesmo assim. O QR do primeiro
> celular (log e `/setup`) passa a existir sempre que não há Monitor ativo (dono ou visualizador não
> revogado), e não só quando nunca houve dono. Assim um servidor sem Monitor volta a aceitar um
> primeiro celular, que vira o novo dono. Se o dono sai e um visualizador fica, não há QR: o
> visualizador adiciona os demais (bullet 1.12.15).

> Revisão (2026-09-25): a página `/setup` mostra um idioma por vez, escolhido pelo `Accept-Language`
> (a primeira preferência entre pt e en; sem nenhuma das duas, inglês), e responde com
> `Vary: Accept-Language`. Sem Monitor: passo a passo numerado ao lado do QR e o código em texto.
> Com Monitor: cartões de Câmeras (online, transmitindo ou parada, assistindo agora, bateria) e de
> Monitores (online), e o passo a passo de adicionar câmera. CSS no próprio HTML, com tema claro e
> escuro (`prefers-color-scheme`). Sem script nenhum: o `onclick` da caixa do código saiu
> (bullet 1.12.16).

> Revisão (2026-09-25): a gravação sai de "Fora do escopo" e vira a Fase 3 do ROADMAP, depois da
> Fase 2 (calor e bateria), porque gravar obriga a câmera a transmitir o tempo todo. As fases de
> gestão de aparelhos e de distribuição passaram a ser 4 e 5. O desenho está na abertura da Fase 3
> do ROADMAP; o bullet 3.1 o traz para uma seção própria deste documento.

> Revisão (2026-09-25): `ReportTelemetry` recebe um objeto só, `{ batteryLevel, isCharging,
> temperatureC }`, com `temperatureC` em °C ou `null` quando o celular não informa (o cliente
> SignalR do Dart não manda argumento nulo). `Device` e `CameraStatus` ganharam `temperatureC`
> (temperatura da bateria, validada entre -40 e 120 °C). A câmera manda a telemetria também quando
> a temperatura muda um grau inteiro (bullet 2.2).

> Revisão (2026-09-25): com a bateria a 42 °C ou mais, a câmera passa a mandar cerca de 480p
> (escala 1,5 sobre a captura de 1280x720, 853x480), 10 fps e 400 kbps, trocando os parâmetros do
> envio sem reabrir a câmera. Volta a 1280x720, 15 fps e 700 kbps abaixo de 38 °C. Entre os dois
> limites, mantém o que estava (bullet 2.3).

> Revisão (2026-09-25): a gravação entrou no escopo (1.1) e ganhou a seção 2.4, com o desenho da
> Fase 3: paths `rec-` gravando em fMP4 no MediaMTX, volume `recam-recordings`, MediaMTX rodando com
> o usuário do servidor para o servidor poder apagar os segmentos, e o registro de que VP8 não é
> gravado (seção 11) (bullet 3.1).

> Revisão (2026-09-25): "Gravar sempre", por câmera (bullet 3.2).
> - `Device` ganhou `RecordingEnabled` e `SupportsH264` (migração `CameraRecording`). A regra é
>   `Device.SetRecording(requester, enabled)`: só um Monitor ativo muda, só numa câmera, e ligar é
>   recusado para câmera sem H.264 (`media.recording_needs_h264`). `Device.ReportVideoCodecs` desliga
>   a gravação de uma câmera que passa a dizer que não tem H.264.
> - Hub: `SetRecording(Guid cameraId, bool enabled)` para Monitores; servidor → câmera
>   `RecordingChanged(bool recording)`, ao conectar e a cada mudança. `ReportTelemetry` aceita
>   `supportsH264`. `CameraStatus` ganhou `recording` e `canRecord`.
> - Câmera gravando recebe `StartPublishing` ao conectar e não recebe `StopPublishing` quando o
>   último Monitor sai. Mudar a gravação com a câmera transmitindo manda `StopPublishing` e
>   `StartPublishing`, para ela publicar de novo no outro path.
> - Proxy: um `POST /whip` ou `/whep` vai para `rec-{id}` com a gravação ligada e para `cam-{id}` sem
>   ela. O `Location` devolvido passa a ser `/whip/{cameraId}/{cam|rec}-{sessão}`: o prefixo diz o
>   path, para `PATCH` e `DELETE` irem ao lugar certo mesmo depois de uma troca. Sessão sem prefixo
>   conhecido devolve 404 `media.session_not_found`.

> Revisão (2026-09-25): espaço para gravações (bullet 3.3). Tabela `RecordingQuota` (linha única,
> migração `RecordingQuota`), padrão 2048 MB, mínimo 100 MB. `GET /api/recordings/quota`
> (Monitores) devolve `{ quotaMb, usedBytes, freeBytes }`; `PUT` com `{ quotaMb }` devolve 204 e
> recusa, com `ValidationProblem` em `quotaMb`, cota menor que 100 MB ou maior que uso + espaço
> livre (`RecordingQuota.ChangeTo`). A cada 60 s, um `BackgroundService` aplica
> `RecordingQuota.PlanCleanup`: apaga tudo de câmeras removidas e depois os segmentos mais antigos,
> de qualquer câmera, até caber, sem apagar o segmento mais novo de cada câmera (o MediaMTX pode
> estar escrevendo nele). O servidor lê as gravações em `RECAM_RECORDINGS_DIR` (padrão
> `/recordings`, configuração interna como `RECAM_MEDIAMTX_URL`). A página do servidor mostra
> "1,2 de 2 GB em uso".

> Revisão (2026-09-25): listar e servir gravações (bullet 3.4), todas para Monitores, horários em UTC.
> - `GET /api/cameras/{id}/recordings?day=AAAA-MM-DD` devolve `[{ start, end, segments: [{ start,
>   end, url }] }]`: os trechos do dia UTC, com segmentos seguidos (folga de 1 s) emendados. Cada
>   segmento termina 60 s depois de começar ou quando o próximo começa. O segmento mais novo fica de
>   fora enquanto a câmera grava e transmite. Dia em outro formato: 400 `recording.invalid_day`.
> - `GET /api/cameras/{id}/recording-days` devolve os dias UTC com gravação, do mais novo ao mais
>   antigo (acrescentado para a tela de dias do 3.5).
> - `GET /api/recordings/{cameraId}/{segmento}` serve o arquivo `video/mp4` com `Range`. Só um nome
>   no formato do MediaMTX (`AAAA-MM-DD_HH-MM-SS-ffffff.mp4`) vira caminho no disco; qualquer outro
>   nome dá 404.

> Revisão (2026-09-25): remover aparelhos (bullet 4.1, com a revisão do próprio bullet). `GET
> /api/devices` (Monitores) devolve `[{ id, name, role, online }]`, câmeras primeiro. `DELETE
> /api/devices/{id}` (Monitores, 204) segue `Device.RevokeBy(requester, now)`: só Monitor ativo
> remove, nunca a si mesmo (409 `device.cannot_remove_itself`; para isso existe "Reiniciar o app"),
> e um aparelho já removido é 404. Remover (e também o `DELETE /api/me`) derruba as conexões do hub
> desse aparelho e, se era câmera, manda aos Monitores `CameraRemoved(Guid cameraId)`. A feature
> `Realtime` implementa isso atrás da interface `IDeviceRemovals`, em `Infrastructure/Realtime`,
> para `Devices` não depender de `Realtime`. A câmera removida tenta reconectar, recebe 401 e o
> modo câmera fecha com a mensagem de pareamento perdido (1.12.3).

> Revisão (2026-09-25): `docker compose exec server ./Recam.Server reset-owner` revoga todos os
> Monitores ativos (dono e visualizadores), não só o dono, e deixa as câmeras pareadas (bullet 4.3,
> revisão do próprio bullet). É a saída quando o único Monitor quebrou ou sumiu. O comando abre o
> banco, aplica as migrações, revoga e imprime o que fazer; o servidor em execução, sem Monitor
> ativo, volta a gerar o QR do primeiro Monitor no log e em `/setup` em até 30 s. As conexões do
> hub desses Monitores, abertas no outro processo, só caem na próxima reconexão.

> Revisão (2026-09-25): modo atrás de proxy reverso (bullet 5.1). `RECAM_TLS=off` faz o Kestrel
> servir HTTP simples na 8443, para o proxy terminar o TLS; o QR sai sem `f`, e o app valida o
> certificado do proxy pelas CAs do sistema (o que o 5.2 da seção 5 já previa). `RECAM_PUBLIC_URLS`
> deve ter o endereço público do proxy. `RECAM_TRUSTED_PROXIES` (IPs ou redes, separados por vírgula)
> liga o `ForwardedHeaders` só para esses proxies, sem nem o loopback por padrão: atrás de um
> proxy confiável o servidor vê o IP real do cliente, e o `/setup` passa a decidir pelo IP real
> (cliente de fora continua recusado). Cabeçalhos `X-Forwarded-*` de qualquer outro endereço são
> ignorados, e o `/setup` continua recusando a requisição que os traz. As duas chaves estão no
> `.env.example` e nos dois composes. O WebRTC continua precisando da UDP 8189 alcançável direto:
> o proxy só leva o HTTP.

> Revisão (2026-09-25): observabilidade com OpenTelemetry (bullet 5.2.1). O servidor gera traces
> das requisições (ASP.NET Core) e das chamadas de saída (o proxy WHIP/WHEP para o MediaMTX, via
> HttpClient), as métricas dessas duas instrumentações e três medidores próprios no meter
> `Recam.Server`, lidos do `DevicePresence` a cada coleta: `recam.cameras.online`,
> `recam.cameras.publishing` e `recam.views.active` (um Monitor vendo duas câmeras conta duas). O
> `DevicePresence` passou a saber quais aparelhos online são câmeras. A exportação OTLP só liga com
> `OTEL_EXPORTER_OTLP_ENDPOINT` definida, e vai para onde o operador apontar: nada sai para
> terceiros por padrão. A query string sai redigida nos traces (padrão das instrumentações), então
> o `access_token` do WebSocket do SignalR não vaza; cabeçalhos não são gravados. As portas 18888 e
> 4317 do Aspire Dashboard só existem com o compose opcional e ficam presas ao loopback, porque o
> painel roda sem login; as portas expostas à rede continuam só a 8443/tcp e a 8189/udp.

> Revisão (2026-09-25): o segundo aparelho de referência é o Xiaomi Redmi 6A (MediaTek Helio A22),
> não o Redmi 7A (Snapdragon), porque é o que o autor tem. O risco de H.264 da seção 11 passa a
> valer para ele: se o 6A não expuser H.264, transmite em VP8 e não grava.

> Revisão (2026-09-25): novo caminho principal, decidido pelo autor. O primeiro Monitor é o
> navegador do computador, que entra com um código de primeira abertura impresso no log (só da rede
> local, só enquanto não há Monitor, novo a cada start e depois de 5 erros). O navegador mostra o
> QR de "Adicionar câmera", e o primeiro celular pareado é sempre uma câmera: a pessoa já vê o
> produto funcionando no primeiro minuto e, se gostar, adiciona outro celular pelo navegador, como
> câmera ou como Monitor. O navegador é um Monitor completo. O token do dono, o QR no log e a página
> `/setup` (QR e painel) saem; `/setup` redireciona para `/`. O navegador guarda a credencial no
> cookie `recam_device` e prova a origem com `X-Recam-Web: 1` nos métodos que mudam estado. Um
> navegador novo, com Monitor já existente, entra pelo "Conectar navegador", aprovado por um
> celular Monitor. Seções 1, 2, 2.5, 3, 4, 5 e 6. Caminho principal do `AGENTS.md` reescrito.

> Revisão (2026-09-25): o Monitor web é um cliente Blazor WebAssembly (`Recam.Web`) do mesmo
> protocolo do app (REST, hub e WHEP), e não componentes Blazor rodando no servidor. Assim nenhuma
> feature do servidor precisa chamar outra, e o navegador não ganha atalho que o app não tenha.
> Vídeo por um módulo JavaScript próprio, sem biblioteca. Testes com bUnit. Nada carregado de fora
> do servidor. Seção 2.5.

> Revisão (2026-09-25): o app deixa de perguntar "Filmar" ou "Assistir". A primeira abertura mostra
> "Ler QR code" (e "Colar código"), e o papel que o QR traz decide: câmera pede o nome e entra no
> modo câmera; Monitor pareia com o nome "Monitor". O `r` do QR passa a ser obrigatório, com
> `camera` ou `viewer`. O celular Monitor ganha no menu "Conectar navegador". Seções 1 e 5.2.

> Revisão (2026-09-26): detecção de movimento nas gravações (bullet 7.2), seção 2.4. O desenho do
> ROADMAP usava a nota `scene` do FFmpeg; medida em cenas de teste, ela é feita para corte de cena e
> quase não vê movimento contínuo (uma forma do tamanho de uma pessoa andando deu o mesmo que a
> cena parada). O serviço `motion` passou a medir a fração da imagem que mudou entre meios
> segundos, com um limiar que deixa o ruído do sensor em zero (a caminhada dá cerca de 5%). Novo
> serviço nos composes, sem porta nem rede; `Device.MotionSensitivity` (migração `CameraMotion`,
> câmeras antigas começam em média); rotas de movimento na seção 5.5.

> Revisão (2026-09-26): depois do teste do autor com o PC, o Samsung A10 e o Redmi 6A:
> - Um papel por celular (bullet 8.1). A decisão "um app com duas abas" deixa de valer: o celular é
>   Câmera ou Monitor, e ler o QR do outro papel troca o papel, saindo do servidor no antigo.
> - Áudio entra (Fase 9): a câmera publica Opus mono junto com o vídeo, a gravação guarda o som e o
>   Monitor ouve ao vivo. O "sem trilha de áudio" do 2.3 muda quando o 9.1 for feito.
> - O estado da lanterna passa a ficar em memória no servidor (8.10), o que revisa a nota do 1.13.

> Revisão (2026-09-26): o código de primeira abertura sai no log num bloco emoldurado, com o
> endereço e o passo a passo, e também fica em `/data/first-open-code` enquanto não existe
> Monitor ativo (apagado quando um navegador vira Monitor). O comando
> `docker compose exec server ./Recam.Server code` lê esse arquivo e mostra o bloco de novo, ou diz
> que o servidor já tem Monitor (bullet 8.9). O código continua não sendo credencial e só vale na
> rede local.
