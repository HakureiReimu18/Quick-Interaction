using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Barotrauma;

namespace QIDependencyInjection
{
  /// <summary>
  /// Stores type mapping and creates injected objects with GetService.  
  /// It works as both ServiceCollection and ServiceProvider from 
  /// Microsoft.Extensions.QIDependencyInjection
  /// </summary>
  public partial class ServiceCollection
  {
    /// <summary>
    /// Safety guard for potential infinite loops
    /// </summary>
    public static int MaxRecursionDepth = 10;
    /// <summary>
    /// Method with this name will be called after injecting all props
    /// </summary>
    public static string AfterInjectMethodName = "AfterInject";
    /// <summary>
    /// Method with this name will be called after injecting all static props
    /// </summary>
    public static string AfterInjectStaticMethodName = "AfterInjectStatic";
    /// <summary>
    /// If true it will log actions to console
    /// </summary>
    public bool Debug { get; set; }
    /// <summary>
    /// Single instances for types.  
    /// If target type maps to a singleton it will be used instead of a new instance
    /// </summary>
    public Dictionary<Type, object> Singletons = new();
    /// <summary>
    /// The type mapping.  
    /// If target type also in the mapping then collection will
    /// recursivelly resolve target type until it reaches the end.    
    /// If there's a loop it will warn you
    /// </summary>
    public Dictionary<Type, Type> Mapping = new();
    /// <summary>
    /// Just clears all data
    /// </summary>
    public void Clear()
    {
      Singletons.Clear();
      Mapping.Clear();
    }
    /// <summary>
    /// Returns final type that chain of mappings is pointing to
    /// </summary>
    /// <typeparam name="ServiceType"></typeparam>
    /// <returns></returns>
    public Type GetTargetType<ServiceType>() => GetTargetType(typeof(ServiceType));
    public Type GetTargetType(Type ServiceType)
    {
      if (!Mapping.ContainsKey(ServiceType)) return ServiceType;

      int depth = 0;

      Type TargetType = Mapping[ServiceType];
      if (TargetType == ServiceType) return TargetType;

      while (Mapping.ContainsKey(TargetType))
      {
        ServiceType = TargetType;
        TargetType = Mapping[ServiceType];
        if (TargetType == ServiceType) return TargetType;

        if (depth++ > MaxRecursionDepth)
        {
          Log($"ServiceCollection: There seems to be a loop in your mapping", Color.Orange);
          Log($"Can't find target type for {ServiceType}", Color.Orange);
          return null;
        }
      }

      return TargetType;
    }

    public string WrapInColor(object msg, string color) => $"‖color:{color}‖{msg}‖end‖";
    public static void Log(object msg, Color? color = null)
    {
      color ??= new Color(128, 255, 128);
      LuaCsLogger.LogMessage($"{msg ?? "null"}", color * 0.8f, color);
    }
    public void Info(object msg, Color? color = null)
    {
      if (Debug == true) Log(msg, color);
    }

    /// <summary>
    /// Will print Mapping and Singletons to console if Debug == true
    /// </summary>
    public void PrintState()
    {
      if (!Debug) return;
      Log("--------------------------------- Mapping ----------------------------------");
      foreach (var (s, i) in Mapping) Log($"{s.Name} -> {i.Name}");
      Log("--------------------------------- Singletons --------------------------------");
      foreach (var (i, o) in Singletons) Log($"{o}({o.GetHashCode()})");
      Log("------------------------------------------------------------------------------");
    }
  }

}