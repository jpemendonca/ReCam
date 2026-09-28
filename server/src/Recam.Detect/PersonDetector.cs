using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Recam.Detect;

/// <summary>Runs the YOLOX model on one frame, already letterboxed by FFmpeg (<see cref="SegmentFrames"/>).</summary>
public sealed class PersonDetector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;

    public PersonDetector(string modelPath, int threads)
    {
        // ReCam sends nothing to anyone (AGENTS.md); the container has no network either.
        // Warnings only: the model and FFmpeg are the only inputs, and they are fixed.
        var environment = new EnvironmentCreationOptions { logLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR };
        OrtEnv.CreateInstanceWithOptions(ref environment);
        OrtEnv.Instance().DisableTelemetryEvents();
        using var options = new SessionOptions { IntraOpNumThreads = threads, InterOpNumThreads = 1 };
        _session = new InferenceSession(modelPath, options);
        _inputName = _session.InputMetadata.Keys.Single();
    }

    /// <param name="bgr">A square frame of <see cref="YoloxDecoder.InputSize"/> pixels, 3 bytes each, blue first.</param>
    public IReadOnlyList<PersonBox> Detect(ReadOnlySpan<byte> bgr, int scaledWidth, int scaledHeight)
    {
        const int size = YoloxDecoder.InputSize;
        var input = new DenseTensor<float>([1, 3, size, size]);
        var planar = input.Buffer.Span;
        for (var pixel = 0; pixel < size * size; pixel++)
        {
            // YOLOX takes raw 0-255 values, channels as planes, in the order OpenCV reads them (BGR).
            planar[pixel] = bgr[pixel * 3];
            planar[(size * size) + pixel] = bgr[(pixel * 3) + 1];
            planar[(2 * size * size) + pixel] = bgr[(pixel * 3) + 2];
        }

        using var results = _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, input)]);
        var output = results[0].AsTensor<float>();
        var values = output is DenseTensor<float> dense ? dense.Buffer.Span : output.ToArray();
        return YoloxDecoder.Decode(values, scaledWidth, scaledHeight);
    }

    public void Dispose() => _session.Dispose();
}
