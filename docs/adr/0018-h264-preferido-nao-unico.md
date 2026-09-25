# ADR 0018: H.264 preferido, não único

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O `SPECS.md` pedia H.264. Alguns aparelhos, como o emulador, não têm encoder H.264, e o MediaMTX
aceita VP8 sem problema para vídeo ao vivo.

## Decisão

O app ordena H.264 primeiro e deixa os outros codecs como reserva. Aparelho sem H.264 publica em
VP8.

## Consequências

- Mais aparelhos funcionam como câmera ao vivo.
- O bullet 2.1 (recusar aparelho sem H.264) foi substituído: com a gravação, só a gravação exige
  H.264 (ADR 0030), porque o MediaMTX não grava VP8.
