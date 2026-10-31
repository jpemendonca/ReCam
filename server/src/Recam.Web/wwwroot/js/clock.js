// Follows a recording as it plays, drawn on the page only (the recording stays as it is): the
// clock over the video, on every "timeupdate" with no round trip to .NET. .NET hears each new
// second, so the timeline bar keeps the moment under its middle line (bullet 13.4).

const format = (seconds) => {
  const s = Math.floor(seconds) % 86400;
  const pad = (n) => String(n).padStart(2, '0');
  return `${pad(Math.floor(s / 3600))}:${pad(Math.floor(s / 60) % 60)}:${pad(s % 60)}`;
};

// Times are seconds after the day's midnight, on the wall clock.
const state = { video: null, show: null, lastSecond: null };

function place() {
  const { video } = state;
  if (!video) {
    return;
  }
  const head = state.fileStart + video.currentTime;
  state.label.textContent = format(head);
  const second = Math.floor(head);
  if (second !== state.lastSecond) {
    state.lastSecond = second;
    state.listener.invokeMethodAsync('OnHead', head);
  }
}

// A new file started: fileStart is where it begins.
export function follow(video, label, fileStart, listener) {
  if (state.video && state.show) {
    state.video.removeEventListener('timeupdate', state.show);
  }
  Object.assign(state, { video, label, fileStart, listener, show: place, lastSecond: null });
  video.addEventListener('timeupdate', place);
  place();
}

// Nothing plays any more.
export function stop() {
  if (state.video && state.show) {
    state.video.removeEventListener('timeupdate', state.show);
  }
  Object.assign(state, { video: null, show: null, lastSecond: null });
}
