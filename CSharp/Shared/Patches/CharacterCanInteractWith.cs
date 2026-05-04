using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QIDependencyInjection;

using Barotrauma.Abilities;
using Barotrauma.Extensions;
using Barotrauma.IO;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using FarseerPhysics;
using FarseerPhysics.Dynamics;
using System.Diagnostics;
using System.Xml.Linq;
#if SERVER
using System.Text;
#endif
#if CLIENT
using QuickInteractions;
#endif

namespace QuickInteractions
{
  [PatchClass]
  public class CanInteractWith
  {
    [Dependency] public static Logger Logger { get; set; }
    [Dependency] public static Debugger Debugger { get; set; }
    [Dependency] public static Fabricators Fabricators { get; set; }

    public static void Initialize()
    {
      try
      {
        Mod.Harmony.Patch(
          original: typeof(Character).GetMethod("CanInteractWith", AccessTools.all, new Type[]{
            typeof(Item),
            typeof(float).MakeByRefType(),
            typeof(bool),
          }),
          postfix: new HarmonyMethod(typeof(CanInteractWith).GetMethod("Character_CanInteractWith_Postfix"))
        );

        Mod.Harmony.Patch(
          original: typeof(Character).GetMethod("CanInteractWith", AccessTools.all, new Type[]{
          typeof(Character),
          typeof(float),
          typeof(bool),
          typeof(bool),
          }),
          prefix: new HarmonyMethod(typeof(CanInteractWith).GetMethod("Character_CanInteractWith_Replace"))
        );
      }
      catch (Exception e)
      {
        Logger.Log(e);
      }
    }

    public static bool Character_CanInteractWith_Replace(Character __instance, ref bool __result, Character c, float maxDist = 200.0f, bool checkVisibility = true, bool skipDistanceCheck = false)
    {
      if (GhostDetector.Check()) return true;

      if (c == __instance || __instance.Removed || !c.Enabled || !c.CanBeSelected || c.InvisibleTimer > 0.0f)
      {
        __result = false; return false;
      }

      if (!c.CharacterHealth.UseHealthWindow && !c.IsDraggable && (c.onCustomInteract == null || !c.AllowCustomInteract))
      {
        __result = false; return false;
      }

#if CLIENT
      bool isRemoteInteraction = FakeInput.IsRemoteInteraction(c.ID);
#endif

      if (__instance.IsPlayer && (c.CampaignInteractionType != CampaignMode.InteractionType.None))
      {
        // 对于有CampaignInteractionType的角色（NPC、商人、医生等），
        // 允许从任何位置进行交互
        // 原因：
        // 1. UI层（QuickInteractionsUI）已经控制了按钮的显示
        // 2. FakeInput发送的网络包表明玩家的交互意图
        // 3. 这些角色本身就是设计用来与玩家交互的

#if CLIENT
        if (isRemoteInteraction)
        {
          if (!Utils.CanUseRemoteQuickInteractionAtCurrentLocation)
          {
            __result = false;
            return false;
          }

          Debugger.Log($"Remote interaction allowed with {c.Name} (ID: {c.ID})", DebugLevel.Networking);
          __result = true;
          return false;
        }
#endif

        // 非远程交互或服务端：仍然执行基本的位置检查
        // 但放宽条件以支持模组的便捷交互功能
        bool playerInAllowedStation = Utils.CanUseRemoteQuickInteractionAtCurrentLocation;
        bool npcInFriendlyLocation = Utils.IsSubmarineInFriendlyLocation(c.Submarine);

        bool allowInteraction = playerInAllowedStation && npcInFriendlyLocation;

        if (!allowInteraction)
        {
          __result = false;
          return false;
        }
      }

      if (__instance.IsPlayer && c.IsHuman && !c.IsOnPlayerTeam) { __result = true; return false; }

#if CLIENT
      if (isRemoteInteraction)
      {
        __result = true;
        return false;
      }
#endif

      // 对于CampaignInteractionType的角色（NPC、商人等），跳过距离检查
      // 因为UI层已经控制了显示，FakeInput负责发送网络包
      if (!skipDistanceCheck && c.CampaignInteractionType == CampaignMode.InteractionType.None)
      {
        maxDist = Math.Max(ConvertUnits.ToSimUnits(maxDist), c.AnimController.Collider.GetMaxExtent());
        if (Vector2.DistanceSquared(__instance.SimPosition, c.SimPosition) > maxDist * maxDist &&
            Vector2.DistanceSquared(__instance.SimPosition, c.AnimController.MainLimb.SimPosition) > maxDist * maxDist)
        {
          __result = false; return false;
        }
      }

      __result = !checkVisibility || __instance.CanSeeTarget(c);
      return false;
    }

    public static void Character_CanInteractWith_Postfix(Character __instance, ref bool __result, Item item)
    {
      if (GhostDetector.Check()) return;

      if (!__instance.IsPlayer) return;

#if CLIENT
      bool isRemoteItemInteraction = FakeInput.IsRemoteItemInteraction(item.ID);
#endif

      // 检查是否在潜艇编辑器中
      bool isInSubEditor = GameMain.SubEditorScreen != null && Screen.Selected == GameMain.SubEditorScreen;

      {
        // 检查设备是否有NonInteractable属性，如果有则不允许交互
        if (item.NonInteractable)
        {
          return;
        }

#if CLIENT
        if (isRemoteItemInteraction)
        {
                  if (!Utils.CanUseRemoteQuickInteractionAtCurrentLocation)
                  {
                    __result = false;
                    return;
                  }

          Debugger.Log($"Remote item interaction allowed with {item.Name} (ID: {item.ID})", DebugLevel.Networking);
          __result = true;
          return;
        }
#endif

        bool isInFriendlyLocation = Utils.IsSubmarineInFriendlyLocation(item.Submarine);

        // 在潜艇编辑器中允许交互所有设备，否则仅允许有效站点内远程交互
        if (isInSubEditor || (Utils.CanUseRemoteQuickInteractionAtCurrentLocation && isInFriendlyLocation))
        {
          __result = true;
        }
      }

      // if (item == Fabricators?.OutpostFabricator) Logger.Log($"{__instance} {item}");
      // if (item == Fabricators?.OutpostDeconstructor) Logger.Log($"{__instance} {item}");
      // if (item == Fabricators?.OutpostMedFabricator) Logger.Log($"{__instance} {item}");
    }
  }
}