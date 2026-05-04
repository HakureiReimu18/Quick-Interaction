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
namespace QuickInteractions
{
  public static partial class Utils
  {
    [Dependency] public static Logger Logger { get; set; }

    public static bool IsThisAnOutpost => GameMain.GameSession?.GameMode is CampaignMode && Level.IsLoadedFriendlyOutpost;
    public static bool IsThisAConnection => Level.Loaded is { Type: LevelData.LevelType.LocationConnection };
    public static bool RoundIsLive => GameMain.GameSession?.IsRunning ?? false;

#if CLIENT
    public static bool IsThisASinglePlayer => GameMain.IsSingleplayer;
    public static bool IsThisASinglePlayerCampaign => GameMain.GameSession?.GameMode is SinglePlayerCampaign;

    /// <summary>
    /// 判断玩家是否在友好的位置（起始哨站或信标站）
    /// </summary>
    public static bool IsPlayerInFriendlyLocation
    {
      get
      {
        if (Character.Controlled == null) return false;
        if (Level.Loaded == null) return false;

        var playerSub = Character.Controlled.Submarine;
        if (playerSub == null) return false;

        // 检查是否在起始哨站
        if (playerSub == Level.Loaded.StartOutpost) return true;

        // 检查是否在信标站
        if (playerSub == Level.Loaded.BeaconStation) return true;

        return false;
      }
    }

    /// <summary>
    /// 判断指定潜艇是否是友好位置（起始哨站或信标站）
    /// </summary>
    public static bool IsSubmarineInFriendlyLocation(Submarine sub)
    {
      if (sub == null || Level.Loaded == null) return false;

      if (sub == Level.Loaded.StartOutpost) return true;

      if (sub == Level.Loaded.BeaconStation) return true;

      return false;
    }
#endif

    public static void PrintMethodParams(MethodInfo mi)
    {
      foreach (ParameterInfo pi in mi.GetParameters())
      {
        Logger.Log(pi.ParameterType);
      }
    }

  }
}