
using System.Collections.Generic;

namespace YamlRecords
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

    public class Test
    {
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