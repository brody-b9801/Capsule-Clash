using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class LobbyTelemetry
{
    [Serializable]
    public class Report
    {
        public string reason;
        public string timeUtc;
        public float sessionSeconds;
        public string gameVersion;
        public string unityVersion;
        public string platform;
        public bool userReported;
        public List<string> state;
        public List<string> events;
        public List<string> logs;
    }

    private const int MaxEvents = 200;
    private const int MaxLogs = 100;
    private const int MaxReports = 20;

    private static readonly Queue<string> events = new Queue<string>();
    private static readonly Queue<string> logs = new Queue<string>();
    private static readonly Stopwatch clock = new Stopwatch();
    private static Report lastReport;
    private static string lastReportPath;

    public static string ReportFolder => Path.Combine(Application.persistentDataPath, "LobbyReports");
    public static string LastReason => lastReport != null ? lastReport.reason : "";
    public static string LastFileName => lastReportPath != null ? Path.GetFileName(lastReportPath) : "report.json";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        lock (events)
        {
            events.Clear();
            logs.Clear();
        }
        lastReport = null;
        lastReportPath = null;
        clock.Restart();
        Application.logMessageReceivedThreaded -= OnLog;
        Application.logMessageReceivedThreaded += OnLog;
    }

    public static void Record(string message)
    {
        Add(events, MaxEvents, Stamp(message));
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        string entry = Stamp("[" + type + "] " + condition);
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            entry += "\n" + stackTrace;
        Add(logs, MaxLogs, entry);
    }

    private static string Stamp(string message)
    {
        return clock.Elapsed.TotalSeconds.ToString("F2") + " " + message;
    }

    private static void Add(Queue<string> queue, int max, string entry)
    {
        lock (events)
        {
            queue.Enqueue(entry);
            while (queue.Count > max)
                queue.Dequeue();
        }
    }

    public static void WriteReport(string reason, List<string> state)
    {
        Record("Report written: " + reason);

        Report report = new Report
        {
            reason = reason,
            timeUtc = DateTime.UtcNow.ToString("o"),
            sessionSeconds = (float)clock.Elapsed.TotalSeconds,
            gameVersion = Application.version,
            unityVersion = Application.unityVersion,
            platform = Application.platform.ToString(),
            state = state
        };
        lock (events)
        {
            report.events = new List<string>(events);
            report.logs = new List<string>(logs);
        }

        lastReport = report;
        lastReportPath = Path.Combine(ReportFolder, "disconnect-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json");
        Save();
        Prune();
    }

    public static string MarkReported()
    {
        if (lastReport == null)
            return "";

        lastReport.userReported = true;
        Save();
        return JsonUtility.ToJson(lastReport, true);
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(lastReportPath, JsonUtility.ToJson(lastReport, true));
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("Failed to save lobby report: " + e.Message);
        }
    }

    private static void Prune()
    {
        try
        {
            string[] files = Directory.GetFiles(ReportFolder, "disconnect-*.json");
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length - MaxReports; i++)
                File.Delete(files[i]);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("Failed to prune lobby reports: " + e.Message);
        }
    }
}
