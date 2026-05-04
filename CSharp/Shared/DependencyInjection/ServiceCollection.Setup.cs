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
    /// Will create mapping to itself.  
    /// Usefull if you have object that doesn't implement any interface but has 
    /// dependencies and should be injected
    /// </summary>
    public ServiceCollection Map<ImplementationType>() => Map<ImplementationType, ImplementationType>();
    /// <summary>
    /// Maps one type to another.  
    /// GetService for ServiceType will return an object of ImplementationType.  
    /// If ImplementationType is also in the mapping final type in the chain will be used as target type
    /// </summary>
    public ServiceCollection Map<ServiceType, ImplementationType>() => Map(typeof(ServiceType), typeof(ImplementationType));
    public ServiceCollection Map(Type ImplementationType) => Map(ImplementationType, ImplementationType);
    public ServiceCollection Map(Type ServiceType, Type ImplementationType)
    {
      Mapping[ServiceType] = ImplementationType;
      return this;
    }
    /// <summary>
    /// Will create a single object for that target type and map it to itself
    /// if ServiceType is pointing to a type that has a singleton, that singleton will be returned 
    /// instead of a new instance  
    /// If T is a ServiceType, singleton will be created for resolved target type
    /// </summary>
    /// <typeparam name="T"> ImplementationType or ServiceType that is already mapped </typeparam>
    /// <param name="args"> for TargetType constructor </param>
    public T CreateSingleton<T>(object?[]? args = null) => (T)CreateSingleton(typeof(T), args);
    public object CreateSingleton(Type T, object?[]? args = null)
    {
      Type TargetType = GetTargetType(T);

      if (!Singletons.ContainsKey(TargetType))
      {
        Info($"creating a singleton for {TargetType}", new Color(255, 64, 255));
        Singletons[TargetType] = Activator.CreateInstance(TargetType, args);
        Map(TargetType);
      }

      return Singletons[TargetType];
    }
  }

}