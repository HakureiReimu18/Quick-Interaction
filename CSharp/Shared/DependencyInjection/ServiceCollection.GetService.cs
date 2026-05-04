using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Barotrauma;

namespace QIDependencyInjection
{

  public partial class ServiceCollection
  {
    /// <summary>
    /// Will return an object of ImplementationType that this ServiceType resolves to
    /// </summary>
    /// <typeparam name="ServiceType"></typeparam>
    /// <param name="args"> will be passed to the new instance, not singleton </param>
    /// <returns></returns>/
    public ServiceType GetService<ServiceType>(object?[]? args = null) => (ServiceType)GetService(typeof(ServiceType), args);
    public object GetService(Type ServiceType, object?[]? args = null) => GetServiceRec(ServiceType, 0, args);
    private object GetServiceRec(Type ServiceType, int depth = 0, object?[]? args = null)
    {
      if (depth > MaxRecursionDepth)
      {
        Log($"GetService {ServiceType} stuck in a loop", Color.Orange);
        return null;
      }

      object o = null;

      Type TargetType = GetTargetType(ServiceType);

      if (TargetType != null)
      {
        if (Singletons.ContainsKey(TargetType))
        {
          o = Singletons[TargetType];
        }
        else
        {
          o = Activator.CreateInstance(TargetType, args);
          InjectProperties(o, depth + 1);
        }
      }

      return o;
    }
  }

}