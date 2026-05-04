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
  public class PreventShopUIFromClosing
  {
    [Dependency] public static Debugger Debugger { get; set; }

    public static void Initialize()
    {
      Mod.Harmony.Patch(
        original: typeof(CampaignMode).GetMethod("NPCInteract", AccessTools.all),
        prefix: new HarmonyMethod(typeof(PreventShopUIFromClosing).GetMethod("CampaignMode_NPCInteract_Replace"))
      );
    }

    public static bool CampaignMode_NPCInteract_Replace(CampaignMode __instance, Character npc, Character interactor)
    {
      if (GhostDetector.Check()) return true;
      Debugger.Log("CampaignMode_NPCInteract_Replace", DebugLevel.PatchExecuted);
      if (!npc.AllowCustomInteract) { return false; }
      if (npc.AIController is HumanAIController humanAi && !humanAi.AllowCampaignInteraction()) { return false; }

      // not compiled on server
#if CLIENT
      __instance.NPCInteractProjSpecific(npc, interactor);
#endif

      string coroutineName = "DoCharacterWait." + (npc?.ID ?? Entity.NullEntityID);
      if (!CoroutineManager.IsCoroutineRunning(coroutineName))
      {
        CoroutineManager.StartCoroutine(DoCharacterWait_Replace(npc, interactor), coroutineName);
      }

      return false;
    }


    public static IEnumerable<CoroutineStatus> DoCharacterWait_Replace(Character npc, Character interactor)
    {
      if (npc == null || interactor == null) { yield return CoroutineStatus.Failure; }

      HumanAIController humanAI = npc.AIController as HumanAIController;
      if (humanAI == null) { yield return CoroutineStatus.Success; }

      var waitOrder = OrderPrefab.Prefabs["wait"].CreateInstance(OrderPrefab.OrderTargetType.Entity);
      humanAI.SetForcedOrder(waitOrder);
      var waitObjective = humanAI.ObjectiveManager.ForcedOrder;
      humanAI.FaceTarget(interactor);

      while (!npc.Removed && !interactor.Removed &&
          //Vector2.DistanceSquared(npc.WorldPosition, interactor.WorldPosition) < 300.0f * 300.0f &&
          humanAI.ObjectiveManager.ForcedOrder == waitObjective &&
          humanAI.AllowCampaignInteraction() &&
          !interactor.IsIncapacitated)
      {
        yield return CoroutineStatus.Running;
      }

#if CLIENT
      if(GameMain.GameSession?.GameMode is CampaignMode mode){
        mode.ShowCampaignUI = false;
      }
      
#endif
      if (!npc.Removed)
      {
        humanAI.ClearForcedOrder();
      }
      yield return CoroutineStatus.Success;
    }
  }

}