namespace JsonDB;

/// <summary>
/// B+Tree内存索引
/// </summary>
/// <typeparam name="V"></typeparam>
public class BTreeJsonIndexManager<V> : JsonIndexManager<V> where V : JsonNode
{
    private readonly JSONTable _table;
    private BPlusTree<string, string> _indexTree;
    private readonly string[] _props;
    private readonly Func<string, string, int> _leftComparer;
    private readonly Func<string, string, int> _comparer;

    public BTreeJsonIndexManager(JSONTable table, string name, Func<string, string, int>? comparer = null,
        Func<string, string, int>? leftComparer = null, params string[] keyProps)
    {
        _table = table;
        Name = name;
        _props = keyProps;
        _indexTree = new BPlusTree<string, string>();
        _comparer = comparer ?? string.CompareOrdinal;
        _leftComparer = leftComparer ?? ((key, searchKey) => key.StartsWith(searchKey, StringComparison.Ordinal) ? 0 : string.Compare(key, searchKey, StringComparison.Ordinal));
    }

    public string Name { get; }

    public void Insert(V value)
    {
        var idKey = value.Get<string>("_id")!;
        var indexKey = GetKey(value);
        _indexTree.Insert(indexKey, idKey, _comparer);
    }

    public void Remove(V value)
    {
        var indexKey = GetKey(value);
        _indexTree.Remove(indexKey, _comparer);
    }

    public void Update(V? oldValue, V newValue)
    {
        if (oldValue == null)
        {
            return;
        }

        var oldIndexKey = GetKey(oldValue);
        var newIndexKey = GetKey(newValue);
        if (oldIndexKey != newIndexKey)
        {
            var idKey = newValue.Get<string>("_id")!;
            _indexTree.Remove(oldIndexKey, _comparer);
            _indexTree.Insert(newIndexKey, idKey, _comparer);
        }
    }

    public object? Find(params object[] args)
    {
        var indexKey = GetKey(args);
        var idKey = _indexTree.Find(indexKey, _comparer);
        
        return idKey == null ? null : _table.Get(idKey);
    }

    public List<V> LeftFind(params object[] args)
    {
        if (_leftComparer == null)
        {
            throw new Exception($"index {Name} not support left find");
        }
        var indexKey = GetKey(args);
        var keyList = _indexTree.LeftFind(indexKey, _leftComparer);
        var resultList = new List<V>(keyList.Count);
        foreach (var key in keyList)
        {
            resultList.Add((V)_table.Get(key)!);
        }

        return resultList;
    }
    
    public List<V> RangeFind(object startValue, object endValue)
    {
        return RangeFind(startValue, endValue, _comparer);
    }
    
    public List<V> RangeFind(object startValue, object endValue, Func<string, string, int> comparer)
    {
        string? startKey = null;
        if (startValue is object[] args1)
        {
            startKey = GetKey(args1);
        }
        else
        {
            startKey = GetKey(startValue);
        }
        string? endKey = null;
        if (endValue is object[] args2)
        {
            endKey = GetKey(args2);
        }
        else
        {
            endKey = GetKey(endValue);
        }

        var keyList = _indexTree.RangeFind(startKey, endKey, comparer);
        var resultList = new List<V>(keyList.Count);
        foreach (var key in keyList)
        {
            resultList.Add((V)_table.Get(key)!);
        }

        return resultList;
    }

    public void Clear()
    {
        _indexTree = new BPlusTree<string, string>();
    }

    private string GetKey(V value)
    {
        var array = new object[_props.Length];
        for (var i = 0; i < _props.Length; i++)
        {
            array[i] = value.Get<object>(_props[i])!;
        }

        return JsonIndexManager<V>.ToString(array);
    }

    private string GetKey(params object[] args)
    {
        return args.Length < _props.Length ? $"{JsonIndexManager<V>.ToString(args)}," : JsonIndexManager<V>.ToString(args);
    }
}
