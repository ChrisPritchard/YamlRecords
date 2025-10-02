using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static class YamlRecords
{
    private const string indent_amount = "  ";
    private static readonly char[] special_characters = ":\"\'\n[]{}#".ToCharArray();

    public static string Serialize(object obj)
    {
        var sb = new StringBuilder();
        SerializeUnknown(sb, obj);
        return sb.ToString();
    }

    public static T Deserialize<T>(string yaml)
    {
        var lines = yaml.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var processed = PreProcess(lines, "").Item1;
        return (T)DeserializeUnknown(processed, typeof(T));
    }

    //
    // - Serialization Methods
    //

    private static void SerializeUnknown(StringBuilder sb, object obj, string indent = "", bool first_line_indented = false)
    {
        if (obj == null)
            return;

        var type = obj.GetType();

        if (IsBasicType(type))
            SerializeBasic(sb, obj, indent);
        else if (IsMap(type, out Type _, out Type _))
            SerializeMap(sb, obj, indent, first_line_indented);
        else if (IsList(type, out Type _))
            SerializeList(sb, obj, indent);
        else
        {
            var dict = ToDictionary(obj);
            SerializeMap(sb, dict, indent, first_line_indented);
        }
    }

    private static void SerializeBasic(StringBuilder sb, object obj, string indent = "", bool convert_string_case = false)
    {
        sb.Append(indent);
        var type = obj.GetType();
        if (obj is not string && !type.IsEnum)
        {
            if (obj is bool b)
                sb.Append(b ? "true" : "false");
            else
                sb.Append(obj);
            return;
        }

        var str = obj.ToString()!; // will also convert enums
        if (type.IsEnum && str == "0")
            str = "";
        if (special_characters.Any(str.Contains))
            sb.Append('"').Append(str.Replace("\"", "\\\"")).Append('"');
        else
            sb.Append(convert_string_case ? CamelCase(str) : str);
    }

    private static void SerializeMap(StringBuilder sb, object obj, string indent = "", bool first_line_indented = false)
    {
        var first_line = true;
        var dict = (IDictionary)obj;
        var keys = dict.Keys.Cast<object>().ToArray();
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            if (key == null)
                continue;

            if (!first_line || !first_line_indented)
                sb.Append(indent);
            if (first_line)
                first_line = false;

            SerializeBasic(sb, key, convert_string_case: true);
            sb.Append(':');

            var val = dict[key];
            if (val != null)
                if (IsBasicType(val.GetType()))
                {
                    sb.Append(' ');
                    SerializeBasic(sb, val, "");
                }
                else
                {
                    sb.AppendLine();
                    SerializeUnknown(sb, val, indent + indent_amount, false);
                }

            if (i != keys.Length - 1)
                sb.AppendLine();
        }
    }

    private static void SerializeList(StringBuilder sb, object obj, string indent = "")
    {
        object[] values;
        if (obj.GetType().IsArray)
        {
            var obj_array = (Array)obj;
            values = new object[obj_array.Length];
            for (var i = 0; i < obj_array.Length; i++)
                values[i] = obj_array.GetValue(i)!;
        }
        else
            values = [.. (IEnumerable<object>)obj];
        for (var i = 0; i < values.Length; i++)
        {
            var v = values[i];
            if (v == null)
                continue;
            sb.Append(indent).Append("- ");
            if (IsBasicType(v.GetType()))
                SerializeBasic(sb, v);
            else
                SerializeUnknown(sb, v, indent + indent_amount, true);

            if (i != values.Length - 1)
                sb.AppendLine();
        }
    }

    //
    // - Deserialization Methods
    //

    private static object DeserializeUnknown(object obj, Type type)
    {
        if (obj == null)
            return default!;
        if (IsBasicType(type))
            return DeserializeBasic(obj, type);
        else if (IsMap(type, out Type dkt, out Type dvt))
            return DeserializeMap(obj, type, dkt, dvt);
        else if (IsList(type, out Type lvt))
            return DeserializeList(obj, type, lvt);
        else
            return DeserializeRecord(obj, type);
    }

    private static object DeserializeBasic(object value, Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var under_type = type.GetGenericArguments()[0];
            if (value == null)
                return default!;
            return DeserializeBasic(value, under_type);
        }

        if (type.IsEnum)
            return Enum.Parse(type, value.ToString()!);
        else if (type != typeof(string))
        {
            if (type == typeof(bool))
                return bool.Parse((string)value);
            else
                return Convert.ChangeType(value, type);
        }

        var str = (string)value;

        if (str.StartsWith('"') && str.EndsWith('"'))
            return str[1..^1].Replace("\\\"", "\"");
        if (str.StartsWith('\'') && str.EndsWith('\''))
            return str[1..^1];
        return value;
    }

    private static IDictionary DeserializeMap(object value, Type type, Type key_type, Type value_type)
    {
        var data = (Dictionary<object, object>)value;
        var result = (IDictionary)Activator.CreateInstance(type)!;

        foreach (var kvp in data)
        {
            var deserializedKey = DeserializeUnknown(kvp.Key, key_type);
            var deserializedValue = DeserializeUnknown(kvp.Value, value_type);

            result.Add(deserializedKey, deserializedValue);
        }

        return result;
    }

    private static IList DeserializeList(object value, Type type, Type value_type)
    {
        var data = (List<object>)value;
        type = GetConcreteListType(type, value_type);

        if (type.IsArray)
        {
            var result = (IList)Array.CreateInstance(type.GetElementType()!, data.Count);
            for (var i = 0; i < result.Count; i++)
                result[i] = DeserializeUnknown(data[i], value_type);
            return result;
        }
        else
        {
            var result = (IList)Activator.CreateInstance(type)!;

            foreach (var item in data)
                result.Add(DeserializeUnknown(item, value_type));

            return result;
        }

    }

    private static object DeserializeRecord(object value, Type type)
    {
        var data = (Dictionary<object, object>)value;

        if (type.IsInterface || type.IsAbstract)
            type = FindConcreteTypeForData(type, data);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var constructor = type.GetConstructors().First();

        var constructorParams = constructor.GetParameters();

        var constructorArgs = new object[constructorParams.Length];
        for (var i = 0; i < constructorParams.Length; i++)
        {
            var paramName = constructorParams[i].Name;
            if (paramName == null)
                continue;
            if (data.TryGetValue(CamelCase(paramName), out var obj))
                constructorArgs[i] = DeserializeUnknown(obj, constructorParams[i].ParameterType);
            else
                constructorArgs[i] = constructorParams[i].HasDefaultValue ?
                    constructorParams[i].DefaultValue! :
                    GetDefaultValue(constructorParams[i].ParameterType);
        }

        var instance = constructor.Invoke(constructorArgs);

        foreach (var prop in properties)
        {
            if (data.TryGetValue(CamelCase(prop.Name), out var obj) && prop.CanWrite)
                prop.SetValue(instance, DeserializeUnknown(obj, prop.PropertyType));
        }

        return instance;
    }

    //
    // - Utility Methods
    //

    private static readonly Type[] basic_non_primitive = [typeof(decimal), typeof(float), typeof(string)];

    private static bool IsBasicType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return IsBasicType(type.GetGenericArguments()[0]);

        return type.IsPrimitive || type.IsEnum || basic_non_primitive.Contains(type);
    }


    private static bool IsMap(Type type, out Type key_type, out Type value_type)
    {
        key_type = null!;
        value_type = null!;

        var dict_type = type.GetInterfaces()
            .Where(i => i.IsGenericType)
            .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dict_type == null)
            return false;

        var args = dict_type.GetGenericArguments();
        key_type = args[0];
        value_type = args[1];
        return true;
    }

    private static bool IsList(Type type, out Type value_type)
    {
        value_type = null!;
        if (type.IsArray)
        {
            value_type = type.GetElementType()!;
            return true;
        }
        if (!type.IsGenericType)
            return false;

        var result = type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        if (result)
        {
            value_type = type.GetGenericArguments()[0];
            return true;
        }

        var inherits = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (inherits == null)
            return false;

        value_type = inherits.GetGenericArguments()[0];
        return true;
    }

    private static Dictionary<string, object> ToDictionary(object obj)
    {
        if (obj == null) return [];

        var dictionary = new Dictionary<string, object>();
        var properties = obj.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        foreach (var p in properties.Where(p => p.CanRead))
            dictionary[p.Name] = p.GetValue(obj) ?? default!;

        return dictionary;
    }

    private static string CamelCase(string orig) => char.ToLowerInvariant(orig[0]) + orig[1..];

    private static (object, int processed_count) PreProcess(string[] lines, string indent)
    {
        var result_dict = new Dictionary<object, object>();
        var result_list = new List<object>();

        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = CleanLine(lines[i]);
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            if (!lines[i].StartsWith(indent))
                return (result_dict.Count > 0 ? result_dict : result_list, i);

            if (lines[i].StartsWith(indent + "- "))
            {
                if (!lines[i].Contains(':'))
                    result_list.Add(lines[i][(indent + "- ").Length..]);
                else
                {
                    lines[i] = lines[i].Replace("- ", indent_amount);
                    var (res, processed_count) = PreProcess(lines[i..], indent + indent_amount);
                    result_list.Add(res);
                    i += processed_count - 1; // because this line is reprocessed
                }
                continue;
            }

            var parts = lines[i].Split(':', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
            {
                var (res, processed_count) = PreProcess(lines[(i + 1)..], indent + indent_amount);
                if (processed_count == 0)
                    result_dict[parts[0]] = default!;
                else
                    result_dict[parts[0]] = res;
                i += processed_count;
            }
            else if (parts[1].StartsWith('[') && parts[1].EndsWith(']'))
            {
                var inline_parts = parts[1].Split("[,]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var new_list = inline_parts.Select(p => (object)p).ToList();
                result_dict[parts[0]] = new_list;
            }
            else
                result_dict[parts[0]] = parts[1];
        }

        return (result_dict.Count > 0 ? result_dict : result_list, lines.Length);
    }

    private static string CleanLine(string line)
    {
        // leading tabs are not allowed in yaml, so if the yaml has them it should probably be rejected
        // but sometimes thats annnoying, and its easy to fix
        var i = 0;
        while (i < line.Length && line[i] == '\t')
            i++;
        if (i != 0)
        {
            var new_indent = string.Concat(Enumerable.Repeat(indent_amount, i));
            line = new_indent + line[i..];
        }

        // comment stripping

        var in_quotes = false;
        var quote_type = '\0';
        var comment_start = -1;

        for (var j = 0; j < line.Length; j++)
        {
            var current = line[j];

            if ((current == '\'' || current == '"') && !in_quotes)
            {
                in_quotes = true;
                quote_type = current;
            }
            else if (in_quotes && current == quote_type)
                in_quotes = false;
            else if (current == '#' && !in_quotes)
            {
                comment_start = j;
                break;
            }
        }

        return comment_start >= 0 ? line[..comment_start].TrimEnd() : line;
    }

    private static object GetDefaultValue(Type type) => type.IsValueType ? Activator.CreateInstance(type)! : null!;

    private static Type GetConcreteListType(Type type, Type valueType)
    {
        if (!type.IsInterface && !type.IsAbstract)
            return type;

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();

            if (genericDefinition == typeof(IEnumerable<>) ||
                genericDefinition == typeof(ICollection<>) ||
                genericDefinition == typeof(IList<>))
                return typeof(List<>).MakeGenericType(valueType);

            if (genericDefinition == typeof(ISet<>))
                return typeof(HashSet<>).MakeGenericType(valueType);
        }

        if (type == typeof(IEnumerable) || type == typeof(ICollection) || type == typeof(IList))
            return typeof(List<object>);

        if (type.IsAssignableFrom(typeof(IEnumerable)))
            return typeof(List<object>);

        throw new NotSupportedException($"Cannot create concrete type for interface/abstract type: {type}");
    }

    private static Type FindConcreteTypeForData(Type baseType, Dictionary<object, object> data)
    {
        var assembly = baseType.Assembly;
        var concreteTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && baseType.IsAssignableFrom(t))
            .ToList();

        var dataKeys = data.Keys.Select(k => k.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var type in concreteTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propertyNames = properties.Select(p => CamelCase(p.Name).ToString()).ToHashSet();

            if (dataKeys.IsSubsetOf(propertyNames))
                return type;
        }

        throw new Exception("could not find concrete type for " + baseType.Name);
    }
}