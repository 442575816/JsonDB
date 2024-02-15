using System.Text;
using System.Text.Json;

namespace JsonDB;

public class LazyJsonValueNode : JsonValueNode<JsonNode>
{
    public new required JsonElement Value { get; init; }

    private JsonNode? _node;

    protected internal override TBase InternalGetValue<TBase>(int pos, string lastKey)
    {
        _node ??= ParseLazyNode();
        return _node.InternalGetValue<TBase>(pos, lastKey);
    }

    internal override JsonNode ParseLazyNode(bool enableLazy = true)
    {
        var node = new JsonValueNode<JsonNode> {Key = Key, NodeType = NodeType};
        JSON.InternalParseNode(Value, parent: node, enableLazy: enableLazy);
        Parent?.ReplaceValue(this, node);
        return node;
    }

    protected internal override (JsonNode? node, int pos) SearchChilds(string key, JsonOptions options)
    {
        _node ??= ParseLazyNode();
        return _node.SearchChilds(key, options);
    }

    protected internal override bool SearchChilds(Span<string> keys, int index, JsonOptions options, out JsonNode? node, out int pos)
    {
        _node ??= ParseLazyNode();
        return _node.SearchChilds(keys, index, options, out node, out pos);
    }

    protected internal override void InternalSetValue<TBase>(int pos, TBase value)
    {
        _node ??= ParseLazyNode();
        _node.InternalSetValue(pos, value);
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
        return _node.AddJson(key, json);
    }

    protected internal override bool InternalRemove(int pos, string lastKey)
    {
        Parent?.RemoveValue(pos);
        return true;
    }

    public override object Clone()
    {
        var node = new LazyJsonValueNode{Key=Key, NodeType = NodeType, Value = Value};
        return node;
    }
    
    

    public override void Serialize(Stream stream, StringBuilder builder, JsonSerializeOptions options, int depth)
    {
        // depth, nodetype, key, value
        builder.Clear();
        builder.Append(depth).Append(options.COMMA);
        builder.Append((int)NodeType.LazyObject).Append(options.COMMA);
        builder.Append(Key ?? options.NullValue).Append(options.COMMA);
        builder.Append((char)ValueType.String).Append(options.COMMA);
        builder.Append(Value.GetRawText());
        stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));
        builder.Append('\n');
    }

    internal override void ToJson(StringBuilder builder, bool appendKey = true)
    {
        if (appendKey)
        {
            builder.Append('"').Append(Key).Append('"').Append(':');
        }

        var text = Value.GetRawText().Replace("\n", "").Replace(" ", "");
        builder.Append(text);
    }
}
