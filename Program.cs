
Console.WriteLine("testing serialization...\n");

var model = Test.CreateModel();

var yaml = YamlRecords.Serialize(model);

Console.WriteLine(yaml);

Console.WriteLine("\ntesting deserialization...\n");

var test = YamlRecords.Deserialize<GameConfig>(yaml);

Console.WriteLine(YamlRecords.Serialize(test));