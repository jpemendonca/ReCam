# ReCam

Turn spare Android phones into security cameras. The server runs on a computer in your home,
the video never leaves your network, and you watch in the browser. Open source, no ads, no
subscription.

This guide installs ReCam with person detection on: the computer looks for people in the recordings,
where there was motion, and marks them on the timeline. It runs only on your computer, with no
network.

You need:

- A computer that stays on, running Linux (Ubuntu, Linux Mint, Debian or similar). An old one is
  fine: the server does not process video.
- An Android phone to film (Android 9 or newer), with the ReCam app.
- The computer and the phone on the same network.

## Install

Run these on the computer, one at a time, in a terminal.

1. Install Docker and Git:

   ```bash
   sudo apt update && sudo apt install -y docker.io docker-compose-v2 git
   ```

2. Get ReCam:

   ```bash
   git clone -b claude/person-activity-detection-4pq7pg https://github.com/jpemendonca/ReCam.git && cd ReCam/deploy
   ```

3. Turn person detection on. From here on, every `docker compose` command includes it:

   ```bash
   echo "COMPOSE_FILE=compose.yaml:compose.detect.yaml" > .env
   ```

4. Build and start it (the first time takes a few minutes):

   ```bash
   sudo bash ../scripts/build-server.sh build && sudo docker compose up -d
   ```

5. Check that it is running. The four lines must say `Up`:

   ```bash
   sudo docker compose ps
   ```

6. Show the address and the first-time code:

   ```bash
   sudo docker compose exec server ./Recam.Server code
   ```

## First use

1. On a computer or phone in the same network, open the address from step 6, like
   `https://192.168.0.10:8443`. The browser warns that the connection is not private: this is
   expected, because the server makes its own certificate. Choose **Advanced**, then
   **Proceed**.
2. Type the first-time code. This browser becomes your **Monitor** and shows an **Add camera**
   QR code.
3. On the phone that will film, install the ReCam app, open it, tap **Scan QR code** and scan the
   QR code on the screen. Give the camera a name.
4. The live video opens in the browser, and the camera already records. Choose how much space the
   recordings may take; when it fills up, the oldest are deleted.

5. When someone walks in front of the camera, the recordings page shows it under **People on this
   day** a minute or two later, and the player draws a box around the person. **Show people**
   turns the boxes off.

To watch from another phone with the app, use **Add Monitor** in the browser. To watch in another
browser, use **Add Monitor › In a browser**.

## If something goes wrong

- **Step 5 shows `Restarting`, or not four lines.** See why:

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

- **Step 6 shows several addresses, or one that is not your network's.** Tell ReCam which one to
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

  "Looked for people in N segment(s)" means it works. It uses at most one CPU core; on a slow
  computer it may run a few minutes behind.

- **Turn person detection off.** Remove the `COMPOSE_FILE=` line from `.env`, then:

  ```bash
  sudo docker compose -f compose.yaml -f compose.detect.yaml rm -sf detect
  ```

- **Lost the code.** Run step 6 again. The code only exists until the first Monitor.

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
