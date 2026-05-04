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

  [PatchClass]
  public class CustomInteractionsTracker
  {
    [Dependency] public static Debugger Debugger { get; set; }
    [Singleton] public static CustomInteractionsTracker Instance { get; set; }

    public event Action<Character> OnConversationEnded;
    public event Action<Character> OnCharacterCreated;
    public event Action<Character> OnCustomInteractSet;
    public event Action<Character> OnCharacterKilled;
    public event Action<Character> OnCharacterDespawned;

    public static void Initialize()
    {
      // Mod.Harmony.Patch(
      //   original: typeof(Character).GetConstructors(AccessTools.all)[1],
      //   postfix: new HarmonyMethod(typeof(CustomInteractionsTracker).GetMethod("Character_Constructor_Postfix"))
      // );

      LuaCsSetup.Instance.Hook.Add("character.created", Mod.Name, (object[] args) =>
      {
        Debugger.Log("character.created", DebugLevel.PatchExecuted);
        if (GhostDetector.Check()) return null;

        Character __instance = args.ElementAtOrDefault(0) as Character;
        if (!IsInFriendlyLocation()) return null;
        Instance.OnCharacterCreated?.Invoke(__instance);

        return null;
      });

      // Mod.Harmony.Patch(
      //   original: typeof(Character).GetMethod("Kill", AccessTools.all),
      //   prefix: new HarmonyMethod(typeof(CustomInteractionsTracker).GetMethod("Character_Kill_Prefix"))
      // );

      LuaCsSetup.Instance.Hook.Add("character.death", Mod.Name, (object[] args) =>
      {
        Debugger.Log("character.death", DebugLevel.PatchExecuted);
        if (GhostDetector.Check()) return null;

        Character __instance = args.ElementAtOrDefault(0) as Character;

        if (!IsInFriendlyLocation()) return null;
        Instance.OnCharacterKilled?.Invoke(__instance);

        return null;
      });

      Mod.Harmony.Patch(
        original: typeof(Character).GetMethod("Despawn", AccessTools.all),
        postfix: new HarmonyMethod(typeof(CustomInteractionsTracker).GetMethod("Character_Despawn_Postfix"))
      );

      Mod.Harmony.Patch(
        original: typeof(Character).GetMethod("Revive", AccessTools.all),
        postfix: new HarmonyMethod(typeof(CustomInteractionsTracker).GetMethod("Character_Revive_Postfix"))
      );

      // Is it inlined lol?
      // Mod.Harmony.Patch(
      //   original: typeof(Character).GetMethod("SetCustomInteract", AccessTools.all),
      //   postfix: new HarmonyMethod(typeof(CustomInteractionsTracker).GetMethod("Character_SetCustomInteract_Postfix"))
      // );

      Mod.Harmony.Patch(
        original: typeof(ConversationAction).GetMethod("ResetSpeaker", AccessTools.all),
        postfix: new HarmonyMethod(typeof(CustomInteractionsTracker).GetMethod("ConversationAction_ResetSpeaker_Postfix"))
      );

      Mod.Harmony.Patch(
        original: typeof(CampaignMode).GetMethod("AssignNPCMenuInteraction", AccessTools.all),
        postfix: new HarmonyMethod(typeof(CustomInteractionsTracker).GetMethod("CampaignMode_AssignNPCMenuInteraction_Postfix"))
      );
    }

    public static void CampaignMode_AssignNPCMenuInteraction_Postfix(Character character, CampaignMode.InteractionType interactionType)
    {
      if (GhostDetector.Check()) return;
      Debugger.Log("CampaignMode_AssignNPCMenuInteraction_Postfix", DebugLevel.PatchExecuted);
      if (!IsInFriendlyLocation()) return;
      Instance.OnCustomInteractSet?.Invoke(null);
    }

    public static void Character_Constructor_Postfix(Character __instance)
    {
      if (GhostDetector.Check()) return;
      Debugger.Log("Character_Constructor_Postfix", DebugLevel.PatchExecuted);
      if (!IsInFriendlyLocation()) return;
      Instance.OnCharacterCreated?.Invoke(__instance);
    }

    public static void Character_Kill_Prefix(Character __instance)
    {
      if (GhostDetector.Check()) return;
      Debugger.Log("Character_Kill_Prefix", DebugLevel.PatchExecuted);
      if (!IsInFriendlyLocation()) return;
      if (!__instance.IsDead) Instance.OnCharacterKilled?.Invoke(__instance);
    }

    public static void Character_Revive_Postfix(Character __instance)
    {
      if (GhostDetector.Check()) return;
      Debugger.Log("Character_Revive_Postfix", DebugLevel.PatchExecuted);
      if (!IsInFriendlyLocation()) return;
      Instance?.OnCharacterKilled?.Invoke(__instance);
    }

    public static void Character_Despawn_Postfix(Character __instance, bool createNetworkEvents = true)
    {
      if (GhostDetector.Check()) return;
      Debugger.Log("Character_Despawn_Postfix", DebugLevel.PatchExecuted);
      if (!IsInFriendlyLocation()) return;
      Instance?.OnCharacterDespawned?.Invoke(__instance);
    }

    public static void Character_SetCustomInteract_Postfix(Character __instance, Action<Character, Character> onCustomInteract, LocalizedString hudText)
    {
      if (GhostDetector.Check()) return;
      Debugger.Log("Character_SetCustomInteract_Prefix", DebugLevel.PatchExecuted);
      if (!IsInFriendlyLocation()) return;
      Instance?.OnCustomInteractSet?.Invoke(__instance);
    }

    public static void ConversationAction_ResetSpeaker_Postfix(ConversationAction __instance)
    {
      if (GhostDetector.Check()) return;
      Debugger.Log($"ConversationAction_ResetSpeaker_Postfix {__instance.Speaker}", DebugLevel.PatchExecuted);
      if (!IsInFriendlyLocation()) return;
      Instance?.OnConversationEnded?.Invoke(__instance.Speaker);
    }

    /// <summary>
    /// 判断是否在友好位置（起始哨站或信标站）
    /// </summary>
    private static bool IsInFriendlyLocation()
    {
      // 优先使用 CampaignMode 判断
      if (Utils.IsThisAnOutpost) return true;

#if CLIENT
      // 额外检查信标站
      return Utils.IsPlayerInFriendlyLocation;
#else
      return false;
#endif
    }
  }
}