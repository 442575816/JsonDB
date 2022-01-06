using System.Diagnostics;

namespace JsonDB.Example;

public class Sample6
{
    public void Run()
    {
        var tree = new BPlusTree<int, string>();
        for (var i = 0; i < 1000; i++)
        {
            tree.Insert(i, i.ToString());
        }

        var list = tree.RangeFind(2, 27, (k1, k2) => k1.CompareTo(k2) <= 0 ? 0 : 1);
        foreach(var v in list)
        {
            Console.WriteLine($"{v}");
        }
    }
}