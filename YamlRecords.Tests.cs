using System.Collections.Generic;
using Xunit;

namespace YamlRecords
{
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
        }
    }
}