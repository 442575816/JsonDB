// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace JsonDB;

public class JSONTable : IEnumerable<JsonNode>
{
    /// <summary>
    /// 内部Json
    /// </summary>
    private JsonNode _root;

    private JsonNode _tableNode;

    private readonly string _tableName;

    private readonly Dictionary<string, JsonNode> _mainTable = new(); // 内存主表
    private readonly Dictionary<string, JsonIndexManager<JsonNode>> _indexTable = new(); // 索引表

    private JSONTable(string tableName)
    {
        _tableName = tableName;
        _root = JSON.CreateRootNode();
    }

    public static JSONTable Create(string tableName)
    {
        var table = new JSONTable(tableName);
        return table;
    }

    public void AddIndex(string indexName, bool unique, params string[] keys)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        JsonIndexManager<JsonNode> manager;
        if (unique)
        {
            manager = new BTreeJsonIndexManager<JsonNode>(this, indexName, keys);
        }
        else
        {
            manager = new MultiBTreeJsonIndexManager<JsonNode>(this, indexName, keys);
        }
        _indexTable[indexName] = manager;
    }

    public T Get<T>(int index)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayValue);
        return _tableNode.Get<T>($"${index + 1}");
    }

    public JsonNode Get(string id)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        if (_mainTable.TryGetValue(id, out var node))
        {
            return node;
        }

        return null;
    }

    public JsonNode Get(int index)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        return _tableNode.Get<JsonNode>($"${index + 1}");
    }

    public object Find(string keyName, params object[] args)
    {
        if (!_indexTable.TryGetValue(keyName, out var indexManager))
        {
            throw new Exception($"unknown key:{keyName}");
        }

        return indexManager.Find(args);
    }

    public List<JsonNode> LeftFind(string keyName, params object[] args)
    {
        if (!_indexTable.TryGetValue(keyName, out var indexManager))
        {
            throw new Exception($"unknown key:{keyName}");
        }

        return indexManager.LeftFind(args);
    }

    public void Set<T>(JsonNode node, string queryKey, T value)
    {
        var clone = JSON.Parse(node.ToString());
        node.Set<T>(queryKey, value);

        var id = node.Get<string>("_id");
        InternalUpdate(id, clone, node);
    }

    public void Insert<T>(T value)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode.Add(value);
    }

    public void Insert<T>(params T[] value)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        foreach (var v in value)
        {
            _tableNode.Add(v);
        }
    }

    public JsonNode Insert(string json)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new Exception("json must must an object");
        }

        var node = JSON.ParseNode(root);
        var id = Guid.NewGuid().ToString();
        _tableNode.AddValue(node);
        node.Add("_id", id);

        InternalInsert(id, node);
        return node;
    }

    public void Insert(JsonNode node)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        if (node.NodeType != NodeType.Object)
        {
            throw new Exception("node must be an object");
        }
        var id = Guid.NewGuid().ToString();
        _tableNode.AddValue(node);
        node.Add("_id", id);

        InternalInsert(id, node);
    }

    public void Update<T>(T old, T newValue)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode.ReplaceValue(old, newValue);
    }

    public void Update<T>(int index, T newValue)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode.Set($"${index + 1}", newValue);
    }

    public void Update(string id, string json)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        if (_mainTable.TryGetValue(id, out var old))
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new Exception("json must must an object");
            }

            var node = JSON.ParseNode(root);
            _tableNode.ReplaceValue(old, node);
            node.Add("_id", id);

            InternalUpdate(id, old, node);
        }
    }

    public void Update(string id, JsonNode node)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        if (_mainTable.TryGetValue(id, out var old))
        {
            if (node.NodeType != NodeType.Object)
            {
                throw new Exception("node must be an object");
            }

            _tableNode.ReplaceValue(old, node);
            node.Add("_id", id);

            InternalUpdate(id, old, node);
        }
    }

    public void Update(int index, string json)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        var old = _tableNode.Get<JsonNode>($"${index + 1}");
        if (null != old)
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new Exception("json must must an object");
            }

            var id = old.Get<string>("_id");
            var node = JSON.ParseNode(root);
            _tableNode.Set($"${index + 1}", node);
            node.Add("_id", id);

            InternalUpdate(id, old, node);
        }
    }

    public void Update(int index, JsonNode node)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        var old = _tableNode.Get<JsonNode>($"${index + 1}");
        if (null != old)
        {
            if (node.NodeType != NodeType.Object)
            {
                throw new Exception("node must be an object");
            }

            var id = old.Get<string>("_id");
            _tableNode.Set($"${index + 1}", node);
            node.Add("_id", id);

            InternalUpdate(id, old, node);
        }
    }

    public void Delete<T>(T value)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode.RemoveValue(value);
    }

    public void Delete(string id)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        if (_mainTable.TryGetValue(id, out var node))
        {
            node.Parent?.RemoveValue(node);

            InternalDelete(id, node);
        }
    }

    public void Delete(JsonNode node)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        var id = node.Get<string>("_id");
        if (null != id)
        {
            Delete(id);
        }
    }

    public JsonNode Table()
    {
        return _tableNode;
    }

    public List<JsonNode> Models()
    {
        return (_tableNode as JsonArrayNode<JsonNode>)?.Value ?? new List<JsonNode>();
    }

    public IEnumerator<JsonNode> GetEnumerator()
    {
        return _tableNode?.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return _root.ToString();
    }

    public void Serialize(string filePath, bool compress = true)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using Stream wrapperStream = compress ? new GZipStream(stream, CompressionLevel.Fastest) : stream;
        var builder = new StringBuilder();
        _root.Serialize(wrapperStream, builder, 0);
        wrapperStream.Flush();
        wrapperStream.Close();
    }

    public void Load(string filePath, bool compress = true)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using Stream wrapperStream = compress ? new GZipStream(stream, CompressionMode.Decompress) : stream;
        using var reader = new StreamReader(new BufferedStream(wrapperStream, bufferSize:8192));
        string line = null;
        var nodeStack = new System.Collections.Generic.Stack<JsonNode>();
        _root = null;

        // load时候关闭排序
        var srcJsonOptions = JSON.JsonOptions.Value;
        JSON.JsonOptions.Value = new JsonOptions { Sort = false };

        var serializeOptions = JSON.JsonSerializeOptions.Value;
        var split = new[] { JSON.JsonSerializeOptions.Value.COMMA };
        while ((line = reader.ReadLine()) != null)
        {
            var node = ParseNode(ref line, nodeStack, ref split, serializeOptions);
            _root ??= node;
        }

        // 还原设置
        JSON.JsonOptions.Value = srcJsonOptions;
        _tableNode = _root.GetNode(_tableName);
    }

    private JsonNode ParseNode(ref string line, System.Collections.Generic.Stack<JsonNode> nodeStack, ref char[] split, JsonSerializeOptions serializeOptions)
    {
        var array = line.Split(split);
        var depth = int.Parse(array[0]);
        var nodeType = array[1][0] - '0';
        var key = serializeOptions.NullValue.Equals(array[2], StringComparison.Ordinal) ? null : array[2];
        var valueType = array[3][0];
        var value = serializeOptions.NullValue.Equals(array[4], StringComparison.Ordinal) ? null : array[4];;
        JsonNode node = null;
        JsonNode parent = null;
        switch (nodeType)
        {
            case (int)NodeType.Value:
                // Value
                node = valueType switch
                {
                    (char)ValueType.Object =>
                        // object
                        new JsonValueNode<object>
                        {
                            Key = key, NodeType = NodeType.Value, Value = GetValue<object>(value, valueType)
                        },
                    (char)ValueType.String =>
                        // string
                        new JsonValueNode<string>
                        {
                            Key = key, NodeType = NodeType.Value, Value = GetValue<string>(value, valueType)
                        },
                    (char)ValueType.Int =>
                        // int
                        new JsonValueNode<int>
                        {
                            Key = key, NodeType = NodeType.Value, Value = GetValue<int>(value, valueType)
                        },
                    (char)ValueType.Long =>
                        // long
                        new JsonValueNode<long>
                        {
                            Key = key, NodeType = NodeType.Value, Value = GetValue<long>(value, valueType)
                        },
                    (char)ValueType.Double =>
                        // double
                        new JsonValueNode<double>
                        {
                            Key = key, NodeType = NodeType.Value, Value = GetValue<double>(value, valueType)
                        },
                    (char)ValueType.Bool =>
                        // bool
                        new JsonValueNode<bool>
                        {
                            Key = key, NodeType = NodeType.Value, Value = GetValue<bool>(value, valueType)
                        },
                    _ => null
                };

                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
                break;
            case (int)NodeType.Object:
                // Object
                if (depth < nodeStack.Count)
                {
                    nodeStack.Pop();
                }
                node = new JsonValueNode<object> { Key = key, NodeType = NodeType.Object };
                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
                nodeStack.Push(node);
                break;
            case (int)NodeType.ArrayObject:
                // ArrayObject
                if (depth < nodeStack.Count)
                {
                    nodeStack.Pop();
                }
                node = new JsonArrayNode<JsonNode> { Key = key, NodeType = NodeType.ArrayObject };
                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
                nodeStack.Push(node);
                break;
        }
        return node;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void AddChild(JsonNode node, JsonNode parent)
    {
        if (null == parent)
        {
            return;
        }
        if (parent.NodeType is NodeType.Object)
        {
            parent.AddChild(node);
        }
        else
        {
            parent.AddValue(node);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private T GetValue<T>(string value, char valueType)
    {
        if (null == value)
        {
            return default;
        }

        switch (valueType)
        {
            case (char)ValueType.Object:
                // object
                return JSON.CastTo<string, T>(ref value);
            case (char)ValueType.String:
                // string
                return JSON.CastTo<string, T>(ref value);
            case (char)ValueType.Int:
                // int
                var iv = int.Parse(value);
                return JSON.CastTo<int, T>(ref iv);
            case (char)ValueType.Long:
                // long
                var lv = long.Parse(value);
                return JSON.CastTo<long, T>(ref lv);
            case (char)ValueType.Double:
                // double
                var dv = double.Parse(value);
                return JSON.CastTo<double, T>(ref dv);
            case (char)ValueType.Bool:
                // bool
                var bv = bool.Parse(value);
                return JSON.CastTo<bool, T>(ref bv);
        }

        return default;
    }

    private void CheckTableNode<T>(NodeType nodeType)
    {
        if (_tableNode == null)
        {
            _tableNode = JSON.CreateJsonArrayNode<T>(_tableName, _root);
        } else if (_tableNode.NodeType != nodeType)
        {
            throw new Exception($"excepted node type {nodeType} but {_tableNode.NodeType}");
        }
    }

    private void InternalInsert(string id, JsonNode node)
    {

        if (_mainTable.TryAdd(id, node))
        {
            foreach (var pair in _indexTable)
            {
                pair.Value.Insert(node);
            }
        }
        else
        {
            _mainTable[id] = node;
        }
    }

    private void InternalUpdate(string id, JsonNode oldNode, JsonNode newNode)
    {
        foreach (var pair in _indexTable)
        {
            pair.Value.Update(oldNode, newNode);
        }
        _mainTable[id] = newNode;
    }

    private void InternalDelete(string id, JsonNode node)
    {
        foreach (var pair in _indexTable)
        {
            pair.Value.Remove(node);
        }
        _mainTable.Remove(id);
    }
}
