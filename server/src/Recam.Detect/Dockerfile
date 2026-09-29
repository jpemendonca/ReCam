# Optional person detection worker (SPECS.md 2.6). Build context: server/.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/Recam.Detect/Recam.Detect.csproj src/Recam.Detect/
RUN dotnet restore src/Recam.Detect/Recam.Detect.csproj -r linux-x64
COPY src/Recam.Detect/ src/Recam.Detect/
RUN dotnet publish src/Recam.Detect/Recam.Detect.csproj -c Release -r linux-x64 --self-contained -o /app --no-restore

# YOLOX-Tiny from the authors' release (Apache-2.0), fetched at build time only, checked by hash.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS model
ADD --checksum=sha256:427cc366d34e27ff7a03e2899b5e3671425c262ea2291f88bb942bc1cc70b0f7 \
    https://github.com/Megvii-BaseDetection/YOLOX/releases/download/0.1.1rc0/yolox_tiny.onnx /model/yolox_tiny.onnx

# The same FFmpeg image as the motion service, so it is already on the machine.
FROM linuxserver/ffmpeg:9.0-cli-ls83
COPY --from=build /app /app
COPY --from=model /model /model
RUN chmod 0644 /model/yolox_tiny.onnx
USER 1654:1654
ENTRYPOINT ["/app/Recam.Detect"]
