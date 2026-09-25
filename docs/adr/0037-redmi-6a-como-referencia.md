# ADR 0037: Redmi 6A como aparelho de referência

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O plano citava o Redmi 7A (Snapdragon) como segundo aparelho de referência, mas o autor tem um
Redmi 6A, com MediaTek Helio A22. MediaTek antigo pode não expor encoder H.264 ao libwebrtc.

## Decisão

Os aparelhos de referência são o Samsung Galaxy A10 e o Xiaomi Redmi 6A. As validações no
aparelho usam esses dois.

## Consequências

- O teste no 6A precisa conferir se ele envia H.264. Sem H.264, ele transmite ao vivo em VP8 e a
  chave "Gravar sempre" fica bloqueada nele.
- O guia de bateria da Xiaomi (MIUI) segue valendo para o 6A.
