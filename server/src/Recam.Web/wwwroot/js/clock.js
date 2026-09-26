// Follows a recording as it plays, drawn on the page only (the recording stays as it is): the
// clock over the video, and the line on the timeline bar. Runs on every "timeupdate", with no
// round trip to .NET, which only hears when the video runs out of the bar's window.

const format = (seconds) => {
  const s = Math.floor(seconds) % 86400;
  const pad = (n) => String(n).padStart(2, '0');
  return `${pad(Math.floor(s / 3600))}:${pad(Math.floor(s / 60) % 60)}:${pad(s % 60)}`;
};

// Times are seconds after the day's midnight, on the wall clock.
const state = { video: null, show: null, window: null, wasInside: false };

function place() {
  const { video, window: shown } = state;
  if (!video || !shown) {
    return;
  }
  const head = state.fileStart + video.currentTime;
  state.label.textContent = format(head);
  const inside = head >= shown.start && head < shown.end;
  if (inside) {
    const x = ((head - shown.start) / (shown.end - shown.start)) * shown.width;
    state.line.setAttribute('x1', x);
    state.line.setAttribute('x2', x);
    state.line.style.visibility = 'visible';
  } else {
    state.line.style.visibility = 'hidden';
    if (state.wasInside) {
      shown.listener.invokeMethodAsync('OnHeadLeft', head);
    }
  }
  state.wasInside = inside;
}

// A new file started: fileStart is where it begins.
export function follow(video, label, line, fileStart) {
  if (state.video && state.show) {
    state.video.removeEventListener('timeupdate', state.show);
  }
  Object.assign(state, { video, label, line, fileStart, show: place, wasInside: true });
  video.addEventListener('timeupdate', place);
  place();
}

// Nothing plays any more: the line goes away.
export function stop() {
  if (state.video && state.show) {
    state.video.removeEventListener('timeupdate', state.show);
  }
  if (state.line) {
    state.line.style.visibility = 'hidden';
  }
  Object.assign(state, { video: null, show: null });
}

// The bar now shows start to end, drawn width units wide.
export function showWindow(start, end, width, listener) {
  state.window = { start, end, width, listener };
  place();
}
