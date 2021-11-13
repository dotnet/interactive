// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Assent;

using Xunit;
using System.Reflection;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.ApiCompatibility.Tests;

public class ApiCompatibilityTests
{
    private readonly Configuration _configuration;

    public ApiCompatibilityTests()
    {
        _configuration = new Configuration()
            .SetInteractive(Debugger.IsAttached)
            .UsingExtension("txt");
    }

    [FactSkipLinux]
    public void Interactive_api_is_not_changed()
    {
        var contract = GenerateContract<Kernel>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void Formatting_api_is_not_changed()
    {
        var contract = GenerateContract<Formatting.FormatContext>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void Document_api_is_not_changed()
    {
        var contract = GenerateContract<Documents.InteractiveDocument>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void PackageManagement_api_is_not_changed()
    {
        var contract = GenerateContract<PackageRestoreContext>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void Journey_api_is_not_changed()
    {
        var contract = GenerateContract<Journey.Lesson>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void csharp_api_is_not_changed()
    {
        var contract = GenerateContract<CSharp.CSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [Fact(Skip = "need to use signature files")]
    public void fsharp_api_is_not_changed()
    {
        var contract = GenerateContract<FSharp.FSharpKernel>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void powershell_api_is_not_changed()
    {
        var contract = GenerateContract<PowerShell.PowerShellKernel>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void mssql_api_is_not_changed()
    {
        var contract = GenerateContract<SqlServer.MsSqlKernelConnector>();
        this.Assent(contract, _configuration);
    }

    [FactSkipLinux]
    public void kql_api_is_not_changed()
    {
        var contract = GenerateContract<Kql.KqlKernelConnector>();
        this.Assent(contract, _configuration);
    }

    private static string GetPropertySignature(PropertyInfo property)
    {
        var getter  = property.GetMethod;

        var accessor = GetMethodAccessor(getter);

        var setter = property.GetSetMethod();
        var setterSignature = string.Empty;
        if (setter is { })
        {
            var setterAccessors = GetMethodAccessor(setter);
            setterAccessors = setterAccessors == accessor ? string.Empty : $"{setterAccessors} ";
            setterSignature = $" {setterAccessors}set;";
        }

        return
            $"{accessor} {GetQualifiedTypeName(property.PropertyType)} {GetQualifiedTypeName(property.DeclaringType)}.{property.Name} {{ get;{setterSignature} }}";

    }

    private static string GetMethodSignature(MethodInfo method)
    {

        var accessor = GetMethodAccessor(method);

        var genericArgs = string.Empty;

        if (method.IsGenericMethod)
        {
            genericArgs = $"<{string.Join(", ", method.GetGenericArguments().Select(GetTypeName))}>";
        }
        
        var methodParameters = method.GetParameters().AsEnumerable();

        var isExtensionMethod = method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);


        if (isExtensionMethod)
        {
            methodParameters = methodParameters.Skip(1);
        }

        var parameters = GetParameterSignatures(methodParameters, isExtensionMethod);

        return
            $"{accessor} {GetQualifiedTypeName(method.ReturnType)} {GetQualifiedTypeName(method.DeclaringType)}.{method.Name}{genericArgs}( {parameters} )";
    }

    private static string GetParameterSignatures(IEnumerable<ParameterInfo> methodParameters, bool isExtensionMethod)
    {
        var signature =  methodParameters.Select(param =>
        {
            var signature = string.Empty;

            if (param.ParameterType.IsByRef)
                signature = "ref ";
            else if (param.IsOut)
                signature = "out ";
            else if (isExtensionMethod && param.Position == 0)
                signature = "this ";

            signature += $"{GetTypeName(param.ParameterType)} {param.Name}";

            if (param.HasDefaultValue)
            {
                signature += $" = {param.DefaultValue?? "null"}";
            }

            return signature;
        });

        return string.Join(", ", signature);
    }

    private static string GetMethodAccessor(MethodInfo method)
    {
        var accessor = string.Empty;

        if (method.IsAssembly)
        {
            accessor = "internal ";

            if (method.IsFamily)
                accessor += "protected ";
        }
        else if (method.IsPublic)
        {
            accessor = "public ";
        }
        else if (method.IsPrivate)
        {
            accessor = "private ";
        }
        else if (method.IsFamily)
        {
            accessor = "protected ";
        }

        if (method.IsStatic)
        {
            accessor += "static ";
        }

        return accessor;
    }

    private static string GetTypeName(Type type)
    {
        var underlyingNullableType = Nullable.GetUnderlyingType(type);
        var isNullableType = underlyingNullableType != null;

        var signatureType = isNullableType
            ? underlyingNullableType
            : type;
        
        var typeName = GetQualifiedTypeName(signatureType);
        var signature = typeName;

        if (isNullableType)
        {
            signature += "?";
        }

        return signature;
    }
    private static void WriteCSharpDeclarationTo(
        Type type,
        TextWriter writer)
    {
        var typeName = type.FullName ?? type.Name;

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

    public static string GetQualifiedTypeName(Type type)
    {
        var builder = new StringBuilder();
        using var writer = new StringWriter(builder);
        WriteCSharpDeclarationTo(type, writer);
        writer.Flush();
        return builder.ToString();
    }

    private string GenerateContract<T>()
    {
        var contract = new StringBuilder();
        var assembly = typeof(T).Assembly;
        var types = assembly.GetExportedTypes().Where(t => t.IsPublic).OrderBy(t => t.FullName).ToArray();

        var printedMethods = new HashSet<MethodInfo>();

        foreach (var type in types)
        {
            // statics
            foreach (var prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(m => m.DeclaringType == type).OrderBy(c => c.Name))
            {
                if (prop.GetMethod?.IsPublic == true)
                {
                    var m = prop.GetMethod;
                    
                    if (m is { } && printedMethods.Add(m))
                    {
                        var setter = prop.GetSetMethod();
                        if (setter is not null)
                        {
                            printedMethods.Add(setter);
                        }
                        contract.AppendLine($"{GetPropertySignature(prop)}");
                    }
                }
            }

            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(m => m.DeclaringType == type).OrderBy(c => c.Name))
            {

                if (printedMethods.Add(method))
                {
                    contract.AppendLine($"{GetMethodSignature(method)}");
                }

            }

            // instance
            foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where( m => m.DeclaringType == type).OrderBy(c => c.Name))
            {
                contract.AppendLine($"{GetTypeName(ctor.DeclaringType)}::.ctor({GetParameterSignatures(ctor.GetParameters(), false)})");
            }

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(m => m.DeclaringType == type).OrderBy(c => c.Name))
            {
                if (prop.GetMethod?.IsPublic == true)
                {
                    var m = prop.GetMethod;

                    if (m is { } && printedMethods.Add(m))
                    {
                        var setter = prop.GetSetMethod();
                        if (setter is not null)
                        {
                            printedMethods.Add(setter);
                        }
                        contract.AppendLine($"{GetPropertySignature(prop)}");
                    }
                }
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic| BindingFlags.DeclaredOnly).Where(m => m.DeclaringType == type && !m.IsPrivate && !m.IsAssembly ).OrderBy(c => c.Name))
            {
                if (printedMethods.Add(method))
                {
                    contract.AppendLine($"{GetMethodSignature(method)}");
                }
            }
        }

        return contract.ToString();
    }
}

