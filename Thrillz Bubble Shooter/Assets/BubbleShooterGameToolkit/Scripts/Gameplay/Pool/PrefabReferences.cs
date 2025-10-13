// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using UnityEngine;

namespace BubbleShooterGameToolkit.Scripts.Gameplay.Pool
{
    [CreateAssetMenu(fileName = "PrefabReferences", menuName = "Bubble Shooter/Prefab References")]
    public class PrefabReferences : ScriptableObject
    {
        [Header("Game Objects")]
        public GameObject bouncing;

        [Header("Balls")]
        public GameObject[] coloredBalls;
        public GameObject randomBall;
        public GameObject carrot;
        public GameObject emptyBall;
        
        private static PrefabReferences _instance;
        public static PrefabReferences Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<PrefabReferences>("Settings/PrefabReferences");
                    if (_instance == null)
                    {
                        Debug.LogError("PrefabReferences not found in Resources folder!");
                    }
                }
                return _instance;
            }
        }

        public GameObject GetColoredBall(int colorIndex)
        {
            if (coloredBalls == null || colorIndex < 0 || colorIndex >= coloredBalls.Length)
            {
                Debug.LogError($"Invalid color index: {colorIndex}");
                return null;
            }
            return coloredBalls[colorIndex];
        }
    }
}