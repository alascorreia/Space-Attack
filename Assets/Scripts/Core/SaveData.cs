using UnityEngine;

namespace OrbitGuard
{
    public sealed class SaveData
    {
        const string Prefix = "OrbitGuard.v1.";
        public int highScore, bestWave, bestClearedWave;
        public float sound = .65f;
        public bool reducedMotion, autoFire = true;
        public SaveData()
        {
            highScore = PlayerPrefs.GetInt(Prefix + "Score", 0);
            bestWave = PlayerPrefs.GetInt(Prefix + "Wave", 0);
            bestClearedWave = PlayerPrefs.GetInt(Prefix + "Cleared", 0);
            sound = PlayerPrefs.GetFloat(Prefix + "Sound", .65f);
            reducedMotion = PlayerPrefs.GetInt(Prefix + "Motion", 0) == 1;
            autoFire = PlayerPrefs.GetInt(Prefix + "AutoFire", 1) == 1;
        }
        public void Flush()
        {
            PlayerPrefs.SetInt(Prefix + "Score", highScore);
            PlayerPrefs.SetInt(Prefix + "Wave", bestWave);
            PlayerPrefs.SetInt(Prefix + "Cleared", bestClearedWave);
            PlayerPrefs.SetFloat(Prefix + "Sound", sound);
            PlayerPrefs.SetInt(Prefix + "Motion", reducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "AutoFire", autoFire ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
