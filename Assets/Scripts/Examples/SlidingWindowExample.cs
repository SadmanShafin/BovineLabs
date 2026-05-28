using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Sliding Window Maximum/Minimum using Monotonic Queue.
    /// O(n) for finding max/min in all sliding windows.
    /// </summary>
    public class SlidingWindowExample : MonoBehaviour
    {
        [Header("Window Settings")]
        [SerializeField] private int windowSize = 3;
        [SerializeField] private int arrayLength = 8;
        
        [Header("Input Data")]
        [SerializeField] private int[] input = { 1, 3, -1, -3, 5, 3, 6, 7 };
        
        [Header("Visualization")]
        [SerializeField] private float barWidth = 0.5f;
        [SerializeField] private float maxValue = 10f;
        
        [Header("Results")]
        [SerializeField] private int[] maxResult;
        [SerializeField] private int[] minResult;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunSlidingWindow();
        }
        
        private void RunSlidingWindow()
        {
            input = new int[arrayLength];
            for (int i = 0; i < arrayLength; i++)
                input[i] = Random.Range(-5, 10);
            
            int len = input.Length;
            maxResult = new int[len - windowSize + 1];
            minResult = new int[len - windowSize + 1];
            
            CalculateSlidingMax(input, maxResult, len, windowSize);
            CalculateSlidingMin(input, minResult, len, windowSize);
            
            hasRun = true;
        }
        
        private void CalculateSlidingMax(int[] src, int[] dst, int len, int wSize)
        {
            if (len == 0 || wSize == 0) return;
            int[] deque = new int[len];
            int front = 0, back = 0;
            
            for (int i = 0; i < len; i++)
            {
                while (front < back && src[deque[back - 1]] <= src[i]) back--;
                deque[back++] = i;
                if (deque[front] <= i - wSize) front++;
                if (i >= wSize - 1) dst[i - wSize + 1] = src[deque[front]];
            }
        }
        
        private void CalculateSlidingMin(int[] src, int[] dst, int len, int wSize)
        {
            if (len == 0 || wSize == 0) return;
            int[] deque = new int[len];
            int front = 0, back = 0;
            
            for (int i = 0; i < len; i++)
            {
                while (front < back && src[deque[back - 1]] >= src[i]) back--;
                deque[back++] = i;
                if (deque[front] <= i - wSize) front++;
                if (i >= wSize - 1) dst[i - wSize + 1] = src[deque[front]];
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || input == null) return;
            
            // Draw input bars
            for (int i = 0; i < input.Length; i++)
            {
                float h = Mathf.Abs(input[i]) / maxValue * 2f;
                Vector3 pos = transform.position + new Vector3(i * barWidth, 0, 0);
                
                Gizmos.color = input[i] >= 0 ? Color.green : Color.red;
                Gizmos.DrawCube(pos + Vector3.up * h / 2, new Vector3(barWidth * 0.8f, h, barWidth * 0.8f));
            }
            
            // Draw window bounds
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            for (int w = 0; w <= input.Length - windowSize; w++)
            {
                Vector3 windowStart = transform.position + new Vector3(w * barWidth, 3f, -0.2f);
                Gizmos.DrawWireCube(windowStart + Vector3.right * (windowSize * barWidth) / 2, 
                                   new Vector3(windowSize * barWidth, 0.5f, barWidth));
            }
            
            // Draw max results above
            if (maxResult != null)
            {
                for (int i = 0; i < maxResult.Length; i++)
                {
                    Vector3 pos = transform.position + new Vector3((i + windowSize - 1) * barWidth, 3.5f, 0);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(pos, barWidth * 0.3f);
                    Gizmos.DrawWireCube(pos + Vector3.up * maxResult[i] / maxValue, 
                                       new Vector3(barWidth * 0.4f, maxResult[i] / maxValue * 2f, barWidth * 0.4f));
                }
            }
        }
    }
}