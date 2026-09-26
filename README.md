# ReCam

[![CI](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml)
![Server coverage](https://github.com/jpemendonca/ReCam/raw/badges/coverage.svg)

Turn spare Android phones into security cameras. Self-hosted, open source, no ads, no
subscriptions required.

You need one phone and one computer to start:

- **The ReCam server**, a Docker container on your own machine or VPS. It pairs your phones,
  relays the video and serves the Monitor you open in the browser.
- **The ReCam app**, on the phone that will film. Later you can add more phones, as cameras or as
  Monitors that watch.

> Status: pre-alpha. The main path is written and tested, but not yet validated on real phones.
> There is no published image or APK yet: build from source. Follow progress in
> [ROADMAP.md](ROADMAP.md).

## How it works

1. **Start the server.** In the `deploy/` folder: on Linux, `docker compose up -d`; on Windows or
   macOS with Docker Desktop, copy `.env.example` to `.env`, set `RECAM_HOST` to the computer's
   network address and run `docker compose -f compose.bridge.yaml up -d`. Allow ports 8443/tcp and
   8189/udp in the firewall.
2. **Read the first-time code.** Run `docker compose exec server ./Recam.Server code` (with Docker
   Desktop, `docker compose -f compose.bridge.yaml exec server ./Recam.Server code`). It shows the
   address to open and a code like `ABCD-EFGH`. The same framed block is at the top of
   `docker compose logs server`, and in the output of `docker compose up` without `-d`.
3. **Open the browser on the computer** at `https://<server-ip>:8443`. The certificate is
   self-signed, so the browser warns once: choose to proceed (in Chrome, **Advanced** and then
   **Proceed**). Type the code. This browser is now the Monitor and shows an **Add camera** QR
   code.
4. **On the phone that will film**, open ReCam, tap **Scan QR code**, scan it and name the
   camera. The phone switches to camera mode, and its live video opens in the browser by itself.
   Toggle the phone's flashlight from there.
5. **Liked it?** From the browser, **Add camera** adds another camera and **Add Monitor** lets your
   main phone watch too. Another browser can join with **Connect browser** on a Monitor phone.

Lost every Monitor? `docker compose exec server ./Recam.Server reset-owner` removes them, and the
server prints a new first-time code.

Requirements: Android 9 or newer. Reference devices: Samsung Galaxy A10 and Xiaomi Redmi 6A.

## Development

Prerequisites: .NET 10 SDK, Flutter, Docker.

```bash
git config core.hooksPath .githooks
bash scripts/gate.sh
```

The gate runs formatting, analysis, build and tests for `server/` and `app/`. Server tests start
a MediaMTX container, so Docker must be running.

Architecture, protocol and decisions: [SPECS.md](SPECS.md). Code rules: [CODESTYLE.md](CODESTYLE.md).
Agent workflow: [AGENTS.md](AGENTS.md).

## License

[AGPL-3.0](LICENSE). Contributions require signing a Contributor License Agreement.
