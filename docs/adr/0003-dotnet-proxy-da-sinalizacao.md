# ADR 0003: .NET como proxy da sinalização WHIP/WHEP

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O MediaMTX tem sua própria porta HTTP de WebRTC. Expor essa porta significaria um segundo
certificado, uma segunda superfície de ataque e um hook de autenticação para o MediaMTX perguntar
ao servidor quem pode publicar ou assistir.

## Decisão

O servidor .NET recebe o `POST /whip/{cameraId}` e o `POST /whep/{cameraId}`, confere a credencial e
o papel do aparelho, e repassa só o SDP ao MediaMTX, que escuta em `127.0.0.1:8889` (ou na rede
interna do compose). O `Location` devolvido é reescrito para passar pelo servidor também.

## Consequências

- Uma só porta TCP pública (8443), um só certificado e a autorização num lugar só.
- O MediaMTX fica sem porta TCP exposta e sem hook de autenticação.
- O .NET não processa vídeo: os pacotes RTP vão direto ao MediaMTX pela UDP 8189.
- Os endpoints `PATCH` e `DELETE` da sessão também passam pelo proxy.
