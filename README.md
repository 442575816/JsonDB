# JsonDB

Json是我们最常用的数据类型，本库旨在提供内存级把Json当做数据库使用

### 一、入门

**创建一个`Table`**

``` c#
var table = JSONTable.Create("students");
table.Insert(@"{""name"":""张三"", ""sex"":""male"", ""age"":1, ""birthday"":""2021-12-14""}");
```

内部的Json为
```json
{
    "students": [
        {"name":"张三", "sex":"male", "age":1, "birthday":"2021-12-14", "_id":"67f49ca6-35f9-4016-9f02-7857ee24d554"}
    ]
}
```

**检索数据**

``` c#
// LINQ
var data = table.Where(JSON.Eq("name", "张三"));

// JsonPath
var data = table.Table().Get<JsonNode>("$1");
var nameData = table.Table().Get<string>("$1.name");

```

**修改数据**

```c#
// 更新节点值
var data = table.Where(JSON.Eq("name", "张三"));
data.Set<string>("name", "李四");

// 删除节点
data.Remove("name");

// 新增节点
data.Add("name", "张三");

// 新增对象节点
data.AddJson("address", @"{""city"":""shanghai"", ""street"":""黄浦区北京路99号"", ""roomNo"":""531""}");
```

通过以上一些列操作，最终输出结果为:
```json
{
    "students": [
        {"_id":"31c46d61-f414-4266-8ae7-1fe2588588a6","address":{"city":"shanghai","roomNo":"531","street":"黄浦区北京路99号"},"age":1,"birthday":"2021-12-14","name":"张三","sex":"male"}
    ]
}
```
详情见[Sample1](src/Example/Sample1.cs)

### 二、高级特性

1. `LINQ`支持，提供大量的`LINQ`函数，帮助快速检索数据

```c#
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
```

目前支持了一下LINQ函数：

|函数名|说明|
|-|-|
|Eq|=|
|Ne|!=|
|Le|<|
|Lte|<=|
|Ge|>|
|Gte|>=|
|And|and操作符|
|Or|or操作符|
|NotNull|判断不为空|
|Null|判断为空|
|Like|模糊查询|
|In|包含查询|
|Len|数量查询|

2. `索引`支持

目前可以对JsonTable增加索引，索引实现基于`B+Tree`

3. `快照`支持和恢复

支持将JsonTable持久化和从快照文件中恢复

### 三、待完善部分

- [ X ] 索引支持范围查找
- 性能更好的序列化和反序列化
- 异步序列化的支持
- 并发的支持