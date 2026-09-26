// The clock over a recording: the file's start plus where the <video> is, updated as it plays.
// Drawn on the page only; the recording stays as it is.

function format(seconds) {
  const s = Math.floor(seconds) % 86400;
  const pad = (n) => String(n).padStart(2, '0');
  return `${pad(Math.floor(s / 3600))}:${pad(Math.floor(s / 60) % 60)}:${pad(s % 60)}`;
}

// startSeconds is the file's start as seconds after midnight, on the wall clock.
export function follow(video, label, startSeconds) {
  if (video.recamClock) {
    video.removeEventListener('timeupdate', video.recamClock);
  }
  const show = () => {
    label.textContent = format(startSeconds + video.currentTime);
  };
  video.recamClock = show;
  video.addEventListener('timeupdate', show);
  show();
}
