using UnityEngine;

namespace OrbitGuard
{
    public sealed class GameAudio
    {
        readonly AudioSource source;
        readonly AudioClip shot, hit, hurt, clear;
        public GameAudio(GameObject root)
        {
            source = root.AddComponent<AudioSource>(); source.playOnAwake = false;
            shot = Tone("Laser", 760, 320, .065f);
            hit = Tone("Burst", 170, 45, .13f);
            hurt = Tone("Damage", 110, 35, .3f);
            clear = Tone("Wave clear", 400, 1100, .45f);
        }
        static AudioClip Tone(string name, float start, float end, float duration)
        {
            const int rate = 22050;
            var samples = new float[(int)(duration * rate)]; float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Mathf.Lerp(start, end, t) / rate;
                samples[i] = Mathf.Sin(phase * Mathf.PI * 2) * (1 - t) * Mathf.Min(1, t * 30) * .22f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        public void Play(int kind, float volume)
        {
            if (volume <= 0) return;
            source.PlayOneShot(kind == 0 ? shot : kind == 1 ? hit : kind == 2 ? hurt : clear, volume);
        }
    }
}
