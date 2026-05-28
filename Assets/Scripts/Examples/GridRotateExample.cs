using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Grid Rotation (90, 180, 270 degrees).
    /// </summary>
    public class GridRotateExample : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int width = 5;
        [SerializeField] private int height = 4;
        
        [Header("Visualization")]
        [SerializeField] private float cellSize = 0.5f;
        
        [Header("Results")]
        [SerializeField] private long[] original;
        [SerializeField] private long[] rotated90;
        [SerializeField] private long[] rotated180;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunRotationDemo();
        }
        
        private void RunRotationDemo()
        {
            int len = width * height;
            original = new long[len];
            rotated90 = new long[len];
            rotated180 = new long[len];
            
            // Fill with sequential values
            for (int i = 0; i < len; i++) original[i] = i + 1;
            
            // Rotate 90 (swaps width/height)
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    rotated90[j * height + (height - 1 - i)] = original[i * width + j];
            
            // Rotate 180
            for (int i = 0; i < len; i++)
                rotated180[len - 1 - i] = original[i];
            
            hasRun = true;
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || original == null) return;
            
            // Original grid
            DrawGrid(original, height, width, 0, Color.cyan);
            
            // Rotated 90
            DrawGrid(rotated90, width, height, width * cellSize + 2f, Color.yellow);
            
            // Rotated 180
            DrawGrid(rotated180, height, width, 0, -2f, Color.magenta);
        }
        
        private void DrawGrid(long[] grid, int h, int w, float xOffset, Color color)
        {
            DrawGrid(grid, h, w, xOffset, 0, color);
        }
        
        private void DrawGrid(long[] grid, int h, int w, float xOffset, float zOffset, Color color)
        {
            for (int r = 0; r < h; r++)
            {
                for (int c = 0; c < w; c++)
                {
                    long val = grid[r * w + c];
                    float t = (float)val / (h * w);
                    
                    Vector3 pos = transform.position + new Vector3(c * cellSize + xOffset, 0, r * cellSize + zOffset);
                    Gizmos.color = Color.Lerp(Color.blue, color, t);
                    Gizmos.DrawWireCube(pos + Vector3.one * cellSize / 2, Vector3.one * cellSize * 0.9f);
                    
                    // Draw value indicator
                    if (val == 1 || val == h * w || val == w)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireSphere(pos + Vector3.one * cellSize / 2, cellSize * 0.3f);
                    }
                }
            }
        }
    }
}