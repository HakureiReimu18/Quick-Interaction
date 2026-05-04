using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QIDependencyInjection;

namespace QuickInteractions
{
  [Singleton]
  public class Debouncer : IDisposable
  {
    private ILuaCsTimer Timer => LuaCsSetup.Instance.Timer;
    [Dependency] public Logger Logger { get; set; }

    private Dictionary<string, bool> Scheduled = new();
    private object lockObj = new();

    public void Debounce(string name, int millisecondDelay, Action action)
    {
      lock (lockObj)
      {
        if (Scheduled.ContainsKey(name))
        {
          Scheduled[name] = false;
        }

        string currentName = name;
        bool isCurrent = true;

        Action wrappedAction = () =>
        {
          lock (lockObj)
          {
            if (!Scheduled.TryGetValue(currentName, out var valid) || !valid)
            {
              Scheduled.Remove(currentName);
              return;
            }
            Scheduled.Remove(currentName);
          }

          try
          {
            action();
          }
          catch (Exception e)
          {
            Logger?.Log(e);
          }
        };

        Scheduled[name] = true;
        Timer.Wait((object[] args) => wrappedAction(), millisecondDelay);
      }
    }

    public void Dispose()
    {
      lock (lockObj)
      {
        Scheduled.Clear();
      }
    }
  }
}
