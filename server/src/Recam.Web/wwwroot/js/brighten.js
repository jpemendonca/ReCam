// "Brighten" (SPECS.md 2.5): a CSS filter on this screen's <video>, set through the CSSOM, which
// the content security policy allows (a style attribute would not be). The setting per camera
// stays in this browser's localStorage.

const prefix = 'recam.brighten.';

export function load(cameraId) {
  try {
    return localStorage.getItem(prefix + cameraId);
  } catch {
    return null;
  }
}

export function save(cameraId, value) {
  try {
    if (value) {
      localStorage.setItem(prefix + cameraId, value);
    } else {
      localStorage.removeItem(prefix + cameraId);
    }
  } catch {
    // Private windows may refuse storage; the setting then lasts for this page only.
  }
}

export function apply(video, brightness, contrast) {
  if (!video) {
    return;
  }
  video.style.filter = brightness === 1 && contrast === 1 ? '' : `brightness(${brightness}) contrast(${contrast})`;
}
