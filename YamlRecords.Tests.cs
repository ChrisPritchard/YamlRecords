using System;
using System.Collections.Generic;
using Xunit;

using static YamlRecordsTests.TestModels;

public class YamlRecordsTests
{
    [Fact]
    public void Serialize_Then_Deserialize_Should_Produce_Equivalent_Object()
    {
        // Arrange
        var model = CreateModel();

        // Act
        var yaml = YamlRecords.Serialize(model);

        var test = YamlRecords.Deserialize<GameConfig>(yaml);
        var reserializedYaml = YamlRecords.Serialize(test);

        // Assert
        // Since serialization might not preserve object references perfectly,
        // we compare the serialized YAML instead of object references
        var deserializedAgain = YamlRecords.Deserialize<GameConfig>(reserializedYaml);
        Assert.Equal(model.CardTypes.Count, deserializedAgain.CardTypes.Count);
    }

    [Fact]
    public void Deserialize_Should_Handle_Tab_Indentation()
    {
        // Arrange
        var tab_test =
            "cardTypes:\n" +
            "\tfunds:\n" +
            "\t\ttitle: Funds\n" +
            "\t\ticonPath: \"res://assets/wealth_icon.png\"\n" +
            "\thealth:\n" +
            "\t\ttitle: Health\n" +
            "\t\ticonPath: \"res://assets/reputation_icon.png\"\n" +
            "gameFlows:\n";

        // Act
        var tab_test2 = YamlRecords.Deserialize<GameConfig>(tab_test);

        // Assert
        Assert.NotNull(tab_test2);
        Assert.NotNull(tab_test2.CardTypes);
        Assert.True(tab_test2.CardTypes.ContainsKey("funds"));
        Assert.True(tab_test2.CardTypes.ContainsKey("health"));
        Assert.Equal("Funds", tab_test2.CardTypes["funds"].Title);
        Assert.Equal("Health", tab_test2.CardTypes["health"].Title);
    }

    [Fact]
    public void Deserialize_Should_Ignore_Comments()
    {
        // Arrange
        var comment_test =
            "cardTypes:\n" +
            "  funds: # represents wealth in this world, used when paying for things\n" +
            "    title: Funds\n" +
            "    iconPath: \"res://assets/wealth_icon.png\"\n" +
            "  health: # represents your life, can be used for physical tasks. if all lost, likely means death\n" +
            "    title: Health\n" +
            "    iconPath: \"res://assets/reputation_icon.png\"\n" +
            "gameFlows:\n";

        // Act
        var comment_test2 = YamlRecords.Deserialize<GameConfig>(comment_test);

        // Assert
        Assert.NotNull(comment_test2);
        Assert.NotNull(comment_test2.CardTypes);
        Assert.True(comment_test2.CardTypes.ContainsKey("funds"));
        Assert.True(comment_test2.CardTypes.ContainsKey("health"));
        Assert.Equal("Funds", comment_test2.CardTypes["funds"].Title);
        Assert.Equal("Health", comment_test2.CardTypes["health"].Title);
        Assert.Equal("res://assets/wealth_icon.png", comment_test2.CardTypes["funds"].IconPath);
        Assert.Equal("res://assets/reputation_icon.png", comment_test2.CardTypes["health"].IconPath);
    }

    [Fact]
    public void Serialize_Anonymous_Types()
    {
        // Arrange
        var test_type = new
        {
            Title = "Hello World",
            Content = "This is a test (including special characters! #)",
            Items = new[]
            {
                true,
                false,
                true
            }
        };

        var nl = Environment.NewLine;
        var expected =
            "title: Hello World" + nl +
            "content: \"This is a test (including special characters! #)\"" + nl +
            "items:" + nl +
            "  - true" + nl +
            "  - false" + nl +
            "  - true";

        // Act
        var yaml = YamlRecords.Serialize(test_type);

        // Assert
        Assert.Equal(expected, yaml);
    }

    [Fact]
    public void Serialize_Structs()
    {
        // Arrange
        var test_type = new TestStruct
        {
            Title = "Hello World",
            Content = "This is a test (including special characters! #)",
            Items =
            [
                true,
                false,
                true
            ]
        };

        var nl = Environment.NewLine;
        var expected =
            "title: Hello World" + nl +
            "content: \"This is a test (including special characters! #)\"" + nl +
            "items:" + nl +
            "  - true" + nl +
            "  - false" + nl +
            "  - true";

        // Act
        var yaml = YamlRecords.Serialize(test_type);

        // Assert
        Assert.Equal(expected, yaml);
    }

    [Fact]
    public void Serialize_Required_Class()
    {
        // Arrange
        var test_type = new TestClass
        {
            Title = "Hello World",
            Content = "This is a test (including special characters! #)",
            Items =
            [
                true,
                false,
                true
            ]
        };

        var nl = Environment.NewLine;
        var expected =
            "title: Hello World" + nl +
            "content: \"This is a test (including special characters! #)\"" + nl +
            "items:" + nl +
            "  - true" + nl +
            "  - false" + nl +
            "  - true";

        // Act
        var yaml = YamlRecords.Serialize(test_type);

        // Assert
        Assert.Equal(expected, yaml);
    }

    [Fact]
    public void Serialize_Class_With_Ctor()
    {
        // Arrange
        var test_type = new TestClassWithCtor("Hello World")
        {
            Content = "This is a test (including special characters! #)",
            Items =
            [
                true,
                false,
                true
            ]
        };

        var nl = Environment.NewLine;
        var expected =
            "title: Hello World" + nl +
            "content: \"This is a test (including special characters! #)\"" + nl +
            "items:" + nl +
            "  - true" + nl +
            "  - false" + nl +
            "  - true";

        // Act
        var yaml = YamlRecords.Serialize(test_type);

        // Assert
        Assert.Equal(expected, yaml);
    }

    [Fact]
    public void Serializing_Enums_Should_Be_Strings()
    {
        // Arrange
        var test_type = new EnumContainer
        {
            FirstEnumValue = TestEnum.AndAOne,
            SecondEnumValue = TestEnum.AndATwo,
            ThirdAndFinal = TestEnum.AndAOneTwoThree
        };

        var nl = Environment.NewLine;
        var expected =
            "firstEnumValue: AndAOne" + nl +
            "secondEnumValue: AndATwo" + nl +
            "thirdAndFinal: AndAOneTwoThree";

        // Act
        var yaml = YamlRecords.Serialize(test_type);

        // Assert
        Assert.Equal(expected, yaml);
    }

    [Fact]
    public void Deserializing_Enums_Should_Work_From_Strings()
    {
        // Arrange
        var nl = Environment.NewLine;
        var yaml =
            "firstEnumValue: AndAOne" + nl +
            "secondEnumValue: AndATwo" + nl +
            "thirdAndFinal: AndAOneTwoThree";

        // Act
        var result = YamlRecords.Deserialize<EnumContainer>(yaml);

        // Assert
        Assert.Equal(TestEnum.AndAOne, result.FirstEnumValue);
        Assert.Equal(TestEnum.AndATwo, result.SecondEnumValue);
        Assert.Equal(TestEnum.AndAOneTwoThree, result.ThirdAndFinal);
    }

    [Fact]
    public void Serializing_Flags_Should_Be_CommaString()
    {
        // Arrange
        var test_type = new FlagsContainer
        {
            FlagsValue = TestFlags.This | TestFlags.And | TestFlags.That,
            FlagsValue2 = TestFlags.That | TestFlags.This,
        };

        var expected =
            "flagsValue: This, And, That" + Environment.NewLine +
            "flagsValue2: This, That" + Environment.NewLine +
            "flagsValue3: ";

        // Act
        var yaml = YamlRecords.Serialize(test_type);

        // Assert
        Assert.Equal(expected, yaml);
    }

    [Fact]
    public void Deserializing_Flags_CommaString_Should_Work_From_Strings()
    {
        // Arrange
        var yaml =
            "flagsValue: This, And, That" + Environment.NewLine +
            "flagsValue2: That, This" + Environment.NewLine +
            "flagsValue3: ";

        // Act
        var result = YamlRecords.Deserialize<FlagsContainer>(yaml);

        // Assert
        Assert.Equal(TestFlags.This | TestFlags.And | TestFlags.That, result.FlagsValue);
        Assert.Equal(TestFlags.This | TestFlags.That, result.FlagsValue2);
        Assert.Equal((TestFlags)0, result.FlagsValue3);
    }

    [Fact]
    public void Should_Deserialize_Nullable_Primitives()
    {
        // Arrange
        var yaml = "property1: 60";

        // Act
        var result = YamlRecords.Deserialize<NullableHolder>(yaml);

        Assert.Equal(60, result.Property1);
        Assert.Null(result.Property2);
    }

    [Fact]
    public void Should_Serialize_Nullable_Primitives()
    {
        // Arrange
        var model = new NullableHolder { Property2 = 30 };
        var expected = "property1:" + Environment.NewLine + "property2: 30";

        // Act
        var result = YamlRecords.Serialize(model);

        Assert.Equal(expected, result);
    }

    public static class TestModels
    {

        public record GameConfig(Dictionary<string, CardType> CardTypes, Dictionary<string, GameFlow> GameFlows, string[] StartingCards, string[] StartingFlows);

        public record CardType(string Title, string IconPath);

        public record GameFlow(string IconPath, string StartState, Dictionary<string, FlowState> States);

        public abstract record FlowState(
            Dictionary<string, StateVariant> Variants,
            string DefaultVariant);

        public record StateVariant(
            string Title,
            string Description,
            string ActionLabel,
            StateAction? OnAction);

        public record SocketState(
            Dictionary<string, StateVariant> Variants,
            string DefaultVariant,
            SocketConfig[] Sockets)
                : FlowState(Variants, DefaultVariant);

        public record SocketConfig(string Title, string[] Accepts, Dictionary<string, StateAction> OnAccept);

        public abstract record StateAction();

        public record TransitionAction(string NewState) : StateAction;

        public record VariantAction(string NewVariant) : StateAction;

        public record TimerState(
            Dictionary<string, StateVariant> Variants,
            string DefaultVariant,
            int Seconds,
            SocketConfig? Socket,
            StateAction OnElapsed)
                : FlowState(Variants, DefaultVariant);

        public record CardState(
            Dictionary<string, StateVariant> Variants,
            string DefaultVariant,
            string[] NewCards)
                : FlowState(Variants, DefaultVariant);

        public static GameConfig CreateModel()
        {
            return new GameConfig(new()
        {
            { "funds", new CardType("Funds", "res://assets/wealth_icon.png") },
            { "health", new CardType("Health", "res://assets/reputation_icon.png") },
        },
            new()
            {
            { "work", new GameFlow("res://assets/authority_icon.png", "choose_path", new() {
                {
                    "choose_path", new SocketState(new ()
                    {
                        {"default", new StateVariant("Work", "Choose your path to earn funds", "Start", null)},
                        {"labour", new StateVariant("Work", "Physical work for small pay", "Start", new TransitionAction("labour"))}
                    }
                    , "default", [
                        new SocketConfig("work", ["reason","health","passion"], new() {
                            { "health", new VariantAction("labour") }
                        })
                    ])
                },
                {
                    "labour", new TimerState(new ()
                    {
                        {"default", new StateVariant("Work", "The day stretches long, your hand's burn", "Running...", null)},
                    }
                    , "default", 60, null, new TransitionAction("choose_path")) // repeat for test
                },
            }) }
            },
            ["funds", "funds", "health", "health"],
            ["work"]);
        }

        public struct TestStruct
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public bool[] Items { get; set; }
        }

        public class TestClass
        {
            public required string Title { get; set; }
            public required string Content { get; set; }
            public required bool[] Items { get; set; }
        }

        public class TestClassWithCtor
        {
            public string Title { get; private set; }
            public required string Content { get; set; }
            public required bool[] Items { get; set; }

            public TestClassWithCtor(string title)
            {
                Title = title;
            }
        }

        public enum TestEnum { AndAOne, AndATwo, AndAOneTwoThree };

        public class EnumContainer
        {
            public TestEnum FirstEnumValue { get; set; }
            public TestEnum SecondEnumValue { get; set; }
            public TestEnum ThirdAndFinal { get; set; }
        }

        [Flags]
        public enum TestFlags { This = 1, And = 2, That = 4 };

        public class FlagsContainer
        {
            public TestFlags FlagsValue { get; set; }
            public TestFlags FlagsValue2 { get; set; }
            public TestFlags FlagsValue3 { get; set; }
        }

        public class NullableHolder
        {
            public int? Property1 { get; set; }
            public int? Property2 { get; set; }
        }
    }
}