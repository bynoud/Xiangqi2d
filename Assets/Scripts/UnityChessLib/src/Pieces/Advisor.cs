using System.Collections.Generic;

namespace UnityXiangqi
{
	public class Advisor : Piece<Advisor> {
		public Advisor() : base(Side.None) {}
		public Advisor(Side owner) : base(owner) {}

		public override string ToText() => Owner==Side.White ? "A" : "a";

		protected override IEnumerable<Movement> EnumeratePossibleMoves (
			Board board,
			Square position
		) {
			foreach (Square offset in SquareUtil.DiagonalOffsets) {
				Movement testMove = new Movement(position, position + offset);
				if (inPalace(testMove.End)) yield return testMove;
			}
		}
	}
}