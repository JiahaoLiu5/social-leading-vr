using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SocialLeading.AllInOne;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs a list of steps in play mode, one step per frame, and collects checks, errors, and pictures. Used by smoke tests,
/// in batch mode and from the menu.
/// </summary>
public sealed class SmokeRunner
{
    [Serializable]
    private sealed class Entry
    {
        public string group, check, detail;
        public bool passed;
    }

    [Serializable]
    private sealed class Result
    {
        public bool passed;
        public int checks, failures;
        public float seconds;
        public List<string> errors = new List<string>();
        public List<Entry> entries = new List<Entry>();
    }

    public const float FrameSeconds = 1f / 30f;
    private const int PictureWidth = 1920, PictureHeight = 1080;

    // Messages that are normal without a headset, a microphone, or a server.
    // "Cancelling DisplayDialog": in batch mode Unity closes dialogs of packages, such as Normcore's update notice in a fresh clone, and logs it as an error.
    private static readonly string[] Allowed = { "[Licensing", "Unable to start Oculus XR Plugin", "No active XR", "XRSettings", "Cancelling DisplayDialog" };

    private static readonly Regex KeyShape = new Regex("key\\W{0,6}[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}", RegexOptions.IgnoreCase);

    private readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    private readonly Result result = new Result();
    private readonly string folder;
    private readonly float started;
    private string group = "Start";
    private int framesLeft;

    public SmokeRunner(string outputFolder, IEnumerator script)
    {
        folder = outputFolder;
        started = Time.realtimeSinceStartup;
        stack.Push(script);
        framesLeft = FramesOf(240f);
        Application.logMessageReceived += OnLog;
    }

    /// <summary>On while a test breaks something on purpose.</summary>
    public bool IgnoreErrors { get; set; }

    public bool Done { get; private set; }

    public bool Passed => result.passed;

    // For the scripts ----------------------------------------------------------------------------------------

    /// <summary>Starts a new group with its own time limit (seconds of frames).</summary>
    public void Group(string name, float limitSeconds = 240f)
    {
        group = name;
        framesLeft = FramesOf(limitSeconds);
        Debug.Log("SMOKE " + name);
    }

    public bool Check(bool passed, string check, string detail = "")
    {
        result.entries.Add(new Entry { group = group, check = check, passed = passed, detail = detail });
        if (!passed) Debug.LogWarning("SMOKE FAILED " + group + ": " + check + " " + detail);
        return passed;
    }

    public void Near(float value, float expected, float tolerance, string check) =>
        Check(Mathf.Abs(value - expected) <= tolerance, check, value.ToString("0.0000") + " expected " + expected.ToString("0.0000") + " +- " + tolerance);

    public void Near(Vector3 value, Vector3 expected, float tolerance, string check) =>
        Check((value - expected).magnitude <= tolerance, check, value.ToString("F4") + " expected " + expected.ToString("F4") + " +- " + tolerance);

    /// <summary>Waits until a condition holds, and records a failure when it does not come in time.</summary>
    public IEnumerator Until(Func<bool> condition, float seconds, string check)
    {
        for (int i = FramesOf(seconds); i > 0 && !condition(); i--) yield return null;
        Check(condition(), check, "within " + seconds + " s");
    }

    public IEnumerator Wait(float seconds)
    {
        for (int i = FramesOf(seconds); i > 0; i--) yield return null;
    }

    /// <summary>Picture of a camera. Other cameras that it depends on are rendered first.</summary>
    public void Shot(Camera camera, string name, params Camera[] before)
    {
        foreach (Camera c in before)
            if (c != null) c.Render();
        ViewCapture.Save(camera, PictureWidth, PictureHeight, Path.Combine(folder, name + ".png"));
    }

    // Running ------------------------------------------------------------------------------------------------

    /// <summary>Runs the script until it waits for the next frame.</summary>
    public void Step()
    {
        if (Done) return;
        if (--framesLeft < 0)
        {
            Check(false, "time limit of the group");
            Finish();
            return;
        }
        try
        {
            Advance();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Check(false, "exception in the test", e.Message);
            Finish();
        }
    }

    private void Advance()
    {
        while (stack.Count > 0)
        {
            IEnumerator top = stack.Peek();
            if (!top.MoveNext())
            {
                stack.Pop();
                continue;
            }
            if (top.Current is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            return;
        }
        Finish();
    }

    private void Finish()
    {
        Done = true;
        Application.logMessageReceived -= OnLog;
        result.seconds = Time.realtimeSinceStartup - started;
        result.checks = result.entries.Count;
        result.failures = result.entries.FindAll(e => !e.passed).Count + result.errors.Count;
        result.passed = result.failures == 0;
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "result.json"), JsonUtility.ToJson(result, true));
        Debug.Log("SMOKE " + (result.passed ? "PASSED" : "FAILED") + ": " + result.checks + " checks, " + result.failures + " failures");
    }

    private void OnLog(string message, string stackTrace, LogType type)
    {
        // Nothing that looks like a key may appear anywhere. The message itself is not repeated.
        if (KeyShape.IsMatch(message)) result.errors.Add("A log line looks like it contains a key.");
        if (type != LogType.Error && type != LogType.Assert && type != LogType.Exception) return;
        // An exception is never on purpose.
        if (IgnoreErrors && type != LogType.Exception) return;
        foreach (string allowed in Allowed)
            if (message.Contains(allowed)) return;
        result.errors.Add(group + ": " + message.Split('\n')[0]);
    }

    private static int FramesOf(float seconds) => Mathf.CeilToInt(seconds / FrameSeconds);
}
