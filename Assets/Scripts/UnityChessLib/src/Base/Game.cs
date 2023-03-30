using System.Collections.Generic;

namespace UnityXiangqi
{
	/// <summary>Representation of a standard chess game including a history of moves made.</summary>
	public class Game {
		public Timeline<GameConditions> ConditionsTimeline { get; }
		public Timeline<Board> BoardTimeline { get; }
		public Timeline<HalfMove> HalfMoveTimeline { get; }
		public Timeline<Dictionary<Piece, Dictionary<(Square, Square), Movement>>> LegalMovesTimeline { get; }

        /// <summary>Creates a Game instance of a given mode with a standard starting Board.</summary>
        public Game() : this(GameConditions.NormalStartingConditions, Board.StartingPositionPieces) { }

		public Game(GameConditions startingConditions, params (Square, Piece)[] squarePiecePairs) {
			Board startingBoard = new(squarePiecePairs);
            BoardTimeline = new Timeline<Board> { startingBoard };
			HalfMoveTimeline = new Timeline<HalfMove>();
			ConditionsTimeline = new Timeline<GameConditions> { startingConditions };
            LegalMovesTimeline = new Timeline<Dictionary<Piece, Dictionary<(Square, Square), Movement>>> {
                startingBoard.CalculateLegalMoves(startingConditions.SideToMove)
            };
		}

		/// <summary>Executes passed move and switches sides; also adds move to history.</summary>
		public bool TryExecuteMove(Movement move) {
			if (!TryGetLegalMove(move.Start, move.End, out Movement validatedMove)) {
				return false;
			}

			//create new copy of previous current board, and execute the move on it
			BoardTimeline.TryGetCurrent(out Board boardBeforeMove);
			Board resultingBoard = new Board(boardBeforeMove);
			resultingBoard.MovePiece(validatedMove);
			BoardTimeline.AddNext(resultingBoard);
			
			ConditionsTimeline.TryGetCurrent(out GameConditions conditionsBeforeMove); 
			Side updatedSideToMove = conditionsBeforeMove.SideToMove.Complement();
			bool causedCheck = Rules.IsPlayerInCheck(resultingBoard, updatedSideToMove);
			bool capturedPiece = boardBeforeMove[validatedMove.End] != null || validatedMove is EnPassantMove;
			
			HalfMove halfMove = new HalfMove(boardBeforeMove[validatedMove.Start], validatedMove, capturedPiece, causedCheck);
			GameConditions resultingGameConditions = conditionsBeforeMove.CalculateEndingConditions(boardBeforeMove, halfMove);
			ConditionsTimeline.AddNext(resultingGameConditions);

			Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMovesByPiece
				= resultingBoard.CalculateLegalMoves(resultingGameConditions.SideToMove);

			int numLegalMoves = GetNumLegalMoves(legalMovesByPiece);

			LegalMovesTimeline.AddNext(legalMovesByPiece);

			halfMove.SetGameEndBools(
				Rules.IsPlayerStalemated(resultingBoard, updatedSideToMove, numLegalMoves),
				Rules.IsPlayerCheckmated(resultingBoard, updatedSideToMove, numLegalMoves)
			);
			HalfMoveTimeline.AddNext(halfMove);
			
			return true;
		}

		public bool TryGetLegalMove(Square startSquare, Square endSquare, out Movement move) {
			move = null;
            // LegalMovesTimeline now include self-checked moves
            return BoardTimeline.TryGetCurrent(out Board currentBoard)
				   && LegalMovesTimeline.TryGetCurrent(out Dictionary<Piece, Dictionary<(Square, Square), Movement>> currentLegalMoves)
				   && currentBoard[startSquare] is Piece movingPiece
				   && currentLegalMoves.TryGetValue(movingPiece, out Dictionary<(Square, Square), Movement> movesByStartEndSquares)
				   && movesByStartEndSquares.TryGetValue((startSquare, endSquare), out move)
				   // need this to confirm if the move dont cause self-checked
				   && !Rules.MoveCauseSelfChecked(currentBoard, move, movingPiece.Owner);
		}

		public bool TryGetLegalMovesForPiece(Square square, out ICollection<Movement> legalMoves)
		{
			legalMoves = null;
			return BoardTimeline.TryGetCurrent(out Board currentBoard)
					&& currentBoard[square] is { } movingPiece
					&& TryGetLegalMovesForPiece(movingPiece, out legalMoves);
        }


        public bool TryGetLegalMovesForPiece(Piece movingPiece, out ICollection<Movement> legalMoves) {
			legalMoves = null;

			if (movingPiece != null
			    && LegalMovesTimeline.TryGetCurrent(out Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMovesByPiece)
			    && legalMovesByPiece.TryGetValue(movingPiece, out Dictionary<(Square, Square), Movement> movesByStartEndSquares)
			    && movesByStartEndSquares != null
			) {
				legalMoves = movesByStartEndSquares.Values;
				return true;
			}

			return false;
		}

		public bool ResetGameToHalfMoveIndex(int halfMoveIndex) {
			if (HalfMoveTimeline.HeadIndex == -1) {
				return false;
			}

			BoardTimeline.HeadIndex = halfMoveIndex + 1;
			ConditionsTimeline.HeadIndex = halfMoveIndex + 1;
			LegalMovesTimeline.HeadIndex = halfMoveIndex + 1;
            HalfMoveTimeline.HeadIndex = halfMoveIndex;

			return true;
		}
		
		internal static int GetNumLegalMoves(Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMovesByPiece) {
			int result = 0;
			
			if (legalMovesByPiece != null) {
				foreach (Dictionary<(Square, Square), Movement> movesByStartEndSquares in legalMovesByPiece.Values) {
					result += movesByStartEndSquares.Count;
				}
			}

			return result;
		}
		
		

	}
}