using System.Collections.Generic;

namespace UnityXiangqi
{
	public class Cannon : Piece<Cannon> {
		public Cannon() : base(Side.None) {}
		public Cannon(Side owner) : base(owner) {}

		public override string ToText() => Owner==Side.White ? "C" : "c";

		protected override IEnumerable<Movement> EnumeratePossibleMoves (
			Board board,
			Square position
		) {
			foreach (Square offset in SquareUtil.CardinalOffsets) {
				int jumped = 0;
				Square endSquare = position + offset;

				while (endSquare.IsValid()) {
					Movement testMove = new Movement(position, endSquare);
                    endSquare += offset;

                    if (board.IsOccupiedAt(testMove.End)) {
						jumped++;

					}
					if (jumped==1) { continue;}

					yield return testMove;

					if (jumped>1) break;
					
				}
			}

		}
	}
}