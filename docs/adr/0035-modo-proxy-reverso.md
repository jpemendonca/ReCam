# ADR 0035: Modo atrás de proxy reverso

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

Quem expõe o servidor com Caddy, Nginx ou Traefik quer que o proxy termine o TLS com um
certificado público.

## Decisão

`RECAM_TLS=off` faz o servidor falar HTTP simples na 8443 e o QR sair sem fingerprint; o app
confere o certificado do proxy pelas CAs do sistema. `RECAM_TRUSTED_PROXIES` liga os
`X-Forwarded-*` só para esses proxies, sem nem o loopback por padrão.

## Consequências

- O `/setup` decide pelo IP real do cliente atrás de proxy confiável.
- O vídeo continua precisando da UDP 8189 alcançável direto.
