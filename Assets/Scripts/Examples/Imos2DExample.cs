using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates the Imos Method (2D) for O(1) range updates.
    /// Mark rectangular regions, then build final grid.
    /// </summary>
    public class Imos2DExample : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        
        [Header("Rectangles to Add")]
        [SerializeField] private Vector2Int rect1Min = new(2, 2);
        [SerializeField] private Vector2Int rect1Max = new(5, 5);
        [SerializeField] private int rect1Value = 1;
        
        [SerializeField] private Vector2Int rect2Min = new(4, 4);
        [SerializeField] private Vector2Int rect2Max = new(8, 8);
        [SerializeField] private int rect2Value = 2;
        
        [Header("Visualization")]
        [SerializeField] private float cellSize = 0.5f;
        
        [Header("Results")]
        [SerializeField] private int[] grid;
        [SerializeField] private bool hasRun;
        
        private int[] _diff;
        
        private void Start()
        {
            RunImos();
        }
        
        private void RunImos()
        {
            int len = width * height;
            grid = new int[len];
            _diff = new int[len];
            
            // Add rectangles
            AddRectangle(rect1Min, rect1Max, rect1Value);
            AddRectangle(rect2Min, rect2Max, rect2Value);
            
            // Build final grid
            BuildGrid();
            
            hasRun = true;
        }
        
        private void AddRectangle(Vector2Int min, Vector2Int max, int val)
        {
            int r1 = min.y, c1 = min.x;
            int r2 = max.y, c2 = max.x;
            
            if (r1 < 0) r1 = 0;
            if (c1 < 0) c1 = 0;
            if (r2 >= height) r2 = height - 1;
            if (c2 >= width) c2 = width - 1;
            if (r1 > r2 || c1 > c2) return;
            
            _diff[r1 * width + c1] += val;
            if (c2 + 1 < width) _diff[r1 * width + c2 + 1] -= val;
            if (r2 + 1 < height) _diff[(r2 + 1) * width + c1] -= val;
            if (r2 + 1 < height && c2 + 1 < width) _diff[(r2 + 1) * width + c2 + 1] += val;
        }
        
        private void BuildGrid()
        {
            for (int i = 0; i < grid.Length; i++)
                grid[i] = _diff[i];
            
            for (int r = 0; r < height; r++)
                for (int c = 1; c < width; c++)
                    grid[r * width + c] += grid[r * width + c - 1];
            
            for (int r = 1; r < height; r++)
                for (int c = 0; c < width; c++)
                    grid[r * width + c] += grid[(r - 1) * width + c];
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || grid == null) return;
            
            int maxVal = 0;
            foreach (int v in grid) maxVal = Mathf.Max(maxVal, v);
            
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    int val = grid[r * width + c];
                    Vector3 pos = transform.position + new Vector3(c * cellSize + cellSize / 2, val * cellSize, r * cellSize + cellSize / 2);
                    
                    float t = maxVal > 0 ? (float)val / maxVal : 0;
                    Gizmos.color = new Color(1f - t, t, 0.5f);
                    Gizmos.DrawCube(pos, Vector3.one * cellSize * 0.9f);
                    
                    // Draw bar from ground
                    Gizmos.color = new Color(0.3f, 0.3f, 0.3f);
                    Gizmos.DrawLine(pos, transform.position + new Vector3(pos.x, 0, pos.z));
                }
            }
            
            // Draw rectangle outlines
            Gizmos.color = Color.yellow;
            DrawRectOutline(rect1Min, rect1Max);
            Gizmos.color = Color.cyan;
            DrawRectOutline(rect2Min, rect2Max);
        }
        
        private void DrawRectOutline(Vector2Int min, Vector2Int max)
        {
            Vector3 bl = transform.position + new Vector3(min.x * cellSize, 0, min.y * cellSize);
            Vector3 tr = transform.position + new Vector3((max.x + 1) * cellSize, 0, (max.y + 1) * cellSize);
            
            Gizmos.DrawLine(bl, bl + new Vector3(0, 0, (max.y - min.y + 1) * cellSize));
            Gizmos.DrawLine(bl, bl + new Vector3((max.x - min.x + 1) * cellSize, 0, 0));
            Gizmos.DrawLine(tr, tr - new Vector3(0, 0, (max.y - min.y + 1) * cellSize));
            Gizmos.DrawLine(tr, tr - new Vector3((max.x - min.x + 1) * cellSize, 0, 0));
        }
    }
}