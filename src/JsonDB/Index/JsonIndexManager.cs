using System.Collections.Generic;
using System.Text;

namespace JsonDB;

/// <summary>
/// 内存索引管理器
/// </summary>
/// <typeparam name="V"></typeparam>
public interface JsonIndexManager<V>
{
    /// <summary>
    /// 索引名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 索引中添加内容
    /// </summary>
    /// <param name="value"></param>
    void Insert(V value);

    /// <summary>
    /// 索引中移除内容
    /// </summary>
    /// <param name="value"></param>
    void Remove(V value);

    /// <summary>
    /// 索引中内容发生更新
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    void Update(V oldValue, V newValue);

    /// <summary>
    /// 索引中查找对象
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    object Find(params object[] args);

    /// <summary>
    /// 索引中，左值查找
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    List<V> LeftFind(params object[] args);

    /// <summary>
    /// 索引清理
    /// </summary>
    void Clear();

    /// <summary>
    /// 转化为string
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static string ToString(params object[] args)
    {
        if (args == null)
        {
            return "null";
        }

        if (args.Length == 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < args.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }
            builder.Append(args[i]);
        }

        return builder.ToString();
    }
}
