using System.Globalization;
using Recam.Detect;

var root = Environment.GetEnvironmentVariable("RECAM_RECORDINGS_DIR") ?? "/recordings";
var model = Environment.GetEnvironmentVariable("RECAM_DETECT_MODEL") ?? "/model/yolox_tiny.onnx";
var interval = TimeSpan.FromSeconds(Setting("RECAM_DETECT_INTERVAL", 20));
var threads = Setting("RECAM_DETECT_THREADS", 1);

using var detector = new PersonDetector(model, threads);
var scanner = new SegmentScanner(root, (frame, scaled) => detector.Detect(frame, scaled.Width, scaled.Height));
Console.WriteLine($"Looking for people in {root} every {interval.TotalSeconds:0} s with {threads} thread(s).");

if (args is ["--once"])
{
    scanner.Scan();
    return;
}

while (true)
{
    var written = scanner.Scan();
    if (written > 0)
    {
        Console.WriteLine($"Looked for people in {written} segment(s).");
    }

    await Task.Delay(interval);
}

static int Setting(string name, int fallback) =>
    int.TryParse(Environment.GetEnvironmentVariable(name), CultureInfo.InvariantCulture, out var value) && value > 0 ? value : fallback;
