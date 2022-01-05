using System.Diagnostics;
using System.Text;

// ReSharper disable All

namespace JsonDB;

/// <summary>
/// B+ Tree的实现
/// </summary>
public class BPlusTreeDictionary<K, V> where K : IComparable<K>
{
    /// <summary>
    /// 默认每个节点存储的数据数量
    /// </summary>
    private const int DEFAULT_M = 10;

    internal readonly int M; // 每个节点存储的数据数量，需要为偶数
    internal LeafNode<K, V> head; // 叶子节点的head节点
    internal Node<K, V> root; // 根节点

    /// <summary>
    /// 构造函数
    /// </summary>
    public BPlusTreeDictionary() : this(DEFAULT_M)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="m">每个节点存储的数据数量</param>
    public BPlusTreeDictionary(int m)
    {
        M = (m % 2 == 0) ? m : m - 1;
        root = new LeafNode<K, V>(this);
        head = (LeafNode<K, V>)root;
    }

    public void Insert(K key, V value) => Insert(key, value, (key1, key2) => key1.CompareTo(key2));

    public void Insert(K key, V value, IComparer<K> comparer) => Insert(key, value, comparer.Compare);

    public void Insert(K key, V value, Func<K, K, int> comparer)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        var node = root.Insert(key, value, comparer);
        if (node != null)
        {
            root = node;
        }
    }

    public V Find(K key) => Find(key, (key1, key2) => key1.CompareTo(key2));

    public V Find(K key, IComparer<K> comparer) => Find(key, comparer.Compare);

    public V Find(K key, Func<K, K, int> comparer)
    {
        return root.Find(key, comparer);
    }

    public List<V> FindAll(K key, IComparer<K> comparer) => FindAll(key, comparer.Compare);

    public List<V> FindAll(K key, Func<K, K, int> comparer)
    {
        return root.FindAll(key, comparer);
    }

    public V Remove(K key) => Remove(key, (key1, key2) => key1.CompareTo(key2));

    public V Remove(K key, IComparer<K> comparer) => Remove(key, comparer.Compare);

    public V Remove(K key, Func<K, K, int> comparer)
    {
        return root.Remove(key, comparer);
    }

    public int Height()
    {
        var height = 1;
        var node = root;
        while (node is not LeafNode<K, V>)
        {
            height++;
            node = (node as InternalNode<K, V>).pointers[0];
        }
        return height;
    }

    public void Print()
    {
        Console.WriteLine($"height: {Height()}");
        var builder = new StringBuilder();
        root.Print(builder, 1);
        Console.Write(builder.ToString());
    }

    public static void Test()
    {
        var stopWatch = Stopwatch.StartNew();
        Random random = new Random();
        for (int j = 0; j < 10000; j++)
        {
            BPlusTreeDictionary<int, string> myTreeDictionary = new BPlusTreeDictionary<int, string>(4);
            int max = 1000;
            for (int i = 1; i <= max; i++)
            {
                myTreeDictionary.Insert(i, i.ToString());
            }

            HashSet<int> set = new HashSet<int>();
            for (int i = 1; i <= max; i++)
            {
                int tmp = random.Next(max) + 1;
                set.Add(tmp);
                myTreeDictionary.Remove(tmp);
            }

            for (int i = 1; i <= max; i++)
            {
                myTreeDictionary.Insert(i, i.ToString());
                set.Remove(i);
            }

            for (int i = max; i > 0; i--)
            {
                int tmp = random.Next(max) + 1;
                set.Add(tmp);
                myTreeDictionary.Remove(tmp);
            }

            for (int i = 1; i <= max; i++)
            {
                myTreeDictionary.Insert(i, i.ToString());
                set.Remove(i);
            }

            for (int i = max; i > 0; i--)
            {
                int tmp = random.Next(max) + 1;
                set.Add(tmp);
                myTreeDictionary.Remove(tmp);
            }

            for (int k = 1; k <= max; k++)
            {
                if (!set.Contains(k))
                {
                    string value = myTreeDictionary.Find(k);
                    if (null == value || value != k.ToString())
                    {
                        Console.WriteLine("ERROR");
                    }
                }
                else
                {
                    string value = myTreeDictionary.Find(k);
                    if (null != value)
                    {
                        Console.WriteLine("ERROR");
                    }
                }
            }

            Console.WriteLine("succ run " + j + " times");
        }
        stopWatch.Stop();
        Console.WriteLine($"btree time:{stopWatch.ElapsedMilliseconds}");
    }
}

/// <summary>
/// 内部类，b+tree里面的一个节点
/// </summary>
/// <typeparam name="K"></typeparam>
/// <typeparam name="V"></typeparam>
abstract class Node<K, V> where K : IComparable<K>
{
    public Node<K, V> Parent { get; set; } // 父节点
    protected K[] keys; // 节点包含的key值
    protected int size; // 节点数据数量
    protected readonly BPlusTreeDictionary<K, V> treeDictionary; // 所属的b+tree
    protected readonly int M; // 每个节点最大容量

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="treeDictionary"></param>
    protected Node(BPlusTreeDictionary<K, V> treeDictionary)
    {
        this.treeDictionary = treeDictionary;
        this.M = treeDictionary.M;
    }

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Node<K, V> Insert(K key, V value) => Insert(key, value, (key1, key2) => key1.CompareTo(key2));

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Node<K, V> Insert(K key, V value, IComparer<K> comparer) =>
        Insert(key, value, comparer.Compare);

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public abstract Node<K, V> Insert(K key, V value, Func<K, K, int> comparer);

    /// <summary>
    /// 移除指定数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public V Remove(K key) => Remove(key, (key1, key2) => key1.CompareTo(key2));

    /// <summary>
    /// 移除指定数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public V Remove(K key, IComparer<K> comparer) => Remove(key, comparer.Compare);

    /// <summary>
    /// 移除指定数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public abstract V Remove(K key, Func<K, K, int> comparer);

    /// <summary>
    /// 查找数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public V Find(K key) => Find(key, (key1, key2) => key1.CompareTo(key2));

    /// <summary>
    /// 查找数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public V Find(K key, IComparer<K> comparer) => Find(key, comparer.Compare);

    /// <summary>
    /// 查找数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public abstract V Find(K key, Func<K, K, int> comparer);

    /// <summary>
    /// 根据查找器查询数据
    /// </summary>
    /// <param name="key"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    public List<V> FindAll(K key, IComparer<K> comparer) => FindAll(key, comparer.Compare);

    /// <summary>
    /// 根据查找器查询数据
    /// </summary>
    /// <param name="key"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    public abstract List<V> FindAll(K key, Func<K, K, int> comparer);

    /// <summary>
    /// >= 查询数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public abstract Node<K, V> GTEFind(K key);

    /// <summary>
    /// 左值查询
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public abstract Node<K, V> LTEFind(K key);

    /// <summary>
    /// 打印节点
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="height"></param>
    public abstract void Print(StringBuilder builder, int height);

    /// <summary>
    /// 二分查找
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected int BinaryFind(K key) => BinaryFind(key, (key1, key2) =>
    {
        return key1.CompareTo(key2);
    });

    /// <summary>
    /// 二分查找
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected int BinaryFind(K key, Func<K, K, int> comparer)
    {
        var start = 0;
        var end = size;
        var middle = (start + end) / 2;
        K middleKey;
        int cvalue;

        while (start < end)
        {
            if (start == middle) {
                break;
            }
            middleKey = keys[middle];
            cvalue = comparer(key, middleKey);


            if (cvalue < 0)
            {
                end = middle;
            }
            else if (cvalue > 0)
            {
                start = middle;
            }
            else
            {
                // 找到了
                return middle;
            }
            middle = (start + end) / 2;
        }

        middleKey = keys[middle];
        cvalue = comparer(key, middleKey);
        if (cvalue == 0)
        {
            return middle;
        }

        // 没有找到
        return -1;
    }
}

/// <summary>
/// 非叶子节点，只存储keys
/// </summary>
/// <typeparam name="K"></typeparam>
/// <typeparam name="V"></typeparam>
internal class InternalNode<K, V> : Node<K, V> where K : IComparable<K>
{
    public Node<K, V>[] pointers; // 指向下一级的指针

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="treeDictionary"></param>
    public InternalNode(BPlusTreeDictionary<K, V> treeDictionary) : base(treeDictionary)
    {
        size = 0;
        pointers = new Node<K,V>[M];
        keys = new K[M];
    }

    public override V Find(K key, Func<K, K, int> comparer)
    {
        var i = 1;
        for (; i < size; i++)
        {
            if (comparer(key, keys[i]) < 0)
            {
                break;
            }
        }
        return pointers[i - 1].Find(key, comparer);
    }

    public override List<V> FindAll(K key, Func<K, K, int> comparer)
    {
        var resultList = new List<V>();
        var i = 1;
        for (; i < size; i++)
        {
            var cvalue = comparer(keys[i], key);
            if (cvalue == 0)
            {
                resultList.AddRange(pointers[i - 1].FindAll(key, comparer));
            } else if (cvalue > 0)
            {
                break;
            }
        }
        resultList.AddRange(pointers[i - 1].FindAll(key, comparer));
        return resultList;
    }

    public override Node<K, V> GTEFind(K key)
    {
        var i = 1;
        for (; i < size; i++)
        {
            if (key.CompareTo(keys[i]) < 0)
            {
                break;
            }
        }
        return pointers[i - 1].GTEFind(key);
    }

    public override Node<K, V> Insert(K key, V value, Func<K, K, int> comparer)
    {
        var i = 1;
        for (; i < size; i++)
        {
            if (comparer(key, keys[i]) < 0)
            {
                break;
            }
        }
        return pointers[i - 1].Insert(key, value, comparer);
    }

    public override Node<K, V> LTEFind(K key)
    {
        var i = 1;
        for (; i < size; i++)
        {
            if (key.CompareTo(keys[i]) < 0)
            {
                break;
            }
        }
        return pointers[i - 1].LTEFind(key);
    }

    public override void Print(StringBuilder builder, int height)
    {
        var i = 0;
        builder.Append('T').Append(height).Append('(');
        for (; i < size; i++)
        {
            builder.Append(keys[i]).Append(' ');
        }
        builder.Append(") ");

        var innerBuilder = new StringBuilder();
        for (i = 0; i < size; i++)
        {
            pointers[i].Print(innerBuilder, height + 1);
        }
        innerBuilder.Append('\n');
        builder.Append('\n').Append(innerBuilder);
    }

    public override V Remove(K key, Func<K, K, int> comparer)
    {
        var i = 1;
        for (; i < size; i++)
        {
            if (comparer(key, keys[i]) < 0)
            {
                break;
            }
        }
        return pointers[i - 1].Remove(key, comparer);
    }

    /// <summary>
    /// 更新key值
    /// </summary>
    /// <param name="newKey"></param>
    /// <param name="oldKey"></param>
    /// <param name="node"></param>
    public void Update(K newKey, K oldKey, Node<K, V> node, Func<K, K, int> comparer)
    {
        // 查找位置
        var middle = BinaryFind(oldKey, comparer);
        if (middle == -1)
        {
            return;
        }

        // 找到位置
        keys[middle] = newKey;
        pointers[middle] = node;

        if (0 == middle && null != Parent)
        {
            (Parent as InternalNode<K,V>).Update(newKey, oldKey, this, comparer);
        }
    }

    /// <summary>
    /// 插入到头部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void InsertToHead(K key, Node<K, V> value)
    {
        Array.Copy(keys, 0, keys, 1, size);
        Array.Copy(pointers, 0, pointers, 1, size);
        keys[0] = key;
        pointers[0] = value;
        size++;
        value.Parent = this;
    }

    /// <summary>
    /// 插入到尾部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void InsertToTail(K key, Node<K, V> value)
    {
        keys[size] = key;
        pointers[size] = value;
        size++;
        value.Parent = this;
    }

    /// <summary>
    /// 从头部删除
    /// </summary>
    public void RemoveFromHead()
    {
        Array.Copy(keys, 1, keys, 0, size - 1);
        Array.Copy(pointers, 1, pointers, 0, size - 1);
        keys[size - 1] = default;
        pointers[size - 1] = null;
        size--;
    }

    /// <summary>
    /// 从尾部删除
    /// </summary>
    public void RemoveFromTail()
    {
        keys[size - 1] = default;
        pointers[size - 1] = null;
        size--;
    }

    /// <summary>
    /// 插入节点
    /// </summary>
    /// <param name="leftKey"></param>
    /// <param name="left"></param>
    /// <param name="rightKey"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public Node<K, V> Insert(K leftKey, Node<K, V> left, K rightKey, Node<K, V> right, Func<K, K, int> comparer)
    {
        if (size == 0)
        {
            keys[0] = leftKey;
            keys[1] = rightKey;

            pointers[0] = left;
            pointers[1] = right;

            left.Parent = this;
            right.Parent = this;

            size += 2;
            return this;
        }

        if (size >= M)
        {
            // 满了，需要分裂
            var i = 0;
            for (; i < size; i++)
            {
                var currKey = keys[i];
                if (comparer(currKey, rightKey) > 0)
                {
                    break;
                }
            }

            // 分裂为一半
            var m = size / 2;
            var rightNode = new InternalNode<K, V>(treeDictionary) {size = size - m};

            Array.Copy(keys, m, rightNode.keys, 0, size - m);
            Array.Copy(pointers, m, rightNode.pointers, 0, size - m);

            for (var j = 0; j < rightNode.size; j++)
            {
                rightNode.pointers[j].Parent = rightNode;
            }

            // 清理自己
            for (var j = m; j < size; j++)
            {
                keys[j] = default;
                pointers[j] = null;
            }
            size = m;

            // 建立新的父节点
            Parent ??= new InternalNode<K, V>(treeDictionary);
            rightNode.Parent = Parent;

            if (i >= m)
            {
                rightNode.Insert(default, null, rightKey, right, comparer);
            }
            else
            {
                Insert(default, null, rightKey, right, comparer);
            }

            return (Parent as InternalNode<K, V>).Insert(keys[0], this, rightNode.keys[0], rightNode, comparer);
        }

        // 查找插入位置
        var k = 0;
        for(; k < size; k++)
        {
            var currKey = keys[k];
            if (comparer(currKey, rightKey) > 0)
            {
                break;
            }
        }

        // 插入
        Array.Copy(keys, k, keys, k + 1, size - k);
        Array.Copy(pointers, k, pointers, k + 1, size - k);
        keys[k] = rightKey;
        pointers[k] = right;
        right.Parent = this;
        size++;

        return null;
    }

    /// <summary>
    /// 移除节点
    /// </summary>
    /// <param name="key"></param>
    public void RemovePointer(K key, Func<K, K, int> comparer)
    {
        // 找到位置
        var middle = BinaryFind(key, comparer);

        var headKey = keys[0];
        Array.Copy(keys, middle + 1, keys, middle, size - middle - 1);
        Array.Copy(pointers, middle + 1, pointers, middle, size - middle - 1);
        keys[size - 1] = default;
        pointers[size - 1] = null;
        size--;

        var m = M / 2;
        if (size < m)
        {
            if (null == Parent && size < 2)
            {
                // 头节点和子节点合并
                treeDictionary.root = pointers[0];
                pointers[0].Parent = null;
            }
            else if (null != Parent)
            {
                var parent = Parent as InternalNode<K, V>;
                var index = parent.BinaryFind(headKey, comparer);
                var prev = ((index > 0) ? parent.pointers[index - 1] : null) as InternalNode<K, V>;
                var next = ((index + 1 < parent.size) ? parent.pointers[index + 1] : null) as InternalNode<K, V>;

                // 少于m/2个节点
                if (prev != null && prev.size > m)
                {
                    // 找前节点补偿
                    // 从尾部删除
                    var k = prev.keys[prev.size - 1];
                    var pointer = prev.pointers[prev.size - 1];
                    prev.RemoveFromTail();

                    // 加入头部
                    InsertToHead(k, pointer);
                    parent.Update(k, headKey, this, comparer);
                }
                else if (next != null && next.size > m)
                {
                    // 找后面节点借
                    // 从头部删除
                    var k = next.keys[0];
                    var pointer = next.pointers[0];
                    next.RemoveFromHead();
                    parent.Update(next.keys[0], k, next, comparer);

                    // 加入尾部
                    InsertToTail(k, pointer);
                }
                else
                {
                    if (prev != null && prev.size <= m)
                    {
                        // 同前面节点合并
                        for (var i = 0; i < size; i++)
                        {
                            prev.InsertToTail(keys[i], pointers[i]);
                        }

                        // 从父节点移除
                        parent.RemovePointer(headKey, comparer);
                    }
                    else if (next != null && next.size <= m)
                    {
                        // 同后面节点合并
                        for (var i = 0; i < next.size; i++)
                        {
                            InsertToTail(next.keys[i], next.pointers[i]);
                        }

                        // 从父节点移除
                        parent.RemovePointer(next.keys[0], comparer);
                    }
                    else
                    {
                        throw new Exception("unkonw error");
                    }
                }
            }
        }
    }

}

/// <summary>
/// 叶子节点
/// </summary>
/// <typeparam name="K"></typeparam>
/// <typeparam name="V"></typeparam>
internal class LeafNode<K, V> : Node<K, V> where K : IComparable<K>
{
    protected LeafNode<K, V> prev; // 前节点
    protected LeafNode<K, V> next; // 后节点
    private readonly V[] values; // 数据

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="treeDictionary"></param>
    public LeafNode(BPlusTreeDictionary<K, V> treeDictionary) : base(treeDictionary)
    {
        size = 0;
        keys = new K[M];
        values = new V[M];
        Parent = null;
    }

    public override V Find(K key, Func<K, K, int> comparer)
    {
        if (size == 0)
        {
            return default;
        }

        var middle = BinaryFind(key, comparer);
        return (middle != -1) ? values[middle] : default;
    }

    public override List<V> FindAll(K key, Func<K, K, int> comparer)
    {
        if (size == 0)
        {
            return new List<V>();
        }

        var resultList = new List<V>(size);
        for (var i = 0; i < size; i++)
        {
            var cvalue = comparer(keys[i], key);
            if (cvalue == 0)
            {
                resultList.Add(values[i]);
            }
            else if (cvalue > 0)
            {
                break;
            }
        }
        return resultList;
    }

    public override Node<K, V> GTEFind(K key)
    {
        return key.CompareTo(keys[0]) >= 0 ? this : null;
    }

    public override Node<K, V> Insert(K key, V value, Func<K, K, int> comparer)
    {
        if (size >= M)
        {
            // 查找插入位置
            var i = 0;
            for (; i < size; i++)
            {
                var currKey = keys[i];

                var cvalue = comparer(currKey, key);
                if (cvalue == 0)
                {
                    values[i] = value;
                    return null;
                }

                if (cvalue > 0)
                {
                    break;
                }
            }

            // 已满，需要分裂
            var m = size / 2;

            // 分裂出一个右节点
            var rightNode = new LeafNode<K, V>(treeDictionary) {size = size - m};

            // 对右节点赋值，并清理自己
            Array.Copy(keys, m, rightNode.keys, 0, rightNode.size);
            Array.Copy(values, m, rightNode.values, 0, rightNode.size);
            for (var j = m; j < size; j++)
            {
                keys[j] = default;
                values[j] = default;
            }
            size = m;

            // 设置链接关系
            if (next != null)
            {
                next.prev = rightNode;
                rightNode.next = next;
            }
            if (prev == null)
            {
                treeDictionary.head = this;
            }
            rightNode.prev = this;
            this.next = rightNode;

            // 插入节点
            if (i >= m)
            {
                rightNode.Insert(key, value, comparer);
            }
            else
            {
                Insert(key, value, comparer);
            }

            // 设置父节点
            Parent ??= new InternalNode<K, V>(treeDictionary);
            rightNode.Parent = Parent;

            // 父节点插入
            return (Parent as InternalNode<K, V>).Insert(keys[0], this, rightNode.keys[0], rightNode, comparer);

        }

        // 插入到合适的位置
        var k = 0;
        var headKey = keys[0];
        for (; k < size; k++)
        {
            var currKey = keys[k];

            var cvalue = comparer(currKey, key);
            if (cvalue == 0)
            {
                values[k] = value;
                return null;
            }
            if (cvalue > 0)
            {
                break;
            }
        }

        if (size > k)
        {
            Array.Copy(keys, k, keys, k + 1, size - k);
            Array.Copy(values, k, values, k + 1, size - k);
        }

        keys[k] = key;
        values[k] = value;
        size++;

        // 更新父节点
        if (k == 0 && null != Parent)
        {
            (Parent as InternalNode<K, V>).Update(key, headKey, this, comparer);
        }
        return null;

    }

    public override Node<K, V> LTEFind(K key)
    {
        return this;
    }

    public override void Print(StringBuilder builder, int height)
    {
        builder.Append('L').Append(height).Append('(');
        int i = 0;
        for (; i < size; i++)
        {
            builder.Append(keys[i]).Append(' ');
        }
        builder.Append(") ");
    }

    public override V Remove(K key, Func<K, K, int> comparer)
    {
        if (size == 0)
        {
            return default;
        }

        var middle = BinaryFind(key, comparer);
        if (middle != -1)
        {
            // 找到了
            var headKey = keys[0];
            var value = values[middle];
            Array.Copy(keys, middle + 1, keys, middle, size - middle - 1);
            Array.Copy(values, middle + 1, values, middle, size - middle - 1);
            keys[size - 1] = default;
            values[size - 1] = default;
            size--;

            var m = M / 2;
            if (size < m)
            {
                // 少于m/2个节点
                if (prev != null && prev.size > m && prev.Parent == Parent)
                {
                    // 找前节点补借
                    // 从尾部删除
                    var k = prev.keys[prev.size - 1];
                    var v = prev.values[prev.size - 1];
                    prev.RemoveFromTail();

                    // 加入头部
                    InsertToHead(k, v);
                }
                else if (next != null && next.size > m && next.Parent == Parent)
                {
                    // 从后面节点借
                    // 从头部删除
                    var k = next.keys[0];
                    var v = next.values[0];
                    next.RemoveFromHead();
                    if (null != next.Parent)
                    {
                        (next.Parent as InternalNode<K, V>).Update(next.keys[0], k, next, comparer);
                    }

                    // 加入尾部
                    InsertToTail(k, v);
                }
                else
                {
                    // 都不够借
                    if (prev != null && prev.size <= m && prev.Parent == Parent)
                    {
                        // 同前面节点合并
                        for (var i = 0; i < size; i++)
                        {
                            prev.InsertToTail(keys[i], values[i]);
                        }

                        // 父节点移除
                        (Parent as InternalNode<K, V>).RemovePointer(headKey, comparer);

                        // 更新头节点
                        headKey = keys[0];

                        // 修正叶子节点链接
                        Parent = null;
                        var node = next;
                        if (node != null)
                        {
                            node.prev = prev;
                            prev.next = node;
                        }
                        else
                        {
                            prev.next = null;
                        }
                    }
                    else if (next != null && next.size <= m && next.Parent == Parent)
                    {
                        // 同后面节点合并
                        for (var i = 0; i < next.size; i++)
                        {
                            InsertToTail(next.keys[i], next.values[i]);
                        }

                        // 父节点移除
                        (Parent as InternalNode<K, V>).RemovePointer(next.keys[0], comparer);

                        // 修正叶子节点链接
                        var node = next;
                        if (node.next != null)
                        {
                            node.next.prev = this;
                            next = node.next;
                        }
                        else
                        {
                            next = null;
                        }
                    }
                    else if (Parent != null)
                    {
                        treeDictionary.Print();
                        throw new Exception("unknow error");
                    }
                }
            }

            if (null != Parent && comparer(headKey, keys[0]) != 0)
            {
                (Parent as InternalNode<K, V>).Update(keys[0], headKey, this, comparer);
            }

            return value;
        }
        else
        {
            return default;
        }

    }

    /// <summary>
    /// 插入到头部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void InsertToHead(K key, V value)
    {
        Array.Copy(keys, 0, keys, 1, size);
        Array.Copy(values, 0, values, 1, size);
        keys[0] = key;
        values[0] = value;
        size++;
    }

    /// <summary>
    /// 插入到尾部
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void InsertToTail(K key, V value)
    {
        keys[size] = key;
        values[size] = value;
        size++;
    }

    /// <summary>
    /// 从头部删除
    /// </summary>
    public void RemoveFromHead()
    {
        Array.Copy(keys, 1, keys, 0, size - 1);
        Array.Copy(values, 1, values, 0, size - 1);
        keys[size - 1] = default;
        values[size - 1] = default;
        size--;
    }

    /// <summary>
    /// 从尾部删除
    /// </summary>
    public void RemoveFromTail()
    {
        keys[size - 1] = default;
        values[size - 1] = default;
        size--;
    }
}
