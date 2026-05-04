using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace QuickInteractions
{
  public class GhostDetector
  {
    public static bool Dead { get; set; }
    public static Action<string> OnDetect;

    /// <summary>
    /// Call this in your harmony patches
    /// </summary>
    public static bool Check(
      [CallerMemberName] string memberName = "",
      [CallerFilePath] string source = "",
      [CallerLineNumber] int lineNumber = 0
    )
    {
      if (Dead)
      {
        string at = $"{CutFilePath(source)}:{lineNumber} {memberName}";

        if (ShouldReport(at)) OnDetect?.Invoke(at);
      }

      return Dead;
    }

    public static string CutFilePath(string path)
    {
      int i = path.IndexOf("CSharp");
      if (i == -1) i = path.IndexOf("SharedSource");
      if (i == -1) i = path.IndexOf("ClientSource");
      if (i == -1) i = path.IndexOf("ServerSource");

      if (i == -1) return path;
      return path.Substring(i);
    }


    public static int MinDetections { get; set; } = 0;
    public static int MaxDetections { get; set; } = 3;
    public static Dictionary<string, int> Detections = new();
    public static bool ShouldReport(string at)
    {
      if (!Detections.ContainsKey(at)) Detections[at] = 1;
      else Detections[at]++;

      return MinDetections < Detections[at] && Detections[at] <= MaxDetections;
    }

    /// <summary>
    /// Call this in IAssemblyPlugin.Initialize
    /// </summary>
    public static void Activate()
    {
      string asmName = Assembly.GetExecutingAssembly().GetName().Name;

      LuaCsSetup.Instance.Hook.Add("stop", $"{asmName} Ghost Detector", (object[] args) =>
      {
        Dead = true; return null;
      });

      OnDetect ??= (at) =>
      {
        Log($"{asmName} proximity detector has detected an invisible creature");
        Log($"At {at}", Color.Orange);
      };
    }

    public static void Log(object msg, Color? color = null)
    {
      color ??= Color.Yellow;
      LuaCsLogger.LogMessage($"{msg ?? "null"}", color * 0.8f, color);
    }
  }
}