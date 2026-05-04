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
          // 客户端远程交互：记录日志并直接允许
          Debugger.Log($"Remote interaction allowed with {c.Name} (ID: {c.ID})", DebugLevel.Networking);
          __result = true;
          return false;
        }
#endif

        // 非远程交互或服务端：仍然执行基本的位置检查
        // 但放宽条件以支持模组的便捷交互功能
        bool playerInOutpost = Level.Loaded != null && Level.Loaded.StartOutpost != null && __instance.Submarine == Level.Loaded.StartOutpost;
        bool playerInPlayerSub = __instance.Submarine != null && __instance.Submarine.TeamID == CharacterTeamType.Team1;

        bool npcOnPlayerSub = c.Submarine != null && c.Submarine.TeamID == CharacterTeamType.Team1;
        bool npcInOutpost = Level.Loaded != null && Level.Loaded.StartOutpost != null && c.Submarine == Level.Loaded.StartOutpost;

        // 放宽的允许条件：
        // 1. 玩家在哨站（可以与哨站NPC交互）
        // 2. NPC在玩家潜艇上（可以在潜艇上与船员交互）
        // 3. 玩家和NPC在同一位置（原始逻辑）
        bool allowInteraction = playerInOutpost ||  // 玩家在哨站
                              npcOnPlayerSub ||     // NPC在玩家潜艇
                              (playerInPlayerSub && npcOnPlayerSub) ||  // 都在潜艇
                              (playerInOutpost && npcInOutpost);       // 都在哨站

        if (!allowInteraction)
        {
          // 最后的兜底：如果是通过快速交互按钮触发的，也允许
          // 因为UI层已经验证过该角色应该显示
          __result = true;  // 允许交互
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

      // 检查物品是否为加工台、解构仪、医疗加工台、深潜通用加工台、矿石精炼机、弹药重装机、深潜钢板修复台、弹药制造机、TSM加工台、SCP加工台或EK Utility加工台，并且位于哨站或玩家潜艇内
      if (item?.Prefab.Identifier != null && item.Submarine != null &&
          (item.Prefab.Identifier.Value == "fabricator" ||
           item.Prefab.Identifier.Value == "deconstructor" ||
           item.Prefab.Identifier.Value == "medicalfabricator" ||
           item.Prefab.Identifier.Value == "deep_general_fabricator" ||
           item.Prefab.Identifier.Value == "ore_refining_machine" ||
           item.Prefab.Identifier.Value == "ammoreload_machine" ||
           item.Prefab.Identifier.Value == "deep_plate_repairtable" ||
           item.Prefab.Identifier.Value == "tsm_fabricator_ammo" ||
           item.Prefab.Identifier.Value == "tsm_fabricator" ||
           item.Prefab.Identifier.Value == "tsm_fabricator_quality" ||
           item.Prefab.Identifier.Value == "tsm_fabricator_skin" ||
           item.Prefab.Identifier.Value == "scp_portableammofabricator" ||
           item.Prefab.Identifier.Value == "scp_portableweaponfabricator" ||
           item.Prefab.Identifier.Value == "scp_advportableweaponfabricator" ||
           item.Prefab.Identifier.Value == "scp_chemistrystation" ||
           item.Prefab.Identifier.Value == "scp_ammofabricator" ||
           item.Prefab.Identifier.Value == "scp_weaponfabricator" ||
           item.Prefab.Identifier.Value == "ekutility_placeablefabricator" ||
           item.Prefab.Identifier.Value == "ekutility_placeablefabricator_adaptive" ||
           item.Prefab.Identifier.Value == "ekutility_placeablemedicalfabricator" ||
           item.Prefab.Identifier.Value == "ekutility_placeabledeconstructor"))
      {
        // 检查设备是否有NonInteractable属性，如果有则不允许交互
        if (item.NonInteractable)
        {
          return;
        }

#if CLIENT
        if (isRemoteItemInteraction)
        {
          Debugger.Log($"Remote item interaction allowed with {item.Name} (ID: {item.ID})", DebugLevel.Networking);
          __result = true;
          return;
        }
#endif

        // 检查设备是否在哨站
        bool isInOutpost = Level.Loaded != null && Level.Loaded.StartOutpost != null && item.Submarine == Level.Loaded.StartOutpost;
        // 检查设备是否在玩家潜艇
        bool isInPlayerSub = item.Submarine.TeamID == CharacterTeamType.Team1;
        // 检查玩家是否在玩家潜艇内
        bool playerInPlayerSub = __instance.Submarine != null && __instance.Submarine.TeamID == CharacterTeamType.Team1;
        // 检查玩家是否在哨站内
        bool playerInOutpost = Level.Loaded != null && Level.Loaded.StartOutpost != null && __instance.Submarine == Level.Loaded.StartOutpost;

        // 在潜艇编辑器中允许交互所有设备，否则按正常规则显示
        if (isInSubEditor || (isInPlayerSub && playerInPlayerSub) || (isInOutpost && playerInOutpost && !playerInPlayerSub))
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