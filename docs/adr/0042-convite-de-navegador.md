# ADR 0042: Convite de Monitor para outro navegador

- Data: 2026-09-27
- Estado: aceita
- Origem: revisão no `SPECS.md` 12 (bullet 11.2)

## Contexto

O autor tem iPhone e não existe app de iPhone. Ele quer usar o iPhone só para assistir. Um
navegador entrava como Monitor de dois jeitos: o código de primeira abertura (só antes do primeiro
Monitor) ou o "Conectar navegador", em que o navegador novo mostra um QR e um celular Monitor com o
app Android lê e aprova. Sem um Android como Monitor, ninguém lê esse QR: o navegador do PC não lê
QR, e o iPhone não tem o app.

## Decisão

O caminho vira ao contrário: um Monitor que já existe cria um convite. É um `BrowserLink` criado já
aprovado por ele, com o `claim` dentro de um link `https://<endereço>/connect#l=<id>&c=<claim>`. O
Monitor web mostra o link como QR e como texto para copiar. A câmera do iPhone lê o QR e abre o
navegador; a página `/connect` resgata o convite pela rota de resgate que já existia e o navegador
vira `Viewer`.

- O `claim` vai no fragmento, que o navegador não manda ao servidor. Ele não aparece em log de
  servidor nem de proxy.
- Vale 10 minutos e uma vez só, como os QR de pareamento.
- O endereço do link é o primeiro de `RECAM_PUBLIC_URLS`, senão o que o Monitor está usando. O
  cookie só vale para o endereço em que foi criado, e a tela avisa isso.
- Só Monitor cria convite, então nada funciona antes da configuração inicial no PC.

## Consequências

- Qualquer navegador moderno vira Monitor sem app, inclusive iPhone, iPad e outro computador.
- Quem tiver o link dentro dos 10 minutos entra. O link é tão sensível quanto um QR de pareamento
  na tela, e a tela diz que vale uma vez só.
- Assistir de fora de casa continua dependendo de o servidor ser alcançável de fora; no iPhone em
  rede móvel só IPv6, o vídeo pode precisar de IPv6 no servidor (anotado na conversa de
  2026-09-27, não resolvido aqui).
- O Monitor web ganhou layout de celular e perdeu os estilos inline que a CSP bloqueava na linha
  do tempo.
