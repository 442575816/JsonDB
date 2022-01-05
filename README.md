# JsonDB

Json是我们最常用的数据类型，本库旨在提供内存级把Json当做数据库使用

### 一、入门

**创建一个`Table`**

``` c#
var table = JSONTable.Create("students");
table.Insert('{"name":"张三", "sex":"male", "age":1, "birthday":"2021-12-14"}');
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
var data = table.Table().Get<JsonNode>("students.$1");
var nameData = table.Table().Get<string>("students.$1.name");

```

**修改数据**

```c#
// 更新节点值
var data = table.Where(JSON.Eq("name", "张三"));
data.Set<string>("name", "李四")

// 删除节点
data.Remove("name");

// 新增节点
data.Add("name", "张三");
```


