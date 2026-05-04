using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QICrabUI;
using QIDependencyInjection;
using Barotrauma.Extensions;

namespace QuickInteractions
{
  /// <summary>
  /// A container for buttons that sync their state
  /// </summary>
  public class CUICompositeButton : CUIComponent
  {
    [CUISerializable] public Color DisabledColor { get; set; }
    [CUISerializable] public Color InactiveColor { get; set; }
    [CUISerializable] public Color MouseOverColor { get; set; }
    [CUISerializable] public Color MousePressedColor { get; set; }

    public Color MasterColor
    {
      set
      {
        InactiveColor = value.Multiply(0.7f);
        MouseOverColor = value.Multiply(0.9f);
        MousePressedColor = value;
        DetermineColor();
      }
    }

    public Color MasterColorOpaque
    {
      set
      {
        InactiveColor = new Color((int)(value.R * 0.7f), (int)(value.G * 0.7f), (int)(value.B * 0.7f), value.A);
        MouseOverColor = new Color((int)(value.R * 0.9f), (int)(value.G * 0.9f), (int)(value.B * 0.9f), value.A);
        MousePressedColor = value;
        DetermineColor();
      }
    }

    public List<CUIButton> Buttons = new();

    public void DetermineColor()
    {
      Color cl = Color.Transparent;
      if (Disabled)
      {
        cl = DisabledColor;
      }
      else
      {
        cl = InactiveColor;
        if (MouseOver) cl = MouseOverColor;
        if (MousePressed) cl = MousePressedColor;
      }

      foreach (CUIButton button in Buttons) button.BackgroundColor = cl;
    }

    public CUICompositeButton() : base()
    {
      ConsumeMouseClicks = true;

      OnChildAdded += (child) =>
      {
        if (child is CUIButton button)
        {
          Buttons.Add(button);
          button.AutoUpdateColor = false;
          button.ConsumeMouseClicks = false;
          OnMouseOff += (e) => DetermineColor();
          OnMouseOn += (e) => DetermineColor();
        }
      };

      DetermineColor();
    }

  }
}