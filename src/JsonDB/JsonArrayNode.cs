using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace JsonDB;

public class JsonArrayNode<T> : JsonValueNode<List<T>>
{
    public override int Count => Value?.Count ?? 0;

    /// <summary>
    /// 添加值
    /// </summary>
    /// <param name="value"></param>
    internal override void AddValue<TBase>(TBase value)
    {
        Value ??= new List<T>();
        Value.Add(JSON.CastTo<TBase, T>(ref value)!);
        if (value is JsonNode node)
        {
            node.Parent = this;
        }
    }

    /// <summary>
    /// 替换指定下标的值
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="newValue"></param>
    internal override void ReplaceValue(int pos, JsonNode newValue)
    {
        Value![pos] = Unsafe.As<JsonNode, T>(ref newValue);
    }

    /// <summary>
    /// 替换节点
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    internal override void ReplaceValue<TBase>(TBase oldValue, TBase newValue)
    {
        var ov = JSON.CastTo<TBase, T>(ref oldValue);
        var nv = JSON.CastTo<TBase, T>(ref newValue);
        if (ov != null && nv != null)
        {
            for (var i = 0; i < Value!.Count; i++)
            {
                if (Value[i]!.Equals(ov))
                {
                    Value[i] = nv;
                    break;
                }
            }
        }
    }

    public override IEnumerator<JsonNode> GetEnumerator()
    {
        if (NodeType == NodeType.ArrayObject)
        {
            for (var i = 0; i < Value!.Count; i++)
            {
                if (Value[i] is JsonNode node)
                {
                    node.ParseLazyNode();
                }
            }
            return (Value as List<JsonNode>)?.GetEnumerator() ?? new List<JsonNode>().GetEnumerator();
        }

        return new List<JsonNode>().GetEnumerator();
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    /// <param name="pos"></param>
    internal override void RemoveValue(int pos)
    {
        Value?.RemoveAt(pos);
    }

    internal override void RemoveValue<TBase>(TBase value)
    {
        var v = JSON.CastTo<TBase, T>(ref value);
        if (v != null)
        {
            Value?.Remove(v);
        }
    }

    public override bool Add<TBase>(TBase value)
    {
        if (NodeType == NodeType.ArrayValue)
        {
            AddValue(value);
            return true;
        }

        throw new Exception("unsupported this operation");
    }

    public override bool AddJson(string json)
    {
        if (NodeType == NodeType.ArrayObject)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new Exception("excepted json object");
            }

            var node = new JsonValueNode<object> { NodeType = NodeType.Object };
            JSON.InternalParseNode(root, node);
            AddValue(node);
            return true;
        }

        throw new Exception("unsupported this operation");
    }

    public override bool Add<TBase>(string key, TBase value)
    {
        throw new Exception("unsupported this operation");
    }

    public override bool AddJson(string key, string json)
    {
        throw new Exception("unsupported this operation");
    }

    protected internal override bool InternalRemove(int pos, string lastKey)
    {
        if (lastKey.StartsWith("$"))
        {
            Value?.RemoveAt(pos);
            return true;
        }
        Parent?.RemoveValue(pos);
        return true;
    }

    protected internal override TBase InternalGetValue<TBase>(int pos, string lastKey)
    {
        if (pos != -1 && lastKey.StartsWith("$"))
        {
            var index = int.Parse(lastKey.AsSpan()[1..]);
            var v = Value![index - 1];
            return JSON.CastTo<T, TBase>(ref v)!;
        }

        var _this = this;
        return Unsafe.As<JsonArrayNode<T>, TBase>(ref _this);
    }

    protected internal override void InternalSetValue<TBase>(int pos, TBase value)
    {
        if (NodeType == NodeType.ArrayValue)
        {
            Value![pos] = JSON.CastTo<TBase, T>(ref value)!;
        } else if (NodeType == NodeType.ArrayObject)
        {
            if (value is string json)
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new Exception("excepted json object");
                }

                var node = JSON.ParseNode(root, Key);
                node.Parent = this;
                Value![pos] = Unsafe.As<JsonNode, T>(ref node);
            }
        }
    }
    
    public override object Clone()
    {
        var node = new JsonArrayNode<T>{Key=Key, NodeType = NodeType};
        if (Value is not null)
        {
            node.Value = new List<T>(Value.Count);
            foreach (var child in Value)
            {
                if (child is JsonNode childNode)
                {
                    node.Value.Add((T)childNode.Clone());
                }
                else
                {
                    node.Value.Add(child);
                }
            }
        }

        return node;
    }

    public override void Serialize(Stream stream, StringBuilder builder, JsonSerializeOptions options, int depth)
    {
        // depth, nodetype, key, value
        builder.Clear();
        builder.Append(depth).Append(options.COMMA);
        builder.Append((int)NodeType).Append(options.COMMA);
        builder.Append(Key ?? options.NullValue).Append(options.COMMA);

        if (NodeType is NodeType.ArrayValue)
        {
            builder.Append((char)ValueType.ArrayValue).Append(options.COMMA);
            if (Value is { Count: > 0 })
            {
                for (var i = 0; i < Value.Count; i++)
                {
                    if (i == 0)
                    {
                        JSON.SerializeValue(Value[i], builder, options);
                        continue;
                    }
                    builder.Append(options.COMMA);
                    JSON.SerializeValueWithoutType(Value[i], builder, options);
                }
            }
            builder.Append('\n');
            stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));
        }
        else
        {
            builder.Append((char)ValueType.ArrayObject).Append(options.COMMA);
            builder.Append('\n');
            stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));
            if (Value is { Count: > 0 })
            {
                foreach (var v in Value)
                {
                    var node = v as JsonNode;
                    node?.Serialize(stream, builder, options, depth + 1);
                }
            }
        }
    }

    internal override void ToJson(StringBuilder builder, bool appendKey = true)
    {
        if (Key != null && appendKey)
        {
            builder.Append('"').Append(Key).Append('"').Append(':');
        }
        
        if (NodeType == NodeType.ArrayValue)
        {
            builder.Append('[');
            if (Value is { Count: > 0 })
            {
                for (var i = 0; i < Value.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    JSON.GetValue(Value[i], builder);
                }
            }
            builder.Append(']');
        }
        else if (NodeType == NodeType.ArrayObject)
        {
            builder.Append('[');
            if (Value is { Count: > 0 })
            {
                for(var i = 0; i < Value.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }
                    var node = Value[i] as JsonNode;
                    node?.ToJson(builder);
                }
            }
            builder.Append(']');
        }
    }

    internal override bool TryGetNode(Span<string> keys, int index, out JsonNode? node, out int pos)
    {
        var key = keys[index];
        if (JSON.JsonOptions.Value!.RecursiveMode && !key.StartsWith("$"))
        {
            // 递归模式才会出现该情况
            // 非索引下标，直接和当前Key比较
            if (Key?.Equals(key, StringComparison.Ordinal) ?? false)
            {
                if (index == keys.Length - 1)
                {
                    // 搜索终结了
                    node = this;
                    pos = -1;
                    return true;
                }
                // 继续在当前节点搜索
                return TryGetNode(keys, ++index, out node, out pos);
            }

            node = null;
            pos = -1;
            return false;
        }

        var num = int.Parse(key.AsSpan()[1..]);
        if (Value == null || Value.Count < num)
        {
            // 索引超过下标
            node = null;
            pos = -1;
            return true;
        }

        if (NodeType == NodeType.ArrayValue)
        {
            // 值类型数值节点，直接返回下标
            node = this;
            pos = num - 1;
            return true;
        }

        // 对象类型节点
        if (index == keys.Length - 1)
        {
            // 索引终点了
            node = Value[num - 1] as JsonNode;
            pos = -1;
            return true;
        }

        // 指定下标位置继续搜索
        (Value[num - 1] as JsonNode)!.TryGetNode(keys, ++index, out node, out pos);
        return true;
    }
}