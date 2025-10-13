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

using BubbleShooterGameToolkit.Scripts.Gameplay.Managers;
using BubbleShooterGameToolkit.Scripts.Gameplay.Pool;
using UnityEngine;

namespace BubbleShooterGameToolkit.Scripts.Gameplay.PlayObjects.Attachables.Covers
{
    class CoverRandom : Cover
    {
        private const string BALL_PLACEHOLDER_NAME = "Ball Placeholder";
        
        public override bool DestroyItem(BallDestructionOptions options)
        {
            if (ball != null && ball.name == BALL_PLACEHOLDER_NAME)
            {
                Ball newBall;
                int colorIndex = ColorManager.instance.GenerateColor();
                
                var prefabRef = PrefabReferences.Instance;
                if (prefabRef != null && prefabRef.coloredBalls != null && 
                    colorIndex >= 0 && colorIndex < prefabRef.coloredBalls.Length)
                {
                    GameObject ballPrefab = prefabRef.GetColoredBall(colorIndex);
                    if (ballPrefab != null)
                    {
                        newBall = PoolObject.GetObject(ballPrefab).GetComponent<Ball>();
                    }
                    else
                    {
                        newBall = PoolObject.GetObject($"Ball {colorIndex}").GetComponent<Ball>();
                    }
                }
                else
                {
                    newBall = PoolObject.GetObject($"Ball {colorIndex}").GetComponent<Ball>();
                }
                
                newBall.SetPosition(options.DestroyedBy.position);
                this.ball.DestroyBall();
            }
            return base.DestroyItem(options);
        }
    }
}