# HTTPS without the browser warning

Out of the box, ReCam serves HTTPS with its own self-signed certificate. It works, but the browser
warns once. To get rid of the warning, put a reverse proxy with a public certificate in front of the
server. This guide uses [Caddy](https://caddyserver.com), which gets a free Let's Encrypt
certificate by itself. Nginx Proxy Manager or Traefik work the same way.

You need ReCam already running (see the [README](../README.md)) and a domain name pointing to the
server, for example `recam.example.com`.

## What changes

- The proxy answers on `443/tcp` with the real certificate and passes everything to ReCam.
- ReCam stops using its own certificate (`RECAM_TLS=off`) and speaks plain HTTP on `8443`, only to
  the proxy.
- Video does not go through the proxy. WebRTC keeps going straight to `8189/udp`.

## 1. Configure ReCam

In `deploy/.env` (copy it from `.env.example`):

```bash
RECAM_TLS=off
RECAM_TRUSTED_PROXIES=127.0.0.1
RECAM_PUBLIC_URLS=https://recam.example.com
```

- `RECAM_TRUSTED_PROXIES` lets ReCam see each visitor's real address behind the proxy. Without it,
  everyone looks like the proxy and shares one limit on first-time code tries. Use the proxy's
  address: `127.0.0.1` when Caddy runs on the same machine, or the Docker network (for example
  `172.18.0.0/16`) when it runs in a container.
- `RECAM_PUBLIC_URLS` is the address the pairing QR codes send phones to.

Restart with `sudo docker compose up -d` in `deploy/`.

## 2. Configure Caddy

`/etc/caddy/Caddyfile`:

```
recam.example.com {
    reverse_proxy 127.0.0.1:8443
}
```

Reload Caddy (`sudo systemctl reload caddy`). It fetches the certificate on the first request.
WebSockets, used by the live updates, pass through without extra settings.

## 3. Firewall

Open `80/tcp` and `443/tcp` (Caddy) and `8189/udp` (video). Close `8443/tcp` to the outside: with
`RECAM_TLS=off` it speaks plain HTTP and should only be reached by the proxy.

## Phones paired before the change

Phones paired with the self-signed certificate keep its fingerprint and the old address. Pair them
again from the Monitor after the switch.
