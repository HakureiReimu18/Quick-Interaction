using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QIDependencyInjection;

#if CLIENT
using QICrabUI;
#endif

namespace QuickInteractions
{
  public class PatchClassAttribute : System.Attribute { }
  public partial class Mod : IAssemblyPlugin
  {
    public static string Name = "Improved Quick Interactions";
    public static Harmony Harmony = new Harmony("Improved.Quick Interactions");

    [EntryPoint] public static Mod Instance { get; set; }

    [Singleton] public Debugger Debugger { get; set; }
    [Singleton] public Logger Logger { get; set; }
    [Singleton] public LogicLayer Logic { get; set; }
    [Singleton] public Debouncer Debouncer { get; set; }

    [Dependency] public GameStageTracker GameStageTracker { get; set; }

    public ModPaths Paths { get; set; }
    public bool Disposed { get; set; }

    public event Action OnPluginLoad;
    public event Action OnPluginUnload;

    public ServiceCollection Services = new ServiceCollection() { Debug = false };

    public void SetupDependencies()
    {

    }

    public void Initialize()
    {
      Stopwatch sw1 = Stopwatch.StartNew();
      Instance = this;

      GhostDetector.Activate();
      AddCommands();

      Paths = new ModPaths(Name);
      sw1.Stop();

      Stopwatch sw2 = Stopwatch.StartNew();
#if CLIENT
      //CUI.Debug = Paths.IsInLocalMods;
      CUI.ModDir = Paths.ModDir;
      CUI.AssetsPath = Paths.AssetsFolder;
      CUI.HookIdentifier = Name;
      //CUI.UseCursedPatches = true;
      CUI.UseLua = false;
      CUI.Initialize();
#endif
      sw2.Stop();

      Stopwatch sw3 = Stopwatch.StartNew();
      SetupDependencies();
      Services.InjectEverything();
      sw3.Stop();

      Debugger.Debug = Paths.IsInLocalMods;
      //Debugger.CurrentLevel = DebugLevel.ButtonColor;

      Debugger.Log($"AddCommands took {sw1.ElapsedMilliseconds}ms", DebugLevel.Performance);
      Debugger.Log($"CUI.Initialize() took {sw2.ElapsedMilliseconds}ms", DebugLevel.Performance);
      Debugger.Log($"InjectEverything took {sw3.ElapsedMilliseconds}ms", DebugLevel.Performance);

      Stopwatch sw = Stopwatch.StartNew();
      PatchAll();
      Debugger.Log($"PatchAll took {sw.ElapsedMilliseconds}ms", DebugLevel.Performance);

#if CLIENT
      InitializeClient();
#endif

      sw.Restart();
      OnPluginLoad?.Invoke();
      if (Utils.RoundIsLive)
      {
        GameStageTracker.InvokeOnRoundStartOrInitialize();
      }
      Debugger.Log($"Hooks took {sw.ElapsedMilliseconds}ms", DebugLevel.Performance);

      Logger.Info($"{Name} initialized");
    }

    public void PatchAll()
    {
      Assembly CallingAssembly = Assembly.GetCallingAssembly();

      foreach (Type type in CallingAssembly.GetTypes())
      {
        if (Attribute.IsDefined(type, typeof(PatchClassAttribute)))
        {
          MethodInfo init = type.GetMethod("Initialize", AccessTools.all);
          if (init != null)
          {
            init.Invoke(null, new object[] { });
          }
        }
      }
    }

    public static void Log(object msg, Color? color = null)
    {
      color ??= Color.Cyan;
      LuaCsLogger.LogMessage($"{msg ?? "null"}", color * 0.8f, color);
    }

    public void OnLoadCompleted() { }
    public void PreInitPatching() { }
    public void Dispose()
    {
      OnPluginUnload?.Invoke();
#if CLIENT
      CUI.Dispose();
#endif
      RemoveCommands();
      Debouncer.Dispose();

      Mod.Harmony.UnpatchSelf();
      Disposed = true;
      //Instance = null;
    }
  }
}