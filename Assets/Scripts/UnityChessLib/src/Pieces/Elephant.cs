using System.Collections.Generic;

namespace UnityXiangqi
{
	public class Elephant : Piece<Elephant> {
		public Elephant() : base(Side.None) {}
		public Elephant(Side owner) : base(owner) {}

		public override string ToText() => Owner==Side.White ? "E" : "e";

		protected override IEnumerable<Movement> EnumeratePossibleMoves (
			Board board,
			Square position
		) {
			foreach (Square offset in SquareUtil.ElephantOffsets) {
				Movement testMove = new Movement(position, position + offset);
				if (!riverCross(testMove.End)) yield return testMove;
			}
		}
	}
}