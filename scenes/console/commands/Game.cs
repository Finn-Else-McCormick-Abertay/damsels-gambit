using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit.Commands;

public class Game : Console.Command
{
    [Verb("default", isDefault: true)]
    class DefaultOptions {
        [Option]
        public bool SkipIntro { get; set; } = false;
    }

    [Verb("start")]
    class StartOptions {
        [Value(0, Required = false)]
        public string Suitor { get; set; } = null;

        [Option]
        public bool SkipIntro { get; set; } = false;
    }

    [Verb("end")]
    class EndOptions {
    }

    [Verb("get")]
    class GetOptions {
        [Value(0)]
        public IEnumerable<string> What { get; set; }

        [Option(SetName = "fulldeck")]
        public bool All { get; set; }
        
        [Option(SetName = "fulldeck")]
        public bool Remaining { get; set; }

        [Option(SetName = "inhand")]
        public bool InHand { get; set; }
        
        [Option]
        public bool? Topic { get; set; }
        [Option]
        public bool? Action { get; set; }
    }

    [Verb("set")]
    class SetOptions {
        [Value(0)]
        public string What { get; set; }
        
        [Value(1)]
        public string NewValue { get; set; }
    }

    public override void Parse(Parser parser, IEnumerable<string> args)
    {
        var cardGame = GameManager.CardGameController;

        var result = parser.ParseArguments<GetOptions, SetOptions, StartOptions, EndOptions, DefaultOptions>(args);
        result.WithParsed<GetOptions>(options => {
            if (cardGame is null) { Console.Error("Game scene not instantiated.", false); return; }

            if (!options.What.Any()) return;

            if (options.What.Count() == 1 && options.What.First().MatchN("round"))
                Console.Info($"Round {cardGame.Round}/{cardGame.NumRounds} ({(cardGame.Round > cardGame.NumRounds ? "Finished" : $"{cardGame.NumRounds - cardGame.Round + 1} Remaining")})");

            if (options.What.First().MatchN("cards") || options.What.First().MatchN("card")) {
                Console.Info($"{options.Topic}, {options.Action}");

                string cardTypeString =
                    (options.Topic is null && options.Action is null) || (options.Topic is not null && (options.Topic ?? false) && options.Action is not null && (options.Action ?? false)) ? "" : options.Topic ?? false ? "Topic" : "Action";
                bool showTopic = (options.Topic is null && options.Action is null) || (options.Topic ?? false);
                bool showAction = (options.Topic is null && options.Action is null) || (options.Action ?? false);

                string msg = ""; IEnumerable<string> cards = [];
                if (options.InHand) {
                    List<Node> nodes = []; if (showTopic) nodes.AddRange(cardGame.TopicHand.GetChildren()); if (showAction) nodes.AddRange(cardGame.ActionHand.GetChildren());
                    cards = nodes.Where(x => x is CardDisplay).Select(x => (x as CardDisplay).CardId.ToString());
                    msg = $"{cardTypeString}{(string.IsNullOrEmpty(cardTypeString) ? "" : " ")}Hand";
                }
                else if ((options.All && !options.Remaining) || (!options.InHand && !options.All && !options.Remaining)) {
                    List<KeyValuePair<string, int>> pairs = []; if (showTopic) pairs.AddRange(cardGame.FullLayoutTopicDeck); if (showAction) pairs.AddRange(cardGame.FullLayoutActionDeck);
                    cards = pairs.Aggregate(new List<string>(), (list, x) => { for (int i = 0; i < x.Value; ++i) list.Add(x.Key); return list; });
                    msg = $"Full {cardTypeString}{(string.IsNullOrEmpty(cardTypeString) ? "" : " ")}Deck";
                }
                else if (options.Remaining) {
                    if (showTopic) cards = cards.Concat(cardGame.TopicDeck.Remaining); if (showAction) cards = cards.Concat(cardGame.ActionDeck.Remaining);
                    msg = $"{cardTypeString}{(string.IsNullOrEmpty(cardTypeString) ? "" : " Cards ")}Remaining In Deck";
                }
                Console.Info($"{msg}: {string.Join(", ", cards)}");
            }
        });

        result.WithParsed<SetOptions>(options => {
            if (cardGame is null) { Console.Error("Game scene not instantiated.", false); return; }

            if (options.What.MatchN("round")) {
                if (int.TryParse(options.NewValue, out int result)) cardGame.Round = result;
                else Console.Error($"'{options.NewValue}' could not be parsed as an integer.", false);
            }

            if (options.What.MatchN("score")) {
                if (int.TryParse(options.NewValue, out int result)) cardGame.Score = result;
                else Console.Error($"'{options.NewValue}' could not be parsed as an integer.", false);
            }
        });

        result.WithParsed<StartOptions>(options => {
            if (options.Suitor is not null) {
                GameManager.SwitchToCardGameScene($"res://scenes/dates/{Case.ToSnake(options.Suitor)}_date.tscn");
                if (options.SkipIntro) CallableUtils.CallDeferred(() => GameManager.CardGameController?.ForceSkipIntro());
            }
            else if (cardGame is null) {
                GameManager.BeginGame();
                if (options.SkipIntro) CallableUtils.CallDeferred(() => GameManager.CardGameController?.ForceSkipIntro());
            }
        });

        result.WithParsed<EndOptions>(options => {
            if (cardGame is null) { Console.Error("Game scene not instantiated.", false); return; }
            cardGame.ForceGameEnd();
        });

        result.WithParsed<DefaultOptions>(options => {
            if (cardGame is null) { Console.Error("Game scene not instantiated.", false); return; }
            if (options.SkipIntro) GameManager.CardGameController?.ForceSkipIntro();
        });

        result.WithNotParsed(err => {
            foreach (var error in err) Console.Error(error.ToString(), false);
            var helpText = HelpText.AutoBuild(result, h => {
                h.Heading = new HeadingInfo(nameof(Game));
                h.Copyright = ""; h.AutoHelp = true; h.AutoVersion = false;
                return h;
            }, e => e);
            Console.Print(helpText);
        });
    }

    
    public override IEnumerable<string> GetAutofill(string[] args) => args.Length switch {
        1 => [ "start", "end", "get", "set" ],
        > 1 => args.First() switch {
            "get" => [ "round", "card", "cards" ],
            "set" when args.Length == 2 => [ "round", "score" ],
            "start" when args.Length == 2 => [ "tutorial", "frostholm", "--skipintro" ],
            "start" when args.Length == 3 && args[2] == "--skipintro" => [ "tutorial", "frostholm" ],
            "start" when args.Length == 3 && args[2] != "--skipintro" => [ "--skipintro" ],
            "end" => [],
            _ => []
        },
        _ => []
    };
}