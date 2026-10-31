"""Local-only Canary transcription helper; the game supplies PCM WAV audio."""
import argparse
import io
import json
import os
import sys
import wave
from http.server import BaseHTTPRequestHandler, HTTPServer

# Windows loads CUDA provider dependencies using PATH.
site = os.path.join(os.path.dirname(sys.executable), "Lib", "site-packages")
cuda_dirs = [os.path.join(site, "nvidia", "cu13", "bin", "x86_64"),
             os.path.join(site, "nvidia", "cudnn", "bin")]
os.environ["PATH"] = os.pathsep.join(cuda_dirs + [os.environ.get("PATH", "")])
import numpy as np
import onnx_asr
import onnxruntime as ort

parser = argparse.ArgumentParser()
parser.add_argument("--port", type=int, required=True)
parser.add_argument("--model", required=True)
parser.add_argument("--language", default="cs")
parser.add_argument("--device", choices=("auto", "cpu", "gpu"), default="auto")
args = parser.parse_args()
os.environ["HF_HUB_OFFLINE"] = "1"
options = ort.SessionOptions()
options.intra_op_num_threads = max(1, min(6, (os.cpu_count() or 2) // 2))
def load(providers):
    return onnx_asr.load_model("nemo-canary-1b-v2", path=args.model, quantization="int8", providers=providers, sess_options=options)


device = "cpu"
fallback = ""
if args.device != "cpu" and "CUDAExecutionProvider" in ort.get_available_providers():
    try:
        model = load(["CUDAExecutionProvider", "CPUExecutionProvider"])
        if "CUDAExecutionProvider" not in model.asr._encoder.get_providers():
            raise RuntimeError("CUDA provider did not initialize")
        device = "cuda"
    except Exception as exc:
        fallback = str(exc)
        model = load(["CPUExecutionProvider"])
else:
    if args.device == "gpu":
        fallback = "CUDA provider unavailable"
    model = load(["CPUExecutionProvider"])


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def respond(self, status, payload):
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        self.respond(200 if self.path == "/health" else 404, {"status": "ok", "device": device, "fallback": fallback})

    def do_POST(self):
        global model, device, fallback
        if self.path != "/transcribe":
            self.respond(404, {"error": "unknown endpoint"})
            return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if length <= 0 or length > 16 * 1024 * 1024:
                raise ValueError("invalid audio length")
            with wave.open(io.BytesIO(self.rfile.read(length)), "rb") as wav:
                if wav.getnchannels() != 1 or wav.getsampwidth() != 2:
                    raise ValueError("expected mono 16-bit PCM")
                rate = wav.getframerate()
                samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype="<i2").astype(np.float32) / 32768.0
            try:
                text = model.recognize(samples, sample_rate=rate, language=args.language, target_language=args.language)
            except Exception as exc:
                if device != "cuda":
                    raise
                fallback = str(exc)
                device = "cpu"
                model = load(["CPUExecutionProvider"])
                text = model.recognize(samples, sample_rate=rate, language=args.language, target_language=args.language)
            self.respond(200, {"text": text, "device": device, "fallback": fallback})
        except Exception as exc:
            self.respond(500, {"error": str(exc)})


HTTPServer(("127.0.0.1", args.port), Handler).serve_forever()
