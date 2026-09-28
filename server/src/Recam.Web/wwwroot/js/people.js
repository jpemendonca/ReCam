// Draws the boxes around people over a recording as it plays, on the page only (the recording
// stays as it is). Where the boxes are at each moment comes from .NET (PersonTrack.Flat); this
// only fits them to the picture, which the <video> shows letterboxed.

const svgNs = 'http://www.w3.org/2000/svg';
const state = { video: null, boxes: null, track: null, frame: 0 };

// The part of the element the picture takes, with object-fit: contain.
function picture(video) {
  const width = video.clientWidth;
  const height = video.clientHeight;
  if (!video.videoWidth || !video.videoHeight || !width || !height) {
    return null;
  }
  const scale = Math.min(width / video.videoWidth, height / video.videoHeight);
  const shownWidth = video.videoWidth * scale;
  const shownHeight = video.videoHeight * scale;
  // The boxes' layer covers the player, which may be larger than the <video>.
  return {
    left: video.offsetLeft + (width - shownWidth) / 2,
    top: video.offsetTop + (height - shownHeight) / 2,
    shownWidth,
    shownHeight,
  };
}

function draw() {
  const { video, boxes, track } = state;
  if (!video || !track) {
    return;
  }
  const area = picture(video);
  const flat = area ? track.invokeMethod('Flat', video.currentTime) : [];
  const count = flat.length / 4;
  boxes.setAttribute('viewBox', `0 0 ${boxes.clientWidth || 1} ${boxes.clientHeight || 1}`);
  while (boxes.childElementCount < count) {
    const rect = document.createElementNS(svgNs, 'rect');
    rect.setAttribute('class', 'person-box');
    boxes.appendChild(rect);
  }
  while (boxes.childElementCount > count) {
    boxes.lastChild.remove();
  }
  for (let index = 0; index < count; index++) {
    const [x, y, w, h] = flat.slice(index * 4, index * 4 + 4);
    const rect = boxes.children[index];
    rect.setAttribute('x', area.left + x * area.shownWidth);
    rect.setAttribute('y', area.top + y * area.shownHeight);
    rect.setAttribute('width', w * area.shownWidth);
    rect.setAttribute('height', h * area.shownHeight);
  }
  state.frame = requestAnimationFrame(draw);
}

export function follow(video, boxes, track) {
  stop();
  Object.assign(state, { video, boxes, track });
  state.frame = requestAnimationFrame(draw);
}

export function stop() {
  cancelAnimationFrame(state.frame);
  if (state.boxes) {
    state.boxes.replaceChildren();
  }
  Object.assign(state, { video: null, boxes: null, track: null, frame: 0 });
}

// The player, not just the <video>, so the boxes go along.
export function fullScreen(player) {
  if (document.fullscreenElement) {
    document.exitFullscreen();
  } else if (player.requestFullscreen) {
    player.requestFullscreen();
  }
}
