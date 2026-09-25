# ReCam: especificação

## 1. Produto

ReCam reaproveita celulares Android parados como câmeras de monitoramento. Três papéis:

- **Câmera** (celular câmera): captura e transmite. Não guarda nada, não decide nada.
- **Servidor**: roda em Docker na casa do usuário ou numa VPS. Pareia aparelhos, autentica,
  repassa comandos e distribui o vídeo.
- **Monitor** (celular que assiste): lista as câmeras, assiste ao vivo, manda comandos.

Um único app Flutter faz os dois papéis de celular, em duas abas: **Câmera** e **Monitor**.

Glossário das telas (revisado em 2026-09-25):

| Onde | Texto | Papel |
|---|---|---|
| Primeira abertura, "Este celular vai ser usado para:" | **Filmar** / Film | vira Câmera |
| Primeira abertura | **Assistir** / Watch | vira Monitor |
| Abas, textos do app, página do servidor, log do QR | **Câmera** / Camera e **Monitor** / Monitor | — |

No código e no protocolo, o Monitor é `Viewer` (ou `Owner`, o primeiro, que as telas não mostram)
e a Câmera é `Camera`. O Monitor pareia com o nome padrão "Monitor".

### 1.1 Dentro do escopo (MVP)

- Instalação com `docker compose up -d` em Linux. Windows via Docker Desktop/WSL como caso
  secundário.
- Pareamento por QR code: primeiro o dono, depois as câmeras.
- Vídeo ao vivo na rede local, com atraso abaixo de 1 s.
- Transmissão sob demanda: a câmera só transmite enquanto alguém assiste.
- Lanterna liga/desliga durante o vídeo ao vivo.
- Telemetria de bateria (nível e se está carregando) e status online/offline.
- App em PT-BR e inglês.
- Android 9 (API 28) ou mais novo. Aparelhos de referência: Samsung Galaxy A10 e Xiaomi
  Redmi 7A.

### 1.2 Fora do escopo (anotado para depois)

- Gravação, com cota de disco escolhida numa barrinha e apagando o mais antigo quando enche.
  Quando entrar: gravação só no servidor, nunca no celular câmera.
- Acesso de fora da rede local (porta aberta, Tailscale, Cloudflare Tunnel em modo rede
  privada). Responsabilidade do usuário. O Cloudflare Tunnel com hostname público não transporta
  UDP, então não serve para o WebRTC.
- Detecção de movimento e notificações.
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
[App: aba Assistir] ──HTTPS: REST, SignalR, WHEP──────────┘         RTP/SRTP via UDP :8189 <────┘
```

- **Recam.Server (.NET 10, ASP.NET Core)**: plano de controle. Única porta TCP pública (8443,
  HTTPS). Serve a API REST, o hub SignalR, a página `/setup` e um proxy de sinalização para o
  WHIP/WHEP do MediaMTX. Não toca em RTP.
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
  Features/
    Health/               GET /health
    Setup/                token do dono, QR no log, página /setup
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
  app.dart                MaterialApp com NavigationBar de duas abas
  core/
    network/              PinnedHttpOverrides, ApiClient, HubConnectionFactory
    pairing/              QrPayload (parse), PairingService
    storage/              CredentialStore (flutter_secure_storage)
    media/                interfaces WebRtcPublisher, WebRtcViewer e implementações
    device/               interfaces de bateria, lanterna, tela
  camera/                 aba Câmera: pareamento, modo câmera
  viewer/                 aba Assistir: pareamento do dono, lista, ao vivo, adicionar câmera
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
  H.264 preferido via `setCodecPreferences`, sem trilha de áudio, `maxBitrate` 700 kbps no
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
```

Estado que não vai para o banco, em memória no servidor: conexões SignalR online, leases de
visualização e se a câmera está publicando.

Hora sempre via `TimeProvider` injetado.

## 4. Dados no primeiro uso

O servidor sobe vazio. No primeiro start ele cria:
- o certificado TLS autoassinado em `/data/tls/server.pfx` (RSA 2048, validade de 10 anos);
- o banco em `/data/recam.db`;
- um token de pareamento do dono, válido por 10 minutos e renovado enquanto não existir dono.

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
  escolher a aba; quem decide é o servidor.
- O app registra o esquema `recam://pair` no Android. Abrir o link abre o app na aba do papel e
  pareia, se aquela aba ainda não estiver pareada.
- `f`: SHA-256 do certificado DER do servidor, hexadecimal minúsculo, 64 caracteres. Opcional.
  Sem `f`, o app valida o certificado pelas CAs do sistema.
- `u`: URL base do servidor, percent-encoded. Uma ou mais, em ordem de preferência. O app tenta
  cada uma e fica com a primeira que responder `GET /health`.

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

### 5.5 REST

| Método e rota | Quem pode | Entrada | Saída |
|---|---|---|---|
| `GET /health` | qualquer um | — | `200 "ok"` |
| `GET /setup` | só IP privado, sem header `X-Forwarded-For`, só enquanto não há dono | — | HTML com o QR em SVG e o texto em inglês e português. Com dono já pareado: página "already configured" |
| `POST /api/pair` | qualquer um, com limite de 5 por minuto por IP | `{ token, name, expectedRoles }` | `201 { deviceId, credential, role, serverName }` |
| `POST /api/pairing-tokens` | Owner, Viewer | `{ role: "camera" \| "viewer" }` | `201 { qrUri, expiresAt }` |
| `GET /api/me` | qualquer dispositivo | — | `{ deviceId, name, role }` |
| `GET /api/cameras` | Owner, Viewer | — | `[{ id, name, online, publishing, batteryLevel, isCharging, telemetryAt }]` |
| `POST /whip/{cameraId}` | Camera, só com `cameraId` igual ao próprio id | SDP offer | proxy para `/cam-{cameraId}/whip` no MediaMTX |
| `PATCH`, `DELETE /whip/{cameraId}/{session}` | a mesma câmera | trickle ICE / encerrar | proxy |
| `POST /whep/{cameraId}` | Owner, Viewer | SDP offer | proxy para `/cam-{cameraId}/whep` |
| `PATCH`, `DELETE /whep/{cameraId}/{session}` | Owner, Viewer | trickle ICE / encerrar | proxy |

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

Cliente → servidor:

| Método | Quem chama | Efeito |
|---|---|---|
| `ReportTelemetry(int batteryLevel, bool isCharging)` | Camera | grava no `Device` e avisa os visualizadores. Devolve `HubResult { ok, code, message }` |
| `ReportPublishing(bool publishing)` | Camera | atualiza o estado e avisa os visualizadores |
| `ReportTorch(bool on)` | Camera | avisa os visualizadores |
| `WatchCamera(Guid cameraId)` | Owner, Viewer | abre um lease. Se for o primeiro, manda `StartPublishing` à câmera |
| `UnwatchCamera(Guid cameraId)` | Owner, Viewer | fecha o lease. Se não sobrar nenhum, manda `StopPublishing` depois de 30 s de carência |
| `SetTorch(Guid cameraId, bool on)` | Owner, Viewer | repassa à câmera. Erro `camera-not-publishing` se ela não estiver transmitindo |

Servidor → cliente:

| Método | Para quem |
|---|---|
| `StartPublishing()` | Camera |
| `StopPublishing()` | Camera |
| `SetTorch(bool on)` | Camera |
| `CameraStatusChanged(CameraStatusDto)` | Owner, Viewer |
| `TorchChanged(Guid cameraId, bool on)` | Owner, Viewer |

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

- Sem usuário e senha. Cada celular pareado é um `Device` com credencial própria.
- O primeiro pareamento cria o `Owner`. Só existe um dono. O app não mostra esse conceito; ele
  existe para a segurança (revogar aparelhos fica na fase 4).
- Dono e visualizadores geram tokens de câmera e de visualizador. Ninguém gera token de dono.
- `DeviceAuthenticationHandler` autentica o bearer. Políticas: `OwnerOnly`, `ViewerOrOwner`,
  `CameraOnly`.
- A página `/setup` entrega o token do dono. Por isso ela só responde a conexões vindas
  diretamente de IP privado ou loopback, e recusa requisições com `X-Forwarded-For`. Quem roda
  atrás de proxy ou numa VPS usa o QR do log.

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
  formato `cam-<32 hexadecimais>`.
- Dados em volume nomeado `recam-data`. O Dockerfile cria `/data` com dono não-root, e o
  Docker copia essa permissão para o volume no primeiro uso.

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
- **App: caminho feliz.**
  - Testes unitários de controllers, parse do QR, backoff de reconexão e pinning.
  - Plugins substituídos por fakes das interfaces de `core/`.
  - Widget test do shell de abas.
- **Aparelho real:** vídeo, lanterna, foreground service e comportamento de bateria só se
  provam no A10 e no 7A. O ROADMAP tem bullets de validação no aparelho para isso.

## 9. Segredos e configuração

Esquema formal. Hoje o servidor não tem segredo configurado por humano: o certificado e o banco
são gerados em `/data`.

| Dado | Onde fica | Versionado? | Quem preenche |
|---|---|---|---|
| `RECAM_HOST`, `RECAM_PUBLIC_URLS` | `deploy/.env` | não. `deploy/.env.example` com valor vazio, sim | usuário |
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
  chip, e o Android não tem H.264 por software no libwebrtc. Exynos (A10) e Snapdragon (7A)
  devem funcionar. MediaTek antigo pode não ter. Tratado na fase 2.
- **Android matando o app**: MIUI e One UI encerram processos em segundo plano de forma
  agressiva. Mitigação: foreground service, wakelock, tela guiada de otimização de bateria
  (fase 2).
- **Docker no Windows**: sem rede host de verdade. O IP do PC precisa ser informado em
  `RECAM_HOST`.
- **Aquecimento**: celular ligado na tomada por dias. Telemetria de temperatura e redução de
  qualidade na fase 2.

## 12. Log de decisões

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

Revisões são adicionadas abaixo, datadas, sem apagar o texto original:
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
