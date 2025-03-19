// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace JsonDB;

/// <summary>
/// 节点类型定义
/// </summary>
public enum NodeType
{
    Value,
    Object,
    ArrayValue,
    ArrayObject,
    LazyObject,
    LazyArray
}

/// <summary>
/// 值类型定义
/// </summary>
public enum ValueType
{
    Object='1',
    String,
    Int,
    Long,
    Double,
    Bool,
    ArrayObject,
    ArrayValue
}


/// <summary>
/// JSON配置项
/// </summary>
public class JsonOptions
{
    public bool Sort { get; set; } = true;
    public bool BinarySearch { get; set; } = true;
    public bool RecursiveMode { get; set; } = false;
    public readonly JsonNode SearchNode = new JsonValueNode<string> { NodeType = NodeType.Value };
}

public class JsonSerializeOptions
{
    /// <summary>
    /// 分隔符
    /// </summary>
    public char COMMA { get; set; } = ',';

    /// <summary>
    /// null标识
    /// </summary>
    public string NullValue { get; set; } = "__null__";

    /// <summary>
    /// 获取真实的value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetValue(string value)
    {
        return NullValue.Equals(value, StringComparison.Ordinal) ? null : value;
    }
}

/// <summary>
/// JsonNode的动态对象
/// </summary>
public class DynamicJsonNode(JsonNode node) : DynamicObject
{
    /// <summary>
    /// 内部Node
    /// </summary>
    private readonly JsonNode _node = node;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        return TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        var key = binder.Name;
        return _node.Set(key, value);
    }

    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        if (indexes is [not null])
        {
            var key = $"${(int)indexes[0] + 1}";
            return TryGetValue(key, out result);
        }
        return base.TryGetIndex(binder, indexes, out result);
    }

    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
    {
        if (indexes is [not null])
        {
            var key = $"${(int)indexes[0] + 1}";
            return _node.Set(key, value);
        }
        return base.TrySetIndex(binder, indexes, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetValue(string queryKey, out object? result)
    {
        if (_node.TryGetValue(queryKey, out result))
        {
            if (result is JsonNode jsonNode)
            {
                result = new DynamicJsonNode(jsonNode);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取内部Node
    /// </summary>
    /// <returns></returns>
    public JsonNode Node()
    {
        return _node;
    }

    public override string ToString()
    {
        return _node.ToString();
    }
}

public static partial class JSON
{
    /// <summary>
    /// Json模块参数设置
    /// </summary>
    public static readonly ThreadLocal<JsonOptions> JsonOptions = new(() => new JsonOptions());

    /// <summary>
    /// Json模块序列化参数设置
    /// </summary>
    public static readonly ThreadLocal<JsonSerializeOptions> JsonSerializeOptions = new(() => new JsonSerializeOptions());

    /// <summary>
    /// 解析Json
    /// </summary>
    /// <param name="json"></param>
    /// <exception cref="Exception"></exception>
    public static JsonNode ParseFile(string filePath, bool enableLazy = false)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var doc = JsonDocument.Parse(fileStream);
        return ParseInternal(doc, enableLazy); 
    }

    /// <summary>
    /// 解析Json
    /// </summary>
    /// <param name="json"></param>
    /// <exception cref="Exception"></exception>
    public static JsonNode Parse(string json, bool enableLazy = false)
    {
        var doc = JsonDocument.Parse(json);
        return ParseInternal(doc, enableLazy);
    }
    
    /// <summary>
    /// 解析Json
    /// </summary>
    /// <param name="json"></param>
    /// <exception cref="Exception"></exception>
    public static JsonNode Parse(ReadOnlyMemory<byte> json, bool enableLazy = false)
    {
        var doc = JsonDocument.Parse(json);
        return ParseInternal(doc, enableLazy);
    }
    
    /// <summary>
    /// 解析Json
    /// </summary>
    /// <param name="json"></param>
    /// <exception cref="Exception"></exception>
    public static JsonNode Parse(ReadOnlySequence<byte> json, bool enableLazy = false)
    {
        var doc = JsonDocument.Parse(json);
        return ParseInternal(doc, enableLazy);
    }

    private static JsonNode ParseInternal(JsonDocument doc, bool enableLazy = false)
    {
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            var node = new JsonValueNode<object> { NodeType = NodeType.Object };
            InternalParseNode(root, node, enableLazy);
            return node;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            var node = ParseArrayNode(root, enableLazy:enableLazy);
            return node;
        }

        throw new Exception("unknown json type, root must is object or array");
    }

    /// <summary>
    /// 解析节点
    /// </summary>
    /// <returns></returns>
    internal static JsonNode ParseNode(JsonElement element, string? elementKey = null, JsonNode? parent = null, bool enableLazy = false)
    {
        if (enableLazy)
        {
            var node = new LazyJsonValueNode { Key = elementKey, Value = element, NodeType = NodeType.Object };
            parent?.AddChild(node);
            return node;
        }
        else
        {
            var node = new JsonValueNode<object> { Key = elementKey, NodeType = NodeType.Object };
            InternalParseNode(element, node);
            parent?.AddChild(node);
            return node;
        }
       
    }

    public static JsonNode CreateRootNode()
    {
        return new JsonValueNode<object> { NodeType = NodeType.Object };
    }

    public static JsonNode CreateJsonNode<T>(string key, T? value = default, JsonNode? parent = null)
    {
        var node = new JsonValueNode<T> { Key = key, Value = value, NodeType = NodeType.Value };
        parent?.AddChild(node);
        return node;
    }

    public static JsonNode CreateJsonArrayNode<T>(string key, JsonNode? parent)
    {
        var nodeType = NodeType.ArrayValue;
        if (typeof(T).IsAssignableTo(typeof(JsonNode)))
        {
            nodeType = NodeType.ArrayObject;
        }

        var node = new JsonArrayNode<T> { Key = key, NodeType = nodeType };
        parent?.AddChild(node);
        return node;
    }

    /// <summary>
    /// 内部解析节点
    /// </summary>
    internal static JsonNode? InternalParseNode(JsonElement element, JsonNode? parent = null, bool enableLazy = false)
    {
        JsonNode? node = null;
        var enumerator = element.EnumerateObject();
        while (enumerator.MoveNext())
        {
            var prop = enumerator.Current;
            var key = prop.Name;
            var value = prop.Value;
            switch (value.ValueKind)
            {
                case JsonValueKind.False:
                case JsonValueKind.True:
                    node = CreateJsonNode(key, value.GetBoolean(), parent);
                    break;
                case JsonValueKind.String:
                    node = CreateJsonNode(key, value.GetString(), parent);
                    break;
                case JsonValueKind.Number:
                    if (value.TryGetInt32(out var intValue))
                    {
                        node = CreateJsonNode(key, intValue, parent);
                    }
                    else if (value.TryGetInt64(out var longValue))
                    {
                        node = CreateJsonNode(key, longValue, parent);
                    }
                    else if (value.TryGetDouble(out var doubleValue))
                    {
                        node = CreateJsonNode(key, doubleValue, parent);
                    }
                    break;
                case JsonValueKind.Null:
                    node = CreateJsonNode<object>(key, null, parent);
                    break;
                case JsonValueKind.Object:
                    node = ParseNode(value, key, parent, enableLazy: enableLazy);
                    break;
                case JsonValueKind.Array:
                    if (enableLazy)
                    {
                        node = new LazyJsonArrayNode{ Key = key, NodeType = NodeType.ArrayObject, Value = value };
                        parent?.AddChild(node);
                    }
                    else
                    {
                        node = ParseArrayNode(value, key, parent);
                    }
                    break;

            }
        }
        return node;
    }

    /// <summary>
    /// 解析Json Array节点
    /// </summary>
    /// <returns></returns>
    internal static JsonNode ParseArrayNode(JsonElement element, string? elementKey = null, JsonNode? parent = null, bool enableLazy = false)
    {
        JsonNode? jsonNode = null;
        var arrayLen = element.GetArrayLength();
        if (arrayLen > 0)
        {
            var firstItem = element[0];
            switch (firstItem.ValueKind)
            {
                case JsonValueKind.False:
                case JsonValueKind.True:
                {
                    var value = element.Deserialize<List<bool>>();
                    jsonNode = new JsonArrayNode<bool> { Key = elementKey, NodeType = NodeType.ArrayValue, Value = value };
                }
                    break;
                case JsonValueKind.String:
                {
                    var value = element.Deserialize<List<string>>();
                    jsonNode = new JsonArrayNode<string> { Key = elementKey, NodeType = NodeType.ArrayValue, Value = value };
                }
                    break;
                case JsonValueKind.Number:
                    if (firstItem.TryGetInt32(out _))
                    {
                        var value = element.Deserialize<List<int>>();
                        jsonNode = new JsonArrayNode<int> { Key = elementKey, NodeType = NodeType.ArrayValue, Value = value };
                    }
                    else if (firstItem.TryGetInt64(out _))
                    {
                        var value = element.Deserialize<List<long>>();
                        jsonNode = new JsonArrayNode<long> { Key = elementKey, NodeType = NodeType.ArrayValue, Value = value };
                    }
                    else if (firstItem.TryGetDouble(out _))
                    {
                        var value = element.Deserialize<List<double>>();
                        jsonNode = new JsonArrayNode<double> { Key = elementKey, NodeType = NodeType.ArrayValue, Value = value };
                    }
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Object:
                {
                    jsonNode = new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject };
                    var elements = element.EnumerateArray().Select(v => new LazyJsonValueNode
                    {
                        Value = v,
                        NodeType = NodeType.Object,
                        Parent = jsonNode
                    }).ToList();
                    ((JsonArrayNode<JsonNode>)jsonNode).Value = [..elements];
                }
                    break;
                case JsonValueKind.Array:
                {
                    jsonNode = new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject };
                    var elements = element.EnumerateArray().Select(v => new LazyJsonArrayNode
                    {
                        Value = v,
                        NodeType = NodeType.ArrayObject,
                        Parent = jsonNode
                    }).ToList();
                    ((JsonArrayNode<JsonNode>)jsonNode).Value = [..elements];
                }
                    break;
            }
        }
        
        
        // for (var i = 0; i < element.GetArrayLength(); i++)
        // {
        //     var item = element[i];
        //     switch (item.ValueKind)
        //     {
        //         case JsonValueKind.False:
        //         case JsonValueKind.True:
        //         {
        //             jsonNode ??= new JsonArrayNode<bool> { Key = elementKey, NodeType = NodeType.ArrayValue };
        //             (jsonNode as JsonArrayNode<bool>).AddValue(item.GetBoolean());
        //         }
        //
        //             break;
        //         case JsonValueKind.String:
        //         {
        //             jsonNode ??= new JsonArrayNode<string> { Key = elementKey, NodeType = NodeType.ArrayValue };
        //             (jsonNode as JsonArrayNode<string>).AddValue(item.GetString());
        //         }
        //
        //             break;
        //         case JsonValueKind.Number:
        //             if (item.TryGetInt32(out var intValue))
        //             {
        //                 jsonNode ??= new JsonArrayNode<int> { Key = elementKey, NodeType = NodeType.ArrayValue };
        //                 (jsonNode as JsonArrayNode<int>).AddValue(intValue);
        //             }
        //             else if (item.TryGetInt64(out var longValue))
        //             {
        //                 jsonNode ??= new JsonArrayNode<long> { Key = elementKey, NodeType = NodeType.ArrayValue };
        //                 (jsonNode as JsonArrayNode<long>).AddValue(longValue);
        //             }
        //             else if (item.TryGetDouble(out var doubleValue))
        //             {
        //                 jsonNode ??= new JsonArrayNode<double> { Key = elementKey, NodeType = NodeType.ArrayValue };
        //                 (jsonNode as JsonArrayNode<double>).AddValue(doubleValue);
        //             }
        //
        //             break;
        //         case JsonValueKind.Null:
        //         {
        //             jsonNode ??= new JsonArrayNode<object> { Key = elementKey, NodeType = NodeType.ArrayValue };
        //             (jsonNode as JsonArrayNode<object>).AddValue((object)null);
        //         }
        //
        //             break;
        //         case JsonValueKind.Object:
        //         {
        //             jsonNode ??= new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject };
        //             (jsonNode as JsonArrayNode<JsonNode>).AddValue(ParseNode(item, enableLazy:enableLazy));
        //         }
        //             break;
        //         case JsonValueKind.Array:
        //         {
        //             if (enableLazy)
        //             {
        //                 jsonNode ??= new LazyJsonArrayNode{ Key = elementKey, NodeType = NodeType.ArrayObject, Value = item };
        //             }
        //             else
        //             {
        //                 jsonNode ??= new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject };
        //                 (jsonNode as JsonArrayNode<JsonNode>).AddValue(ParseArrayNode(item));
        //             }
        //         }
        //             break;
        //     }
        // }

        jsonNode ??= new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject, Value = []};
        parent?.AddChild(jsonNode);
        return jsonNode;
    }

    internal static string GetValue<T>(T value)
    {
        var builder = new StringBuilder();
        GetValue(value, builder);
        return builder.ToString();
    }

    internal static void GetValue<T>(T value, StringBuilder builder)
    {
        if (value == null)
        {
            builder.Append("null");
        }
        else if (value is string sv)
        {
            var span = sv.AsSpan();
            builder.Append('"');
            foreach (var c in span)
            {
                if (c == '\\')
                {
                    builder.Append("\\\\");
                }
                else
                {
                    builder.Append(c);
                }
            }
            builder.Append('"');
        }
        else if (value is int iv)
        {
            builder.Append(iv);
        }
        else if (value is long lv)
        {
            builder.Append(lv);
        }
        else if (value is double dv)
        {
            builder.Append(dv);
        }
        else if (value is bool bv)
        {
            builder.Append(bv ? "true" : "false");
        }
        else
        {
            builder.Append(value);
        }
    }

    internal static void SerializeValue<T>(T value, StringBuilder builder, JsonSerializeOptions options)
    {
        if (value == null)
        {
            builder.Append((char)ValueType.Object).Append(options.COMMA);
            builder.Append(options.NullValue);
        }
        else if (value is string sv)
        {
            builder.Append((char)ValueType.String).Append(options.COMMA);
            builder.Append(sv);
        }
        else if (value is int iv)
        {
            builder.Append((char)ValueType.Int).Append(options.COMMA);
            builder.Append(iv);
        }
        else if (value is long lv)
        {
            builder.Append((char)ValueType.Long).Append(options.COMMA);
            builder.Append(lv);
        }
        else if (value is double dv)
        {
            builder.Append((char)ValueType.Double).Append(options.COMMA);
            builder.Append(dv);
        }
        else if (value is bool bv)
        {
            builder.Append((char)ValueType.Bool).Append(options.COMMA);
            builder.Append(bv);
        }
        else
        {
            builder.Append((char)ValueType.Object).Append(options.COMMA);
            builder.Append(value);
        }
    }
    
    internal static void SerializeValueWithoutType<T>(T value, StringBuilder builder, JsonSerializeOptions options)
    {
        if (value == null)
        {
            builder.Append(options.NullValue);
        }
        else if (value is string sv)
        {
            builder.Append(sv);
        }
        else if (value is int iv)
        {
            builder.Append(iv);
        }
        else if (value is long lv)
        {
            builder.Append(lv);
        }
        else if (value is double dv)
        {
            builder.Append(dv);
        }
        else if (value is bool bv)
        {
            builder.Append(bv);
        }
        else
        {
            builder.Append(value);
        }
    }

    /// <summary>
    /// 泛型类型转换
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="TFrom"></typeparam>
    /// <typeparam name="TTo"></typeparam>
    /// <returns></returns>
    internal static TTo? CastTo<TFrom, TTo>(ref TFrom value)
    {
        var typeTFrom = typeof(TFrom);
        var typeTTo = typeof(TTo);
        if (typeTFrom == typeTTo)
        {
            return Unsafe.As<TFrom, TTo>(ref value);
        }
        
        if (typeTTo == typeof(object))
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            object vv = value;
            return (TTo)vv;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }
        
        if (typeTFrom == typeof(string))
        {
            return StringCastTo<TTo>(value as string ?? null, typeTTo);
        }

        if (typeTFrom == typeof(int))
        {
            return IntCastTo<TTo>(value is int i ? i : null, typeTTo);
        }
        
        if (typeTFrom == typeof(long))
        {
            return LongCastTo<TTo>(value is long l ? l : null, typeTTo);
        }
        
        if (typeTFrom == typeof(double))
        {
            return DoubleCastTo<TTo>(value is double d ? d : null, typeTTo);
        }
        
        if (typeTFrom == typeof(float))
        {
            return FloatCastTo<TTo>(value is float f ? f : null, typeTTo);
        }
        
        if (typeTFrom == typeof(byte))
        {
            return ByteCastTo<TTo>(value is byte b ? b : null, typeTTo);
        }
        
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        object v = value;
        return (TTo)v;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }

    /// <summary>
    /// string到各种类型的转换
    /// </summary>
    /// <param name="src"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static T? StringCastTo<T>(string? src, Type typeTTo)
    {
        if (null == src)
        {
            return default;
        }

        if (typeTTo == typeof(bool))
        {
            bool.TryParse(src, out var result);
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(int))
        {
            int.TryParse(src, out var result);
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(long))
        {
            long.TryParse(src, out var result);
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(double))
        {
            double.TryParse(src, out var result);
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(float))
        {
            float.TryParse(src, out var result);
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(byte))
        {
            byte.TryParse(src, out var result);
            return result is T t ? t : default;
        }

        return default;
    }
    
    /// <summary>
    /// int到各种类型的转换
    /// </summary>
    /// <param name="src"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static T? IntCastTo<T>(int? src, Type typeTTo)
    {
        if (null == src)
        {
            return default;
        }

        if (typeTTo == typeof(bool))
        {
            var result = src == 1;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(long))
        {
            var result = (long)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(double))
        {
            var result = (double)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(float))
        {
            var result = (float)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(byte))
        {
            var result = (byte)src;
            return result is T t ? t : default;
        }
        
        if (typeTTo == typeof(string))
        {
            var result = $"{src}";
            return result is T t ? t : default;
        }

        return default;
    }
    
    /// <summary>
    /// long到各种类型的转换
    /// </summary>
    /// <param name="src"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static T? LongCastTo<T>(long? src, Type typeTTo)
    {
        if (null == src)
        {
            return default;
        }

        if (typeTTo == typeof(bool))
        {
            var result = src == 1;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(int))
        {
            var result = (int)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(double))
        {
            var result = (double)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(float))
        {
            var result = (float)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(byte))
        {
            var result = (byte)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(string))
        {
            var result = $"{src}";
            return result is T t ? t : default;
        }
        
        return default;
    }
    
    /// <summary>
    /// double到各种类型的转换
    /// </summary>
    /// <param name="src"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static T? DoubleCastTo<T>(double? src, Type typeTTo)
    {
        if (null == src)
        {
            return default;
        }

        if (typeTTo == typeof(bool))
        {
            var result = src >= 1;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(int))
        {
            var result = (int)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(long))
        {
            var result = (long)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(float))
        {
            var result = (float)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(byte))
        {
            var result = (byte)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(string))
        {
            var result = $"{src}";
            return result is T t ? t : default;
        }
        
        return default;
    }
    
    /// <summary>
    /// float到各种类型的转换
    /// </summary>
    /// <param name="src"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static T? FloatCastTo<T>(float? src, Type typeTTo)
    {
        if (null == src)
        {
            return default;
        }

        if (typeTTo == typeof(bool))
        {
            var result = src >= 1;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(int))
        {
            var result = (int)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(long))
        {
            var result = (long)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(double))
        {
            var result = (double)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(byte))
        {
            var result = (byte)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(string))
        {
            var result = $"{src}";
            return result is T t ? t : default;
        }
        
        return default;
    }
    
    /// <summary>
    /// byte到各种类型的转换
    /// </summary>
    /// <param name="src"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static T? ByteCastTo<T>(byte? src, Type typeTTo)
    {
        if (null == src)
        {
            return default;
        }

        if (typeTTo == typeof(bool))
        {
            var result = src == 1;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(int))
        {
            var result = (int)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(long))
        {
            var result = (long)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(double))
        {
            var result = (double)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(float))
        {
            var result = (float)src;
            return result is T t ? t : default;
        }

        if (typeTTo == typeof(string))
        {
            var result = $"{src}";
            return result is T t ? t : default;
        }
        
        return default;
    }
}
