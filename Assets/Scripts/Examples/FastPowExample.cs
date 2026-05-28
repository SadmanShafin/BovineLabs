using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Fast Power (Exponentiation by Squaring).
    /// O(log n) modular exponentiation.
    /// </summary>
    public class FastPowExample : MonoBehaviour
    {
        [Header("Operation")]
        [SerializeField] private long baseValue = 2;
        [SerializeField] private long exponent = 10;
        [SerializeField] private long modulus = 1000000007;
        
        [Header("Visualization")]
        [SerializeField] private float stepHeight = 0.5f;
        
        [Header("Results")]
        [SerializeField] private long result;
        [SerializeField] private int steps;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunFastPow();
        }
        
        private void RunFastPow()
        {
            result = FastPow(baseValue, exponent, modulus);
            hasRun = true;
        }
        
        private long FastPow(long a, long e, long mod)
        {
            long res = 1 % mod;
            a %= mod;
            steps = 0;
            
            while (e > 0)
            {
                steps++;
                if ((e & 1) == 1)
                    res = (res * a) % mod;
                a = (a * a) % mod;
                e >>= 1;
            }
            
            return res;
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun) return;
            
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw exponentiation steps
            long a = baseValue % modulus;
            long e = exponent;
            long res = 1;
            float y = 0;
            
            int step = 0;
            while (e > 0)
            {
                bool useResult = (e & 1) == 1;
                Vector3 pos = transform.position + new Vector3(step % 5 * stepHeight * 1.5f, y, step / 5 * stepHeight * 1.5f);
                
                if (useResult)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(pos, Vector3.one * stepHeight);
                }
                else
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(pos, Vector3.one * stepHeight * 0.5f);
                }
                
                // Labels
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(pos + Vector3.up * stepHeight * 2, stepHeight * 0.2f);
                
                a = (a * a) % modulus;
                e >>= 1;
                step++;
                if (step % 2 == 0) y += stepHeight;
            }
            
            // Draw final result position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + new Vector3(3f, 0, 0), 0.3f);
        }
    }
}