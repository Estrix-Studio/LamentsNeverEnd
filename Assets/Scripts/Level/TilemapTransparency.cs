using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Level
{
	public class TilemapTransparency : MonoBehaviour
	{
		public Tilemap tilemap;
		public Transform player;
		public Collider2D playerCollider;
		private readonly HashSet<Vector3Int> _allTreePositions = new();

		private readonly HashSet<Vector3Int> _transparentTiles = new();
		private readonly HashSet<TileBase> _treeTiles = new();

		private void Start()
		{
			player = GameObject.FindWithTag("Player").transform;
			playerCollider = player.GetComponent<Collider2D>();

			FindAllTreeTilesAndPositions();
		}

		private void Update()
		{
			foreach (var cell in _transparentTiles)
				RestoreTransparency(cell);
			_transparentTiles.Clear();

			var treeColliders = tilemap.GetComponentsInChildren<Collider2D>();
			foreach (var treeCollider in treeColliders)
				if (treeCollider != null && treeCollider != playerCollider && playerCollider.IsTouching(treeCollider))
				{
					var bounds = treeCollider.bounds;
					var minCell = tilemap.WorldToCell(bounds.min);
					var maxCell = tilemap.WorldToCell(bounds.max);

					for (var x = minCell.x; x <= maxCell.x; x++)
					for (var y = minCell.y; y <= maxCell.y; y++)
					{
						var cell = new Vector3Int(x, y, 0);
						var tile = tilemap.GetTile(cell);
						if (tile != null && _allTreePositions.Contains(cell))
						{
							// Debug.Log($"Player is touching a tree collider: {treeCollider.name}, {cell}");
							SetTransparency(cell, 0.5f);
							_transparentTiles.Add(cell);
						}
					}
				}
		}

		private void FindAllTreeTilesAndPositions()
		{
			var bounds = tilemap.cellBounds;

			for (var x = bounds.xMin; x < bounds.xMax; x++)
			for (var y = bounds.yMin; y < bounds.yMax; y++)
			{
				var cell = new Vector3Int(x, y, 0);
				var tile = tilemap.GetTile(cell);

				if (tile == null) continue;
				_treeTiles.Add(tile);
				_allTreePositions.Add(cell);
			}
			// Debug.Log($"Found {treeTiles.Count} unique tree tile types.");
			// Debug.Log($"Found {allTreePositions.Count} tree tile positions.");
		}

		private void SetTransparency(Vector3Int cell, float alpha = 1.0f)
		{
			var c = tilemap.GetColor(cell);
			c.a = alpha;
			//tilemap.SetColor(cell, c);
			tilemap.color = c;
		}

		private void RestoreTransparency(Vector3Int cell)
		{
			var c = tilemap.GetColor(cell);
			c.a = 1.0f;
			//tilemap.SetColor(cell, c);
			tilemap.color = c;
		}
	}
}