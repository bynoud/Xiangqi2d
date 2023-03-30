using System;
using System.Collections.Generic;
using static UnityXiangqi.GlobalVar;

namespace UnityXiangqi
{

	class SideBoard
	{
		private readonly Side Owner;
		private Board board = null;
		internal Piece KingPiece = null;
		internal List<Piece> Pieces = new ();
		private Dictionary<Piece, Dictionary<(Square, Square), Movement>> legalMoves = null;
		private Dictionary<Square, bool> attackedSquares = null;

        public SideBoard(Side side)
		{
			Owner = side;
		}

		public void Clear()
		{
            Pieces.Clear();
			board = null;
            KingPiece = null;
            legalMoves = null;
            attackedSquares = null;
		}

		public void SetBoard(Board setBoard) {
			Clear();
			board = setBoard;
            for (int file = 1; file <= FILE_MAX; file++)
            {
                for (int rank = 1; rank <= RANK_MAX; rank++)
                {
					Piece piece = board[file, rank];
                    if (piece == null || piece.Owner != Owner) continue;
					if (piece is King) KingPiece = piece;
                    Pieces.Add (piece);
                }
            }
        }

		public Dictionary<Piece, Dictionary<(Square, Square), Movement>> GetLegalMoves()
		{
			if (legalMoves != null) return legalMoves;

            legalMoves = new();
			foreach (Piece piece in Pieces)
			{
				var calculatedMoves = piece.CalculateLegalMoves(board, piece.Position);
                if (calculatedMoves != null) legalMoves[piece] = calculatedMoves;
			}

            return legalMoves;
        }

		public bool IsAttackingSquare(Square target)
		{
			//if (attackedSquares != null && attackedSquares.ContainsKey(target))
			//{
			//	return attackedSquares[target];
			//}

			//attackedSquares ??= new();
			foreach (Piece piece in Pieces)
			{
				if (piece.IsAttackingTo(board, piece.Position, target))
				{
					//attackedSquares[target] = true;
					return true;
				}
			}
			//attackedSquares[target] = false;
			return false;
        }

    }

	/// <summary>An 8x8 matrix representation of a chessboard.</summary>
	public class Board {
		private readonly Piece[,] boardMatrix;
		//private readonly Dictionary<Side, Square?> currentKingSquareBySide = new Dictionary<Side, Square?> {
		//	[Side.White] = null,
		//	[Side.Black] = null
		//};
		private Dictionary<Side, SideBoard> sideBoard = new()
		{
			[Side.White] = new(Side.White),
			[Side.Black] = new(Side.Black),
		};
		//private Dictionary<Side, List<Piece>> currentPiecesBySide = null;

        public Piece this[Square position] {
			get {
				if (position.IsValid()) return boardMatrix[position.File - 1, position.Rank - 1];
				throw new ArgumentOutOfRangeException($"Position was out of range: {position}");
			}

			set {
				if (position.IsValid()) boardMatrix[position.File - 1, position.Rank - 1] = value;
				else throw new ArgumentOutOfRangeException($"Position was out of range: {position}");
			}
		}

		public Piece this[int file, int rank] {
			get => this[new Square(file, rank)];
			set => this[new Square(file, rank)] = value;
		}

		/// <summary>Creates a Board given the passed square-piece pairs.</summary>
		public Board(params (Square, Piece)[] squarePiecePairs) {
			boardMatrix = new Piece[9, 10];
			
			foreach ((Square position, Piece piece) in squarePiecePairs) {
				piece.Position = position;
				this[position] = piece;
			}
			Refresh();
        }

		/// <summary>Creates a deep copy of the passed Board.</summary>
		public Board(Board board) {
			// TODO optimize this method
			// Creates deep copy (makes copy of each piece and deep copy of their respective ValidMoves lists) of board (list of BasePiece's)
			// this may be a memory hog since each Board has a list of Piece's, and each piece has a list of Movement's
			// avg number turns/Board's per game should be around ~80. usual max number of pieces per board is 32
			boardMatrix = new Piece[9, 10];
			for (int file = 1; file <= 9; file++) {
				for (int rank = 1; rank <= 10; rank++) {
					Piece pieceToCopy = board[file, rank];
					if (pieceToCopy == null) { continue; }

					this[file, rank] = pieceToCopy.DeepCopy();
				}
			}
            Refresh();
        }

		private void Refresh()
		{
			sideBoard[Side.White].SetBoard(this);
            sideBoard[Side.Black].SetBoard(this);
        }

        public void ClearBoard() {
			for (int file = 1; file <= 9; file++) {
				for (int rank = 1; rank <= 10; rank++) {
					this[file, rank] = null;
				}
			}

			sideBoard[Side.White].Clear();
            sideBoard[Side.Black].Clear();
			//currentAttackedSquares = null;
			//currentPiecesBySide = null;

            //currentKingSquareBySide[Side.White] = null;
			//currentKingSquareBySide[Side.Black] = null;
		}


		public static readonly (Square, Piece)[] StartingPositionPieces = {
			(new Square("a1"), new Rook(Side.White)),
			(new Square("b1"), new Horse(Side.White)),
			(new Square("c1"), new Elephant(Side.White)),
			(new Square("d1"), new Advisor(Side.White)),
			(new Square("e1"), new King(Side.White)),
			(new Square("f1"), new Advisor(Side.White)),
			(new Square("g1"), new Elephant(Side.White)),
			(new Square("h1"), new Horse(Side.White)),
			(new Square("i1"), new Rook(Side.White)),

			(new Square("b3"), new Cannon(Side.White)),
			(new Square("h3"), new Cannon(Side.White)),
			(new Square("a4"), new Pawn(Side.White)),
			(new Square("c4"), new Pawn(Side.White)),
			(new Square("e4"), new Pawn(Side.White)),
			(new Square("g4"), new Pawn(Side.White)),
			(new Square("i4"), new Pawn(Side.White)),
			
			(new Square("a10"), new Rook(Side.Black)),
			(new Square("b10"), new Horse(Side.Black)),
			(new Square("c10"), new Elephant(Side.Black)),
			(new Square("d10"), new Advisor(Side.Black)),
			(new Square("e10"), new King(Side.Black)),
			(new Square("f10"), new Advisor(Side.Black)),
			(new Square("g10"), new Elephant(Side.Black)),
			(new Square("h10"), new Horse(Side.Black)),
			(new Square("i10"), new Rook(Side.Black)),
			
			(new Square("b8"), new Cannon(Side.Black)),
			(new Square("h8"), new Cannon(Side.Black)),
			(new Square("a7"), new Pawn(Side.Black)),
			(new Square("c7"), new Pawn(Side.Black)),
			(new Square("e7"), new Pawn(Side.Black)),
			(new Square("g7"), new Pawn(Side.Black)),
			(new Square("i7"), new Pawn(Side.Black)),
		};

		public static Square ViewPosition(Square position, Side player) {
			if (player == Side.White) return position;
			return new Square(10-position.File, 11-position.Rank);	// 1-based
		}

		public void MovePiece(Movement move) {
			if (this[move.Start] is not { } pieceToMove) {
				throw new ArgumentException($"No piece was found at the given position: {move.Start}");
			}

			this[move.Start] = null;
			this[move.End] = pieceToMove;
			pieceToMove.Position = move.End;

            (move as SpecialMove)?.HandleAssociatedPiece(this);

			Refresh();
		}
		
		internal bool IsOccupiedAt(Square position) => this[position] != null;

		internal bool IsOccupiedBySideAt(Square position, Side side) => this[position] is Piece piece && piece.Owner == side;

		public Square GetKingSquare(Side player) {
			return sideBoard[player].KingPiece.Position;
			//if (currentKingBySide == null) {
			//	currentKingBySide = new();
			//             for (int file = 1; file <= 9; file++) {
			//		for (int rank = 1; rank <= 10; rank++) {
			//			if (this[file, rank] is King king) {
			//                         currentKingBySide[king.Owner] = king;
			//			}
			//		}
			//	}
			//}

			//return currentKingBySide[player].Position;
		}

        //public Dictionary<Square, Piece> GetPiecesBySide(Side side) {
        //	if (currentPiecesBySide == null)
        //	{
        //		currentPiecesBySide = new Dictionary<Side, Dictionary<Square, Piece>>()
        //		{
        //			[Side.White] = new Dictionary<Square, Piece>(),
        //                  [Side.Black] = new Dictionary<Square, Piece>(),
        //              };
        //		Dictionary<Square, Piece> result = new Dictionary<Square, Piece>();
        //		for (int file = 1; file <= 9; file++)
        //		{
        //			for (int rank = 1; rank <= 10; rank++)
        //			{
        //				Piece piece = boardMatrix[file - 1, rank - 1];
        //				if (piece != null && piece.Owner == side) result[new Square(file, rank)] = piece;
        //			}
        //		}
        //	}
        //	return currentPiecesBySide[side];
        //}

        public Dictionary<Piece, Dictionary<(Square, Square), Movement>> CalculateLegalMoves(Side sideToMove)
        {
			return sideBoard[sideToMove].GetLegalMoves();
        }

   //     private void UpdateAttackedSquares(Side side) {
			//Side enemySide = side.Complement();
   //         currentAttackedSquares[side] = new();
   //         foreach (var pieceMove in legalMoves[side])
   //         {
   //             foreach (var move in pieceMove.Value)
   //             {
   //                 Square target = move.Value.End;

   //                 if (IsOccupiedBySideAt(target, enemySide))
   //                 {
   //                     if (!currentAttackedSquares[side].ContainsKey(target)) {
			//				currentAttackedSquares[side][target] = new List<Piece>();
			//			}
   //                     currentAttackedSquares[side][target].Add(pieceMove.Key);
   //                 }
   //             }
   //         }
   //     }

		public bool IsKingAttacked(Side friendlySide)
		{
			Side enemySide = friendlySide.Complement();
            Square ourKing = GetKingSquare(friendlySide);
			if (sideBoard[enemySide].IsAttackingSquare(ourKing)) return true;

			// 2 King "saw" each other
			Square theirKing = GetKingSquare(enemySide);
			if (ourKing.File != theirKing.File) return false;

			int attackDir = friendlySide.ForwardDirection();
			for (int rank = ourKing.Rank + attackDir; rank != theirKing.Rank; rank += attackDir)
			{
				if (this[ourKing.File, rank] != null) return false;
			}
			return true;
		}

        public string ToTextArt() {
			string result = string.Empty;
			
			for (int rank = 10; rank >= 1; --rank) {
				for (int file = 1; file <= 9; ++file) {
					Piece piece = this[file, rank];
					result += piece.ToTextArt();
					result += file != 8
						? "|"
						: $"\t {rank}";
				}

				result += "\n";
			}
			
			result += "a b c d e f g h i";

			return result;
		} 
	}
}