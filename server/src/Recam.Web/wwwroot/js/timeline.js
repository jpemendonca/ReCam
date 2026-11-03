// Taps and drags on the timeline bar (bullet 13.4). The bar is captured while the pointer is
// down, so a drag keeps going when the finger or the mouse leaves it. .NET hears fractions of the
// bar's width: where a tap landed, how far a drag moved, and where the mouse is resting.

// Less than this, in pixels, is a tap with a shaky hand, not a drag.
const tapSlack = 5;

export function attach(bar, listener) {
  if (bar.dataset.timelineAttached) {
    return;
  }
  bar.dataset.timelineAttached = '1';
  let down = null;
  let frame = 0;
  let pending = null;
  let hoverFrame = 0;
  let hovering = false;

  const width = () => bar.getBoundingClientRect().width || 1;

  const endHover = () => {
    cancelAnimationFrame(hoverFrame);
    hoverFrame = 0;
    if (hovering) {
      hovering = false;
      listener.invokeMethodAsync('OnHoverEnd');
    }
  };

  // One call per frame at most, and only for a mouse: a finger has nothing to rest on the bar.
  const hover = (event) => {
    if (event.pointerType !== 'mouse') {
      return;
    }
    const { left, width: wide } = bar.getBoundingClientRect();
    const fraction = (event.clientX - left) / (wide || 1);
    if (!hoverFrame) {
      hoverFrame = requestAnimationFrame(() => {
        hoverFrame = 0;
        hovering = true;
        listener.invokeMethodAsync('OnHover', Math.min(Math.max(fraction, 0), 1));
      });
    }
  };

  bar.addEventListener('pointerdown', (event) => {
    down = { x: event.clientX, left: bar.getBoundingClientRect().left, moved: false };
    bar.setPointerCapture(event.pointerId);
    event.preventDefault();
  });

  bar.addEventListener('pointermove', (event) => {
    if (!down) {
      hover(event);
      return;
    }
    const dx = event.clientX - down.x;
    if (!down.moved && Math.abs(dx) < tapSlack) {
      return;
    }
    down.moved = true;
    endHover();
    // One call per frame at most: the page redraws the bar for each.
    pending = dx / width();
    if (!frame) {
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (pending !== null) {
          listener.invokeMethodAsync('OnDrag', pending);
          pending = null;
        }
      });
    }
  });

  const end = (event) => {
    if (!down) {
      return;
    }
    // A drag frame still waiting would land after the drop.
    cancelAnimationFrame(frame);
    frame = 0;
    pending = null;
    const dx = event.clientX - down.x;
    if (down.moved) {
      listener.invokeMethodAsync('OnDrop', dx / width());
    } else {
      listener.invokeMethodAsync('OnTap', (event.clientX - down.left) / width());
    }
    down = null;
  };
  bar.addEventListener('pointerup', end);
  bar.addEventListener('pointerleave', endHover);
  bar.addEventListener('pointercancel', () => {
    down = null;
  });
}
