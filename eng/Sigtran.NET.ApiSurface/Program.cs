using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;

if (args.Length != 2)
{
    Console.Error.WriteLine(
        "Usage: Sigtran.NET.ApiSurface <assembly-path> <output-path>");
    return 2;
}

string assemblyPath = Path.GetFullPath(args[0]);
string outputPath = Path.GetFullPath(args[1]);
if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Assembly does not exist: {assemblyPath}");
    return 2;
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
Assembly assembly = Assembly.LoadFrom(assemblyPath);
SortedSet<string> surface = new(StringComparer.Ordinal);
foreach (Type type in assembly.GetExportedTypes())
{
    AddType(surface, type);
}

await File.WriteAllLinesAsync(outputPath, surface);
string hash = Convert.ToHexString(
        SHA256.HashData(await File.ReadAllBytesAsync(outputPath)))
    .ToLowerInvariant();
Console.WriteLine(
    $$"""
      {
        "outputPath": "{{Escape(outputPath)}}",
        "sha256": "{{hash}}",
        "memberCount": {{surface.Count}}
      }
      """);
return 0;

static void AddType(ISet<string> surface, Type type)
{
    string typeName = FormatType(type);
    string kind = type.IsInterface
        ? "interface"
        : type.IsEnum
            ? "enum"
            : type.IsValueType
                ? "struct"
                : typeof(Delegate).IsAssignableFrom(type.BaseType)
                    ? "delegate"
                    : "class";
    string baseType = type.BaseType is null
        ? "-"
        : FormatType(type.BaseType);
    string interfaces = string.Join(
        ",",
        type.GetInterfaces()
            .Select(FormatType)
            .OrderBy(static name => name, StringComparer.Ordinal));
    surface.Add(
        $"T:{typeName}|kind={kind}|base={baseType}|interfaces={interfaces}|"
        + $"abstract={type.IsAbstract}|sealed={type.IsSealed}");

    const BindingFlags flags =
        BindingFlags.Public
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;
    foreach (ConstructorInfo constructor in type.GetConstructors(flags))
    {
        surface.Add(
            $"C:{typeName}({FormatParameters(constructor.GetParameters())})");
    }

    foreach (MethodInfo method in type.GetMethods(flags))
    {
        if (method.IsSpecialName
            && !method.Name.StartsWith("op_", StringComparison.Ordinal))
        {
            continue;
        }

        surface.Add(
            $"M:{typeName}.{method.Name}`{method.GetGenericArguments().Length}"
            + $"({FormatParameters(method.GetParameters())})"
            + $":{FormatType(method.ReturnType)}"
            + $"|static={method.IsStatic}|abstract={method.IsAbstract}");
    }

    foreach (PropertyInfo property in type.GetProperties(flags))
    {
        MethodInfo? getter = property.GetMethod;
        MethodInfo? setter = property.SetMethod;
        surface.Add(
            $"P:{typeName}.{property.Name}"
            + $"({FormatParameters(property.GetIndexParameters())})"
            + $":{FormatType(property.PropertyType)}"
            + $"|get={getter?.IsPublic == true}|set={setter?.IsPublic == true}"
            + $"|static={getter?.IsStatic == true || setter?.IsStatic == true}");
    }

    foreach (FieldInfo field in type.GetFields(flags))
    {
        string constant = field.IsLiteral
            ? Convert.ToString(
                field.GetRawConstantValue(),
                CultureInfo.InvariantCulture) ?? "null"
            : "-";
        surface.Add(
            $"F:{typeName}.{field.Name}:{FormatType(field.FieldType)}"
            + $"|static={field.IsStatic}|readonly={field.IsInitOnly}"
            + $"|literal={field.IsLiteral}|value={constant}");
    }

    foreach (EventInfo eventInfo in type.GetEvents(flags))
    {
        surface.Add(
            $"E:{typeName}.{eventInfo.Name}:"
            + FormatType(eventInfo.EventHandlerType!));
    }
}

static string FormatParameters(IEnumerable<ParameterInfo> parameters)
{
    return string.Join(
        ",",
        parameters.Select(static parameter =>
            $"{FormatType(parameter.ParameterType)}"
            + $"|optional={parameter.IsOptional}"
            + $"|out={parameter.IsOut}"));
}

static string FormatType(Type type)
{
    if (type.IsByRef)
    {
        return $"{FormatType(type.GetElementType()!)}&";
    }

    if (type.IsPointer)
    {
        return $"{FormatType(type.GetElementType()!)}*";
    }

    if (type.IsArray)
    {
        return $"{FormatType(type.GetElementType()!)}"
            + $"[{new string(',', type.GetArrayRank() - 1)}]";
    }

    if (type.IsGenericParameter)
    {
        return $"!{type.Name}";
    }

    if (type.IsGenericType)
    {
        string definitionName =
            type.GetGenericTypeDefinition().FullName
            ?? type.GetGenericTypeDefinition().Name;
        int marker = definitionName.IndexOf('`');
        if (marker >= 0)
        {
            definitionName = definitionName[..marker];
        }

        return $"{definitionName.Replace('+', '.')}"
            + $"<{string.Join(",", type.GetGenericArguments().Select(FormatType))}>";
    }

    return (type.FullName ?? type.Name).Replace('+', '.');
}

static string Escape(string value)
{
    return value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);
}
