using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QICrabUI;
using QIDependencyInjection;

namespace QuickInteractions
{

  public partial class Mod : IAssemblyPlugin
  {
    [Singleton] public UILayer UI { get; set; }

    public void InitializeClient()
    {

    }
  }
}