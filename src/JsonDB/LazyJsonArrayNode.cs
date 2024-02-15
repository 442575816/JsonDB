using System.Text;
using System.Text.Json;

namespace JsonDB;

public class LazyJsonArrayNode : JsonValueNode<JsonElement>
{
    public override int Count => Value.GetArrayLength();

    private JsonNode? _node;

    /// <summary>
    /// 添加值
    /// </summary>
    /// <param name="value"></param>
    internal override void AddValue<TBase>(TBase value)
    {
        _node ??= ParseLazyNode();
        _node.AddValue(value);
    }

    /// <summary>
    /// 替换指定下标的值
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="newValue"></param>
    internal override void ReplaceValue(int pos, JsonNode newValue)
    {
        _node ??= ParseLazyNode();
        _node.ReplaceValue(pos, newValue);
    }

    /// <summary>
    /// 替换节点
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    internal override void ReplaceValue<TBase>(TBase oldValue, TBase newValue)
    {
        _node ??= ParseLazyNode();
        _node.ReplaceValue(oldValue, newValue);
    }

    public override IEnumerator<JsonNode> GetEnumerator()
    {
        _node ??= ParseLazyNode(false);
        return _node.GetEnumerator();
    }
    
    internal override JsonNode ParseLazyNode(bool enableLazy = true)
    {
        var node = JSON.ParseArrayNode(Value, Key, enableLazy: enableLazy);
        Parent?.ReplaceValue(this, node);
        return node;
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    /// <param name="pos"></param>
    internal override void RemoveValue(int pos)
    {
        _node ??= ParseLazyNode();
        _node.RemoveValue(pos);
    }

    internal override void RemoveValue<TBase>(TBase value)
    {
        _node ??= ParseLazyNode();
        _node.RemoveValue(value);
    }

    public override bool Add<TBase>(TBase value)
    {
        _node ??= ParseLazyNode();
        return _node.Add(value);
    }

    public override bool AddJson(string json)
    {
        _node ??= ParseLazyNode();
        return _node.AddJson(json);
    }

    public override bool Add<TBase>(string key, TBase value)
    {
        _node ??= ParseLazyNode();
        return _node.Add(key, value);
    }

    public override bool AddJson(string key, string json)
    {
        _node ??= ParseLazyNode();
        return _node.Add(key, json);
    }

    protected internal override bool InternalRemove(int pos, string lastKey)
    {
        _node ??= ParseLazyNode();
        return _node.InternalRemove(pos, lastKey);
    }

    protected internal override TBase InternalGetValue<TBase>(int pos, string lastKey)
    {
        _node ??= ParseLazyNode();
        return _node.InternalGetValue<TBase>(pos, lastKey);
    }

    protected internal override void InternalSetValue<TBase>(int pos, TBase value)
    {
        _node ??= ParseLazyNode();
        _node.InternalSetValue(pos, value);
    }
    
    public override object Clone()
    {
        return new LazyJsonArrayNode { Key = Key, Value = Value, NodeType = NodeType };
    }

    public override void Serialize(Stream stream, StringBuilder builder, JsonSerializeOptions options, int depth)
    {
        // depth, nodetype, key, value
        builder.Clear();
        builder.Append(depth).Append(options.COMMA);
        builder.Append((int)NodeType.LazyArray).Append(options.COMMA);
        builder.Append(Key ?? options.NullValue).Append(options.COMMA);
        builder.Append((char)ValueType.String).Append(options.COMMA);
        builder.Append(Value.GetRawText());
        builder.Append('\n');
    }

    internal override void ToJson(StringBuilder builder, bool appendKey = true)
    {
        if (Key != null && appendKey)
        {
            builder.Append('"').Append(Key).Append('"').Append(':');
        }

        var text = Value.GetRawText().Replace("\n", "").Replace(" ", "");
        builder.Append(text);
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

        _node ??= ParseLazyNode();
        return _node.TryGetNode(keys, index, out node, out pos);
    }
}
