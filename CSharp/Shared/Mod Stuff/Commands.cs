using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.IO;

using Barotrauma;
using Microsoft.Xna.Framework;
using HarmonyLib;

namespace QuickInteractions
{
  public partial class Mod
  {
    public List<DebugConsole.Command> AddedCommands = new List<DebugConsole.Command>();

    public void AddCommands()
    {
      AddedCommands.Add(new DebugConsole.Command("qi_setdebuglevel", "", (string[] args) =>
      {
        if (args.Length == 0) return;
        if (Mod.Instance == null) return;

        if (int.TryParse(args[0], out int level))
        {
          Mod.Instance.Debugger.CurrentLevel = (DebugLevel)level;
          Mod.Log((DebugLevel)level);
        }
      }));

      AddedCommands.Add(new DebugConsole.Command("qi_debug", "", (string[] args) =>
      {
        if (Mod.Instance == null) return;

        Mod.Instance.Debugger.Debug = !Mod.Instance.Debugger.Debug;
        Mod.Log($"Quick interactions Debug = {Mod.Instance.Debugger.Debug}");
      }));

      // AddedCommands.Add(new DebugConsole.Command("qi_simulate_round_start", "", (string[] args) =>
      // {
      //   if (Mod.Instance == null) return;
      //   GameStageTracker.Instance?.InvokeOnRoundStartOrInitialize();
      // }));

      DebugConsole.Commands.InsertRange(0, AddedCommands);
    }

    public void RemoveCommands()
    {
      AddedCommands.ForEach(c => DebugConsole.Commands.Remove(c));
      AddedCommands.Clear();
    }
  }
}