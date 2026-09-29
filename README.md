# ReCam

[![CI](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml)
![Server coverage](https://github.com/jpemendonca/ReCam/raw/badges/coverage.svg)

Turn spare Android phones into security cameras. Self-hosted, open source, no ads, no
subscriptions required.

You need:

- **A computer that stays on**, running Linux (Ubuntu, Debian or similar), or a VPS. Any computer
  from the last ten years is enough.
- **An Android phone to film** (Android 9 or newer), with the ReCam app. Later you can add more
  phones, as cameras or as Monitors that watch.
- The computer and the phone on the same network, or a VPS both can reach.

> Status: pre-alpha. There is no published image or APK yet: the server is built from source, as
> below. Follow progress in [ROADMAP.md](ROADMAP.md).

## Install

Run these on the computer, one at a time, in a terminal:

```bash
sudo apt update && sudo apt install -y docker.io docker-compose-v2 git
```

```bash
git clone https://github.com/jpemendonca/ReCam.git && cd ReCam/deploy
```

```bash
echo "COMPOSE_FILE=compose.yaml:compose.detect.yaml" >> .env
```

```bash
sudo bash ../scripts/build-server.sh build && sudo docker compose up -d
```

The last one takes a few minutes the first time. Then check that the four lines say `Up`:

```bash
sudo docker compose ps
```

And show the address and the first-time code:

```bash
sudo docker compose exec server ./Recam.Server code
```

The third command turns on person detection, which marks people in the recordings. To install
without it, skip that command; see [Person detection](#person-detection).

On a VPS, add `RECAM_HOST=` with the VPS public IP to `.env` before building, and allow ports
8443/tcp and 8189/udp in the provider's panel. On Windows or macOS with Docker Desktop, set
`RECAM_HOST` to the computer's network address and use `-f compose.bridge.yaml` in every
`docker compose` command.

## First use

1. On a computer or phone in the same network, open the address shown by the last command, like
   `https://192.168.0.10:8443`. The browser warns that the connection is not private: this is
   expected, because the server makes its own certificate. Choose **Advanced**, then **Proceed**.
2. Type the first-time code. This browser becomes your **Monitor** and shows an **Add camera** QR
   code.
3. On the phone that will film, install the ReCam app, open it, tap **Scan QR code** and scan the
   QR code on the screen. Give the camera a name.
4. The live video opens in the browser, and the camera already records. Choose how much space the
   recordings may take; when it fills up, the oldest are deleted. Toggle the phone's flashlight
   from the live view.
5. When someone walks in front of the camera, the recordings page shows
   it under **People on this day** a minute or two later, and the player draws a box around the
   person. **Show people** turns the boxes off.

To watch from another phone with the app, use **Add Monitor** in the browser. To watch in another
browser, use **Add Monitor › In a browser**, which shows a link as a QR code.

Want the padlock without the warning? Put a reverse proxy in front:
[docs/reverse-proxy.md](docs/reverse-proxy.md).

## If something goes wrong

- **`docker compose ps` shows `Restarting`, or a line is missing.** See why:

  ```bash
  sudo docker compose logs server
  ```

  "address already in use" means another program uses port 8443 or 8189. Stop it, then run
  `sudo docker compose up -d` again.

- **The browser cannot open the address.** The computer and the phone or other computer must be
  on the same network. If the computer has a firewall on, allow ReCam's two ports:

  ```bash
  sudo ufw allow 8443/tcp && sudo ufw allow 8189/udp
  ```

- **The code command shows several addresses, or one that is not your network's.** Tell ReCam which one to
  use: add a line `RECAM_HOST=` with the computer's address in your network (like
  `RECAM_HOST=192.168.0.10`) to `.env`, and start again:

  ```bash
  nano .env
  ```

  ```bash
  sudo docker compose up -d
  ```

- **The live video stays black or keeps loading.** The video uses port 8189/udp. Allow it in the
  firewall (above). The **Diagnostics** button under the video shows what the browser receives.

- **Nobody shows up under People on this day.** Detection only looks at cameras that record, in
  the seconds with motion. See what it is doing:

  ```bash
  sudo docker compose logs detect
  ```

  "Looked for people in N segment(s)" means it works. On a slow computer it may run a few minutes
  behind.

- **Lost the code.** Run the code command again. The code only exists until the first Monitor.

- **Lost every Monitor.** This removes them and makes a new first-time code:

  ```bash
  sudo docker compose exec server ./Recam.Server reset-owner
  ```

- **Start over from nothing.** This deletes every pairing and every recording:

  ```bash
  sudo docker compose down -v && sudo docker compose up -d
  ```

## Update

In `ReCam/deploy`:

```bash
git pull && sudo bash ../scripts/build-server.sh build && sudo docker compose up -d
```

Pairings and recordings stay.

## Stop

```bash
sudo docker compose down
```

Pairings and recordings stay until you start it again.

## The containers

- **`server`**: .NET 10 with a SQLite database. Serves the Monitor in the browser on port
  `8443`, pairs phones, relays the WebRTC signaling (WHIP/WHEP), sends commands such as the
  flashlight, and keeps recordings within their space. About 210 MB of RAM.
- **`mediamtx`**: receives the video from the cameras on port `8189/udp`, sends it live to whoever
  watches, and writes the recordings in 1-minute files. About 50 MB of RAM.
- **`motion`**: FFmpeg, with no network. Scores motion in each closed recording, for the timeline.
  About 60 MB of RAM and almost no CPU at rest.
- **`detect`** (on by default, see [Person detection](#person-detection)): looks for people in the seconds with motion, with the YOLOX-Tiny model
  on the CPU, with no network. About 150 MB of RAM and at most one CPU core.

On a very small machine, `motion` can be left out: live video, pairing, recording and playback go
on working, only the motion marks on the timeline disappear (and, with them, person detection).

## Person detection

On by default with the commands above. The `detect` service looks for people only in the
recordings, only in the seconds with motion, one frame per second, on this computer and with no
network. It uses about 150 MB of RAM and, for a few seconds after each minute with motion, up to
one CPU core; the rest of the time, almost nothing.

It may not be worth it:

- On a machine with a single CPU core or 1 GB of RAM, like the smallest free VPS plans: while it
  works, live video and the Monitor may slow down. `RECAM_DETECT_CPUS=0.5` in `.env` halves its
  share, and it just runs further behind.
- On a small disk: its image takes about 1.4 GB.
- When people pass in front of the camera all day (a shop, a living room): every event becomes
  "person", and motion alone says as much.

To turn it off, remove the `COMPOSE_FILE=` line from `.env`, then:

```bash
sudo docker compose -f compose.yaml -f compose.detect.yaml rm -sf detect
```

## Development

Prerequisites: .NET 10 SDK, Flutter, Docker.

```bash
git config core.hooksPath .githooks
bash scripts/gate.sh
```

Builds that carry the version from Git: `bash scripts/build-server.sh` (Docker image) and
`bash scripts/build-apk.sh` (APK). Other builds show the version `dev`.

The gate runs formatting, analysis, build and tests for `server/` and `app/`. Server tests start
a MediaMTX container, so Docker must be running.

Architecture, protocol and decisions: [SPECS.md](SPECS.md). Code rules: [CODESTYLE.md](CODESTYLE.md).
Agent workflow: [AGENTS.md](AGENTS.md).

## License

[AGPL-3.0](LICENSE). Contributions require signing a Contributor License Agreement.
