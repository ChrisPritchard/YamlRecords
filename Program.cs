
Console.WriteLine("testing serialization...\n");

var model = Test.CreateModel();

var yaml = YamlRecords.Serialize(model);

Console.WriteLine(yaml);
File.WriteAllText("./out.yml", yaml);

Console.WriteLine("\ntesting deserialization...\n");

var test = YamlRecords.Deserialize<GameConfig>(yaml);

Console.WriteLine(YamlRecords.Serialize(test));

Console.WriteLine("\ntesting tab conversion...\n");

var tab_test =
    "cardTypes:\n" +
    "\tfunds:\n" +
    "\t\ttitle: Funds\n" +
    "\t\ticonPath: \"res://assets/wealth_icon.png\"\n" +
    "\thealth:\n" +
    "\t\ttitle: Health\n" +
    "\t\ticonPath: \"res://assets/reputation_icon.png\"\n" +
    "gameFlows:\n";

var tab_test2 = YamlRecords.Deserialize<GameConfig>(tab_test);

Console.WriteLine(YamlRecords.Serialize(tab_test2));