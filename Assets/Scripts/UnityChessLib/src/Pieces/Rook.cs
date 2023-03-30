using System.Collections.Generic;

namespace UnityXiangqi
{
	public class Rook : Piece<Rook> {
		public Rook() : base(Side.None) {}
		public Rook(Side owner) : base(owner) {}

		public override string ToText() => Owner==Side.White ? "R" : "r";

		protected override IEnumerable<Movement> EnumeratePossibleMoves(
			Board board,
			Square position
		) {
			foreach (Square offset in SquareUtil.CardinalOffsets) {
				Square endSquare = position + offset;

				while (endSquare.IsValid()) {
					Movement testMove = new Movement(position, endSquare);
                    //if (Rules.MoveObeysRules(board, testMove, Owner)) { yield return testMove.End; }
                    yield return testMove;
                    if (board.IsOccupiedAt(endSquare)) break;
					endSquare += offset;
				}
			}

		}
	}
}