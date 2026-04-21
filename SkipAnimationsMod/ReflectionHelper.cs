using System;
using System.Reflection;
using HarmonyLib;

namespace SkipAnimationsMod
{
    internal static class ReflectionHelper
    {
        public static void InvokeHidden(object instance, string methodName, params object[] args)
        {
            if (instance == null)
            {
                return;
            }

            MethodInfo method = AccessTools.Method(instance.GetType(), methodName);
            if (method == null)
            {
                Plugin.Log?.LogWarning(
                    $"[SkipAnimations] Method not found: {instance.GetType().Name}.{methodName}"
                );
                return;
            }

            try
            {
                method.Invoke(instance, args);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError(
                    $"[SkipAnimations] Failed invoking {instance.GetType().Name}.{methodName}: {ex}"
                );
            }
        }

        public static T GetHiddenField<T>(object instance, string fieldName)
            where T : class
        {
            if (instance == null)
            {
                return null;
            }

            FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);
            if (field == null)
            {
                return null;
            }

            return field.GetValue(instance) as T;
        }
    }
}
