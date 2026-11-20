using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CheatNotify.Core), "CheatNotify", "1.0.2", "alydev", null)]
[assembly: MelonGame("Finn Schuuring", "King of the Bridge")]

namespace CheatNotify;

public class Core : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("Initialized.");
    }

    [HarmonyPatch(typeof(ChessMatchManager), nameof(ChessMatchManager.CompleteMove))]
    public static class CompleteMoveWatcher
    {
        private static int _lastCheatLength;
        private static ChessPieceObject _lastCheatPiece;

        private static string Position(Vector3 position)
        {
            string rank = $"{position.x}";
            if (Ranks.TryGetValue(rank, out char letter))
                rank = $"{letter}";
            else
                rank = $"({rank})";
            string file = $"{position.y}";
            if (Files.TryGetValue(file, out char number))
                file = $"{number}";
            else
                file = $"({file})";
            return rank + file;
        }
        private readonly static Dictionary<string, char> Ranks = new() {
            { "-3.5", 'A' },
            { "-2.5", 'B' },
            { "-1.5", 'C' },
            { "-0.5", 'D' },
            { "0.5", 'E' },
            { "1.5", 'F' },
            { "2.5", 'G' },
            { "3.5", 'H' },
        };
        private readonly static Dictionary<string, char> Files = new() {
            { "-3.5", '1' },
            { "-2.5", '2' },
            { "-1.5", '3' },
            { "-0.5", '4' },
            { "0.5", '5' },
            { "1.5", '6' },
            { "2.5", '7' },
            { "3.5", '8' },
        };

        private static (RuleBookPage page, int score) CheatReasonsToRuleBookPage(
            RuleBookScreenManager manager,
            List<ChessMatchManager.ChessCheatReason> cheatReasons,
            ChessPieceData.ChessPieceType pieceType)
        {
            RuleBookScreenData ruleBookScreenData = manager.ruleBookScreens[1];
            RuleBookPage result = ruleBookScreenData.ruleBookPages[1];
            int score = 0;
            for (int i = 0; i <= manager.ruleBookScreens.Count - 1; i++)
            {
                foreach (ChessMatchManager.ChessCheatReason cheatReason in cheatReasons)
                {
                    foreach (RuleBookPage ruleBookPage in manager.ruleBookScreens[i].ruleBookPages)
                    {
                        int _score = 0;
                        if (ruleBookPage.ruleCheatReason == cheatReason)
                        {
                            _score = ruleBookPage.ruleCheatScore;
                            if (!ruleBookPage.checkForSpecificPiece)
                            {
                                _score *= 2;
                            }
                        }

                        if (ruleBookPage.ruleSpecificPiece == pieceType && ruleBookPage.checkForSpecificPiece)
                        {
                            _score += ruleBookPage.ruleCheatScore;
                        }

                        if (_score > score)
                        {
                            result = ruleBookPage;
                            ruleBookScreenData = manager.ruleBookScreens[i];
                            score = _score;
                        }
                    }
                }
            }
            return (result, score);
        }

        public static void Postfix(ChessMatchManager __instance, ref Vector3 originPosition, ref Vector3 movedPosition)
        {
            ChessPieceObject piece = __instance.lastTurnMovedChessPiece;
            List<ChessMatchManager.ChessCheatReason> cheatReasons = __instance.lastTurnCheatReasons;
            if (cheatReasons.Count == _lastCheatLength)
                return;
            _lastCheatLength = cheatReasons.Count;
            if (_lastCheatLength == 0)
                return;
            _lastCheatPiece = piece;
            string reasons = string.Join(", ", cheatReasons.Select(x => Enum.GetName(typeof(ChessMatchManager.ChessCheatReason), x)));
            (var ruleBookPage, var score) = CheatReasonsToRuleBookPage(__instance.ruleBookScreenManager, cheatReasons, piece.pieceData.pieceType);
            SoundManager.LoadSoundEffect(piece.transform, SoundManager.instance.chess_accusation_false);
            Melon<Core>.Logger.Warning($"Cheater! {_lastCheatPiece.pieceColor} {_lastCheatPiece.pieceData.name} {ruleBookPage.ruleBreakText} (Position: {Position(originPosition)} to {Position(movedPosition)}; Score: {score}; {reasons})");
        }

        internal static void Reset()
        {
            _lastCheatLength = 0;
            _lastCheatPiece = null;
        }
    }

    [HarmonyPatch(typeof(ChessMatchManager), nameof(ChessMatchManager.SetupChessMatch))]
    public static class SetupMatchWatcher
    {
        public static void Postfix()
        {
            Melon<Core>.Logger.Msg("Resetting.");
            CompleteMoveWatcher.Reset();
        }
    }
}
