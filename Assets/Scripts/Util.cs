using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TLKS
{
    public static class Theme
    {
        public static readonly Color Bg = new Color(0.025f, 0.045f, 0.085f, 0.86f);
        public static readonly Color Bg2 = new Color(0.05f, 0.08f, 0.14f, 0.92f);
        public static readonly Color Cyan = new Color(0.31f, 0.82f, 1f, 1f);
        public static readonly Color Amber = new Color(1f, 0.71f, 0.28f, 1f);
        public static readonly Color Red = new Color(1f, 0.36f, 0.36f, 1f);
        public static readonly Color Green = new Color(0.36f, 1f, 0.62f, 1f);
        public static readonly Color Text = new Color(0.9f, 0.95f, 1f, 1f);
        public static readonly Color Dim = new Color(0.55f, 0.63f, 0.74f, 1f);
        public static Color Heat(float p) // 0 red -> 0.5 amber -> 1 green
        {
            return p < 0.5f ? Color.Lerp(Red, Amber, p * 2f) : Color.Lerp(Amber, Green, (p - 0.5f) * 2f);
        }
    }

    /// <summary>Yield instruction completed by code (button click etc).</summary>
    public class Wait : CustomYieldInstruction
    {
        public bool done;
        public override bool keepWaiting { get { return !done; } }
        public void Done() { done = true; }
    }

    public static class Mats
    {
        static Texture2D glow, white;
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        public static Shader S(string n) { return Resources.Load<Shader>("Shaders/" + n); }

        public static Texture2D Tex(string n)
        {
            Texture2D t;
            if (!texCache.TryGetValue(n, out t)) { t = Resources.Load<Texture2D>("Textures/" + n); texCache[n] = t; }
            return t;
        }

        public static Texture2D Glow()
        {
            if (glow != null) return glow;
            int s = 128;
            glow = new Texture2D(s, s, TextureFormat.RGBA32, false);
            glow.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - s / 2f) / (s / 2f), dy = (y - s / 2f) / (s / 2f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    glow.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            glow.Apply();
            return glow;
        }

        public static Texture2D White()
        {
            if (white != null) return white;
            white = new Texture2D(2, 2); white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); white.Apply();
            return white;
        }

        static Texture2D noise, ground;
        public static Texture2D NoiseTex()
        {
            if (noise != null) return noise;
            int s = 256; noise = new Texture2D(s, s, TextureFormat.RGBA32, true); noise.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = x / (float)s, v = y / (float)s;
                float n = Mathf.PerlinNoise(u * 8f, v * 8f) * 0.5f + Mathf.PerlinNoise(u * 24f + 5f, v * 24f) * 0.3f + Mathf.PerlinNoise(u * 64f, v * 64f + 9f) * 0.2f;
                float g = 0.78f + n * 0.3f; noise.SetPixel(x, y, new Color(g, g, g, 1));
            }
            noise.Apply(); return noise;
        }
        public static Material Ground()
        {
            if (ground == null)
            {
                int s = 256; ground = new Texture2D(s, s, TextureFormat.RGBA32, true); ground.wrapMode = TextureWrapMode.Repeat; ground.anisoLevel = 8;
                for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.12f + UnityEngine.Random.value * 0.03f;
                    float g = 0.62f + n; bool line = (x % 128) < 2 || (y % 128) < 2; if (line) g = 0.3f;
                    ground.SetPixel(x, y, new Color(g, g, g * 1.04f, 1));
                }
                ground.Apply();
            }
            var m = Lit(new Color(0.6f, 0.63f, 0.7f), 0.35f, null, ground); m.SetTextureScale("_BaseMap", new Vector2(120, 120)); return m;
        }

        public static Material Lit(Color c, float smooth = 0.4f, Color? emission = null, Texture tex = null, bool vertexColor = false)
        {
            var m = new Material(S("TLKS_Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetColor("_EmissionColor", emission ?? Color.black);
            m.SetTexture("_BaseMap", tex != null ? tex : White());
            m.SetFloat("_VC", vertexColor ? 1f : 0f);
            return m;
        }

        public static Material Additive(Color tint, Texture tex = null)
        {
            var m = new Material(S("TLKS_Additive"));
            m.SetColor("_Tint", tint);
            m.SetTexture("_BaseMap", tex != null ? tex : Glow());
            return m;
        }
    }

    public static class Fx
    {
        public static ParticleSystem Make(Transform parent, string name, Color c, float size, float life, float speed, float rate, float cone = 8f, bool world = true, float hdr = 1.5f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.7f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = c;
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = 3000;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = cone; sh.radius = size * 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1, 0.6f, 0.3f), 0.5f), new GradientColorKey(new Color(0.4f, 0.1f, 0.05f), 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(1, 1.6f)));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = Mats.Additive(new Color(hdr, hdr, hdr, 1f));
            r.sortingFudge = 2;
            ps.Play();
            return ps;
        }

        public static ParticleSystem Flame(Transform parent, Vector3 localPos, float scale, Color core)
        {
            var ps = Make(parent, "Flame", core, 1.2f * scale, 0.5f, 7f * scale, 160, 4f, true, 3f);
            ps.transform.localPosition = localPos;
            ps.transform.localRotation = Quaternion.Euler(0, 180, 0);
            return ps;
        }

        public static void Burst(Vector3 pos, Color c, int count, float size, float speed, float life = 1.4f)
        {
            var go = new GameObject("Burst");
            go.transform.position = pos;
            var ps = Make(go.transform, "p", c, size, life, speed, 0, 180f, true, 4f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = size * 0.2f;
            ps.Emit(count);
            UnityEngine.Object.Destroy(go, life + 1f);
        }

        /// <summary>Drifting spores / dust motes that follow the camera.</summary>
        public static void Motes(Transform parent, Color c)
        {
            var go = new GameObject("Motes"); go.transform.SetParent(parent, false); go.AddComponent<FollowCam>();
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.duration = 5f; main.startLifetime = 9f; main.startSpeed = 0.15f; main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f); main.startColor = c; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 600;
            var em = ps.emission; em.rateOverTime = 45;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(60, 18, 60);
            var n = ps.noise; n.enabled = true; n.strength = 0.6f; n.frequency = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().material = Mats.Additive(new Color(2f, 1.8f, 1.2f, 1f));
            ps.Play();
        }

        public static void SetupPost(GameObject host)
        {
            var vol = host.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 1;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = p.Add<Bloom>(true);
            bloom.intensity.Override(0.9f); bloom.threshold.Override(0.95f); bloom.scatter.Override(0.72f);
            bloom.highQualityFiltering.Override(true);
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var vg = p.Add<Vignette>(true); vg.intensity.Override(0.32f); vg.smoothness.Override(0.55f);
            var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(0.15f); ca.contrast.Override(14f); ca.saturation.Override(10f);
            var ch = p.Add<ChromaticAberration>(true); ch.intensity.Override(0.08f);
            var fg = p.Add<FilmGrain>(true); fg.type.Override(FilmGrainLookup.Thin1); fg.intensity.Override(0.18f);
            vol.sharedProfile = p;
        }
    }

    public class Sfx : MonoBehaviour
    {
        public AudioSource music, loop, one;
        AudioClip click, beep, alarm, boom, pad, rumble, whoosh, ok, bad;

        public static AudioClip Make(string n, float secs, Func<float, int, float> f, bool stereo = false)
        {
            int sr = 44100, len = (int)(secs * sr);
            var d = new float[len];
            for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(f(i / (float)sr, i), -1f, 1f);
            var c = AudioClip.Create(n, len, 1, sr, false);
            c.SetData(d, 0);
            return c;
        }

        public void Init()
        {
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = 0.5f;
            loop = gameObject.AddComponent<AudioSource>(); loop.loop = true; loop.volume = 0f;
            one = gameObject.AddComponent<AudioSource>();
            var rnd = new System.Random(7);
            click = Make("click", 0.06f, (t, i) => Mathf.Sin(t * 2 * Mathf.PI * 2100f) * Mathf.Exp(-t * 70f) * 0.5f);
            beep = Make("beep", 0.18f, (t, i) => Mathf.Sin(t * 2 * Mathf.PI * 880f) * Mathf.Exp(-t * 12f) * 0.5f);
            ok = Make("ok", 0.5f, (t, i) => (Mathf.Sin(t * 2 * Mathf.PI * 660f) + Mathf.Sin(t * 2 * Mathf.PI * (t > 0.12f ? 990f : 770f)) * 0.7f) * Mathf.Exp(-t * 5f) * 0.3f);
            bad = Make("bad", 0.6f, (t, i) => Mathf.Sign(Mathf.Sin(t * 2 * Mathf.PI * (t < 0.2f ? 220f : 165f))) * Mathf.Exp(-t * 4f) * 0.25f);
            alarm = Make("alarm", 1.0f, (t, i) => Mathf.Sin(t * 2 * Mathf.PI * (Mathf.Repeat(t * 3f, 1f) < 0.5f ? 740f : 520f)) * 0.3f * (1f - t * 0.3f));
            float last = 0;
            boom = Make("boom", 2.5f, (t, i) => { last = last * 0.96f + ((float)rnd.NextDouble() * 2 - 1) * 0.12f; return (last * 5f + Mathf.Sin(t * 2 * Mathf.PI * 45f) * 0.4f) * Mathf.Exp(-t * 2.2f); });
            whoosh = Make("whoosh", 1.6f, (t, i) => { last = last * 0.8f + ((float)rnd.NextDouble() * 2 - 1) * 0.2f; return last * Mathf.Sin(t / 1.6f * Mathf.PI) * 0.8f; });
            float b = 0;
            rumble = Make("rumble", 4f, (t, i) => { b = b * 0.985f + ((float)rnd.NextDouble() * 2 - 1) * 0.06f; return b * 3f; });
            pad = Make("pad", 16f, (t, i) =>
            {
                float lfo = 0.5f + 0.5f * Mathf.Sin(t * 2 * Mathf.PI / 16f * 2f);
                float v = Mathf.Sin(t * 2 * Mathf.PI * 55f) * 0.25f + Mathf.Sin(t * 2 * Mathf.PI * 82.5f) * 0.14f * lfo
                        + Mathf.Sin(t * 2 * Mathf.PI * 110.5f) * 0.1f + Mathf.Sin(t * 2 * Mathf.PI * 165.25f) * 0.05f * (1 - lfo)
                        + Mathf.Sin(t * 2 * Mathf.PI * 329.5f) * 0.015f * lfo;
                return v * 0.8f;
            });
            music.clip = pad; music.Play();
        }

        public void Click() { one.PlayOneShot(click, 0.6f); }
        public void Beep() { one.PlayOneShot(beep, 0.5f); }
        public void Ok() { one.PlayOneShot(ok, 0.7f); }
        public void Bad() { one.PlayOneShot(bad, 0.8f); }
        public void Alarm() { one.PlayOneShot(alarm, 0.6f); }
        public void Boom() { one.PlayOneShot(boom, 1f); }
        public void Whoosh() { one.PlayOneShot(whoosh, 0.7f); }
        public void Rumble(float vol)
        {
            if (loop.clip != rumble) { loop.clip = rumble; loop.Play(); }
            loop.volume = vol;
        }
        public void StopRumble() { loop.volume = 0; }
    }
}
