namespace JsonDB.Example;

public class Sample1
{
    public void Run()
    {
        var table = JSONTable.Create("students");
        table.Insert(@"{""name"":""张三"", ""sex"":""male"", ""age"":1, ""birthday"":""2021-12-14""}");
        
        Console.WriteLine($"{table}");
        
        // LINQ
        var data = table.Where(JSON.Eq("name", "张三")).FirstOrDefault();
        Console.WriteLine($"{data}");
        
        // JsonPath
        data = table.Table().Get<JsonNode>("$1")!;
        Console.WriteLine($"{data}");
        
        var nameData = table.Table().Get<string>("$1.name");
        Console.WriteLine($"{nameData}");

        data.Set<string>("name", "李四");
        Console.WriteLine($"{data}");
        
        // 删除节点
        data.Remove("name");
        Console.WriteLine($"{data}");
        
        // 新增节点
        data.Add("name", "张三");
        Console.WriteLine($"{data}");
        
        // 新增对象节点
        data.AddJson("address", @"{""city"":""shanghai"", ""street"":""黄浦区北京路99号"", ""roomNo"":""531""}");
        Console.WriteLine($"{data}");
    }
}