using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Integer Math operations (Sqrt, Cbrt, Power of Two).
    /// </summary>
    public class MathBasicExample : MonoBehaviour
    {
        [Header("Test Values")]
        [SerializeField] private long testValue = 100;
        
        [Header("Visualization")]
        [SerializeField] private float scale = 0.05f;
        
        [Header("Results")]
        [SerializeField] private long sqrtResult;
        [SerializeField] private long cbrtResult;
        [SerializeField] private bool isPerfectSquare;
        [SerializeField] private bool isPowerOfTwo;
        [SerializeField] private long nextPowerOfTwo;
        [SerializeField] private long prevPowerOfTwo;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunMathDemo();
        }
        
        private void RunMathDemo()
        {
            sqrtResult = IntegerSqrt(testValue);
            cbrtResult = IntegerCbrt(testValue);
            isPerfectSquare = sqrtResult * sqrtResult == testValue;
            isPowerOfTwo = testValue > 0 && (testValue & (testValue - 1)) == 0;
            nextPowerOfTwo = NextPowerOfTwo(testValue);
            prevPowerOfTwo = PrevPowerOfTwo(testValue);
            
            hasRun = true;
        }
        
        private long IntegerSqrt(long x)
        {
            if (x < 0) return -1;
            if (x == 0) return 0;
            long lo = 0, hi = 3037000499L;
            while (lo < hi)
            {
                long mid = lo + (hi - lo + 1) / 2;
                if (mid * mid <= x) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }
        
        private long IntegerCbrt(long x)
        {
            if (x < 0) return -1;
            if (x == 0) return 0;
            long lo = 0, hi = 2097151L;
            while (lo < hi)
            {
                long mid = lo + (hi - lo + 1) / 2;
                if (mid * mid * mid <= x) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }
        
        private long NextPowerOfTwo(long x)
        {
            if (x <= 0) return 1;
            x--;
            x |= x >> 1; x |= x >> 2; x |= x >> 4;
            x |= x >> 8; x |= x >> 16; x |= x >> 32;
            return x + 1;
        }
        
        private long PrevPowerOfTwo(long x)
        {
            if (x <= 0) return 0;
            x |= x >> 1; x |= x >> 2; x |= x >> 4;
            x |= x >> 8; x |= x >> 16; x |= x >> 32;
            return x - (x >> 1);
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun) return;
            
            // Draw number line
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(transform.position - Vector3.right * 5, transform.position + Vector3.right * 15);
            
            // Draw test value
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + Vector3.right * testValue * scale, 0.5f);
            
            // Draw sqrt
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1 + Vector3.right * sqrtResult * scale, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.right * testValue * scale, 
                           transform.position + Vector3.up * 1 + Vector3.right * sqrtResult * scale);
            
            // Draw cbrt
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2 + Vector3.right * cbrtResult * scale, 0.3f);
            Gizmos.DrawLine(transform.position + Vector3.right * testValue * scale,
                           transform.position + Vector3.up * 2 + Vector3.right * cbrtResult * scale);
            
            // Draw power of two bounds
            Gizmos.color = isPowerOfTwo ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3 + Vector3.right * prevPowerOfTwo * scale, 0.2f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3 + Vector3.right * nextPowerOfTwo * scale, 0.2f);
            Gizmos.DrawLine(transform.position + Vector3.up * 3 + Vector3.right * prevPowerOfTwo * scale,
                           transform.position + Vector3.up * 3 + Vector3.right * nextPowerOfTwo * scale);
        }
    }
}