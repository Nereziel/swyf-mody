using System;
using System.IO;
using System.Threading.Tasks;
using CzechSpeechEnhanced;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    private static async Task Main()
    {
        Check(ParakeetModels.FirstChoice("Moonshine", true, "ultra", true, true) == "Moonshine", "Saved native selection changed");
        Check(ParakeetModels.FirstChoice("WhisperSmall", false, "ultra", true, true) == "ParakeetUltra", "Configured Ultra not preferred");
        Check(ParakeetModels.FirstChoice("WhisperSmall", false, "ultra", true, false) == "Parakeet", "Available TDT not selected");
        Check(ParakeetModels.FirstChoice("WhisperBase", false, "parakeet", false, true) == "ParakeetUltra", "Only Ultra ignored");
        Check(ParakeetModels.FirstChoice("WhisperBase", false, "parakeet", false, false) == "WhisperBase", "Missing models changed native setting");
        Check(ParakeetModels.EngineFor("WhisperSmall") == null, "Native recognition intercepted");
        Check(ParakeetModels.EngineFor("ParakeetUltra") == "ultra", "Wrong Ultra engine");
        string directory = Path.Combine(AppContext.BaseDirectory, "fixtures");
        Directory.CreateDirectory(directory);
        string model = Path.Combine(directory, "model.gguf");
        File.WriteAllBytes(model, new byte[] { 71, 71, 85, 70, 0, 0, 0, 0 });
        Check(ParakeetModels.ValidModel(model, 8), "Valid GGUF rejected");
        Check(!ParakeetModels.ValidModel(model, 9), "Partial model accepted");
        Check(!ParakeetModels.ValidModel(model + ".part", 8), "Nonexistent model accepted");
        File.WriteAllText(model, "BAD!xxxx");
        Check(!ParakeetModels.ValidModel(model, 8), "Invalid header accepted");
        File.WriteAllBytes(model, Array.Empty<byte>());
        Check(!ParakeetModels.ValidModel(model, 0), "Empty model accepted");
        var work = new SerialWork();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool stopped = false;
        var request = work.Run(async () => { entered.SetResult(true); await release.Task; Check(!stopped, "Process stopped during inference"); return "old-model-result"; });
        await entered.Task;
        var change = work.Run(() => { stopped = true; return Task.FromResult(0); });
        Check(!change.IsCompleted, "Switch did not wait for inference");
        release.SetResult(true);
        Check(await request == "old-model-result", "In-flight request lost");
        await change;
        Check(stopped, "Queued stop never ran");
        try { await work.Run<int>(() => throw new InvalidOperationException("Expected")); } catch (InvalidOperationException) { }
        Check(await work.Run(() => Task.FromResult(7)) == 7, "Failure leaked gate");
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool disabled = false;
        var failingRequest = work.Run(async () => { started.SetResult(true); await finish.Task; disabled = true; return 0; });
        await started.Task;
        var reset = work.AfterCurrent(() => disabled = false);
        finish.SetResult(true);
        await failingRequest;
        await reset;
        Check(!disabled, "Old request overwrote new selection reset");
        File.Delete(model);
        Console.WriteLine($"PASS: {checks} model-selection, file-validation and live-switch serialization checks.");
    }
}
