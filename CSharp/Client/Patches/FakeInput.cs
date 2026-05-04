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
  public struct InteractPackage
  {
    public Character.InputNetFlags ForsedKeys;
    public ushort EntityID;
    public InteractPackage(ushort id, Character.InputNetFlags keys) => (EntityID, ForsedKeys) = (id, keys);
  }

  [PatchClass]
  public class FakeInput
  {
    [Singleton] public static FakeInput Instance { get; set; }
    [Dependency] public static Logger Logger { get; set; }
    [Dependency] public static Debugger Debugger { get; set; }

    public Queue<InteractPackage> ScheduledPackages = new();

    private static HashSet<ushort> remoteInteractionTargets = new();
    private static HashSet<ushort> remoteItemInteractionTargets = new();
    public static bool IsRemoteInteraction(ushort targetID) => remoteInteractionTargets.Contains(targetID);
    public static bool IsRemoteItemInteraction(ushort targetID) => remoteItemInteractionTargets.Contains(targetID);

    public void SendInteractPackage(Character character)
    {
      ScheduledPackages.Enqueue(new InteractPackage(character.ID, Character.InputNetFlags.Use));
    }

    public void SendInteractPackage(Item item)
    {
      ScheduledPackages.Enqueue(new InteractPackage(item.ID, Character.InputNetFlags.Select));
    }

    public static void StartRemoteInteraction(ushort characterID)
    {
      remoteInteractionTargets.Add(characterID);
      Debugger.Log($"Started remote interaction with character {characterID}", DebugLevel.Networking);
    }

    public static void EndRemoteInteraction(ushort characterID)
    {
      remoteInteractionTargets.Remove(characterID);
      Debugger.Log($"Ended remote interaction with character {characterID}", DebugLevel.Networking);
    }

    public static void StartRemoteItemInteraction(ushort itemID)
    {
      remoteItemInteractionTargets.Add(itemID);
      Debugger.Log($"Started remote item interaction with item {itemID}", DebugLevel.Networking);
    }

    public static void EndRemoteItemInteraction(ushort itemID)
    {
      remoteItemInteractionTargets.Remove(itemID);
      Debugger.Log($"Ended remote item interaction with item {itemID}", DebugLevel.Networking);
    }

    public static void Initialize()
    {
      // Mod.Harmony.Patch(
      //   original: typeof(Character).GetMethod("IsKeyDown", AccessTools.all),
      //   postfix: new HarmonyMethod(typeof(FakeInput).GetMethod("Character_IsKeyDown_Postfix"))
      // );

      Mod.Harmony.Patch(
        original: typeof(Character).GetMethod("UpdateNetInput", AccessTools.all),
        postfix: new HarmonyMethod(typeof(FakeInput).GetMethod("Character_UpdateNetInput_Postfix"))
      );
    }

    public static void Character_UpdateNetInput_Postfix(Character __instance)
    {
      if (GhostDetector.Check()) return;
      if (Instance == null) return;
      if (__instance != Character.Controlled) return;

      if (Instance.ScheduledPackages.Count > 0)
      {
        InteractPackage fakePackage = Instance.ScheduledPackages.Dequeue();

        Debugger.Log($"sending a fake package {fakePackage.EntityID} {fakePackage.ForsedKeys}", DebugLevel.Networking);

        __instance.memInput[0] = new Character.NetInputMem()
        {
          states = __instance.memInput[0].states | fakePackage.ForsedKeys,
          intAim = __instance.memInput[0].intAim,
          interact = fakePackage.EntityID,
        };
      }
    }


  }
}