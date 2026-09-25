# Recam: guia do agente

Recam transforma celulares Android parados em câmeras de monitoramento. Um servidor self-hosted
em Docker recebe o vídeo, e um celular visualizador assiste ao vivo. Open source (AGPL-3.0),
sem anúncio, sem telemetria para terceiros, leve o bastante para rodar num celular de 2019 com
2 GB de RAM.

## Caminho principal

É a régua para decidir se um bullet importa. Tudo no MVP existe para este percurso funcionar:

1. A pessoa sobe o servidor com `docker compose up -d`.
2. No celular que vai assistir, abre o Recam na aba **Assistir** e lê o QR que o servidor mostra (no
   `docker compose logs` ou na página `https://<ip>:8443/setup`). Esse celular vira o dono.
3. Nesse mesmo celular, toca em **Adicionar câmera**. Aparece um QR.
4. No celular câmera, abre o Recam na aba **Câmera**, lê esse QR e dá um nome à câmera. Ele fica
   em modo câmera, com a tela preta.
5. No celular que assiste, a câmera aparece na lista, online, com o nível de bateria.
6. Toca na câmera e vê o vídeo ao vivo, com atraso abaixo de 1 segundo na rede local.
7. Na tela do vídeo ao vivo, liga e desliga a lanterna do celular câmera.

## Áreas do repositório

| Pasta | O que é | Estado |
|---|---|---|
| `server/` | Servidor .NET 10: pareamento, autenticação, hub em tempo real, proxy de sinalização WebRTC | ativa (criada na fase 0) |
| `app/` | App Flutter, Android: abas Câmera e Assistir | ativa (criada na fase 0) |
| `deploy/` | `compose.yaml`, `compose.bridge.yaml`, configuração do MediaMTX, `.env.example` | ativa (criada na fase 0) |
| `scripts/`, `.githooks/` | Gate de qualidade e hook de pre-commit | ativa |
| raiz | Estes documentos | ativa |

## Leitura obrigatória antes de trabalhar

1. `CODESTYLE.md`, sempre, inteiro.
2. `SPECS.md`: a seção da área que o bullet toca, mais "Protocolo" se o bullet cruza servidor
   e app.
3. `ROADMAP.md`: a definição de pronto no topo e o bullet atual.

## Loop de trabalho

1. Pegue o primeiro bullet não marcado do `ROADMAP.md`, em ordem. Não pule, não agrupe bullets.
   Se ele for `[junto]` ou `[opus]` e você não for Claude Opus, pare e avise o autor para trocar
   de modelo. Se for `[junto]` e o autor não estiver na conversa, pare e avise.
2. Implemente seguindo o `CODESTYLE.md` e os contratos do `SPECS.md`.
3. Rode o gate: `bash scripts/gate.sh`. Ele precisa passar.
4. Marque o bullet com `[x]` e escreva embaixo a nota `> Validação (AAAA-MM-DD): ...` dizendo o
   que foi feito e o que foi observado funcionando. Declare se foi só código escrito ou se o
   caminho foi percorrido.
5. Faça um commit em Conventional Commits (ver `CODESTYLE.md`).
   Quando o autor precisar testar algo que o agente não consegue rodar sozinho (celular,
   emulador, navegador), entregue os comandos exatos, um por bloco `bash`, e diga o que
   conferir na tela.
6. Se o bullet estiver errado ou impossível, não redefina em silêncio. Deixe sem marcar, escreva
   `> Bloqueado (AAAA-MM-DD): ...` com o motivo e siga para o próximo.
7. Se parar no meio, escreva `> Em andamento (AAAA-MM-DD): ...` com o que existe, o que falta e
   o próximo passo.

## Gate de qualidade

```bash
bash scripts/gate.sh
```

Roda, para cada área que já existe:
- `server/`: `dotnet format --verify-no-changes`, `dotnet build -warnaserror`, `dotnet test --solution` (Microsoft Testing Platform, ligado no `global.json`).
  Os testes de integração sobem o MediaMTX com Testcontainers, então o Docker precisa estar
  rodando.
- `app/`: `dart format --set-exit-if-changed`, `flutter analyze`, `flutter test`.

O hook `.githooks/pre-commit` roda o gate. Ative uma vez por clone:
`git config core.hooksPath .githooks`.

O gate não abre o app num celular, não transmite vídeo entre aparelhos e não prova que o
caminho principal funciona. Gate verde é condição necessária, não prova de que existe produto.

## Regras duras

Valem para qualquer bullet:

- Toda mudança vira bullet no `ROADMAP.md` antes de virar código, inclusive correção pedida pelo
  usuário depois de testar. Única exceção: mudança que não altera comportamento (erro de
  digitação, formatação, link, nome de variável local). Se o usuário reportou, é bullet.
- Nenhuma dependência nova (NuGet, pub, imagem Docker) sem um bullet que a peça.
- Warning e erro de lint se resolvem consertando o código, nunca com supressão
  (`#pragma warning disable`, `// ignore:`, `<NoWarn>`).
- Nunca contornar o gate (`--no-verify` ou equivalente).
- Bullet bloqueado não é redefinido em silêncio.
- Nada entra sem quem chame. Função, classe, endpoint ou tela sem chamador no código de produção
  não é bullet pronto. Teste não conta como chamador.

Regras do domínio:

- O .NET não processa vídeo. Ele repassa só a sinalização WHIP/WHEP (HTTP com SDP). Pacotes
  RTP vão direto entre os celulares e o MediaMTX.
- O app no modo câmera não grava nem guarda imagem ou vídeo no aparelho.
- O app só fala com o servidor que o usuário pareou. Sem analytics, sem crash reporting de
  terceiros, sem anúncio, sem chamada de rede para outro destino.
- Portas expostas pelo servidor: `8443/tcp` e `8189/udp`. Porta nova exige bullet e revisão do
  `SPECS.md`.
- Vídeo em H.264, teto de 1280x720 a 15 fps e 700 kbps. Nada de áudio no MVP.
- Token e credencial nunca aparecem em log, nem truncados. No banco, só o hash. Única exceção:
  o QR do token do dono, que o servidor imprime no log enquanto ninguém é dono
  (`Features/Setup/OwnerSetup.cs`).
- Todo texto que o usuário vê no app vem dos arquivos ARB, em `en` e `pt`. Nunca string literal
  em widget.

## Mapa do repositório

Estado planejado. As pastas são criadas nos bullets da fase 0.

```
AGENTS.md            este arquivo (canônico)
CLAUDE.md, GEMINI.md ponteiros para este arquivo
SPECS.md             produto, arquitetura, contratos, decisões
CODESTYLE.md         regras verificáveis num diff
ROADMAP.md           fila de execução
README.md            documento para humanos (inglês)
LICENSE              AGPL-3.0
scripts/gate.sh      gate de qualidade
.githooks/pre-commit roda o gate
deploy/              compose.yaml, compose.bridge.yaml, mediamtx.yml, .env.example
server/
  Recam.slnx
  Directory.Build.props
  src/Recam.Server/  Program.cs, Domain/, Infrastructure/, Features/<Feature>/
  tests/Recam.Server.Tests/
app/
  lib/core/          rede, pareamento, armazenamento, abstrações de plugin
  lib/camera/        aba Câmera
  lib/viewer/        aba Assistir
  lib/l10n/          app_en.arb, app_pt.arb
  test/
```
