// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace DotNetGameFramework.JsonDB;

/// <summary>
/// 节点类型定义
/// </summary>
public enum NodeType
{
    Value,
    Object,
    ArrayValue,
    ArrayObject
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
    ArrayObject
}

/// <summary>
/// JsonNode的动态对象
/// </summary>
public class DynamicJsonNode : DynamicObject
{
    /// <summary>
    /// 内部Node
    /// </summary>
    private JsonNode _node;

    public DynamicJsonNode(JsonNode node)
    {
        this._node = node;
    }

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
        if (indexes.Length == 1 && indexes[0] != null)
        {
            var key = $"${(int)indexes[0] + 1}";
            return TryGetValue(key, out result);
        }
        return base.TryGetIndex(binder, indexes, out result);
    }

    public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
    {
        if (indexes.Length == 1 && indexes[0] != null)
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
            if (result is JsonNode node)
            {
                result = new DynamicJsonNode(node);
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

public abstract class JsonNode : IEnumerable<JsonNode>, IComparable<JsonNode>
{
    public string Key { get; set; }
    public NodeType NodeType { get; set; }
    public List<JsonNode> ChildNodes { get; set; }
    public JsonNode Parent { get; set; }
    public virtual int Count { get; set; }

    /// <summary>
    /// 增加子节点
    /// </summary>
    /// <param name="node"></param>
    internal void AddChild(JsonNode node)
    {
        ChildNodes ??= new List<JsonNode>();

        if (JSON.JsonOptions.Value.Sort)
        {
            // 排序插入
            var index = ChildNodes.BinarySearch(node);
            if (index > 0)
            {
                ChildNodes[index] = node;
            }
            else
            {
                index = ~index;
                ChildNodes.Insert(index, node);
            }
        }
        else
        {
            ChildNodes.Add(node);
        }

        node.Parent = this;
    }

    /// <summary>
    /// 添加值
    /// </summary>
    /// <param name="value"></param>
    internal virtual void AddValue<T>(T value)
    {
    }

    /// <summary>
    /// 替换节点
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="newValue"></param>
    internal virtual void ReplaceValue(int pos, JsonNode newValue)
    {
        ChildNodes[pos] = newValue;
    }

    /// <summary>
    /// 替换节点
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    internal virtual void ReplaceValue<T>(T oldValue, T newValue)
    {
        var ov = JSON.CastTo<T, JsonNode>(ref oldValue);
        var nv = JSON.CastTo<T, JsonNode>(ref newValue);
        if (ov != null && nv != null)
        {
            for (var i = 0; i < ChildNodes.Count; i++)
            {
                if (ChildNodes[i] == ov)
                {
                    ChildNodes[i] = nv;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    /// <param name="pos"></param>
    internal virtual void RemoveValue(int pos)
    {
        ChildNodes.RemoveAt(pos);
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    /// <param name="value">指定的值</param>
    internal virtual void RemoveValue<T>(T value)
    {
        var v = JSON.CastTo<T, JsonNode>(ref value);
        if (v != null)
        {
            ChildNodes.Remove(v);
        }
    }

    /// <summary>
    /// 内部获取值的方法
    /// </summary>
    /// <param name="pos"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected abstract T InternalGetValue<T>(int pos, string lastKey);

    /// <summary>
    /// 内部设置值的方法
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    protected abstract void InternalSetValue<T>(int pos, T value);

    /// <summary>
    /// 移除节点
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="lastKey"></param>
    protected abstract bool InternalRemove(int pos, string lastKey);

    /// <summary>
    /// 内部ToString方法
    /// </summary>
    /// <param name="builder"></param>
    internal abstract void InternalToString(StringBuilder builder);

    /// <summary>
    /// 查找json节点的值
    /// </summary>
    /// <param name="queryKey"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetValue<T>(string queryKey, out T value)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetValue(keys, 0, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 查找json节点的值
    /// </summary>
    /// <returns></returns>
    public T Get<T>()
    {
        return InternalGetValue<T>(0, "");
    }

    /// <summary>
    /// 查找json节点的值
    /// </summary>
    /// <returns></returns>
    public T Get<T>(string queryKey)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetValue(keys, 0, out T value))
        {
            return value;
        }

        return default;
    }

    /// <summary>
    /// 查找json节点
    /// </summary>
    /// <returns></returns>
    public JsonNode GetNode(string queryKey)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetNode(keys, 0, out var node, out _))
        {
            return node;
        }
        return null;
    }

    /// <summary>
    /// 设置节点的值
    /// </summary>
    /// <param name="queryKey"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool Set<T>(string queryKey, T value)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (SetValue(keys, 0, value))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 添加值
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public abstract bool Add<T>(T value);

    /// <summary>
    /// 添加值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public abstract bool Add<T>(string key, T value);

    /// <summary>
    /// 添加值
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public abstract bool AddJson(string json);

    /// <summary>
    /// 添加值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="json"></param>
    /// <returns></returns>
    public abstract bool AddJson(string key, string json);

    /// <summary>
    /// 添加节点
    /// </summary>
    /// <returns></returns>
    public bool Append<T>(string queryKey, T value)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetNode(keys, 0, out var node, out _))
        {
            return node?.Add(value) ?? false;
        }
        return false;
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    /// <returns></returns>
    public bool Append<T>(string queryKey, string key, T value)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetNode(keys, 0, out var node, out _))
        {
            return node?.Add(key, value) ?? false;
        }
        return false;
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    /// <returns></returns>
    public bool AppendJson(string queryKey, string json)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetNode(keys, 0, out var node, out _))
        {
            return node?.AddJson(json) ?? false;
        }
        return false;
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    /// <returns></returns>
    public bool AppendJson(string queryKey, string key, string json)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetNode(keys, 0, out var node, out _))
        {
            return node?.AddJson(key, json) ?? false;
        }
        return false;
    }

    /// <summary>
    /// 移除节点
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool Remove(string queryKey)
    {
        var keys = queryKey.Split(new[] {'.'});
        if (TryGetNode(keys, 0, out var node, out var pos))
        {
            return node?.InternalRemove(pos, keys[^1]) ?? false;
        }
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public virtual IEnumerator<JsonNode> GetEnumerator()
    {
        return ChildNodes?.GetEnumerator();
    }

    public int CompareTo(JsonNode other)
    {
        return string.CompareOrdinal(this.Key, other?.Key);
    }

    public abstract void Serialize(Stream stream, StringBuilder builder, int depth);

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('{');
        InternalToString(builder);
        builder.Append('}');
        return builder.ToString();
    }

    /// <summary>
    /// 获取指定节点的值
    /// </summary>
    /// <param name="keys">搜索keys路径</param>
    /// <param name="index">当前搜索到的节点</param>
    /// <param name="value">修改成的值</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal bool TryGetValue<T>(Span<string> keys, int index, out T value)
    {
        if (TryGetNode(keys, index, out var node, out var pos))
        {
            value = node != null ? node.InternalGetValue<T>(pos, keys[^1]) : default;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 尝试查找指定节点
    /// </summary>
    /// <param name="keys">搜索keys路径</param>
    /// <param name="index">当前搜索到的节点</param>
    /// <param name="node">输出node</param>
    /// <param name="pos">搜索到的位置</param>
    /// <returns></returns>
    internal virtual bool TryGetNode(Span<string> keys, int index, out JsonNode node, out int pos)
    {
        if (JSON.JsonOptions.Value.RecursiveMode)
        {
            return TryGetNodeRecursiveMode(keys, index, out node, out pos);
        }

        return TryGetNodeLoopMode(keys, index, out node, out pos);
    }

    /// <summary>
    /// 尝试查找指定节点 循环模式
    /// </summary>
    /// <param name="keys">搜索keys路径</param>
    /// <param name="index">当前搜索到的节点</param>
    /// <param name="node">输出node</param>
    /// <param name="pos">搜索到的位置</param>
    /// <returns></returns>
    internal bool TryGetNodeLoopMode(Span<string> keys, int index, out JsonNode node, out int pos)
    {
        var options = JSON.JsonOptions.Value;
        var curr = this;
        while (curr != null)
        {
            if (index >= keys.Length)
            {
                break;
            }
            var key = keys[index];
            var nodeType = curr.NodeType;
            if (nodeType == NodeType.Value)
            {
                if (Key?.Equals(key, StringComparison.Ordinal) ?? false)
                {
                    node = this;
                    pos = -1;
                    return true;
                }
            }
            else if (nodeType is NodeType.Object)
            {
                // 如果当前节点
                (curr, var findPos) = curr.SearchChilds(key, options);
                if (curr != null && index == keys.Length - 1)
                {
                    // 搜索终结了
                    node = curr;
                    pos = findPos;
                    return true;
                }
                index++;
            }
            else
            {
                // 数组类型
                if (curr.TryGetNode(keys, index, out node, out pos))
                {
                    return true;
                }
            }
        }

        // 没找到
        node = null;
        pos = -1;
        return false;
    }

    /// <summary>
    /// 尝试查找指定节点，递归模式
    /// </summary>
    /// <param name="keys">搜索keys路径</param>
    /// <param name="index">当前搜索到的节点</param>
    /// <param name="node">输出node</param>
    /// <param name="pos">搜索到的位置</param>
    /// <returns></returns>
    internal bool TryGetNodeRecursiveMode(Span<string> keys, int index, out JsonNode node, out int pos)
    {
        // 递归模式
        var options = JSON.JsonOptions.Value;
        var key = keys[index];
        if (NodeType == NodeType.Value)
        {
            if (Key?.Equals(key, StringComparison.Ordinal) ?? false)
            {
                node = this;
                pos = -1;
                return true;
            }
        } else
        {
            if (Key?.Equals(key, StringComparison.Ordinal) ?? false)
            {
                if (index == keys.Length - 1)
                {
                    // 搜索终结
                    node = this;
                    pos = -1;
                    return true;
                }

                index++;
            }
            // 子节点中查找
            return SearchChilds(keys, index, options, out node, out pos);
        }

        // 没找到
        node = null;
        pos = -1;
        return false;
    }

    private (JsonNode node, int pos) SearchChilds(string key, JsonOptions options)
    {
        if (ChildNodes == null || ChildNodes.Count == 0)
        {
            return (null, -1);
        }

        if (options.Sort && options.BinarySearch)
        {
            var temp = options.SearchNode;
            temp.Key = key;
            var searchIndex = ChildNodes.BinarySearch(temp);
            if (searchIndex >= 0)
            {
                return (ChildNodes[searchIndex], searchIndex);
            }
        }
        else
        {
            for (var i = 0; i < ChildNodes.Count; i++)
            {
                var cnode = ChildNodes[i];
                if (cnode.Key?.Equals(key, StringComparison.Ordinal) ?? false)
                {
                    return (cnode, i);
                }
            }
        }

        return (null, -1);
    }

    private bool SearchChilds(Span<string> keys, int index, JsonOptions options, out JsonNode node, out int pos)
    {
        if (ChildNodes == null || ChildNodes.Count == 0)
        {
            node = null;
            pos = -1;
            return false;
        }

        if (options.Sort && options.BinarySearch)
        {
            var temp = options.SearchNode;
            temp.Key = keys[index];
            var searchIndex = ChildNodes.BinarySearch(temp);
            if (searchIndex > 0)
            {
                var cnode = ChildNodes[searchIndex];
                if (cnode.TryGetNode(keys, index, out node, out pos))
                {
                    pos = (pos == -1) ? searchIndex : pos;
                    return true;
                }
            }
        }
        else
        {
            for (var i = 0; i < ChildNodes.Count; i++)
            {
                var cnode = ChildNodes[i];
                if (cnode.TryGetNode(keys, index, out node, out pos))
                {
                    pos = (pos == -1) ? i : pos;
                    return true;
                }
            }
        }

        node = null;
        pos = -1;
        return false;
    }

    /// <summary>
    /// 修改指定节点的值
    /// </summary>
    /// <param name="keys">搜索keys路径</param>
    /// <param name="index">当前搜索到的节点</param>
    /// <param name="value">修改成的值</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected bool SetValue<T>(Span<string> keys, int index, T value)
    {
        if (TryGetNode(keys, index, out var node, out var arrayIndex))
        {
            node?.InternalSetValue(arrayIndex, value);
            return true;
        }

        return false;
    }
}

public class JsonValueNode<T> : JsonNode
{
    public T Value { get; set; }

    protected override TBase InternalGetValue<TBase>(int pos, string lastKey)
    {
        if (NodeType == NodeType.Value)
        {
            var v = Value;
            return JSON.CastTo<T, TBase>(ref v);
        }

        var _this = this;
        return Unsafe.As<JsonValueNode<T>, TBase>(ref _this);
    }

    protected override void InternalSetValue<TBase>(int pos, TBase value)
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

    protected override bool InternalRemove(int pos, string lastKey)
    {
        Parent?.RemoveValue(pos);
        return true;
    }

    public override void Serialize(Stream stream, StringBuilder builder, int depth)
    {
        // depth, nodetype, key, value
        builder.Clear();
        builder.Append(depth).Append(JSON.JsonSerializeOptions.Value.COMMA);
        builder.Append((int)NodeType).Append(JSON.JsonSerializeOptions.Value.COMMA);
        builder.Append(Key ?? JSON.JsonSerializeOptions.Value.NullValue).Append(JSON.JsonSerializeOptions.Value.COMMA);
        JSON.GetSerializeValue(Value, builder);
        builder.Append('\n');
        stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));

        if (NodeType is NodeType.Object)
        {
            if (ChildNodes is { Count: > 0 })
            {
                foreach (var node in ChildNodes)
                {
                    node.Serialize(stream, builder, depth + 1);
                }
            }
        }
    }

    internal override void InternalToString(StringBuilder builder)
    {
        if (NodeType == NodeType.Value)
        {
            builder.Append('"').Append(Key).Append('"').Append(':');
            JSON.GetValue(Value, builder);
        }
        else if (NodeType is NodeType.Object)
        {
            if (Key != null)
            {
                builder.Append('"').Append(Key).Append('"').Append(':');
                builder.Append('{');
            }

            if (ChildNodes is { Count: > 0 })
            {
                for(var i = 0; i < ChildNodes.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }
                    var node = ChildNodes[i];
                    node.InternalToString(builder);
                }
            }

            if (Key != null)
            {
                builder.Append('}');
            }
        }
    }
}

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
        Value.Add(JSON.CastTo<TBase, T>(ref value));
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
        Value[pos] = Unsafe.As<JsonNode, T>(ref newValue);
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
            for (var i = 0; i < Value.Count; i++)
            {
                if (Value[i].Equals(ov))
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
            return (Value as List<JsonNode>)?.GetEnumerator();
        }

        return null;
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    /// <param name="pos"></param>
    internal override void RemoveValue(int pos)
    {
        Value.RemoveAt(pos);
    }

    internal override void RemoveValue<TBase>(TBase value)
    {
        var v = JSON.CastTo<TBase, T>(ref value);
        if (v != null)
        {
            Value.Remove(v);
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

    protected override bool InternalRemove(int pos, string lastKey)
    {
        if (lastKey.StartsWith("$"))
        {
            Value.RemoveAt(pos);
            return true;
        }
        Parent?.RemoveValue(pos);
        return true;
    }

    protected override TBase InternalGetValue<TBase>(int pos, string lastKey)
    {
        if (lastKey.StartsWith("$"))
        {
            var v = Value[pos];
            return JSON.CastTo<T, TBase>(ref v);
        }

        var _this = this;
        return Unsafe.As<JsonArrayNode<T>, TBase>(ref _this);
    }

    protected override void InternalSetValue<TBase>(int pos, TBase value)
    {
        if (NodeType == NodeType.ArrayValue)
        {
            Value[pos] = JSON.CastTo<TBase, T>(ref value);
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
                Value[pos] = Unsafe.As<JsonNode, T>(ref node);
            }
        }
    }

    public override void Serialize(Stream stream, StringBuilder builder, int depth)
    {
        // depth, nodetype, key, value
        builder.Clear();
        builder.Append(depth).Append(JSON.JsonSerializeOptions.Value.COMMA);
        builder.Append((int)NodeType).Append(JSON.JsonSerializeOptions.Value.COMMA);
        builder.Append(Key ?? JSON.JsonSerializeOptions.Value.NullValue).Append(JSON.JsonSerializeOptions.Value.COMMA);

        if (NodeType is NodeType.ArrayValue)
        {
            if (Value is { Count: > 0 })
            {
                for (var i = 0; i < Value.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(JSON.JsonSerializeOptions.Value.COMMA);
                    }
                    JSON.GetSerializeValue(Value, builder);
                }
            }
            builder.Append('\n');
            stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));
        }
        else
        {
            builder.Append((char)ValueType.ArrayObject).Append(JSON.JsonSerializeOptions.Value.COMMA);
            builder.Append('\n');
            stream.Write(Encoding.UTF8.GetBytes(builder.ToString()));
            if (Value is { Count: > 0 })
            {
                foreach (var v in Value)
                {
                    var node = v as JsonNode;
                    node?.Serialize(stream, builder, depth + 1);
                }
            }
        }
    }

    internal override void InternalToString(StringBuilder builder)
    {
        if (Key != null)
        {
            builder.Append('"').Append(Key).Append('"').Append(':');
        }

        builder.Append('[');
        if (NodeType == NodeType.ArrayValue)
        {
            if (Value is { Count: > 0 })
            {
                for (var i = 0; i < Value.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(Value[i]);
                }
            }
        }
        else if (NodeType == NodeType.ArrayObject)
        {
            if (Value is { Count: > 0 })
            {
                for(var i = 0; i < Value.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }
                    builder.Append('{');
                    var node = Value[i] as JsonNode;
                    node?.InternalToString(builder);
                    builder.Append('}');
                }
            }
        }

        builder.Append(']');
    }

    internal override bool TryGetNode(Span<string> keys, int index, out JsonNode node, out int pos)
    {
        var key = keys[index];
        if (JSON.JsonOptions.Value.RecursiveMode && !key.StartsWith("$"))
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
            pos = num - 1;
            return true;
        }

        // 指定下标位置继续搜索
        (Value[num - 1] as JsonNode).TryGetNode(keys, ++index, out node, out pos);
        return true;
    }
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
    public string GetValue(string value)
    {
        return NullValue.Equals(value, StringComparison.Ordinal) ? null : value;
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
    public static JsonNode ParseFile(string filePath)
    {
        var node = new JsonValueNode<object> { NodeType = NodeType.Object };

        var doc = JsonDocument.Parse(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new Exception("json must is object");
        }
        InternalParseNode(root, node);

        return node;
    }

    /// <summary>
    /// 解析Json
    /// </summary>
    /// <param name="json"></param>
    /// <exception cref="Exception"></exception>
    public static JsonNode Parse(string json)
    {
        var node = new JsonValueNode<object> { NodeType = NodeType.Object };

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new Exception("json must is object");
        }
        InternalParseNode(root, node);

        return node;
    }

    /// <summary>
    /// 解析节点
    /// </summary>
    /// <returns></returns>
    public static JsonNode ParseNode(JsonElement element, string elementKey = null, JsonNode parent = null)
    {
        var node = new JsonValueNode<object> { Key = elementKey, NodeType = NodeType.Object };
        InternalParseNode(element, node);
        parent?.AddChild(node);
        return node;
    }

    public static JsonNode CreateRootNode()
    {
        return new JsonValueNode<object> { NodeType = NodeType.Object };
    }

    public static JsonNode CreateJsonNode<T>(string key, T value = default, JsonNode parent = null)
    {
        var node = new JsonValueNode<T> { Key = key, Value = value, NodeType = NodeType.Value };
        parent?.AddChild(node);
        return node;
    }

    public static JsonNode CreateJsonArrayNode<T>(string key, JsonNode parent)
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
    internal static JsonNode InternalParseNode(JsonElement element, JsonNode parent = null)
    {
        JsonNode node = null;
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
                    node = ParseNode(value, key, parent);
                    break;
                case JsonValueKind.Array:
                    node = ParseArrayNode(value, key, parent);
                    break;

            }
        }
        return node;
    }

    /// <summary>
    /// 解析Json Array节点
    /// </summary>
    /// <returns></returns>
    internal static JsonNode ParseArrayNode(JsonElement element, string elementKey = null, JsonNode parent = null)
    {
        JsonNode jsonNode = null;
        for (var i = 0; i < element.GetArrayLength(); i++)
        {
            var item = element[i];
            switch (item.ValueKind)
            {
                case JsonValueKind.False:
                case JsonValueKind.True:
                {
                    jsonNode ??= new JsonArrayNode<bool> { Key = elementKey, NodeType = NodeType.ArrayValue };
                    (jsonNode as JsonArrayNode<bool>).AddValue(item.GetBoolean());
                }

                    break;
                case JsonValueKind.String:
                {
                    jsonNode ??= new JsonArrayNode<string> { Key = elementKey, NodeType = NodeType.ArrayValue };
                    (jsonNode as JsonArrayNode<string>).AddValue(item.GetString());
                }

                    break;
                case JsonValueKind.Number:
                    if (item.TryGetInt32(out var intValue))
                    {
                        jsonNode ??= new JsonArrayNode<int> { Key = elementKey, NodeType = NodeType.ArrayValue };
                        (jsonNode as JsonArrayNode<int>).AddValue(intValue);
                    }
                    else if (item.TryGetInt64(out var longValue))
                    {
                        jsonNode ??= new JsonArrayNode<long> { Key = elementKey, NodeType = NodeType.ArrayValue };
                        (jsonNode as JsonArrayNode<long>).AddValue(longValue);
                    }
                    else if (item.TryGetDouble(out var doubleValue))
                    {
                        jsonNode ??= new JsonArrayNode<double> { Key = elementKey, NodeType = NodeType.ArrayValue };
                        (jsonNode as JsonArrayNode<double>).AddValue(doubleValue);
                    }

                    break;
                case JsonValueKind.Null:
                {
                    jsonNode ??= new JsonArrayNode<object> { Key = elementKey, NodeType = NodeType.ArrayValue };
                    (jsonNode as JsonArrayNode<object>).AddValue((object)null);
                }

                    break;
                case JsonValueKind.Object:
                {
                    jsonNode ??= new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject };
                    (jsonNode as JsonArrayNode<JsonNode>).AddValue(ParseNode(item));
                }
                    break;
                case JsonValueKind.Array:
                {
                    jsonNode ??= new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject };
                    (jsonNode as JsonArrayNode<JsonNode>).AddValue(ParseArrayNode(item));
                }
                    break;
            }
        }

        jsonNode ??= new JsonArrayNode<JsonNode> { Key = elementKey, NodeType = NodeType.ArrayObject };
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
            builder.Append('"').Append(sv).Append('"');
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

    internal static void GetSerializeValue<T>(T value, StringBuilder builder)
    {
        if (value == null)
        {
            builder.Append((char)ValueType.Object).Append(JsonSerializeOptions.Value.COMMA);
            builder.Append(JsonSerializeOptions.Value.NullValue);
        }
        else if (value is string sv)
        {
            builder.Append((char)ValueType.String).Append(JsonSerializeOptions.Value.COMMA);
            builder.Append(sv);
        }
        else if (value is int iv)
        {
            builder.Append((char)ValueType.Int).Append(JsonSerializeOptions.Value.COMMA);
            builder.Append(iv);
        }
        else if (value is long lv)
        {
            builder.Append((char)ValueType.Long).Append(JsonSerializeOptions.Value.COMMA);
            builder.Append(lv);
        }
        else if (value is double dv)
        {
            builder.Append((char)ValueType.Double).Append(JsonSerializeOptions.Value.COMMA);
            builder.Append(dv);
        }
        else if (value is bool bv)
        {
            builder.Append((char)ValueType.Bool).Append(JsonSerializeOptions.Value.COMMA);
            builder.Append(bv);
        }
        else
        {
            builder.Append((char)ValueType.Object).Append(JsonSerializeOptions.Value.COMMA);
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
    internal static TTo CastTo<TFrom, TTo>(ref TFrom value)
    {
        if (typeof(TFrom) == typeof(TTo))
        {
            return Unsafe.As<TFrom, TTo>(ref value);
        }

        object v = value;
        return (TTo)v;
    }
}
