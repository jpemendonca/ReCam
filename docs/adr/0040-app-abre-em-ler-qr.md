# ADR 0040: App abre em "Ler QR code" e o QR decide o papel

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

Com o navegador como primeiro Monitor, o primeiro celular é sempre câmera. A pergunta "Filmar" ou
"Assistir" na primeira tela vira um passo a mais e uma chance de escolher errado, e o QR já diz o
papel desde a revisão de 2026-09-25 (ADR 0019).

## Decisão

A primeira abertura do app mostra "Ler QR code" e "Colar código". O `r` do QR passa a ser
obrigatório, com `camera` ou `viewer`. Câmera pede o nome e entra no modo câmera; Monitor pareia
com o nome "Monitor". O celular Monitor ganha no menu "Conectar navegador".

## Consequências

- A primeira tela fica com uma escolha só.
- Os textos "Filmar" e "Assistir" (bullet 1.12.12) saem do app e do glossário.
- QR antigo sem `r` deixa de ser aceito, o que não afeta ninguém antes do lançamento.
