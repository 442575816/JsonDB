using System;
using System.Collections.Generic;

namespace JsonDB;

/// <summary>
/// B+Tree内存索引
/// </summary>
/// <typeparam name="V"></typeparam>
public class MultiBTreeJsonIndexManager<V> : JsonIndexManager<V> where V : JsonNode
{
    /// <summary>
    /// 默认集合大小
    /// </summary>
    private const int DEFAULT_VALUES_PER_KEY = 3;

    private readonly JSONTable _table;
    private readonly string[] _props;
    private BPlusTreeDictionary<string, List<string>> _indexTree;
    private readonly Func<string, string, int> _leftComparer;

    public MultiBTreeJsonIndexManager(JSONTable table, string name, params string[] keyProps)
    {
        this._table = table;
        this.Name = name;
        this._props = keyProps;
        this._indexTree = new BPlusTreeDictionary<string, List<string>>();
        this._leftComparer = (key, seachKey) => key.StartsWith(seachKey) ? 0 : string.Compare(key, seachKey, StringComparison.Ordinal);
    }

    public string Name { get; }

    public void Insert(V value)
    {
        var idKey = value.Get<string>("_id");
        var indexKey = GetKey(value);
        var list = _indexTree.Find(indexKey, string.CompareOrdinal);
        if (null == list)
        {
            list = new List<string>();
            _indexTree.Insert(indexKey, list, string.CompareOrdinal);
        }
        list.Add(idKey);
    }

    public void Remove(V value)
    {
        var indexKey = GetKey(value);
        var idKey = value.Get<string>("_id");
        var keyCollection = _indexTree.Find(indexKey, string.CompareOrdinal);
        keyCollection.Remove(idKey);
        if (keyCollection.Count == 0)
        {
            _indexTree.Remove(indexKey, string.CompareOrdinal);
        }
    }

    public void Update(V oldValue, V newValue)
    {
        if (oldValue == null)
        {
            return;
        }

        var oldIndexKey = GetKey(oldValue);
        var newIndexKey = GetKey(newValue);
        if (oldIndexKey != newIndexKey)
        {
            Remove(oldValue);
            Insert(newValue);
        }
    }

    public object Find(params object[] args)
    {
        var indexKey = GetKey(args);
        var keyList = _indexTree.Find(indexKey, string.CompareOrdinal);

        if (keyList == null)
        {
            return new List<V>();
        }

        var resultList = new List<V>(keyList.Count);
        foreach (var key in keyList)
        {
            resultList.Add((V)_table.Get(key));
        }

        return resultList;
    }

    public List<V> LeftFind(params object[] args)
    {
        var indexKey = GetKey(args);
        var keyLists = _indexTree.FindAll(indexKey, _leftComparer);
        var resultList = new List<V>(keyLists.Count * DEFAULT_VALUES_PER_KEY);
        foreach (var keyList in keyLists)
        {
            foreach (var key in keyList)
            {
                resultList.Add((V)_table.Get(key));
            }
        }

        return resultList;
    }

    public void Clear()
    {
        this._indexTree = new BPlusTreeDictionary<string, List<string>>();
    }

    private string GetKey(V value)
    {
        var array = new object[_props.Length];
        for (var i = 0; i < _props.Length; i++)
        {
            array[i] = value.Get<object>(_props[i]);
        }

        return JsonIndexManager<V>.ToString(array);
    }

    private string GetKey(params object[] args)
    {
        return JsonIndexManager<V>.ToString(args);
    }
}
