using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MultigridProjectorClient.Utilities
{
    public static class Reflection
    {
        public static object GetValue(object instance, string typeName)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            if (fieldInfo == null)
                return null;

            return fieldInfo.GetValue(instance);
        }

        public static object GetValue(Type targetClass, string typeName)
        {
            FieldInfo fieldInfo = targetClass.GetField(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);

            if (fieldInfo == null)
                return null;

            return fieldInfo.GetValue(null);
        }

        public static object GetValue(Type targetClass, object instance, string typeName)
        {
            FieldInfo fieldInfo = targetClass.GetField(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            if (fieldInfo == null)
                return null;

            return fieldInfo.GetValue(instance);
        }

        public static bool SetValue(object instance, string typeName, object value)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            if (fieldInfo == null)
                return false;

            fieldInfo.SetValue(instance, value);

            return true;
        }

        public static bool SetValue(Type targetClass, string typeName, object value)
        {
            FieldInfo fieldInfo = targetClass.GetField(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);

            if (fieldInfo == null)
                return false;

            fieldInfo.SetValue(null, value);

            return true;
        }

        public static bool SetValue(Type targetClass, object instance, string typeName, object value)
        {
            FieldInfo fieldInfo = targetClass.GetField(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            if (fieldInfo == null)
                return false;

            fieldInfo.SetValue(instance, value);

            return true;
        }

        public static Delegate GetMethod(object instance, string methodName, Type[] overload = null)
        {
            Type instanceType = instance.GetType();
            MethodInfo methodInfo;

            if (overload != null)
            {
                methodInfo = instanceType.GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null, overload, null);
            }
            else
            {
                methodInfo = instanceType.GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
            }

            if (methodInfo == null)
                return null;

            // Reconstruct the type
            Type[] methodArgs = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            Type type = Expression.GetDelegateType(methodArgs.Concat(new[] { methodInfo.ReturnType }).ToArray());

            // Create a delegate
            return Delegate.CreateDelegate(type, instance, methodInfo);
        }

        public static Delegate GetMethod(Type targetClass, string methodName, Type[] overload = null)
        {
            MethodInfo methodInfo;

            if (overload != null)
            {
                methodInfo = targetClass.GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static,
                    null, overload, null);
            }
            else
            {
                methodInfo = targetClass.GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static);
            }

            if (methodInfo == null)
                return null;

            // Reconstruct the type
            Type[] methodArgs = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            Type type = Expression.GetDelegateType(methodArgs.Concat(new[] { methodInfo.ReturnType }).ToArray());

            // Create a delegate
            return Delegate.CreateDelegate(type, null, methodInfo);
        }

        public static Delegate GetMethod(Type targetClass, object instance, string methodName, Type[] overload = null)
        {
            MethodInfo methodInfo;

            if (overload != null)
            {
                methodInfo = targetClass.GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null, overload, null);
            }
            else
            {
                methodInfo = targetClass.GetMethod(
                    methodName,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
            }

            if (methodInfo == null)
                return null;

            // Reconstruct the type
            Type[] methodArgs = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            Type type = Expression.GetDelegateType(methodArgs.Concat(new[] { methodInfo.ReturnType }).ToArray());

            // Create a delegate
            return Delegate.CreateDelegate(type, instance, methodInfo);
        }

        public static Delegate GetGenericMethod(object instance, Func<MethodInfo, bool> predicate, Type[] inputTypes)
        {
            MethodInfo genericMethodInfo = instance.GetType().GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance).FirstOrDefault(predicate);

            if (genericMethodInfo == null)
                return null;

            // Convert the genetic method 
            MethodInfo methodInfo = genericMethodInfo.MakeGenericMethod(inputTypes);

            // Reconstruct the type
            Type[] methodArgs = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            Type type = Expression.GetDelegateType(methodArgs.Concat(new[] { methodInfo.ReturnType }).ToArray());

            // Create a delegate
            return Delegate.CreateDelegate(type, instance, methodInfo);
        }

        public static Delegate GetGenericMethod(Type targetClass, Func<MethodInfo, bool> predicate, Type[] inputTypes)
        {
            MethodInfo genericMethodInfo = targetClass.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static).FirstOrDefault(predicate);

            if (genericMethodInfo == null)
                return null;

            // Convert the genetic method 
            MethodInfo methodInfo = genericMethodInfo.MakeGenericMethod(inputTypes);

            // Reconstruct the type
            Type[] methodArgs = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            Type type = Expression.GetDelegateType(methodArgs.Concat(new[] { methodInfo.ReturnType }).ToArray());

            // Create a delegate
            return Delegate.CreateDelegate(type, null, methodInfo);
        }

        public static Delegate GetGenericMethod(Type targetClass, object instance, Func<MethodInfo, bool> predicate, Type[] inputTypes)
        {
            MethodInfo genericMethodInfo = targetClass.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance).FirstOrDefault(predicate);

            if (genericMethodInfo == null)
                return null;

            // Convert the genetic method 
            MethodInfo methodInfo = genericMethodInfo.MakeGenericMethod(inputTypes);

            // Reconstruct the type
            Type[] methodArgs = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            Type type = Expression.GetDelegateType(methodArgs.Concat(new[] { methodInfo.ReturnType }).ToArray());

            // Create a delegate
            return Delegate.CreateDelegate(type, instance, methodInfo);
        }

        public static Type GetType(object instance, string typeName)
        {
            Type type = instance.GetType().GetNestedType(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            return type;
        }

        public static Type GetType(Type targetClass, string typeName)
        {
            Type type = targetClass.GetNestedType(typeName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);

            return type;
        }
    }
}
