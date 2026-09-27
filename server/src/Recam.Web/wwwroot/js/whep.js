// Receives one camera's live video with WHEP through the server (SPECS.md 2.5). The offer carries
// all ICE candidates, so no trickle is needed, like in the app. No library.

const sessions = new Map();

// The last connection that never got media, kept so the diagnostics can still show why.
let lastFailed = null;
let nextId = 1;

function waitForIceGathering(connection, timeoutMs) {
  if (connection.iceGatheringState === 'complete') {
    return Promise.resolve();
  }
  return new Promise((resolve) => {
    const timer = setTimeout(resolve, timeoutMs);
    connection.addEventListener('icegatheringstatechange', () => {
      if (connection.iceGatheringState === 'complete') {
        clearTimeout(timer);
        resolve();
      }
    });
  });
}

// How long the media connection may take to open after the server answered.
const mediaTimeoutMs = 10000;

// Resolves true once media flows (ICE and DTLS done), false if it fails or takes too long.
function waitForMedia(connection) {
  if (connection.connectionState === 'connected') {
    return Promise.resolve(true);
  }
  return new Promise((resolve) => {
    const timer = setTimeout(() => resolve(false), mediaTimeoutMs);
    const check = () => {
      const state = connection.connectionState;
      if (state === 'connected' || state === 'failed' || state === 'closed') {
        clearTimeout(timer);
        connection.removeEventListener('connectionstatechange', check);
        resolve(state === 'connected');
      }
    };
    connection.addEventListener('connectionstatechange', check);
  });
}

// Safari, and every browser on an iPhone, does not always start a <video> whose srcObject
// changed, even with autoplay: it stays on a black first frame. Asking to play fixes that; a
// refusal is kept for the diagnostics instead of vanishing.
function play(video, session) {
  const playing = video.play();
  if (playing && playing.catch) {
    playing.catch((error) => {
      session.playError = `${error.name}: ${error.message}`;
    });
  }
}

// Returns a session id; 0 when the server refused (camera not publishing yet, no access), so the
// caller tries again; -1 when the server answered but no media connection opened.
export async function start(video, cameraId, listener) {
  const connection = new RTCPeerConnection({ iceServers: [] });
  connection.addTransceiver('video', { direction: 'recvonly' });
  // A camera without a microphone rejects this line, and the video plays alone.
  connection.addTransceiver('audio', { direction: 'recvonly' });
  const stream = new MediaStream();
  const session = { connection, video, playError: null };
  connection.ontrack = (event) => {
    stream.addTrack(event.track);
    if (video.srcObject !== stream) {
      video.srcObject = stream;
    }
    play(video, session);
  };

  try {
    await connection.setLocalDescription(await connection.createOffer());
    await waitForIceGathering(connection, 3000);
    const response = await fetch(`whep/${cameraId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/sdp', 'X-Recam-Web': '1' },
      body: connection.localDescription.sdp,
    });
    if (response.status !== 201) {
      connection.close();
      return 0;
    }
    session.resource = response.headers.get('Location');
    await connection.setRemoteDescription({ type: 'answer', sdp: await response.text() });

    const id = nextId++;
    sessions.set(id, session);
    if (!(await waitForMedia(connection))) {
      lastFailed = await diagnostics(id);
      await stop(id);
      return -1;
    }
    connection.onconnectionstatechange = () => {
      const state = connection.connectionState;
      if (sessions.has(id) && (state === 'failed' || state === 'disconnected')) {
        listener.invokeMethodAsync('OnEnded');
      }
    };
    return id;
  } catch {
    connection.close();
    return 0;
  }
}

export async function stop(id) {
  const session = sessions.get(id);
  if (!session) {
    return;
  }
  sessions.delete(id);
  session.connection.onconnectionstatechange = null;
  session.connection.close();
  session.video.srcObject = null;
  if (session.resource) {
    try {
      await fetch(session.resource, { method: 'DELETE', headers: { 'X-Recam-Web': '1' } });
    } catch {
      // The session times out on MediaMTX by itself.
    }
  }
}

// Browsers only autoplay silent video, so the live view starts muted and a button turns the
// sound on. The muted property, not the attribute, is what the element obeys after loading.
// Before any click on the page, unmuting would pause the video, so it stays silent; returns
// whether it ended up muted.
export function setMuted(video, muted) {
  const clicked = !navigator.userActivation || navigator.userActivation.hasBeenActive;
  video.muted = muted || !clicked;
  return video.muted;
}

// What the diagnostics panel shows: the connection, what arrives and what the <video> does with
// it. Read from the browser's own statistics; nothing leaves the page.
export async function diagnostics(id) {
  const session = sessions.get(id);
  if (!session) {
    return lastFailed;
  }
  const { connection, video } = session;
  const result = {
    connection: connection.connectionState,
    ice: connection.iceConnectionState,
    path: null,
    bytesReceived: null,
    framesReceived: null,
    framesDecoded: null,
    codec: null,
    width: null,
    height: null,
    player: `${video.paused ? 'paused' : 'playing'}, readyState ${video.readyState}, ${video.videoWidth}x${video.videoHeight}`,
    playError: session.playError,
  };
  const stats = await connection.getStats();
  let pairId = null;
  stats.forEach((report) => {
    if (report.type === 'transport' && report.selectedCandidatePairId) {
      pairId = report.selectedCandidatePairId;
    }
  });
  stats.forEach((report) => {
    if (!pairId && report.type === 'candidate-pair' && report.nominated && report.state === 'succeeded') {
      pairId = report.id;
    }
    if (report.type === 'inbound-rtp' && (report.kind === 'video' || report.mediaType === 'video')) {
      result.bytesReceived = report.bytesReceived ?? null;
      result.framesReceived = report.framesReceived ?? null;
      result.framesDecoded = report.framesDecoded ?? null;
      result.width = report.frameWidth ?? null;
      result.height = report.frameHeight ?? null;
      const codec = report.codecId && stats.get(report.codecId);
      if (codec) {
        result.codec = [codec.mimeType, codec.sdpFmtpLine].filter(Boolean).join(' ');
      }
    }
  });
  const pair = pairId && stats.get(pairId);
  if (pair) {
    const local = stats.get(pair.localCandidateId);
    const remote = stats.get(pair.remoteCandidateId);
    const describe = (candidate) =>
      candidate ? `${candidate.candidateType ?? '?'} ${candidate.protocol ?? '?'}` : '?';
    result.path = `${describe(local)} -> ${describe(remote)}`;
  }
  return result;
}
