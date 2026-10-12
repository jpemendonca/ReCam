<div align="center">

<img src="docs/brand/icon.svg" width="88" alt="ReCam icon">

# ReCam

**Turn a spare Android phone into a security camera.**<br>
Your own server, your own recordings, live in any browser.

[![CI](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/jpemendonca/ReCam/actions/workflows/ci.yml)
![Server coverage](https://github.com/jpemendonca/ReCam/raw/badges/coverage.svg)
[![License: AGPL-3.0](https://img.shields.io/badge/license-AGPL--3.0-00695C)](LICENSE)
![Android 9+](https://img.shields.io/badge/Android-9%2B-3DDC84?logo=android&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Docker](https://img.shields.io/badge/self--hosted-Docker-2496ED?logo=docker&logoColor=white)

[Install](#install) · [First use](#first-use) · [Download the app](https://github.com/jpemendonca/ReCam/releases/latest) · [Troubleshooting](#if-something-goes-wrong)

<br>

<img src="docs/images/hero.png" alt="A phone in camera mode next to the browser showing its live video">

</div>

<br>

You run one `docker compose up` on a computer at home or on a VPS. You scan a QR code with the
phone that will film. A few seconds later you watch it live in the browser. It records around
the clock, and the recordings page tells you when a person walked by. No account, no cloud, no
ads, no subscription.

## What you get

<table>
<tr>
<td width="50%" valign="top">
<img src="docs/images/mockup-cameras.png" alt="Camera list with battery, temperature and recording status">
<h3>Live in the browser</h3>
Video arrives in under a second on your network. Each camera shows its battery and temperature,
and you can turn its flashlight and sound on from the live view.
</td>
<td width="50%" valign="top">
<img src="docs/images/mockup-recordings.png" alt="Recordings page with the motion timeline and the people found">
<h3>Recordings with a timeline</h3>
Tap any hour to watch from that moment. Orange marks motion, green marks people, and
<b>People on this day</b> lists each person the server spotted. The player draws a box around them.
</td>
</tr>
<tr>
<td width="50%" valign="top">
<img src="docs/images/mockup-app.png" alt="The ReCam app: welcome screen and camera mode">
<h3>Pair with one QR code</h3>
Install the APK, tap <b>Scan QR code</b> and point it at the browser. The QR decides if the phone
films or watches. In camera mode the phone saves no video.
</td>
<td width="50%" valign="top">
<h3>Light on the phone</h3>
Built for phones from 2019 with 2 GB of RAM. Video goes as H.264 at up to 720p and 15 fps, and the
phone lowers the quality when it gets hot.
<h3>No third parties</h3>
The app talks only to the server you paired. No analytics, no third-party crash reports, nothing
loaded from a CDN. Recordings stay on your disk, and ReCam deletes the oldest when they reach the
space you chose.
<h3>Open source</h3>
AGPL-3.0. Server in .NET 10, browser Monitor in Blazor WebAssembly, app in Flutter.
</td>
</tr>
</table>

## How it works

<img src="docs/images/how-it-works.png" alt="Android phone sends video over WebRTC to your server, and the browser watches it">

The phone sends video over WebRTC to [MediaMTX](https://github.com/bluenviron/mediamtx) on your
server. The .NET server pairs devices, relays the WebRTC signaling and serves the Monitor page.
Video never passes through .NET. Two more containers, both without network access, score motion
in each recorded minute and look for people in the seconds that moved.

## What you need

- A computer that stays on, with Linux (Ubuntu, Debian or similar), or a VPS. Any Intel or AMD
  computer from the last ten years works.
- An Android phone to film, with Android 9 or newer and the ReCam app. Download the APK from
  [Releases](https://github.com/jpemendonca/ReCam/releases/latest): `arm64-v8a` for most phones,
  `armeabi-v7a` for older 32-bit ones. Android asks you to allow installs from this source.
- The computer and the phone on the same network, or a VPS both can reach.

## Install

Open a terminal on the computer and run these, one at a time. Skip the first one if you already
have Docker and Git:

```bash
sudo apt update && sudo apt install -y docker.io docker-compose-v2 git
```

```bash
git clone https://github.com/jpemendonca/ReCam.git && cd ReCam/deploy
```

```bash
sudo bash ../scripts/build-server.sh && sudo docker compose up -d
```

The build takes a few minutes the first time. Check that the four lines say `Up`:

```bash
sudo docker compose ps
```

Then get the address and the first-time code:

```bash
sudo docker compose exec server ./Recam.Server code
```

Keep this output at hand. You need the address and the code in the next section:

```text
==================== ReCam ====================
No Monitor yet. On a computer:
  1. Open https://192.168.0.10:8443 in the browser and accept the certificate warning.
  2. Type the first-time code RHGJ-JCAU
To see this again: docker compose exec server ./Recam.Server code
===============================================
```

The code works once: the first browser that types it becomes your Monitor. If you lose it, run the
same command again.

On a VPS, add a line `RECAM_HOST=` with the VPS public IP to `.env` before you start the containers, and open
ports 8443/tcp and 8189/udp in the provider's panel. With Docker Desktop on Windows or macOS, set
`RECAM_HOST` to the computer's network address and add `-f compose.bridge.yaml` to each
`docker compose` command.

## First use

1. On a computer or phone in the same network, open the address the last command showed, like
   `https://192.168.0.10:8443`. The browser warns that the connection is not private, because the
   server signs its own certificate. Open the warning's details and continue to the site.
2. Type the first-time code from the same output. This browser becomes your **Monitor** and shows an **Add camera** QR
   code.
3. On the phone that will film, open this page, download the APK from
   [Releases](https://github.com/jpemendonca/ReCam/releases/latest) and install it. Open the ReCam
   app, tap **Scan QR code** and scan the code on the screen. Give the camera a name.
4. The live video opens in the browser, and the camera starts recording. Choose how much space the
   recordings may take; ReCam deletes the oldest when it fills up. You can switch the phone's
   flashlight from the live view.
5. When someone walks in front of the camera, the recordings page lists them under **People on
   this day** a minute or two later, and the player draws a box around each person.

To watch from another phone, install the app there and use **Add Monitor** in the browser. To
watch from another browser, use **Add Monitor › In a browser** and open the link it shows in the other browser, or scan
the QR code with a phone's camera.

To get the padlock without the warning, put a reverse proxy in front:
[docs/reverse-proxy.md](docs/reverse-proxy.md).

## If something goes wrong

<details>
<summary>Open the list of common problems and fixes</summary>

- **`docker compose ps` shows `Restarting`, or a line is missing.** Read the log:

  ```bash
  sudo docker compose logs server
  ```

  "address already in use" means another program holds port 8443 or 8189. Stop that program and
  run `sudo docker compose up -d` again.

- **The browser cannot open the address.** Put the phone or the other computer on the same
  network as the server. If the server has a firewall on, open ReCam's two ports:

  ```bash
  sudo ufw allow 8443/tcp && sudo ufw allow 8189/udp
  ```

- **The code command shows several addresses, or one outside your network.** Add a line
  `RECAM_HOST=` with the computer's address in your network (like `RECAM_HOST=192.168.0.10`) to
  `.env`:

  ```bash
  nano .env
  ```

  Then start again:

  ```bash
  sudo docker compose up -d
  ```

- **The live video stays black or keeps loading.** The video travels on port 8189/udp, so open it
  in the firewall (above). The **Diagnostics** button under the video shows what your browser
  receives.

- **Nobody shows up under People on this day.** ReCam looks for people only in cameras that record,
  and only in the seconds with motion. Check the log:

  ```bash
  sudo docker compose logs detect
  ```

  "Looked for people in N segment(s)" means it works. A slow computer can fall a few minutes
  behind.

- **You lost the code.** Run the code command again. The code exists until the first Monitor.

- **You lost every Monitor.** This removes them. Wait 30 seconds and run the code command again
  for a new first-time code:

  ```bash
  sudo docker compose exec server ./Recam.Server reset-owner
  ```

- **You want to start over.** This deletes every pairing and every recording:

  ```bash
  sudo docker compose down -v && sudo docker compose up -d
  ```

</details>

## Update

In `ReCam/deploy`:

```bash
git pull && sudo bash ../scripts/build-server.sh && sudo docker compose up -d
```

Your pairings and recordings stay.

## Stop

```bash
sudo docker compose down
```

Your pairings and recordings stay until you start it again.

## The containers

- **`server`**: .NET 10 with a SQLite database. It serves the Monitor on port `8443`, pairs
  phones, relays the WebRTC signaling (WHIP/WHEP), sends commands like the flashlight and keeps
  recordings within their space. About 210 MB of RAM.
- **`mediamtx`**: receives video from the cameras on port `8189/udp`, sends it live to viewers and
  writes recordings in 1-minute files. About 50 MB of RAM.
- **`motion`**: FFmpeg with no network access. It scores motion in each finished recording for the
  timeline. About 60 MB of RAM and close to zero CPU at rest.
- **`detect`**: a small .NET program that looks for people. It reads the seconds `motion` marked,
  one frame per second, and runs the YOLOX-Tiny model on the CPU, with no network access. About
  150 MB of RAM, and up to one CPU core for a few seconds after each minute with motion.

## Development

You need the .NET 10 SDK, Flutter and Docker.

```bash
git config core.hooksPath .githooks
bash scripts/gate.sh
```

The gate checks formatting, analysis, build and tests for `server/` and `app/`. Server tests start
a MediaMTX container, so keep Docker running.

`bash scripts/build-server.sh` builds the Docker images and `bash scripts/build-apk.sh` builds the
APK, both stamped with the version from Git. Other builds show the version `dev`.

In Portuguese: architecture, protocol and decisions in [SPECS.md](SPECS.md), code rules in
[CODESTYLE.md](CODESTYLE.md), agent workflow in [AGENTS.md](AGENTS.md).

## License

[AGPL-3.0](LICENSE).
