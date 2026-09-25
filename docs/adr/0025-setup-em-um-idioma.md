# ADR 0025: Página do servidor em um idioma por vez

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

A página `/setup` mostrava inglês e português juntos e tinha um `onclick` na caixa do código.

## Decisão

A página escolhe o idioma pelo `Accept-Language` (a primeira preferência entre pt e en; sem
nenhuma, inglês) e responde com `Vary: Accept-Language`. Sem Monitor: passo a passo ao lado do QR
e o código em texto. Com Monitor: cartões de Câmeras e de Monitores. CSS no próprio HTML, com
tema claro e escuro. Nenhum script.

## Consequências

- Textos da página ficam em `SetupTexts`, nos dois idiomas.
- A página continua sem JavaScript, o que simplifica a política de segurança.
