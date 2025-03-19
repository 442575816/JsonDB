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
    /// json 根节点
    /// </summary>
    private JsonNode _root;

    /// <summary>
    /// 内部table json
    /// </summary>
    private JsonNode? _tableNode;

    /// <summary>
    /// json库表名
    /// </summary>
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

    /// <summary>
    /// 添加索引
    /// </summary>
    /// <param name="indexName">索引名称</param>
    /// <param name="unique">是否是唯一索引</param>
    /// <param name="keys">索引对应的列名，大小写敏感</param>
    public void AddIndex(string indexName, bool unique, params string[] keys)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        JsonIndexManager<JsonNode> manager;
        if (unique)
        {
            manager = new BTreeJsonIndexManager<JsonNode>(this, indexName, keyProps: keys);
        }
        else
        {
            manager = new MultiBTreeJsonIndexManager<JsonNode>(this, indexName, keyProps:keys);
        }
        _indexTable[indexName] = manager;
    }
    
    /// <summary>
    /// 添加索引
    /// </summary>
    /// <param name="indexName">索引名称</param>
    /// <param name="unique">是否是唯一索引</param>
    /// <param name="keys">索引对应的列名，大小写敏感</param>
    public void AddIndex(string indexName, bool unique, Func<string, string, int> comparer, Func<string, string, int>? leftComparer, params string[] keys)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        JsonIndexManager<JsonNode> manager;
        if (unique)
        {
            manager = new BTreeJsonIndexManager<JsonNode>(this, indexName, comparer, leftComparer, keys);
        }
        else
        {
            manager = new MultiBTreeJsonIndexManager<JsonNode>(this, indexName, comparer, leftComparer, keys);
        }
        _indexTable[indexName] = manager;
    }

    /// <summary>
    /// 获取指定下标对应的节点
    /// </summary>
    /// <param name="index">下标</param>
    /// <typeparam name="T">指定返回值</typeparam>
    /// <returns></returns>
    public T? Get<T>(int index)
    {
        return _tableNode == null ? default : _tableNode.Get<T>($"${index + 1}");
    }

    /// <summary>
    /// 通过主键获取指定节点
    /// </summary>
    /// <param name="id">主键</param>
    /// <returns></returns>
    public JsonNode? Get(string id)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        return _mainTable.GetValueOrDefault(id);
    }

    /// <summary>
    /// 通过指定索引查找
    /// </summary>
    /// <param name="keyName">索引名称</param>
    /// <param name="args">查找参数</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public object? Find(string keyName, params object[] args)
    {
        if (!_indexTable.TryGetValue(keyName, out var indexManager))
        {
            throw new Exception($"unknown key:{keyName}");
        }

        return indexManager.Find(args);
    }

    /// <summary>
    /// 通过指定索引，进行左值匹配
    /// </summary>
    /// <param name="keyName">索引名称</param>
    /// <param name="args">查找参数</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<JsonNode> LeftFind(string keyName, params object[] args)
    {
        if (!_indexTable.TryGetValue(keyName, out var indexManager))
        {
            throw new Exception($"unknown key:{keyName}");
        }

        return indexManager.LeftFind(args);
    }

    public List<JsonNode> RangeFind(string keyName, object startValue, object endValue)
    {
        if (!_indexTable.TryGetValue(keyName, out var indexManager))
        {
            throw new Exception($"unknown key:{keyName}");
        }

        return indexManager.RangeFind(startValue, endValue);
    }
    
    public List<JsonNode> RangeFind(string keyName, object startValue, object endValue, Func<string, string, int> comparer)
    {
        if (!_indexTable.TryGetValue(keyName, out var indexManager))
        {
            throw new Exception($"unknown key:{keyName}");
        }

        return indexManager.RangeFind(startValue, endValue, comparer);
    }
    
    /// <summary>
    /// 往表中插入对象
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void Insert<T>(T value)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode?.Add(value);
    }
    
    /// <summary>
    /// 往表中批量插入对象
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void Insert<T>(params T[] value)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        foreach (var v in value)
        {
            _tableNode?.Add(v);
        }
    }

    /// <summary>
    /// 往表中插入json数据
    /// </summary>
    /// <param name="json">json数据文本</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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
        _tableNode?.AddValue(node);
        node.Add("_id", id);

        InternalInsert(id, node);
        return node;
    }

    /// <summary>
    /// 往表中插入json节点
    /// </summary>
    /// <param name="node"></param>
    /// <exception cref="Exception"></exception>
    public void Insert(JsonNode node)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        if (node.NodeType != NodeType.Object)
        {
            throw new Exception("node must be an object");
        }
        var id = Guid.NewGuid().ToString();
        _tableNode?.AddValue(node);
        node.Add("_id", id);

        InternalInsert(id, node);
    }

    /// <summary>
    /// 更新指定对象
    /// </summary>
    /// <param name="old"></param>
    /// <param name="newValue"></param>
    /// <typeparam name="T"></typeparam>
    public void Update<T>(T old, T newValue)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode?.ReplaceValue(old, newValue);
    }

    /// <summary>
    /// 将指定下标的值更新为新值
    /// </summary>
    /// <param name="index"></param>
    /// <param name="newValue"></param>
    /// <typeparam name="T"></typeparam>
    public void Update<T>(int index, T newValue)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode?.Set($"${index + 1}", newValue);
    }

    /// <summary>
    /// 将指定主键的值更新为新的json
    /// </summary>
    /// <param name="id"></param>
    /// <param name="json"></param>
    /// <exception cref="Exception"></exception>
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
            _tableNode?.ReplaceValue(old, node);
            node.Add("_id", id);

            InternalUpdate(id, old, node);
        }
    }
    
    /// <summary>
    /// 将指定主键的值更新为指定节点
    /// </summary>
    /// <param name="id"></param>
    /// <param name="node"></param>
    /// <exception cref="Exception"></exception>
    public void Update(string id, JsonNode node)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        if (_mainTable.TryGetValue(id, out var old))
        {
            if (node.NodeType != NodeType.Object)
            {
                throw new Exception("node must be an object");
            }

            _tableNode?.ReplaceValue(old, node);
            node.Add("_id", id);

            InternalUpdate(id, old, node);
        }
    }

    /// <summary>
    /// 根据下标更新指定节点
    /// </summary>
    /// <param name="index"></param>
    /// <param name="json"></param>
    /// <exception cref="Exception"></exception>
    public void Update(int index, string json)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        var old = _tableNode?.Get<JsonNode>($"${index + 1}");
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
            _tableNode?.Set($"${index + 1}", node);
            node.Add("_id", id);

            InternalUpdate(id!, old, node);
        }
    }

    /// <summary>
    /// 根据下标更新指定节点
    /// </summary>
    /// <param name="index"></param>
    /// <param name="node"></param>
    /// <exception cref="Exception"></exception>
    public void Update(int index, JsonNode node)
    {
        CheckTableNode<JsonNode>(NodeType.ArrayObject);
        var old = _tableNode?.Get<JsonNode>($"${index + 1}");
        if (null != old)
        {
            if (node.NodeType != NodeType.Object)
            {
                throw new Exception("node must be an object");
            }

            var id = old.Get<string>("_id");
            _tableNode?.Set($"${index + 1}", node);
            node.Add("_id", id);

            InternalUpdate(id!, old, node);
        }
    }
    
    /// <summary>
    /// 修改指定节点的属性为指定值
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="queryKey">属性key</param>
    /// <param name="value">指定值</param>
    /// <typeparam name="T"></typeparam>
    public void Set<T>(string id, string queryKey, T value)
    {
        if (!_mainTable.TryGetValue(id, out var node))
        {
            return;
        }
        
        var clone = JSON.Parse(node.ToString());
        node.Set(queryKey, value);
        
        InternalUpdate(id, clone, node);
    }
    
    /// <summary>
    /// 为指定节点的属性添加值
    /// </summary>
    /// <param name="id"></param>
    /// <param name="queryKey"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void Add<T>(string id, string queryKey, T value)
    {
        if (!_mainTable.TryGetValue(id, out var node))
        {
            return;
        }
        var clone = node.Clone() as JsonNode;
        node.Append(queryKey, value);

        InternalUpdate(id, clone!, node);
    }
    
    /// <summary>
    /// 为指定节点的属性添加属性
    /// </summary>
    /// <param name="id"></param>
    /// <param name="queryKey"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void Add<T>(string id, string queryKey, string key, T value)
    {
        if (!_mainTable.TryGetValue(id, out var node))
        {
            return;
        }
        var clone = node.Clone() as JsonNode;
        node.Append(queryKey, key, value);

        InternalUpdate(id, clone!, node);
    }
    
    /// <summary>
    /// 为指定节点的属性添加json属性
    /// </summary>
    /// <param name="id"></param>
    /// <param name="queryKey"></param>
    /// <param name="json"></param>
    public void AddJson(string id, string queryKey, string json)
    {
        if (!_mainTable.TryGetValue(id, out var node))
        {
            return;
        }
        var clone = node.Clone() as JsonNode;
        node.AddJson(queryKey, json);

        InternalUpdate(id, clone!, node);
    }
    
     /// <summary>
     /// 为指定节点的数学添加json属性 
     /// </summary>
     /// <param name="id"></param>
     /// <param name="queryKey"></param>
     /// <param name="key"></param>
     /// <param name="json"></param>
    public void AddJson(string id, string queryKey, string key, string json)
    {
        if (!_mainTable.TryGetValue(id, out var node))
        {
            return;
        }
        var clone = node.Clone() as JsonNode;
        node.AppendJson(queryKey, key, json);

        InternalUpdate(id, clone!, node);
    }

    public void Delete<T>(T value)
    {
        CheckTableNode<T>(NodeType.ArrayValue);
        _tableNode?.RemoveValue(value);
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
        return _tableNode!;
    }

    public List<JsonNode> Models()
    {
        return (_tableNode as JsonArrayNode<JsonNode>)?.Value ?? new List<JsonNode>();
    }

    public IEnumerator<JsonNode> GetEnumerator()
    {
        return _tableNode?.GetEnumerator() ?? new List<JsonNode>().GetEnumerator();
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
        var serializeOptions = JSON.JsonSerializeOptions.Value!;
        
        _root.Serialize(wrapperStream, builder, serializeOptions,0);
        wrapperStream.Flush();
        wrapperStream.Close();
    }

    public void Load(string filePath, bool compress = true)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using Stream wrapperStream = compress ? new GZipStream(stream, CompressionMode.Decompress) : stream;
        using var reader = new StreamReader(new BufferedStream(wrapperStream, bufferSize:8192));
        var nodeStack = new Stack<JsonNode>();
        JsonNode? root = null;

        // load时候关闭排序
        var srcJsonOptions = JSON.JsonOptions.Value;
        JSON.JsonOptions.Value = new JsonOptions { Sort = false };

        var serializeOptions = JSON.JsonSerializeOptions.Value!;
        var split = new[] { JSON.JsonSerializeOptions.Value!.COMMA };
        while (reader.ReadLine() is { } line)
        {
            var node = ParseNode(ref line, nodeStack, ref split, serializeOptions);
            root ??= node;
        }

        // 还原设置
        _root = root!;
        JSON.JsonOptions.Value = srcJsonOptions!;
        _tableNode = _root.GetNode(_tableName);
    }

    private JsonNode? ParseNode(ref string line, Stack<JsonNode> nodeStack, ref char[] split, JsonSerializeOptions serializeOptions)
    {
        var array = line.Split(split);
        var depth = int.Parse(array[0]);
        var nodeType = array[1][0] - '0';
        var key = serializeOptions.NullValue.Equals(array[2], StringComparison.Ordinal) ? null : array[2];
        var valueType = array[3][0];
        var value = serializeOptions.NullValue.Equals(array[4], StringComparison.Ordinal) ? null : array[4];
        JsonNode? node = null;
        JsonNode? parent = null;
        
        if (depth < nodeStack.Count)
        {
            nodeStack.Pop();
        }
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
                node = new JsonValueNode<object> { Key = key, NodeType = NodeType.Object };
                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
                nodeStack.Push(node);
                break;
            case (int)NodeType.ArrayObject:
                // ArrayObject
                node = new JsonArrayNode<JsonNode> { Key = key, NodeType = NodeType.ArrayObject };
                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
                nodeStack.Push(node);
                break;
            case (int)NodeType.ArrayValue:
                // ArrayValue
                if (string.IsNullOrEmpty(value))
                {
                    node = new JsonArrayNode<object> { Key = key, NodeType = NodeType.ArrayValue };
                }
                else
                {
                    var vvtype = value[0];
                    for (var i = 5; i < array.Length; i++)
                    {
                        var vvalue = array[i];
                        switch (vvtype)
                        {
                            case (char)ValueType.Object:
                                node ??= new JsonArrayNode<object> { Key = key, NodeType = NodeType.ArrayValue };
                                var ov = GetValue<object>(vvalue, vvtype);
                                node.AddValue(ov);
                                break;
                            case (char)ValueType.String:
                                node ??= new JsonArrayNode<string> { Key = key, NodeType = NodeType.ArrayValue };
                                var sv = GetValue<string>(vvalue, vvtype);
                                node.AddValue(sv);
                                break;
                            case (char)ValueType.Int:
                                node ??= new JsonArrayNode<int> { Key = key, NodeType = NodeType.ArrayValue };
                                var iv = GetValue<int>(vvalue, vvtype);
                                node.AddValue(iv);
                                break;
                            case (char)ValueType.Long:
                                node ??= new JsonArrayNode<long> { Key = key, NodeType = NodeType.ArrayValue };
                                var lv = GetValue<long>(vvalue, vvtype);
                                node.AddValue(lv);
                                break;
                            case (char)ValueType.Double:
                                node ??= new JsonArrayNode<double> { Key = key, NodeType = NodeType.ArrayValue };
                                var dv = GetValue<double>(vvalue, vvtype);
                                node.AddValue(dv);
                                break;
                            case (char)ValueType.Bool:
                                node ??= new JsonArrayNode<bool> { Key = key, NodeType = NodeType.ArrayValue };
                                var bv = GetValue<bool>(vvalue, vvtype);
                                node.AddValue(bv);
                                break;
                        }
                    }
                }
                
                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
                break;
            case (int)NodeType.LazyArray:
                // LazyArray
            {
                var elememt = JsonDocument.Parse(value!).RootElement;
                node = new LazyJsonArrayNode { Key = key, NodeType = NodeType.ArrayObject, Value = elememt};
                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
            }
                break;
            case (int)NodeType.LazyObject:
                // LazyValue
            {
                var elememt = JsonDocument.Parse(value!).RootElement;
                node = new LazyJsonValueNode { Key = key, NodeType = NodeType.Value, Value = elememt};
                parent = depth > 0 ? nodeStack.Peek() : null;
                AddChild(node, parent);
            }
                break;
        }
        return node;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void AddChild(JsonNode? node, JsonNode? parent)
    {
        if (null == node)
        {
            return;
        }
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
    private T? GetValue<T>(string? value, char valueType)
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
