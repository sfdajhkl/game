using UnityEngine;

namespace Arvito
{
    internal sealed class FieldAudio
    {
        private readonly AudioSource source;
        private readonly AudioClip rifle, hostile, hit, wreck, salvage, wave, exit;

        public FieldAudio(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            rifle = Tone("리벳",540,105,.10f,.16f);
            hostile = Tone("적탄",280,85,.12f,.11f);
            hit = Tone("금속 충돌",710,200,.075f,.30f);
            wreck = Tone("기체 정지",160,33,.35f,.48f);
            salvage = Tone("접속",470,910,.16f,.02f);
            wave = Tone("신호",270,175,.40f,0);
            exit = Tone("귀환",410,820,.42f,.01f);
        }

        private static AudioClip Tone(string name,float start,float end,float length,float noise)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(sampleRate*length)];
            var random = new System.Random((int)start);
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Mathf.Lerp(start,end,t)*2*Mathf.PI/sampleRate;
                float env = Mathf.Min(1,t*36) * (1-t)*(1-t);
                samples[i] = env * (.60f*Mathf.Sin(phase) + .14f*Mathf.Sin(phase*2) + noise*((float)random.NextDouble()*2-1));
            }
            AudioClip clip = AudioClip.Create(name,samples.Length,1,sampleRate,false);
            clip.SetData(samples,0);
            return clip;
        }

        public void Play(FieldEvent e,bool friendly,float proximity)
        {
            AudioClip clip;
            switch (e)
            {
                case FieldEvent.Shot: clip = friendly ? rifle : hostile; break;
                case FieldEvent.Hit: clip = hit; break;
                case FieldEvent.Wreck: clip = wreck; break;
                case FieldEvent.Salvage: clip = salvage; break;
                case FieldEvent.Wave: clip = wave; break;
                default: clip = exit; break;
            }
            source.PlayOneShot(clip, (.16f + .36f*proximity) * (e == FieldEvent.Shot ? .55f : 1));
        }

        public void Dispose()
        {
            foreach (AudioClip clip in new[]{rifle,hostile,hit,wreck,salvage,wave,exit}) Object.Destroy(clip);
        }
    }
}
