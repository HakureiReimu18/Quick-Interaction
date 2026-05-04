using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Barotrauma;
using HarmonyLib;

namespace QIDependencyInjection
{

  public partial class ServiceCollection
  {
    /// <summary>
    /// Will scan the object for properties with [Dependency] attribute that are mapped 
    /// to something and inject them with instances of the resolved target type  
    /// </summary>
    public void InjectProperties(object o, int depth = 0)
    {
      if (o is null) return;

      if (depth > MaxRecursionDepth)
      {
        Log($"InjectProperties {o} stuck in a loop", Color.Orange);
        return;
      }

      List<PropertyInfo> entryPoints = new();
      List<PropertyInfo> dependencies = new();

      foreach (PropertyInfo pi in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
      {
        if (Attribute.IsDefined(pi, typeof(EntryPointAttribute))) entryPoints.Add(pi);
        if (Attribute.IsDefined(pi, typeof(SingletonAttribute))) dependencies.Add(pi);
        if (Attribute.IsDefined(pi, typeof(DependencyAttribute))) dependencies.Add(pi);
      }

      foreach (PropertyInfo pi in entryPoints)
      {

        object value = pi.GetValue(o);
        if (value is null)
        {
          try
          {
            value = Activator.CreateInstance(pi.PropertyType);
          }
          catch (Exception e)
          {
            Log($"Failed to create instance of {pi.PropertyType}");
            Log($"{e.Message}");
          }
          Info($"injecting EntryPoint {pi.Name} -> {o}", new Color(255, 64, 255));
          pi.SetValue(o, value);
        }
        InjectProperties(value, depth + 1);
      }

      foreach (PropertyInfo pi in dependencies)
      {
        object service = GetServiceRec(pi.PropertyType, depth + 1);
        Info($"injecting {pi.Name} -> {o}", new Color(255, 255, 0));
        pi.SetValue(o, service);
      }


      MethodInfo afterInject = o.GetType().GetMethod(AfterInjectMethodName, AccessTools.all);
      //Log($"{o} afterInject{afterInject}");
      try
      {
        afterInject?.Invoke(o, null);
      }
      catch (Exception e)
      {
        Log($"AfterInject on {o} failed", Color.Red);
        Log($"{e.Message}", Color.Red);
      }
    }

    public void InjectEverything() => InjectEverything(Assembly.GetCallingAssembly());
    /// <summary>
    /// Will InjectEverything for all types in provided assembly,
    /// </summary>
    /// <param name="onlyIfHasStaticDependencies"> if true will ignore classes without [HasStaticDependencies] </param>
    public void InjectEverything(Assembly assembly, bool onlyIfHasStaticDependencies = false)
    {
      Stopwatch sw = new Stopwatch();
      sw.Restart();

      IEnumerable<Type> types = assembly.GetTypes().Where(T =>
      {
        if (T.DeclaringType != null)
        {
          if (Attribute.IsDefined(T.DeclaringType, typeof(DontInjectStaticAttribute)))
          {
            return false;
          }
        }

        return !Attribute.IsDefined(T, typeof(DontInjectStaticAttribute));
      });

      if (onlyIfHasStaticDependencies)
      {
        types = types.Where(T => Attribute.IsDefined(T, typeof(HasStaticDependenciesAttribute)));
      }

      InjectEverything(types.ToArray());

      sw.Stop();
      Info($"InjectEverything for assembly [{assembly.GetName().Name}] took {sw.ElapsedMilliseconds}ms");
      if (Debug) PrintState();
    }
    public void InjectEverything(Type type) => InjectEverything(new Type[] { type });
    public void InjectEverything(Type[] types)
    {
      FindAndCreateAllSingletons(types);
      InjectStaticProperties(types);
    }

    public void InjectStaticProperties(Type[] types)
    {
      foreach (Type T in types)
      {
        List<PropertyInfo> entryPoints = new();
        List<PropertyInfo> dependencies = new();

        foreach (PropertyInfo pi in T.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
          if (Attribute.IsDefined(pi, typeof(EntryPointAttribute))) entryPoints.Add(pi);
          if (Attribute.IsDefined(pi, typeof(SingletonAttribute))) dependencies.Add(pi);
          if (Attribute.IsDefined(pi, typeof(DependencyAttribute))) dependencies.Add(pi);
        }

        foreach (PropertyInfo pi in entryPoints)
        {
          Info($"injecting static EntryPoint {pi.Name} -> {pi.DeclaringType.Name}", new Color(0, 255, 255));
          object value = pi.GetValue(null);
          if (value is null)
          {
            try
            {
              value = Activator.CreateInstance(pi.PropertyType);
            }
            catch (Exception e)
            {
              Log($"Failed to create instance of {pi.PropertyType}");
              Log($"{e.Message}");
            }
            pi.SetValue(null, value);
          }
          InjectProperties(value);
        }

        foreach (PropertyInfo pi in dependencies)
        {
          Info($"injecting static dependency {pi.Name} -> {pi.DeclaringType.Name}", new Color(0, 255, 255));
          pi.SetValue(null, GetService(pi.PropertyType));
        }


        MethodInfo afterInject = T.GetMethod(AfterInjectStaticMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        try
        {
          afterInject?.Invoke(null, null);
        }
        catch (Exception e)
        {
          Log($"{AfterInjectStaticMethodName} on {T} failed", Color.Red);
          Log($"{e.Message}", Color.Red);
        }
      }


    }

    public void FindAndCreateAllSingletons(Type[] types)
    {
      foreach (Type T in types)
      {
        if (Attribute.IsDefined(T, typeof(SingletonAttribute))) CreateSingleton(T);

        foreach (PropertyInfo pi in T.GetProperties(AccessTools.all))
        {
          if (Attribute.IsDefined(pi, typeof(SingletonAttribute)))
          {
            CreateSingleton(pi.PropertyType);
          }
        }
      }

      foreach (var (T, singleton) in Singletons)
      {
        InjectProperties(singleton);
      }
    }

  }


}