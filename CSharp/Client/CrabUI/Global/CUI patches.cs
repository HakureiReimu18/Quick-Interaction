using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;

using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using HarmonyLib;
using EventInput;

namespace QICrabUI
{
  public partial class CUI
  {
    public static void CheckOtherPatches(string msg = "")
    {
      CUI.Log(msg);
      CUI.Log($"Harmony.GetAllPatchedMethods:", Color.Lime);
      foreach (MethodBase mb in Harmony.GetAllPatchedMethods())
      {
        Patches patches = Harmony.GetPatchInfo(mb);

        if (patches.Prefixes.Count() > 0 || patches.Postfixes.Count() > 0)
        {
          CUI.Log($"{mb.DeclaringType}.{mb.Name}:");
          if (patches.Prefixes.Count() > 0)
          {
            CUI.Log($"    Prefixes:");
            foreach (Patch patch in patches.Prefixes) { CUI.Log($"        {patch.owner}"); }
          }

          if (patches.Postfixes.Count() > 0)
          {
            CUI.Log($"    Postfixes:");
            foreach (Patch patch in patches.Postfixes) { CUI.Log($"        {patch.owner}"); }
          }
        }
      }
    }

    public static void CheckPatches(string typeName, string methodName)
    {
      CUI.Log($"Harmony.GetAllPatchedMethods:", Color.Lime);
      foreach (MethodBase mb in Harmony.GetAllPatchedMethods())
      {
        if (
          !string.Equals(typeName, mb.DeclaringType.Name, StringComparison.OrdinalIgnoreCase) ||
          !string.Equals(methodName, mb.Name, StringComparison.OrdinalIgnoreCase)
        ) continue;

        Patches patches = Harmony.GetPatchInfo(mb);

        if (patches.Prefixes.Count() > 0 || patches.Postfixes.Count() > 0)
        {
          CUI.Log($"{mb.DeclaringType}.{mb.Name}:");
          if (patches.Prefixes.Count() > 0)
          {
            CUI.Log($"    Prefixes:");
            foreach (Patch patch in patches.Prefixes) { CUI.Log($"        {patch.owner}"); }
          }

          if (patches.Postfixes.Count() > 0)
          {
            CUI.Log($"    Postfixes:");
            foreach (Patch patch in patches.Postfixes) { CUI.Log($"        {patch.owner}"); }
          }
        }
      }
    }


    private static void PatchAll()
    {
      LuaCsSetup.Instance.Hook.Add("GUI_Draw_Prefix", CUIHookID, (object[] args) =>
      {
        GUI_Draw_Prefix((SpriteBatch)args.ElementAtOrDefault(0));
        return null;
      });

      LuaCsSetup.Instance.Hook.Add("GUI_DrawCursor_Prefix", CUIHookID, (object[] args) =>
      {
        GUI_DrawCursor_Prefix((SpriteBatch)args.ElementAtOrDefault(0));
        return null;
      });

      LuaCsSetup.Instance.Hook.Add("think", CUIHookID, (object[] args) =>
      {
        CUIUpdateMouseOn();
        CUIUpdate(Timing.TotalTime);
        return null;
      });

      // this hook seems to do nothing
      // LuaCsSetup.Instance.Hook.Add("Camera_MoveCamera_Prefix", CUIHookID, (object[] args) =>
      // {
      //   return Camera_MoveCamera_Prefix(); ;
      // });

      LuaCsSetup.Instance.Hook.Add("KeyboardDispatcher_set_Subscriber_Prefix", CUIHookID, (object[] args) =>
      {
        KeyboardDispatcher_set_Subscriber_Prefix(
          (KeyboardDispatcher)args.ElementAtOrDefault(0),
          (IKeyboardSubscriber)args.ElementAtOrDefault(1)
        );
        return null;
      });

      LuaCsSetup.Instance.Hook.Add("GUI_InputBlockingMenuOpen_Postfix", CUIHookID, (object[] args) =>
      {
        return GUI_InputBlockingMenuOpen_Postfix();
      });

      LuaCsSetup.Instance.Hook.Add("GUI_TogglePauseMenu_Postfix", CUIHookID, (object[] args) =>
      {
        GUI_TogglePauseMenu_Postfix();
        return null;
      });
    }


    private static void GameMain_Update_Postfix(GameTime gameTime)
    {
      CUIUpdate(gameTime.TotalGameTime.TotalSeconds);
    }
    private static void CUIUpdate(double time)
    {
      if (Main == null) CUI.Error($"CUIUpdate: CUI.Main in {HookIdentifier} was null, tell the dev", 20);
      try
      {
        CUIAnimation.UpdateAllAnimations(time);
        CUI.Input?.Scan(time);
        TopMain?.Update(time);
        Main?.Update(time);
      }
      catch (Exception e)
      {
        CUI.Warning($"CUI: {e}");
      }
    }

    private static void GUI_Draw_Prefix(SpriteBatch spriteBatch)
    {
      try { Main?.Draw(spriteBatch); }
      catch (Exception e) { CUI.Warning($"CUI: {e}"); }
    }

    private static void GUI_DrawCursor_Prefix(SpriteBatch spriteBatch)
    {
      try { TopMain?.Draw(spriteBatch); }
      catch (Exception e) { CUI.Warning($"CUI: {e}"); }
    }

    private static void GUI_UpdateMouseOn_Postfix(ref GUIComponent __result)
    {
      CUIUpdateMouseOn();
    }

    private static void CUIUpdateMouseOn()
    {
      if (Main == null) CUI.Error($"CUIUpdateMouseOn: CUI.Main in {HookIdentifier} was null, tell the dev", 20);
      if (GUI.MouseOn == null && Main != null && Main.MouseOn != null && Main.MouseOn != Main) GUI.MouseOn = CUIComponent.dummyComponent;
      if (TopMain != null && TopMain.MouseOn != null && TopMain.MouseOn != TopMain) GUI.MouseOn = CUIComponent.dummyComponent;
    }

    private static Dictionary<string, bool> Camera_MoveCamera_Prefix()
    {
      if (GUI.MouseOn != CUIComponent.dummyComponent) return null;

      return new Dictionary<string, bool>()
      {
        ["allowZoom"] = false,
      };
    }

    private static void KeyboardDispatcher_set_Subscriber_Prefix(KeyboardDispatcher __instance, IKeyboardSubscriber value)
    {
      FocusResolver?.OnVanillaIKeyboardSubscriberSet(value);
    }

    public static bool GUI_InputBlockingMenuOpen_Postfix()
    {
      return CUI.InputBlockingMenuOpen;
    }

    public static void GUI_TogglePauseMenu_Postfix()
    {
      try
      {
        if (GUI.PauseMenu != null)
        {
          GUIFrame frame = GUI.PauseMenu;
          GUIComponent pauseMenuInner = frame.GetChild(1);
          GUIComponent list = frame.GetChild(1).GetChild(0);
          GUIButton resumeButton = (GUIButton)list.GetChild(0);

          GUIButton.OnClickedHandler oldHandler = resumeButton.OnClicked;

          resumeButton.OnClicked = (GUIButton button, object obj) =>
          {
            bool guh = oldHandler(button, obj);
            CUI.InvokeOnPauseMenuToggled();
            return guh;
          };
        }
      }
      catch (Exception e) { CUI.Warning(e); }

      CUI.InvokeOnPauseMenuToggled();
    }


  }
}