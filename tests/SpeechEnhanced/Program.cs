using System;
using CzechSpeechEnhanced;

static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
}

var original = new float[320];
Array.Fill(original, 0.01f);
var snapshot = (float[])original.Clone();

var prepared = Speech.PrepareForParakeet(original, normalize: true);
Check(!ReferenceEquals(original, prepared), "Normalization must operate on a copy.");
Check(original.AsSpan().SequenceEqual(snapshot), "Original samples changed; Whisper fallback would receive modified audio.");
Check(Math.Abs(prepared[0] - 0.1f) < 0.0001f, "Prepared samples were not normalized as expected.");

var unchanged = Speech.PrepareForParakeet(original, normalize: false);
Check(ReferenceEquals(original, unchanged), "Disabled normalization should not allocate a copy.");

Console.WriteLine("PASS: speech audio preparation preserves fallback samples.");
