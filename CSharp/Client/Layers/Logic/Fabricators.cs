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
using Barotrauma.Items.Components;

namespace QuickInteractions
{
  public partial class Fabricators
  {
    [Dependency] public FakeInput FakeInput { get; set; }

    public void SelectItem(Item item)
    {
      if (Character.Controlled == null) return;

      try
      {
        Character.Controlled.SelectedItem = item;
        if (GameMain.IsMultiplayer)
        {
          FakeInput.StartRemoteItemInteraction(item.ID);
          FakeInput.SendInteractPackage(item);
        }
      }
      finally
      {
        if (GameMain.IsMultiplayer)
        {
          FakeInput.EndRemoteItemInteraction(item.ID);
        }
      }
    }
  }
}