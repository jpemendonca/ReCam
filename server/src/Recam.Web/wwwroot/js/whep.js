// Receives one camera's live video with WHEP through the server (SPECS.md 2.5). The offer carries
// all ICE candidates, so no trickle is needed, like in the app. No library.

const sessions = new Map();
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

// Returns a session id, or 0 when the server refused (camera not publishing yet, no access).
export async function start(video, cameraId, listener) {
  const connection = new RTCPeerConnection({ iceServers: [] });
  connection.addTransceiver('video', { direction: 'recvonly' });
  connection.ontrack = (event) => {
    video.srcObject = event.streams[0] ?? new MediaStream([event.track]);
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
    const resource = response.headers.get('Location');
    await connection.setRemoteDescription({ type: 'answer', sdp: await response.text() });

    const id = nextId++;
    sessions.set(id, { connection, resource, video });
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
