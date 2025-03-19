using System.Diagnostics;

namespace JsonDB.Example;

public class Sample4
{
    public void Run()
    {
        var table = JSONTable.Create("students");
        table.AddIndex("name_sex", false, "name", "sex");
        table.AddIndex("address_city_roomNo", false, "address.city", "address.roomNo");
        var random = new Random();
        for (var i = 0; i < 10; i++)
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
        }
        Console.WriteLine($"{table}");
        
        table.Serialize("students.json", false);
        Console.WriteLine(File.ReadAllText("students.json"));

        table.Load("students.json", false);
        Console.WriteLine($"{table}");
    }
}