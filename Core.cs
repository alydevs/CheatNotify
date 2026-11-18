using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CheatNotify.Core), "CheatNotify", "1.0.0", "alydev", null)]
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

        public static void Postfix(ChessMatchManager __instance)
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
            Melon<Core>.Logger.Warning($"Cheater! {_lastCheatPiece.pieceColor} {_lastCheatPiece.pieceData.name}: {reasons}");
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
