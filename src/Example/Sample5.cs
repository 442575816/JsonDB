namespace JsonDB.Example;

public class Sample5
{
    public void Run()
    {
        var table = JSONTable.Create("students");
        table.AddIndex("name_sex", false, "name", "sex");
        table.AddIndex("age", false, (k1, k2) => int.Parse(k1).CompareTo(int.Parse(k2)), null, "age");
        var random = new Random();
        var dict = new Dictionary<int, List<JsonNode>>();
        for (var i = 0; i < 1000000; i++)
        {
            var male = random.NextDouble() < 0.5 ? "male" : "female";
            var age = random.Next(21) + 10;
            var data = table.Insert($@"{{""name"":""张三{i}"", ""sex"":""{male}"", ""age"":{age}, ""birthday"":""2021-12-14"", ""scores"":[""1"",""2""]}}");
            if (male == "male")
            {
                var roomNum = random.Next(1000) + 100;
                var id = data.Get<string>("_id")!;
                table.AddJson(id, "address", $@"{{""city"":""shanghai"", ""street"":""黄浦区北京路99号"", ""roomNo"":""{roomNum}""}}");
                // data.AddJson("address", $@"{{""city"":""shanghai"", ""street"":""黄浦区北京路99号"", ""roomNo"":""{roomNum}""}}");
            }

            if (dict.ContainsKey(age))
            {
                dict[age].Add(data);
            }
            else
            {
                dict[age] = new List<JsonNode> { data };
            }
        }

        var nodeList = table.RangeFind("age", 12, 20);
        var set = new HashSet<string>();
        foreach (var node in nodeList)
        {
            set.Add(node.Get<string>("_id")!);
            // Console.WriteLine($"{node.Get<string>("name")}, {node.Get<int>("age")}");
        }
        Console.WriteLine("-----------------");
        foreach (var pair in dict)
        {
            if (pair.Key is >= 12 and <= 20)
            {
                foreach (var node in pair.Value)
                {
                    var id = node.Get<string>("_id")!;
                    if (set.Contains(id))
                    {
                        set.Remove(id);
                    }
                    else
                    {
                        Console.WriteLine("failed");
                        break;
                    }
                    
                }
            }
        }

        if (set.Count > 0)
        {
            Console.WriteLine("failed");
        }
        else
        {
            Console.WriteLine("succ");
        }
    }
}