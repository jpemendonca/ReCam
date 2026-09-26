#!/bin/bash
# ReCam motion scores (SPECS.md 2.4). For each closed recording segment, writes next to it a
# "<segment>.motion" file with one line per half second: "<seconds from the start> <fraction of
# the picture that changed>". A pixel counts as changed when it moved more than 30 of 255 levels
# from the previous half second, so sensor noise stays out. The server turns the scores into
# motion events; no video leaves this container.
set -u

root="${RECAM_RECORDINGS_DIR:-/recordings}"
interval="${RECAM_MOTION_INTERVAL:-20}"
# A segment nobody wrote to for this long is closed, even if it is the camera's newest.
closed_after="${RECAM_MOTION_CLOSED_AFTER:-90}"
segment_name='^[0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9]{2}-[0-9]{2}-[0-9]{2}-[0-9]{6}\.mp4$'

score() {
  ffmpeg -nostdin -loglevel error -i "$1" -an \
    -vf "fps=2,scale=160:-2,format=gray,tblend=all_mode=difference,lut=c0='if(gt(val,30),255,0)',signalstats,metadata=print:key=lavfi.signalstats.YAVG:file=-" \
    -f null - |
    awk '/pts_time/ { split($0, a, "pts_time:"); t = a[2] + 0; if (first == "") first = t }
         /YAVG/ { split($0, b, "="); printf "%.1f %.4f\n", t - first + 0.5, b[2] / 255 }'
}

scan() {
  local now dir newest name segment
  now=$(date +%s)
  for dir in "$root"/rec-*/; do
    [ -d "$dir" ] || continue
    newest=$(ls -1 "$dir" | grep -E "$segment_name" | sort | tail -n 1)
    for name in $(ls -1 "$dir" | grep -E "$segment_name" | sort); do
      segment="$dir$name"
      [ -e "$segment.motion" ] && continue
      if [ "$name" = "$newest" ] && [ $((now - $(stat -c %Y "$segment" 2>/dev/null || echo "$now"))) -lt "$closed_after" ]; then
        continue
      fi
      if score "$segment" > "$segment.motion.tmp" && [ -e "$segment" ]; then
        mv "$segment.motion.tmp" "$segment.motion"
      elif [ -e "$segment" ]; then
        # Unreadable file: an empty score file, so it is not tried again every scan.
        : > "$segment.motion"
        rm -f "$segment.motion.tmp"
      else
        # The server deleted the segment meanwhile (quota).
        rm -f "$segment.motion.tmp"
      fi
    done
  done
}

if [ "${1:-}" = "--once" ]; then
  scan
  exit 0
fi

while true; do
  scan
  sleep "$interval"
done
