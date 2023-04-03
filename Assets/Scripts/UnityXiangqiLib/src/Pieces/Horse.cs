using System.Collections.Generic;

namespace UnityXiangqi
{
	public class Horse : Piece<Horse> {
		public Horse() : base(Side.None) {}
		public Horse(Side owner) : base(owner) {}

		public override string ToText() => Owner==Side.White ? "H" : "h";

		protected override IEnumerable<Movement> EnumeratePossibleMoves (
			Board board,
			Square position
		) {

			foreach (Square offset in SquareUtil.CardinalOffsets) {
				List<Square> horseOffsets = new List<Square>();
				Square immPos = position + offset;
				if (immPos.IsValid() && board.IsOccupiedAt(immPos)) {
					continue;
				}

				if (offset.Row != 0) {
					horseOffsets.Add(new Square(1, offset.Row*2));
					horseOffsets.Add(new Square(-1, offset.Row*2));
				} else {
					horseOffsets.Add(new Square(offset.Col*2, 1));
					horseOffsets.Add(new Square(offset.Col*2, -1));
				}

				foreach (Square horseOffset in horseOffsets) {
					Movement testMove = new(position, position + horseOffset);
					yield return testMove;
				}
			}

			// foreach (Square offset in SquareUtil.HorseOffsets) {
			// 	Square endSquare = position + offset;

			// 	while (endSquare.IsValid()) {
			// 		Movement testMove = new Movement(position, endSquare);

			// 		if (Rules.MoveObeysRules(board, testMove, Owner)) {
			// 			if (result == null) {
			// 				result = new Dictionary<(Square, Square), Movement>();
			// 			}

			// 			result[(testMove.Start, testMove.End)] = new Movement(testMove);
			// 		}
					
			// 		if (board.IsOccupiedAt(endSquare)) {
			// 			break;
			// 		}

			// 		endSquare += offset;
			// 	}
			// }

			// return result;
		}
	}
}