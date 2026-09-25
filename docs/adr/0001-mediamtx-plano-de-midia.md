# ADR 0001: MediaMTX como plano de mídia

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O ReCam precisa receber vídeo de vários celulares e entregá-lo a quem assiste, com atraso baixo.
Implementar WebRTC dentro do .NET (SIPSorcery, por exemplo) significaria cuidar de ICE, DTLS,
SRTP, jitter buffer e codecs à mão, o que custaria meses em problemas de mídia que não são o
foco do projeto.

## Decisão

O vídeo passa por um servidor de mídia pronto, o MediaMTX, rodando num container ao lado do
servidor .NET. Ele fala WHIP e WHEP e sabe gravar em fMP4, o que atende a gravação sem outra peça.
A imagem usa tag exata (`bluenviron/mediamtx:1.21.1`), nunca `latest`.

## Consequências

- O servidor .NET não toca em pacote de vídeo; ele fica com pareamento, autorização e estado.
- O deploy passa a ter dois containers, com a configuração do MediaMTX versionada em
  `deploy/mediamtx.yml`.
- Os testes de integração da mídia sobem o MediaMTX real com Testcontainers, na mesma tag do
  compose.
- Atualizar o MediaMTX é uma troca de tag revisada, porque o comportamento de gravação e de ICE
  muda entre versões.
