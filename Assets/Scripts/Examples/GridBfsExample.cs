using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates GridBFS - Breadth-First Search on a 2D grid.
    /// Finds shortest path distances from source to all reachable cells.
    /// Uses native C# arrays with proper bounds checking.
    /// </summary>
    public class GridBfsExample : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private Vector2Int source = new(0, 0);
        
        [Header("Visualization")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Color nearColor = Color.green;
        [SerializeField] private Color farColor = Color.red;
        
        [Header("Results (Read Only)")]
        [SerializeField] private int[] distances;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunBFS();
        }
        
        private void RunBFS()
        {
            int len = width * height;
            distances = new int[len];
            
            for (int i = 0; i < len; i++)
                distances[i] = -1;
            
            // BFS implementation
            var queue = new System.Collections.Generic.Queue<int>();
            int startIdx = source.y * width + source.x;
            
            queue.Enqueue(startIdx);
            distances[startIdx] = 0;
            
            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };
            
            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                int r = idx / width;
                int c = idx % width;
                int dist = distances[idx];
                
                for (int d = 0; d < 4; d++)
                {
                    int nr = r + dr[d];
                    int nc = c + dc[d];
                    
                    if (nr >= 0 && nr < height && nc >= 0 && nc < width)
                    {
                        int nIdx = nr * width + nc;
                        if (distances[nIdx] < 0)
                        {
                            distances[nIdx] = dist + 1;
                            queue.Enqueue(nIdx);
                        }
                    }
                }
            }
            
            hasRun = true;
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || distances == null) return;
            
            int maxDist = 0;
            for (int i = 0; i < distances.Length; i++)
                if (distances[i] > maxDist) maxDist = distances[i];
            
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    int d = distances[r * width + c];
                    Vector3 pos = transform.position + new Vector3(c * cellSize + cellSize / 2, 0, r * cellSize + cellSize / 2);
                    
                    if (d >= 0)
                    {
                        float t = maxDist > 0 ? (float)d / maxDist : 0;
                        Gizmos.color = Color.Lerp(nearColor, farColor, t);
                        Gizmos.DrawCube(pos, Vector3.one * (cellSize * 0.9f));
                    }
                    else
                    {
                        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                        Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
                    }
                }
            }
            
            // Draw source marker
            Vector3 srcPos = transform.position + new Vector3(source.x * cellSize + cellSize / 2, 0.5f, source.y * cellSize + cellSize / 2);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(srcPos, cellSize * 0.3f);
        }
    }
}