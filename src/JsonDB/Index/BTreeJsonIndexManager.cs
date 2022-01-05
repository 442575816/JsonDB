using System;
using System.Collections.Generic;

namespace JsonDB;

/// <summary>
/// B+Tree内存索引
/// </summary>
/// <typeparam name="V"></typeparam>
public class BTreeJsonIndexManager<V> : JsonIndexManager<V> where V : JsonNode
{
    private readonly JSONTable _table;
    private BPlusTree<string, string> _indexTree;
    private readonly Func<string, string, int> _leftComparer;
    private readonly string[] _props;

    public BTreeJsonIndexManager(JSONTable table, string name, params string[] keyProps)
    {
        this._table = table;
        this.Name = name;
        this._props = keyProps;
        this._indexTree = new BPlusTree<string, string>();
        this._leftComparer = (key, seachKey) => key.StartsWith(seachKey) ? 0 : string.Compare(key, seachKey, StringComparison.Ordinal);
    }

    public string Name { get; }

    public void Insert(V value)
    {
        var idKey = value.Get<string>("_id");
        var indexKey = GetKey(value);
        _indexTree.Insert(indexKey, idKey, string.CompareOrdinal);
    }

    public void Remove(V value)
    {
        var indexKey = GetKey(value);
        _indexTree.Remove(indexKey, string.CompareOrdinal);
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
            var idKey = newValue.Get<string>("_id");
            _indexTree.Remove(oldIndexKey, string.CompareOrdinal);
            _indexTree.Insert(newIndexKey, idKey, string.CompareOrdinal);
        }
    }

    public object Find(params object[] args)
    {
        var indexKey = GetKey(args);
        var idKey = _indexTree.Find(indexKey, string.CompareOrdinal);

        return _table.Get(idKey);
    }

    public List<V> LeftFind(params object[] args)
    {
        var indexKey = GetKey(args);
        var keyList = _indexTree.FindAll(indexKey, _leftComparer);
        var resultList = new List<V>(keyList.Count);
        foreach (var key in keyList)
        {
            resultList.Add((V)_table.Get(key));
        }

        return resultList;
    }

    public void Clear()
    {
        this._indexTree = new BPlusTree<string, string>();
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
