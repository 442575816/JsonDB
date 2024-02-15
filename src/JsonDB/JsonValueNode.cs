using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace JsonDB;

public class JsonValueNode<T> : JsonNode
{
    public T? Value { get; set; }

    protected internal override TBase InternalGetValue<TBase>(int pos, string lastKey)
    {
        if (NodeType == NodeType.Value)
        {
            var v = Value;
            if (v is null)
            {
                return default!;
            }
            return JSON.CastTo<T, TBase>(ref v)!;
        }

        var _this = this;
        return Unsafe.As<JsonValueNode<T>, TBase>(ref _this);
    }

    protected internal override void InternalSetValue<TBase>(int pos, TBase value)
    {
        if (NodeType == NodeType.Value)
        {
            Value = JSON.CastTo<TBase, T>(ref value);
        }
        else if (NodeType == NodeType.Object)
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
                node.Parent = Parent;
                Parent?.ReplaceValue(pos, node);
            }
        }
    }

    public override bool Add<TBase>(TBase value)
    {
        throw new Exception("unsupported operation");
    }

    public override bool AddJson(string json)
    {
        throw new Exception("unsupported operation");
    }

    public override bool Add<TBase>(string key, TBase value)
    {
        JSON.CreateJsonNode(key, value, this);
        return true;
    }

    public override bool AddJson(string key, string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            var node = JSON.ParseNode(root, key);
            AddChild(node);
        }
        else
        {
            var node = JSON.ParseArrayNode(root, key);
            AddChild(node);
        }

        return true;
    }

    protected internal override bool InternalRemove(int pos, string lastKey)
    {
        Parent?.RemoveValue(pos);
        return true;
    }

    public override object Clone()
    {
        var node = new JsonValueNode<T>{Key=Key, NodeType = NodeType, Value = Value};
        if (ChildNodes is not null)
        {
            node.ChildNodes = new List<JsonNode>(ChildNodes.Count);
            foreach (var childNode in ChildNodes)
            {
                node.AddChild((childNode.Clone() as JsonNode)!);
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
        JSON.SerializeValue(Value, builder, options);
        builder.Append('\n');
        stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));

        if (NodeType is NodeType.Object)
        {
            if (ChildNodes is { Count: > 0 })
            {
                foreach (var node in ChildNodes)
                {
                    node.Serialize(stream, builder, options, depth + 1);
                }
            }
        }
    }

    internal override void ToJson(StringBuilder builder, bool appendKey = true)
    {
        if (NodeType == NodeType.Value)
        {
            if (appendKey)
            {
                builder.Append('"').Append(Key).Append('"').Append(':');
            }
            JSON.GetValue(Value, builder);
        }
        else if (NodeType is NodeType.Object)
        {
            if (Key != null && appendKey)
            {
                builder.Append('"').Append(Key).Append('"').Append(':');
            }
            builder.Append('{');
            if (ChildNodes is { Count: > 0 })
            {
                for(var i = 0; i < ChildNodes.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }
                    var node = ChildNodes[i];
                    node.ToJson(builder);
                }
            }
            builder.Append('}');
        }
    }
}