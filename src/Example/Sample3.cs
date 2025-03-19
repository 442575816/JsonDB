namespace JsonDB.Example;

public class Sample3
{
    public void Run()
    {
        var table = JSONTable.Create("students");
        table.AddIndex("name_sex", false, "name", "sex");
        table.AddIndex("address_city_roomNo", false, "address.city", "address.roomNo");
        var random = new Random();
        for (var i = 0; i < 1000; i++)
        {
            var male = random.NextDouble() < 0.5 ? "male" : "female";
            var age = random.Next(21) + 10;
            var data = table.Insert($@"{{""name"":""张三{i}"", ""sex"":""{male}"", ""age"":{age}, ""birthday"":""2021-12-14""}}");
            if (male == "male")
            {
                var roomNum = random.Next(1000) + 100;
                var id = data.Get<string>("_id")!;
                table.AddJson(id, "address", $@"{{""city"":""shanghai"", ""street"":""黄浦区北京路99号"", ""roomNo"":""{roomNum}""}}");
                // data.AddJson("address", $@"{{""city"":""shanghai"", ""street"":""黄浦区北京路99号"", ""roomNo"":""{roomNum}""}}");
            }
        }
        
        // 根据索引查找
        var nodeList = table.LeftFind("name_sex", "张三1");
        Console.WriteLine($"Count:{nodeList.Count}, First:{nodeList[0]}");
        
        nodeList = table.LeftFind("address_city_roomNo", "shanghai");
        Console.WriteLine($"Count:{nodeList.Count}, First:{nodeList[0]}");


    }
}