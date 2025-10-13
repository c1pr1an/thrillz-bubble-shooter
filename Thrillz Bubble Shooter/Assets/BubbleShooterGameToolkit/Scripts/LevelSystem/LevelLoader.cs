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

using System;
using BubbleShooterGameToolkit.Scripts.System;
using UnityEngine;

namespace BubbleShooterGameToolkit.Scripts.LevelSystem
{
    public class LevelLoader : Singleton<LevelLoader>
    {
        public static Action OnLevelLoaded;

        // Current loaded level
        public Level CurrentLevel { get; private set; }
        public Level LoadLevel(int num)
        {
            CurrentLevel = Resources.Load<Level>("Levels/Level_" + num);
            return CurrentLevel;
        }
        
        public void LoadLevel(Level levelToLoad = null)
        {
            CurrentLevel = levelToLoad;
            if (CurrentLevel != null)
                return;
            var levelName = PlayerPrefs.GetString("OpenLevelName");
            if (!string.IsNullOrEmpty(levelName))
            {
                CurrentLevel = Resources.Load<Level>("Levels/"+levelName);
                PlayerPrefs.DeleteKey("OpenLevelName");
            }
            else
            {
                var levelNumber = PlayerPrefs.GetInt("OpenLevel", 1);
                CurrentLevel = LoadLevel(levelNumber);
            }
            OnLevelLoaded?.Invoke();
        }
    }
}