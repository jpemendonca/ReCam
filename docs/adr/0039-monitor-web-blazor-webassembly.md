# ADR 0039: Monitor web em Blazor WebAssembly, cliente do mesmo protocolo

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O Monitor no navegador precisa de lista de câmeras, vídeo ao vivo, lanterna, gravações e gestão.
Componentes Blazor rodando no servidor chamariam a lógica de várias features ao mesmo tempo, o que
o teste de arquitetura proíbe, ou duplicariam a lógica dos endpoints. O projeto também é portfólio
.NET.

## Decisão

O Monitor web é o `Recam.Web`, um app Blazor WebAssembly que fala com o servidor pela mesma API
REST, pelo mesmo hub SignalR e pelo mesmo WHEP do app Flutter, com os próprios tipos do protocolo.
O `Recam.Server` só serve os arquivos. O vídeo usa um módulo JavaScript próprio, sem biblioteca.
Testes com bUnit e fakes escritos à mão. Nada é carregado de fora do servidor.

## Consequências

- O servidor não ganha caminho novo de regra: o navegador é mais um cliente, como o app.
- A primeira abertura baixa alguns MB, guardados em cache pelo navegador.
- Entram dependências novas pedidas pela fase 6: o hospedeiro e o cliente do Blazor WebAssembly,
  o cliente SignalR para .NET no navegador e o bUnit.
