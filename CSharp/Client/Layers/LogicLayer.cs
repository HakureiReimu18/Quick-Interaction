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

namespace QuickInteractions
{
  public partial class LogicLayer
  {
    [Singleton] public QuickTalk QuickTalk { get; set; }
  }
}