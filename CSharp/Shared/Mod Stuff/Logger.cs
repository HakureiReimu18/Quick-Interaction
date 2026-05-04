using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QIDependencyInjection;
using System.Runtime.CompilerServices;

namespace QuickInteractions
{
  // I think is is unnecessary
  // All this stuff is inherently static, why is it injected instance?
  // And debugger probably also should be static
  // Lets say i was just testing dependency injection system
  [Singleton]
  public class Logger
  {
    [QIDependencyInjection.Dependency] public Debugger Debugger { get; set; }
    public Color BaseColor { get; set; } = Color.Cyan;

    public string WrapInColor(object msg, string color) => $"‖color:{color}‖{msg}‖end‖";
    public string GetCallerFolderPath([CallerFilePath] string source = "") => Path.GetDirectoryName(source);
    public void Log(object msg, Color? color = null)
    {
      color ??= BaseColor;
      LuaCsLogger.LogMessage($"{msg ?? "null"}", color * 0.8f, color);
    }

    public void Warning(object msg) => Log(msg, Color.Yellow);

    public void Info(object msg, [CallerFilePath] string source = "", [CallerLineNumber] int lineNumber = 0)
    {
      if (Debugger?.Debug == true) Log(msg);
    }

    public void Error(object msg, [CallerFilePath] string source = "", [CallerLineNumber] int lineNumber = 0)
    {
      if (Debugger?.Debug == true)
      {
        var fi = new FileInfo(source);

        Log($"{fi.Directory.Name}/{fi.Name}:{lineNumber}", Color.Orange * 0.5f);
        Log(msg, Color.Orange);
      }
    }
  }
}