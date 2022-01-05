namespace JsonDB.Example;

public class Sample2
{
    public void Run()
    {
        var table = JSONTable.Create("students");
        var random = new Random();
        for (var i = 0; i < 1000; i++)
        {
            var male = random.NextDouble() < 0.5 ? "male" : "female";
            var age = random.Next(21) + 10;
            var data = table.Insert($@"{{""name"":""张三{i}"", ""sex"":""{male}"", ""age"":{age}, ""birthday"":""2021-12-14""}}");
            if (male == "male")
            {
                var roomNum = random.Next(1000) + 100;
                data.AddJson("address", $@"{{""city"":""shanghai"", ""street"":""黄浦区北京路99号"", ""roomNo"":""{roomNum}""}}");
            }
        }
        
        // 查找名字叫 张三2的
        var node = table.Where(JSON.Eq("name", "张三2")).FirstOrDefault();
        Console.WriteLine(node);
        
        // 查找地址不为空的
        var nodeList = table.Where(JSON.NotNull("address")).ToList();
        
        // 查找地址不为空的女性
        nodeList = table.Where(JSON.And(JSON.NotNull("address"), JSON.Eq("sex", "female"))).ToList();
        Console.WriteLine($"Count:{nodeList.Count}");
        
        // 查找年龄大于等于15岁的
        nodeList = table.Where(JSON.Gte("age", 15)).ToList();
        Console.WriteLine($"Count:{nodeList.Count}, First:{nodeList[0]}");
        
        
        
    }
}