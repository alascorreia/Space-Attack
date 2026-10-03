using System;
using UnityEngine;

namespace OrbitGuard
{
    [Serializable]
    public class WaveDefinition
    {
        public string name = "FIRST CONTACT";
        [Range(3, 10)] public int columns = 7;
        [Range(1, 5)] public int rows = 3;
        [Range(.15f, 3)] public float marchSpeed = .65f;
        [Range(.2f, 4)] public float shotInterval = 1.3f;
        [Range(2, 10)] public float bulletSpeed = 4;
        [Range(0, 3)] public int armoredRows;
        public bool divingEnemies;
    }

    [CreateAssetMenu(menuName = "Orbit Guard/Campaign")]
    public class CampaignDefinition : ScriptableObject
    {
        public WaveDefinition[] waves = {
            new WaveDefinition(),
            new WaveDefinition { name = "CROSS FIRE", rows = 4, marchSpeed = .8f, shotInterval = 1.05f },
            new WaveDefinition { name = "IRON SKY", rows = 4, armoredRows = 1, marchSpeed = .9f, shotInterval = .85f },
            new WaveDefinition { name = "BREAK FORMATION", rows = 4, divingEnemies = true, marchSpeed = 1, shotInterval = .75f },
            new WaveDefinition { name = "LAST DEFENCE", rows = 5, armoredRows = 2, divingEnemies = true, marchSpeed = 1.1f, shotInterval = .65f }
        };
        public bool endlessAfterCampaign = true;
        [Range(1, 5)] public int startingLives = 3;
        [Range(3, 15)] public float playerSpeed = 8;
        [Range(.08f, .5f)] public float fireInterval = .18f;
        [Range(2, 15)] public float shieldCooldown = 7;

        public WaveDefinition GetWave(int index)
        {
            if (waves == null || waves.Length == 0) return new WaveDefinition();
            return waves[Mathf.Min(index, waves.Length - 1)] ?? new WaveDefinition();
        }
    }
}
