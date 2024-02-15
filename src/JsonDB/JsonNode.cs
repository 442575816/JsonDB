using System.Collections;
using System.Text;

namespace JsonDB;

public abstract class JsonNode : IEnumerable<JsonNode>, IComparable<JsonNode>, ICloneable
{
    public string? Key { get; set; }
    public NodeType NodeType { get; set; }
    public List<JsonNode>? ChildNodes { get; set; }
    public JsonNode? Parent { get; set; }
    public virtual int Count { get; set; }

    /// <summary>
    /// 增加子节点
    /// </summary>
    /// <param name="node"></param>
    internal void AddChild(JsonNode node)
    {
        ChildNodes ??= new List<JsonNode>();

        if (JSON.JsonOptions.Value!.Sort)
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
        ChildNodes![pos] = newValue;
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
            for (var i = 0; i < ChildNodes!.Count; i++)
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
        ChildNodes!.RemoveAt(pos);
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
            ChildNodes!.Remove(v);
        }
    }
    
    /// <summary>
    /// 解析懒加载节点
    /// </summary>
    /// <param name="enableLazy"></param>
    internal virtual JsonNode ParseLazyNode(bool enableLazy = true)
    {
        return this;
    }

    /// <summary>
    /// 内部获取值的方法
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="keyLen"></param>
    /// <param name="lastKey"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected internal abstract T InternalGetValue<T>(int pos, string lastKey);

    /// <summary>
    /// 内部设置值的方法
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    protected internal abstract void InternalSetValue<T>(int pos, T value);

    /// <summary>
    /// 移除节点
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="lastKey"></param>
    protected internal abstract bool InternalRemove(int pos, string lastKey);

    /// <summary>
    /// 查找json节点的值
    /// </summary>
    /// <param name="queryKey"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetValue<T>(string queryKey, out T? value)
    {
        var keys = queryKey.Split('.');
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
    public T GetNodeValue<T>()
    {
        return InternalGetValue<T>(0, "");
    }

    /// <summary>
    /// 安全获取值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? SafeGetNodeValue<T>()
    {
        try
        {
            return GetNodeValue<T>();
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>
    /// 查找json节点的值
    /// </summary>
    /// <returns></returns>
    public T? Get<T>(string queryKey)
    {
        var keys = queryKey.Split('.');
        if (TryGetValue(keys, 0, out T? value))
        {
            return value;
        }

        return default;
    }
    
    /// <summary>
    /// 查找json节点的值
    /// </summary>
    /// <returns></returns>
    public T? GetRaw<T>(string queryKey)
    {
        var keys = new[] {queryKey};
        if (TryGetValue(keys, 0, out T? value))
        {
            return value;
        }

        return default;
    }

    /// <summary>
    /// 查找json节点
    /// </summary>
    /// <returns></returns>
    public JsonNode? GetNode(string queryKey)
    {
        var keys = queryKey.Split('.');
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
        var keys = queryKey.Split('.');
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
        var keys = queryKey.Split('.');
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
        var keys = queryKey.Split('.');
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
        var keys = queryKey.Split('.');
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
        var keys = queryKey.Split('.');
        if (TryGetNode(keys, 0, out var node, out _))
        {
            return node?.AddJson(key, json) ?? false;
        }
        return false;
    }
    
    /// <summary>
    /// 添加节点
    /// </summary>
    /// <returns></returns>
    public bool AppendRootJson(string key, string json)
    {
        var keys = new[] {key};
        if (TryGetNode(keys, 0, out _, out _))
        {
            throw new Exception($"node: {key} exists already.");
        }
        AddJson(key, json);
        return true;
    }

    /// <summary>
    /// 移除节点
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool Remove(string queryKey)
    {
        var keys = queryKey.Split('.');
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
        return ChildNodes?.GetEnumerator() ?? new List<JsonNode>().GetEnumerator();
    }

    public int CompareTo(JsonNode? other)
    {
        return string.CompareOrdinal(Key, other?.Key);
    }

    public abstract void Serialize(Stream stream, StringBuilder builder, JsonSerializeOptions options, int depth);

    public override string ToString()
    {
        var builder = new StringBuilder();
        ToJson(builder);
        return builder.ToString();
    }
    
    /// <summary>
    /// 获取Json
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        return ToString();
    }

    /// <summary>
    /// 获取值的Json
    /// </summary>
    /// <returns></returns>
    public string ToValueJson()
    {
        var builder = new StringBuilder();
        ToJson(builder, false);
        return builder.ToString();
    }
    
    /// <summary>
    /// 内部ToString方法
    /// </summary>
    /// <param name="builder"></param>
    internal abstract void ToJson(StringBuilder builder, bool appendKey = true);

    public abstract object Clone();

    /// <summary>
    /// 获取指定节点的值
    /// </summary>
    /// <param name="keys">搜索keys路径</param>
    /// <param name="index">当前搜索到的节点</param>
    /// <param name="value">修改成的值</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal bool TryGetValue<T>(Span<string> keys, int index, out T? value)
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
    internal virtual bool TryGetNode(Span<string> keys, int index, out JsonNode? node, out int pos)
    {
        if (JSON.JsonOptions.Value!.RecursiveMode)
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
    internal bool TryGetNodeLoopMode(Span<string> keys, int index, out JsonNode? node, out int pos)
    {
        var options = JSON.JsonOptions.Value!;
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
                break;
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
    internal bool TryGetNodeRecursiveMode(Span<string> keys, int index, out JsonNode? node, out int pos)
    {
        // 递归模式
        var options = JSON.JsonOptions.Value!;
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

    protected internal virtual (JsonNode? node, int pos) SearchChilds(string key, JsonOptions options)
    {
        if (ChildNodes == null || ChildNodes.Count == 0)
        {
            return (null, -1);
        }

        if (options is { Sort: true, BinarySearch: true })
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

    protected internal virtual bool SearchChilds(Span<string> keys, int index, JsonOptions options, out JsonNode? node, out int pos)
    {
        if (ChildNodes == null || ChildNodes.Count == 0)
        {
            node = null;
            pos = -1;
            return false;
        }

        if (options is { Sort: true, BinarySearch: true })
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