using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityXiangqi;
using UnityXiangqi.Engine;

using static UnityXiangqi.GlobalVar;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    public static event Action NewGameStartedEvent;
    public static event Action GameEndedEvent;
    public static event Action GameResetToHalfMoveEvent;
    public static event Action MoveExecutedEvent;

    public delegate void NewMessageAction(string message);
    public static event NewMessageAction NewMessage;

    public Board CurrentBoard
    {
        get
        {
            game.BoardTimeline.TryGetCurrent(out Board currentBoard);
            return currentBoard;
        }
    }

    public Side SideToMove
    {
        get
        {
            game.ConditionsTimeline.TryGetCurrent(out GameConditions currentConditions);
            return currentConditions.SideToMove;
        }
    }

    public Side StartingSide => game.ConditionsTimeline[0].SideToMove;
    public Timeline<HalfMove> HalfMoveTimeline => game.HalfMoveTimeline;
    public int LatestHalfMoveIndex => game.HalfMoveTimeline.HeadIndex;
    public int FullMoveNumber => StartingSide switch
    {
        Side.White => LatestHalfMoveIndex / 2 + 1,
        Side.Black => (LatestHalfMoveIndex + 1) / 2 + 1,
        _ => -1
    };

    private bool isWhiteAI;
    private bool isBlackAI;

    public List<(Square, Piece)> CurrentPieces
    {
        get
        {
            currentPiecesBacking.Clear();
            for (int file = 1; file <= FILE_MAX; file++)
            {
                for (int rank = 1; rank <= RANK_MAX; rank++)
                {
                    Piece piece = CurrentBoard[file, rank];
                    if (piece != null) currentPiecesBacking.Add((new Square(file, rank), piece));
                }
            }

            return currentPiecesBacking;
        }
    }


    private readonly List<(Square, Piece)> currentPiecesBacking = new List<(Square, Piece)>();

    private Game game;
    private FENSerializer fenSerializer;
    private PGNSerializer pgnSerializer;
    private CancellationTokenSource promotionUITaskCancellationTokenSource;
    private ElectedPiece userPromotionChoice = ElectedPiece.None;
    private Dictionary<GameSerializationType, IGameSerializer> serializersByType;
    private GameSerializationType selectedSerializationType = GameSerializationType.FEN;

    private IUCIEngine uciEngine;

    public void Start()
    {
        //VisualPiece.VisualPieceMoved += OnPieceMoved;
        //VisualPiece.VisualPieceSelected += OnPieceSelected;
        BoardClicker.VisualPieceMoved += OnPieceMoved;
        BoardClicker.VisualPieceSelected += OnPieceSelected;

        serializersByType = new Dictionary<GameSerializationType, IGameSerializer>
        {
            [GameSerializationType.FEN] = new FENSerializer(),
            [GameSerializationType.PGN] = new PGNSerializer()
        };

        StartNewGame();

#if DEBUG_VIEW
		unityChessDebug.gameObject.SetActive(true);
		unityChessDebug.enabled = true;
#endif
    }

    private void OnDestroy()
    {
        uciEngine?.ShutDown();
    }

#if AI_TEST
	public async void StartNewGame(bool isWhiteAI = true, bool isBlackAI = true) {
#else
    public async void StartNewGame(bool isWhiteAI = false, bool isBlackAI = true /* FIXME false*/)
    {
#endif
        game = new Game();

        this.isWhiteAI = isWhiteAI;
        this.isBlackAI = isBlackAI;

        if (isWhiteAI || isBlackAI)
        {
            if (uciEngine == null)
            {
#if UNITY_ANDROID
                uciEngine = new AndroidUCIEngine();
#elif UNITY_STANDALONE_WIN
                uciEngine = new MockUCIEngine();
#else
                Debug.Log("Unsupported platform");
#endif
                uciEngine.Start();
            }

            uciEngine.SetupNewGame(game);
            NewGameStartedEvent?.Invoke();

            //if (isWhiteAI)
            //{
            //    Movement bestMove = await uciEngine.GetBestMove(10_000);
            //    DoAIMove(bestMove);
            //}
        }
        else
        {
            NewGameStartedEvent?.Invoke();
        }
    }

    public string SerializeGame()
    {
        return serializersByType.TryGetValue(selectedSerializationType, out IGameSerializer serializer)
            ? serializer?.Serialize(game)
            : null;
    }

    public void LoadGame(string serializedGame)
    {
        game = serializersByType[selectedSerializationType].Deserialize(serializedGame);
        NewGameStartedEvent?.Invoke();
    }

    public void ResetGameToHalfMoveIndex(int halfMoveIndex)
    {
        if (!game.ResetGameToHalfMoveIndex(halfMoveIndex)) return;

        //UIManager.Instance.SetActivePromotionUI(false);
        //promotionUITaskCancellationTokenSource?.Cancel();
        GameResetToHalfMoveEvent?.Invoke();
    }

    private bool TryExecuteMove(Movement move)
    {
        if (!game.TryExecuteMove(move))
        {
            return false;
        }

        HalfMoveTimeline.TryGetCurrent(out HalfMove latestHalfMove);
        if (latestHalfMove.CausedCheckmate || latestHalfMove.CausedStalemate)
        {
            BoardManager.Instance.SetActiveAllPieces(false);
            GameEndedEvent?.Invoke();
        }
        else
        {
            BoardManager.Instance.EnsureOnlyPiecesOfSideAreEnabled(SideToMove);
        }

        MoveExecutedEvent?.Invoke();

        return true;
    }

    //private async Task<bool> TryHandleSpecialMoveBehaviourAsync(SpecialMove specialMove)
    //{
    //    switch (specialMove)
    //    {
    //        //case CastlingMove castlingMove:
    //        //    BoardManager.Instance.CastleRook(castlingMove.RookSquare, castlingMove.GetRookEndSquare());
    //        //    return true;
    //        //case EnPassantMove enPassantMove:
    //        //    BoardManager.Instance.TryDestroyVisualPiece(enPassantMove.CapturedPawnSquare);
    //        //    return true;
    //        //case PromotionMove { PromotionPiece: null } promotionMove:
    //        //    UIManager.Instance.SetActivePromotionUI(true);
    //        //    BoardManager.Instance.SetActiveAllPieces(false);

    //        //    promotionUITaskCancellationTokenSource?.Cancel();
    //        //    promotionUITaskCancellationTokenSource = new CancellationTokenSource();

    //        //    ElectedPiece choice = await Task.Run(GetUserPromotionPieceChoice, promotionUITaskCancellationTokenSource.Token);

    //        //    UIManager.Instance.SetActivePromotionUI(false);
    //        //    BoardManager.Instance.SetActiveAllPieces(true);

    //        //    if (promotionUITaskCancellationTokenSource == null
    //        //        || promotionUITaskCancellationTokenSource.Token.IsCancellationRequested
    //        //    ) { return false; }

    //        //    promotionMove.SetPromotionPiece(
    //        //        PromotionUtil.GeneratePromotionPiece(choice, SideToMove)
    //        //    );
    //        //    BoardManager.Instance.TryDestroyVisualPiece(promotionMove.Start);
    //        //    BoardManager.Instance.TryDestroyVisualPiece(promotionMove.End);
    //        //    BoardManager.Instance.CreateAndPlacePieceGO(promotionMove.PromotionPiece, promotionMove.End);

    //        //    promotionUITaskCancellationTokenSource = null;
    //        //    return true;
    //        case PromotionMove promotionMove:
    //            BoardManager.Instance.TryDestroyVisualPiece(promotionMove.Start);
    //            BoardManager.Instance.TryDestroyVisualPiece(promotionMove.End);
    //            BoardManager.Instance.CreateAndPlacePieceGO(promotionMove.PromotionPiece, promotionMove.End);

    //            return true;
    //        default:
    //            return false;
    //    }
    //}

    //private ElectedPiece GetUserPromotionPieceChoice()
    //{
    //    while (userPromotionChoice == ElectedPiece.None) { }

    //    ElectedPiece result = userPromotionChoice;
    //    userPromotionChoice = ElectedPiece.None;
    //    return result;
    //}

    public void ElectPiece(ElectedPiece choice)
    {
        userPromotionChoice = choice;
    }

    private void OnPieceSelected(Square selectedSquare)
    {
        if (game.TryGetLegalMovesForPiece(selectedSquare, out ICollection<Movement> legalMoves))
        {
            Debug.Log($"Select {selectedSquare} got {legalMoves.Count} moves");
            //foreach (Movement move in legalMoves)
            //{
            //    Debug.Log($"  {move.Start} -> {move.End}");
            //}
            BoardManager.Instance.HighlightSquares(selectedSquare, legalMoves);
        }

    }

//    private async void OnPieceMoved_Async(Square movedPieceInitialSquare, Transform movedPieceTransform, Transform closestBoardSquareTransform, Piece promotionPiece = null)
//    {
//        Square endSquare = new Square(closestBoardSquareTransform.name);

//        BoardManager.Instance.StopHighlight();

//        Debug.Log($"moving {movedPieceInitialSquare} -> {endSquare}");
//        if (movedPieceInitialSquare == endSquare)
//        {
//            movedPieceTransform.position = movedPieceTransform.parent.position;
//            return;
//        }

//        if (!game.TryGetLegalMove(movedPieceInitialSquare, endSquare, out Movement move))
//        {
//            movedPieceTransform.position = movedPieceTransform.parent.position;
//            if (move == null) NewMessage?.Invoke($"Illegal Move");
//            else NewMessage?.Invoke($"In-checked");
//#if DEBUG_VIEW
//			Piece movedPiece = CurrentBoard[movedPieceInitialSquare];
//			game.TryGetLegalMovesForPiece(movedPiece, out ICollection<Movement> legalMoves);
//			UnityChessDebug.ShowLegalMovesInLog(legalMoves);
//#endif
//            return;
//        }

//        if (move is PromotionMove promotionMove)
//        {
//            promotionMove.SetPromotionPiece(promotionPiece);
//        }

//        if ((move is not SpecialMove specialMove || await TryHandleSpecialMoveBehaviourAsync(specialMove))
//            && TryExecuteMove(move)
//        )
//        {
//            if (move is not SpecialMove) { BoardManager.Instance.TryDestroyVisualPiece(move.End); }

//            if (move is PromotionMove)
//            {
//                movedPieceTransform = BoardManager.Instance.GetPieceGOAtPosition(move.End).transform;
//            }

//            movedPieceTransform.parent = closestBoardSquareTransform;
//            movedPieceTransform.position = closestBoardSquareTransform.position;
//        }

//        bool gameIsOver = game.HalfMoveTimeline.TryGetCurrent(out HalfMove lastHalfMove)
//                          && lastHalfMove.CausedStalemate || lastHalfMove.CausedCheckmate;
//        if (!gameIsOver
//            && (SideToMove == Side.White && isWhiteAI
//                || SideToMove == Side.Black && isBlackAI)
//        )
//        {
//            Movement bestMove = await uciEngine.GetBestMove(10_000);
//            DoAIMove(bestMove);
//        }
//    }

    private async void OnPieceMoved(/*Square movedPieceInitialSquare, Transform movedPieceTransform*/VisualPiece movedPiece, Transform closestBoardSquareTransform) {
        Square movedPieceInitialSquare = movedPiece.CurrentSquare;
        Square endSquare = new Square(closestBoardSquareTransform.name);


        Debug.Log($"moving {movedPieceInitialSquare} -> {endSquare}");
        if (movedPieceInitialSquare == endSquare) {
            //movedPieceTransform.position = movedPieceTransform.parent.position;
            return;
        }

        if (!game.TryGetLegalMove(movedPieceInitialSquare, endSquare, out Movement move)) {
            Debug.Log($"Cannot get the move {movedPieceInitialSquare} {endSquare} {move}");
            //movedPieceTransform.position = movedPieceTransform.parent.position;
            if (move == null) NewMessage?.Invoke($"Illegal Move");
            else NewMessage?.Invoke($"In-checked");
#if DEBUG_VIEW
			Piece movedPiece = CurrentBoard[movedPieceInitialSquare];
			game.TryGetLegalMovesForPiece(movedPiece, out ICollection<Movement> legalMoves);
			UnityChessDebug.ShowLegalMovesInLog(legalMoves);
#endif
            return;
        }

        //if (move is PromotionMove promotionMove) {
        //    promotionMove.SetPromotionPiece(promotionPiece);
        //}

        if (TryExecuteMove(move)) {
            BoardClicker.Instance.DeselectPiece();
            BoardManager.Instance.updateMove(movedPiece, endSquare);
            //BoardManager.Instance.TryDestroyVisualPiece(move.End);
            //movedPiece.transform.parent = closestBoardSquareTransform;
            //movedPiece.transform.position = closestBoardSquareTransform.position;
        }

        bool gameIsOver = game.HalfMoveTimeline.TryGetCurrent(out HalfMove lastHalfMove)
                          && lastHalfMove.CausedStalemate || lastHalfMove.CausedCheckmate;
        if (!gameIsOver
            && (SideToMove == Side.White && isWhiteAI
                || SideToMove == Side.Black && isBlackAI)
        ) {
            Movement bestMove = uciEngine.GetBestMove(1000);
            DoAIMove(bestMove);
        }
    }


    private void DoAIMove(Movement move) {
        //GameObject movedPiece = BoardManager.Instance.GetPieceGOAtPosition(move.Start);
        VisualPiece movedPiece = BoardManager.Instance.GetVisualPieceAtPosition(move.Start);
        GameObject endSquareGO = BoardManager.Instance.GetSquareGOByPosition(move.End);
        OnPieceMoved(
            movedPiece,
            endSquareGO.transform
        );
    }

    //public bool HasLegalMoves(Piece piece) {
    //	return game.TryGetLegalMovesForPiece(piece, out _);
    //}
}