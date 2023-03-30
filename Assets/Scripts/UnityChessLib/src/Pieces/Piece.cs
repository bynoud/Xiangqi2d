using System.Collections.Generic;

namespace UnityXiangqi
{
	/// <summary>Base class for any chess piece.</summary>
	public abstract class Piece {
		public Side Owner { get; protected set; }

		public Square Position;
		public bool Captured = false;

		protected Piece(Side owner) {
			Owner = owner;
		}

		public abstract Piece DeepCopy();

		public abstract string ToText();
		protected abstract IEnumerable<Movement> EnumeratePossibleMoves(
			Board board,
			Square position
		);

		private IEnumerable<Movement> PossibleMoves(Board board, Square position, bool selfChecked)
		{
			foreach (Movement move in EnumeratePossibleMoves(board, position))
			{
                if (Rules.MoveObeysRules(board, move, Owner, selfChecked)) yield return move;
			}
		}

		// This now always return the "possible" moves, instead of "legal" moves
		// which can have the move that cause self-checked
		public Dictionary<(Square, Square), Movement> CalculateLegalMoves(
			Board board,
			Square position
		)
		{
			Dictionary<(Square, Square), Movement> result = new Dictionary<(Square, Square), Movement>();
			foreach (Movement move in PossibleMoves(board, position, false))
			{
				result[(move.Start, move.End)] = move;
			}
			return result.Count == 0 ? null : result;
		}

		public bool IsAttackingTo(
			Board board,
			Square position,
			Square target
		)
		{
			Side enemySide = Owner.Complement();
			foreach (Movement move in PossibleMoves(board, position, false))
			{
				if (move.End == target && board.IsOccupiedBySideAt(move.End, enemySide)) { return true; }
			}
			return false;
		}


		protected bool inPalace(Square position) {
			Square viewPos = Board.ViewPosition(position, Owner);
			if (viewPos.Row > 3 || viewPos.Col < 4 || viewPos.Col > 6) return false;
			return true;
		}

		protected bool riverCross(Square position) {
			Square viewPos = Board.ViewPosition(position, Owner);
			if (viewPos.Row > 5) return true;
			return false;
		}

		public override string ToString() => $"{Owner} {GetType().Name}";

		public string ToTextArt() => this switch {
			// Bishop { Owner: Side.White } => "♝",
			// Bishop { Owner: Side.Black } => "♗",
			// King { Owner: Side.White } => "♚",
			// King { Owner: Side.Black } => "♔",
			// Knight { Owner: Side.White } => "♞",
			// Knight { Owner: Side.Black } => "♘",
			// Queen { Owner: Side.White } => "♛",
			// Queen { Owner: Side.Black } => "♕",
			// Pawn { Owner: Side.White } => "♟",
			// Pawn { Owner: Side.Black } => "♙",
			// Rook { Owner: Side.White } => "♜",
			// Rook { Owner: Side.Black } => "♖",
			King { Owner: Side.White } => "K",
			King { Owner: Side.Black } => "k",
			Advisor { Owner: Side.White } => "A",
			Advisor { Owner: Side.Black } => "a",
			Elephant { Owner: Side.White } => "E",
			Elephant { Owner: Side.Black } => "e",
			Rook { Owner: Side.White } => "R",
			Rook { Owner: Side.Black } => "r",
			Cannon { Owner: Side.White } => "C",
			Cannon { Owner: Side.Black } => "c",
			Horse { Owner: Side.White } => "H",
			Horse { Owner: Side.Black } => "h",
			Pawn { Owner: Side.White } => "P",
			Pawn { Owner: Side.Black } => "p",
			_ => "."
		};

		public string ToChar() {
			string result = this switch {
				King => "K",
				Advisor => "A",
				Elephant => "E",
				Rook => "R",
				Cannon => "C",
				Horse => "H",
				Pawn => "P",
				_ => "."
			};
			return this switch {
				{ Owner: Side.Black } => result.ToLower(),
				_ => result
			};
		}

		public static Piece FromChar(string character) {
			string upperSymbol = character.ToUpper();
			Side side = upperSymbol == character ? Side.White : Side.Black;

			return upperSymbol switch {
				"K" => new King(side),
				"A" => new Advisor(side),
				"E" => new Elephant(side),
				"R" => new Rook(side),
				"C" => new Cannon(side),
				"H" => new Horse(side),
				"P" => new Pawn(side),
				_ => null
			};
		}
	}

	public abstract class Piece<T> : Piece where T : Piece<T>, new() {
		protected Piece(Side owner) : base(owner) { }
		
		public override Piece DeepCopy() {
			return new T {
				Owner = Owner,
				Position = Position
			};
		}
	}
}