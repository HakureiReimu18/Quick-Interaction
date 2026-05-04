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
  public class ShouldInterrupt
  {
    [Dependency] public static Logger Logger { get; set; }
    public static void Initialize()
    {
      Mod.Harmony.Patch(
        original: typeof(ConversationAction).GetMethod("ShouldInterrupt", AccessTools.all),
        prefix: new HarmonyMethod(typeof(ShouldInterrupt).GetMethod("ConversationAction_ShouldInterrupt_Replace"))
      );
    }

    public static bool ConversationAction_ShouldInterrupt_Replace(ConversationAction __instance, ref bool __result, bool requireTarget)
    {
      ConversationAction _ = __instance;

      IEnumerable<Entity> targets = Enumerable.Empty<Entity>();
      if (!_.TargetTag.IsEmpty && requireTarget)
      {
        targets = _.ParentEvent.GetTargets(_.TargetTag).Where(e => _.IsValidTarget(e, requireTarget));
        if (!targets.Any()) { __result = true; return false; }
      }

      if (_.Speaker != null)
      {
        // if (!_.TargetTag.IsEmpty && requireTarget && !_.IgnoreInterruptDistance)
        // {
        //   if (targets.All(t => Vector2.DistanceSquared(t.WorldPosition, _.Speaker.WorldPosition) > ConversationAction.InterruptDistance * ConversationAction.InterruptDistance)) { __result = true; return false; }
        // }
        if (_.Speaker.AIController is HumanAIController humanAI && !humanAI.AllowCampaignInteraction())
        {
          __result = true; return false;
        }
        __result = _.Speaker.Removed || _.Speaker.IsDead || _.Speaker.IsIncapacitated;
        return false;
      }

      __result = false; return false;
    }

  }
}