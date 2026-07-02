using UnityEngine;

namespace EndlessSeamlessLevels
{
	public static class LevelDirectionUtility
	{
		public static Vector2Int ToCellOffset(LevelDirection direction)
		{
			return direction switch
			{
				LevelDirection.North => Vector2Int.up,
				LevelDirection.East => Vector2Int.right,
				LevelDirection.South => Vector2Int.down,
				LevelDirection.West => Vector2Int.left,
				_ => Vector2Int.zero
			};
		}

		public static LevelDirection Opposite(LevelDirection direction)
		{
			return direction switch
			{
				LevelDirection.North => LevelDirection.South,
				LevelDirection.East => LevelDirection.West,
				LevelDirection.South => LevelDirection.North,
				LevelDirection.West => LevelDirection.East,
				_ => LevelDirection.None
			};
		}
	}
}
