using System;
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
        
                private HashSet<Vector3Int> transparentTiles = new HashSet<Vector3Int>();
                private HashSet<Vector3Int> allTreePositions = new HashSet<Vector3Int>();
                private HashSet<TileBase> treeTiles = new HashSet<TileBase>();
        
                private void Start()
                {
                        player = GameObject.FindWithTag("Player").transform;
                        playerCollider = player.GetComponent<Collider2D>();
                    
                    FindAllTreeTilesAndPositions();
                }
        
                private void FindAllTreeTilesAndPositions()
                {
                    BoundsInt bounds = tilemap.cellBounds;
        
                    for (int x = bounds.xMin; x < bounds.xMax; x++)
                    {
                        for (int y = bounds.yMin; y < bounds.yMax; y++)
                        {
                            Vector3Int cell = new Vector3Int(x, y, 0);
                            TileBase tile = tilemap.GetTile(cell);
        
                            if (tile != null)
                            {
                                treeTiles.Add(tile);
                                allTreePositions.Add(cell);
                            }
                        }
                    }
                    // Debug.Log($"Found {treeTiles.Count} unique tree tile types.");
                    // Debug.Log($"Found {allTreePositions.Count} tree tile positions.");
                }
        
                private void Update()
                {
                    foreach (var cell in transparentTiles)
                        RestoreTransparency(cell);
                    transparentTiles.Clear();

                    var treeColliders = tilemap.GetComponentsInChildren<Collider2D>();
                    foreach (var treeCollider in treeColliders)
                    {
                        if (treeCollider != null && treeCollider != playerCollider && playerCollider.IsTouching(treeCollider))
                        {
                            Bounds bounds = treeCollider.bounds;
                            Vector3Int minCell = tilemap.WorldToCell(bounds.min);
                            Vector3Int maxCell = tilemap.WorldToCell(bounds.max);

                            for (int x = minCell.x; x <= maxCell.x; x++)
                            {
                                for (int y = minCell.y; y <= maxCell.y; y++)
                                {
                                    Vector3Int cell = new Vector3Int(x, y, 0);
                                    TileBase tile = tilemap.GetTile(cell);
                                    if (tile != null && allTreePositions.Contains(cell))
                                    {
                                        // Debug.Log($"Player is touching a tree collider: {treeCollider.name}, {cell}");
                                        SetTransparency(cell, 0.5f);
                                        transparentTiles.Add(cell);
                                    }
                                }
                            }
                        }
                    }
                }
        
                public void SetTransparency(Vector3Int cell, float alpha = 1.0f)
                {
                    var c = tilemap.GetColor(cell);
                    c.a = alpha;
                    //tilemap.SetColor(cell, c);
                    tilemap.color = c;
                }
        
                public void RestoreTransparency(Vector3Int cell)
                {
                    var c = tilemap.GetColor(cell);
                    c.a = 1.0f;
                    //tilemap.SetColor(cell, c);
                    tilemap.color = c;
                }
            }
        }