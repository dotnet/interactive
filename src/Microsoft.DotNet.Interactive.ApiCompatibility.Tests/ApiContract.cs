using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.DotNet.Interactive.ApiCompatibility.Tests;

internal static class ApiContract
{
    public static string GenerateContract<T>()
    {
        var output = new StringBuilder();
        var assembly = typeof(T).Assembly;
        var types = assembly.GetExportedTypes().OrderBy(t => t.FullName).ToArray();
        var namespaces = types.Select(t => t.Namespace).Distinct().OrderBy(n => n).ToArray();

        var printedMethods = new HashSet<MethodInfo>();

        foreach (var ns in namespaces)
        {
            output.AppendLine(ns);

            foreach (var type in types.Where(t => t.Namespace == ns))
            {
                var isDelegate = typeof(Delegate).IsAssignableFrom(type);

                var typeKind = type.IsValueType
                                   ? type.IsEnum
                                         ? "enum"
                                         : "struct"
                                   : isDelegate
                                       ? "delegate"
                                       : "class";

                output.AppendLine($"  {type.GetAccessModifiers()} {typeKind} {type.GetReadableTypeName(type.Namespace == ns)}");
                if (type.IsEnum)
                {
                    WriteContractForEnum(type, output);
                }
                else
                {
                    WriteContractForClassOrStruct(type, printedMethods, output);
                }
            }
        }

        return output.ToString();
    }

    private static void WriteContractForEnum(
        Type type,
        StringBuilder output)
    {
        var names = Enum.GetNames(type);
        var values = Enum.GetValues(type).Cast<int>().ToArray();

        foreach (var (name, value) in names.Zip(values.Select(v => v.ToString())))
        {
            output.AppendLine($"    {name}={value}");
        }
    }

    private static void WriteContractForClassOrStruct(
        Type type, 
        HashSet<MethodInfo> printedMethods, 
        StringBuilder output)
    {
        // statics
        // properties
        foreach (var prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 .Where(m => m.DeclaringType == type)
                                 .OrderBy(c => c.Name))
        {
            if (prop.GetMethod?.IsPublic == true)
            {
                if (printedMethods.Add(prop.GetMethod))
                {
                    var setter = prop.GetSetMethod();
                    if (setter is not null)
                    {
                        printedMethods.Add(setter);
                    }

                    output.AppendLine($"    {GetPropertySignature(prop, type.Namespace == prop.PropertyType.Namespace)}");
                }
            }
        }

        // methods
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                   .Where(m => m.DeclaringType == type)
                                   .OrderBy(c => c.Name))
        {
            if (printedMethods.Add(method))
            {
                output.AppendLine($"    {GetMethodSignature(method, type.Namespace)}");
            }
        }

        // instance
        foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 .Where(m => m.DeclaringType == type)
                                 .OrderBy(c => c.Name))
        {
            output.AppendLine($"    .ctor({GetParameterSignatures(ctor.GetParameters(), false, type.Namespace)})");
        }

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 .Where(m => m.DeclaringType == type)
                                 .OrderBy(c => c.Name))
        {
            if (prop.GetMethod?.IsPublic == true)
            {
                if (printedMethods.Add(prop.GetMethod))
                {
                    var setter = prop.GetSetMethod();
                    if (setter is not null)
                    {
                        printedMethods.Add(setter);
                    }

                    output.AppendLine($"    {GetPropertySignature(prop)}");
                }
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                   .Where(m => m.DeclaringType == type &&
                                               !m.IsPrivate &&
                                               !m.IsAssembly &&
                                               !m.IsPropertyAccessor())
                                   .OrderBy(c => c.Name))
        {
            if (printedMethods.Add(method))
            {
                output.AppendLine($"    {GetMethodSignature(method, type.Namespace)}");
            }
        }
    }

    public static string GetPropertySignature(this PropertyInfo property, bool omitNamespace = false)
    {
        var getter = property.GetGetMethod();
        var setter = property.GetSetMethod();

        var getterAccessModifier = GetAccessModifiers(getter);
        var setterAccessModifier = GetAccessModifiers(setter);
        var defaultAccessModifier = getterAccessModifier;

        if (setterAccessModifier is null)
        {
            getterAccessModifier = null;
        }
        else if (setterAccessModifier == getterAccessModifier)
        {
            setterAccessModifier = "";
            defaultAccessModifier = "";
        }

        var getterSignature = string.Empty;
        var setterSignature = string.Empty;

        if (getter is { })
        {
            getterSignature = $"{getterAccessModifier} get;";
        }

        if (setter is { })
        {
            setterSignature = $"{setterAccessModifier} set;";
        }

        return
            $"{defaultAccessModifier} {GetReadableTypeName(property.PropertyType, omitNamespace)} {property.Name} {{ {getterSignature}{setterSignature}}}".Replace("  ", " ");
    }

    public static string GetMethodSignature(
        this MethodInfo method,
        string omitNamespace)
    {
        var accessor = GetAccessModifiers(method);

        var genericArgs = string.Empty;

        if (method.IsGenericMethod)
        {
            genericArgs = $"<{string.Join(", ", method.GetGenericArguments().Select(a => GetReadableTypeName(a)))}>";
        }

        var methodParameters = method.GetParameters().AsEnumerable();

        var isExtensionMethod = method.IsDefined(typeof(ExtensionAttribute), false);

        if (isExtensionMethod)
        {
            methodParameters = methodParameters.Skip(1);
        }

        var parameters = GetParameterSignatures(methodParameters, isExtensionMethod, omitNamespace);

        return
            $"{accessor} {GetReadableTypeName(method.ReturnType, method.ReturnType.Namespace == omitNamespace)} {method.Name}{genericArgs}({parameters})";
    }

    public static string GetParameterSignatures(
        this IEnumerable<ParameterInfo> methodParameters, 
        bool isExtensionMethod, 
        string omitNamespace)
    {
        var signature = methodParameters.Select(param =>
        {
            var signature = string.Empty;

            if (param.ParameterType.IsByRef)
                signature = "ref ";
            else if (param.IsOut)
                signature = "out ";
            else if (isExtensionMethod && param.Position == 0)
                signature = "this ";

            signature += $"{GetReadableTypeName(param.ParameterType, param.ParameterType.Namespace == omitNamespace)} {param.Name}";

            if (param.HasDefaultValue)
            {
                signature += $" = {param.DefaultValue ?? "null"}";
            }

            return signature;
        });

        return string.Join(", ", signature);
    }

    private static string GetAccessModifiers(this MethodBase method)
    {
        var modifier = string.Empty;

        if (method is null)
        {
            return null;
        }

        if (method.IsAssembly)
        {
            modifier = "internal";

            if (method.IsFamily)
            {
                modifier += " protected";
            }
        }
        else if (method.IsPublic)
        {
            modifier = "public";
        }
        else if (method.IsPrivate)
        {
            modifier = "private";
        }
        else if (method.IsFamily)
        {
            modifier = "protected";
        }

        if (method.IsStatic)
        {
            modifier += " static";
        }

        return modifier;
    }

    private static string GetAccessModifiers(this Type type)
    {
        var modifier = string.Empty;

        if (type.IsPublic)
        {
            modifier = "public";
        }

        if (type.IsAbstract)
        {
            if (type.IsSealed)
            {
                modifier += " static";
            }
            else
            {
                modifier += " abstract";
            }
        }

        return modifier;
    }

    public static bool IsPropertyAccessor(this MethodInfo methodInfo) =>
        methodInfo.DeclaringType.GetProperties().Any(prop => prop.GetSetMethod() == methodInfo);

    public static string GetReadableTypeName(this Type type, bool omitNamespace = false)
    {
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder);
        WriteCSharpDeclarationTo(type, writer, omitNamespace);
        writer.Flush();
        return builder.ToString();
    }

    private static void WriteCSharpDeclarationTo(
        this Type type,
        TextWriter writer,
        bool omitNamespace = false)
    {
        var typeName = omitNamespace
                           ? type.Name
                           : type.FullName ?? type.Name;

        if (typeName.Contains("`"))
        {
            writer.Write(typeName.Remove(typeName.IndexOf('`')));
            writer.Write("<");
            var genericArguments = type.GetGenericArguments();

            for (var i = 0; i < genericArguments.Length; i++)
            {
                WriteCSharpDeclarationTo(genericArguments[i], writer);
                if (i < genericArguments.Length - 1)
                {
                    writer.Write(",");
                }
            }

            writer.Write(">");
        }
        else
        {
            writer.Write(typeName);
        }
    }
}