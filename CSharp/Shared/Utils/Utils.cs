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


    public static bool IsCurrentOutpostAbandoned
    {
      get
      {
        var level = Level.Loaded;
        if (level == null) return false;

        object outpostInfo = level.GetType().GetProperty("StartOutpostInfo", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(level);
        if (outpostInfo != null)
        {
          object abandoned = outpostInfo.GetType().GetProperty("IsAbandoned", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(outpostInfo);
          if (abandoned is bool b) return b;
        }

        object startLocation = level.GetType().GetProperty("StartLocation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(level);
        if (startLocation != null)
        {
          object abandoned = startLocation.GetType().GetProperty("IsAbandoned", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(startLocation);
          if (abandoned is bool b) return b;
        }

        return false;
      }
    }

    public static float? CurrentOutpostReputationPercent
    {
      get
      {
        try
        {
          object campaign = GameMain.GameSession?.GameMode;
          if (campaign == null) return null;

          object map = campaign.GetType().GetProperty("Map", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(campaign);
          object location = map?.GetType().GetProperty("CurrentLocation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(map)
                           ?? Level.Loaded?.GetType().GetProperty("StartLocation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(Level.Loaded);
          if (location == null) return null;

          var getRep = campaign.GetType().GetMethod("GetReputation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { location.GetType() }, null);
          object reputation = getRep?.Invoke(campaign, new[] { location });
          if (reputation == null) return null;

          object normalized = reputation.GetType().GetProperty("NormalizedValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(reputation);
          if (normalized is float f) return f * 100f;
          if (normalized is double d) return (float)(d * 100.0);

          object value = reputation.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(reputation);
          if (value is float f2) return f2;
          if (value is double d2) return (float)d2;
        }
        catch { }

        return null;
      }
    }

    public static bool CanUseRemoteQuickInteractionAtCurrentLocation
    {
      get
      {
        if (!IsPlayerInFriendlyLocation) return false;
        if (IsCurrentOutpostAbandoned) return false;

        float? reputation = CurrentOutpostReputationPercent;
        if (reputation.HasValue && reputation.Value < -60f) return false;

        return true;
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