using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLKS
{
    /// <summary>Spins an object (used for clouds, sun, beacons).</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up; public float dps = 1f;
        void Update() { transform.Rotate(axis, dps * Time.deltaTime, Space.Self); }
    }

    /// <summary>Keeps the sky dome centred on the camera.</summary>
    public class FollowCam : MonoBehaviour
    {
        void LateUpdate() { if (Camera.main != null) transform.position = Camera.main.transform.position; }
    }

    public static class World
    {
        static readonly Dictionary<string, Color> bldCol = new Dictionary<string, Color>
        {
            {"glass_blue", new Color(.25f,.45f,.75f)}, {"glass_tinted", new Color(.1f,.2f,.22f)}, {"glass_clear", new Color(.55f,.68f,.75f)},
            {"concrete_dark", new Color(.3f,.3f,.31f)}, {"concrete_light", new Color(.7f,.7f,.66f)}, {"brick", new Color(.6f,.32f,.22f)},
            {"roof_tile", new Color(.4f,.2f,.15f)}, {"siding_white", new Color(.9f,.9f,.86f)}, {"roof_dark", new Color(.2f,.2f,.22f)},
            {"metal_corrugated", new Color(.5f,.5f,.55f)}, {"steel_frame", new Color(.4f,.42f,.45f)}, {"asphalt", new Color(.13f,.13f,.15f)},
            {"grass", new Color(.2f,.42f,.16f)}, {"wood_bench", new Color(.45f,.3f,.15f)}, {"tree_bark", new Color(.35f,.22f,.12f)},
            {"tree_leaves", new Color(.15f,.5f,.1f)}, {"lamp_metal", new Color(.3f,.3f,.32f)}, {"sidewalk", new Color(.6f,.58f,.55f)}, {"antenna_red", new Color(.8f,.1f,.1f)}
        };

        public static bool alienFlora;

        public static GameObject Model(string path, Transform parent, Material mat)
        {
            var pf = Resources.Load<GameObject>(path);
            if (pf == null) { Debug.LogWarning("Missing model " + path); return new GameObject("missing"); }
            var go = UnityEngine.Object.Instantiate(pf, parent);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                r.sharedMaterials = arr; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
        }

        /// <summary>City / spaceport pieces: per-material colours, glass glows faintly.</summary>
        public static GameObject Building(string name, Transform parent)
        {
            var pf = Resources.Load<GameObject>("Models/buildings/" + name);
            if (pf == null) { Debug.LogWarning("Missing building " + name); return new GameObject("missing"); }
            var go = UnityEngine.Object.Instantiate(pf, parent);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var src = r.sharedMaterials; var arr = new Material[src.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    string n = src[i] != null ? src[i].name.Replace(" (Instance)", "") : "concrete_light";
                    Color c; if (!bldCol.TryGetValue(n, out c)) c = new Color(.6f, .6f, .6f);
                    if (alienFlora && n == "tree_leaves") c = Color.HSVToRGB(UnityEngine.Random.Range(0.42f, 0.55f), 0.7f, 0.65f);
                    if (alienFlora && n == "tree_bark") c = new Color(0.22f, 0.18f, 0.2f);
                    bool glass = n.StartsWith("glass");
                    arr[i] = Mats.Lit(c, glass ? 0.9f : 0.25f, glass ? c * 0.35f : (Color?)null);
                }
                r.sharedMaterials = arr;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
            }
            return go;
        }

        /// <summary>Daytime gradient sky with a sun disc; toSun = direction towards the sun.</summary>
        public static GameObject SkyDome(Transform parent, Color top, Color horizon, Vector3 toSun, Color sunCol)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); UnityEngine.Object.Destroy(g.GetComponent<Collider>());
            g.name = "SkyDome"; g.transform.SetParent(parent, false); g.transform.localScale = Vector3.one * 3000f; g.AddComponent<FollowCam>();
            var m = new Material(Mats.S("TLKS_SkyGrad")); m.SetColor("_Top", top); m.SetColor("_Horizon", horizon); m.SetVector("_Sun", toSun.normalized); m.SetColor("_SunCol", sunCol);
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return g;
        }

        public static GameObject Sky(Transform parent)
        {
            var mat = new Material(Mats.S("TLKS_Sky")); mat.SetTexture("_BaseMap", Mats.Tex("stars_milkyway")); mat.SetFloat("_Exposure", 1.1f);
            var go = Model("Models/space/space_skybox", parent, mat);
            go.name = "Sky"; go.AddComponent<FollowCam>();
            return go;
        }

        public static Light MakeSun(Transform parent, Vector3 euler, float intensity = 2.2f, bool shadows = false)
        {
            var go = new GameObject("SunLight"); go.transform.SetParent(parent, false); go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = intensity; l.color = new Color(1f, 0.96f, 0.9f);
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None; l.shadowStrength = 0.9f;
            return l;
        }

        public class EarthRig { public GameObject root, surface, clouds, atmos; }

        public static EarthRig Earth(Transform parent)
        {
            var rig = new EarthRig();
            rig.root = new GameObject("Earth"); rig.root.transform.SetParent(parent, false);
            var em = new Material(Mats.S("TLKS_Earth"));
            em.SetTexture("_DayMap", Mats.Tex("earth_daymap")); em.SetTexture("_NightMap", Mats.Tex("earth_nightmap"));
            rig.surface = Model("Models/earth/earth", rig.root.transform, em); rig.surface.name = "Surface";
            rig.surface.AddComponent<Spin>().dps = 1.2f;
            var cm = new Material(Mats.S("TLKS_Clouds")); cm.SetTexture("_BaseMap", Mats.Tex("earth_clouds")); cm.SetFloat("_Density", 1.4f);
            rig.clouds = Model("Models/earth/earth_clouds", rig.root.transform, cm); rig.clouds.name = "Clouds";
            rig.clouds.AddComponent<Spin>().dps = 1.6f;
            var am = new Material(Mats.S("TLKS_Atmos")); am.SetColor("_Color", new Color(0.25f, 0.55f, 1.6f)); am.SetFloat("_Power", 3.2f); am.SetFloat("_Gain", 1.5f);
            rig.atmos = Model("Models/earth/earth_atmosphere", rig.root.transform, am); rig.atmos.name = "Atmosphere";
            rig.atmos.transform.localScale = Vector3.one * 1.015f;
            return rig;
        }

        public static GameObject Moon(Transform parent)
        {
            var m = Mats.Lit(Color.white, 0.05f, null, Mats.Tex("moon")); m.SetFloat("_Ambient", 0.25f);
            var go = Model("Models/moon/moon", parent, m); go.name = "Moon"; go.AddComponent<Spin>().dps = 0.4f;
            return go;
        }

        public static GameObject Sun(Transform parent)
        {
            var root = new GameObject("Sun"); root.transform.SetParent(parent, false);
            var sm = new Material(Mats.S("TLKS_Sun")); sm.SetTexture("_BaseMap", Mats.Tex("sun")); sm.SetColor("_Tint", new Color(3.2f, 2.2f, 0.9f));
            var body = Model("Models/sun/sun", root.transform, sm); body.AddComponent<Spin>().dps = 0.8f;
            var cm = new Material(Mats.S("TLKS_Atmos")); cm.SetColor("_Color", new Color(3f, 1.4f, 0.4f)); cm.SetFloat("_Power", 2.2f); cm.SetFloat("_Gain", 1.2f); cm.SetFloat("_SunLit", 0f);
            var cor = Model("Models/sun/sun_corona", root.transform, cm); cor.transform.localScale = Vector3.one * 1.12f;
            // billboard glow
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); UnityEngine.Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(root.transform, false); g.transform.localScale = Vector3.one * 160f;
            g.GetComponent<MeshRenderer>().sharedMaterial = Mats.Additive(new Color(1.6f, 0.9f, 0.4f, 0.55f));
            g.AddComponent<Billboard>();
            return root;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t);
            var c = g.GetComponent<Collider>(); if (c != null) UnityEngine.Object.Destroy(c);
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = scale;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        /// <summary>style 0 = the son's rugged ship, 1 = the father's nuclear ship, 2 = stranger (son's ship, dark running lights).
        /// Nose points along +Z. Child "Nozzle" is the engine exit.</summary>
        public static GameObject Ship(int style, Transform parent)
        {
            var root = new GameObject(style == 1 ? "FatherShip" : "SonShip"); root.transform.SetParent(parent, false);
            Color hull = style == 1 ? new Color(0.88f, 0.9f, 0.95f) : new Color(0.55f, 0.52f, 0.5f);
            Color trim = style == 1 ? new Color(0.2f, 0.45f, 0.9f) : new Color(0.9f, 0.45f, 0.12f);
            var mh = Mats.Lit(hull, 0.65f); var mt = Mats.Lit(trim, 0.5f, trim * 0.15f); var dark = Mats.Lit(new Color(0.08f, 0.09f, 0.1f), 0.6f);
            Prim(PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.22f, 0.32f, 0.22f), mh);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, 0, 0.3f), Vector3.zero, new Vector3(0.22f, 0.22f, 0.34f), mh);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0, 0.12f), new Vector3(90, 0, 0), new Vector3(0.235f, 0.03f, 0.235f), mt);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0, -0.12f), new Vector3(90, 0, 0), new Vector3(0.235f, 0.03f, 0.235f), mt);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0, -0.36f), new Vector3(90, 0, 0), new Vector3(0.16f, 0.06f, 0.16f), dark);
            var glass = Mats.Lit(new Color(0.05f, 0.1f, 0.15f), 0.95f, new Color(0.1f, 0.5f, 0.9f) * 0.8f);
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.08f, 0.34f), Vector3.zero, new Vector3(0.12f, 0.07f, 0.15f), glass);
            // solar wings / fins
            float span = style == 1 ? 0.55f : 0.34f;
            var panel = Mats.Lit(new Color(0.08f, 0.14f, 0.3f), 0.8f, new Color(0.02f, 0.05f, 0.12f));
            for (int s = -1; s <= 1; s += 2)
            {
                Prim(PrimitiveType.Cube, root.transform, new Vector3(s * (0.11f + span / 2), 0, -0.1f), Vector3.zero, new Vector3(span, 0.012f, 0.22f), style == 1 ? panel : mt);
                Prim(PrimitiveType.Cube, root.transform, new Vector3(s * 0.1f, 0, -0.1f), Vector3.zero, new Vector3(0.05f, 0.03f, 0.05f), dark);
            }
            // dish
            var dish = Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.15f, -0.08f), new Vector3(-50, 0, 0), new Vector3(0.14f, 0.02f, 0.14f), mh);
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.12f, -0.08f), Vector3.zero, new Vector3(0.012f, 0.03f, 0.012f), dark);
            // nav lights
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(0.3f * (style == 1 ? 1.4f : 1f), 0, -0.1f), Vector3.zero, Vector3.one * 0.025f, Mats.Lit(Color.red, 0.2f, new Color(4, 0.2f, 0.2f)));
            Prim(PrimitiveType.Sphere, root.transform, new Vector3(-0.3f * (style == 1 ? 1.4f : 1f), 0, -0.1f), Vector3.zero, Vector3.one * 0.025f, Mats.Lit(Color.green, 0.2f, new Color(0.2f, 4, 0.2f)));
            if (style == 1) // nuclear reactor boom
            {
                Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0, -0.62f), new Vector3(90, 0, 0), new Vector3(0.05f, 0.24f, 0.05f), mh);
                Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, 0, -0.9f), Vector3.zero, new Vector3(0.14f, 0.14f, 0.1f), Mats.Lit(new Color(0.3f, 0.35f, 0.4f), 0.6f, new Color(0.1f, 0.8f, 1f) * 0.7f));
            }
            // detail: radiators, window strip, engine glow
            for (int s = -1; s <= 1; s += 2)
            {
                if (style == 1) Prim(PrimitiveType.Cube, root.transform, new Vector3(0, s * 0.2f, -0.45f), Vector3.zero, new Vector3(0.012f, 0.2f, 0.5f), mh);
                for (int w = 0; w < 4; w++) Prim(PrimitiveType.Cube, root.transform, new Vector3(s * 0.111f, 0.035f, 0.02f + w * 0.07f), Vector3.zero, new Vector3(0.006f, 0.025f, 0.035f), Mats.Lit(new Color(1f, 0.85f, 0.5f), 0.2f, new Color(2.2f, 1.6f, 0.7f)));
            }
            Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0, style == 1 ? -1.0f : -0.435f), new Vector3(90, 0, 0), new Vector3(style == 1 ? 0.12f : 0.14f, 0.004f, style == 1 ? 0.12f : 0.14f), Mats.Lit(Color.black, 0.2f, style == 1 ? new Color(0.6f, 2.2f, 3.4f) : new Color(3.4f, 1.6f, 0.5f)));
            var nz = new GameObject("Nozzle"); nz.transform.SetParent(root.transform, false); nz.transform.localPosition = new Vector3(0, 0, style == 1 ? -0.97f : -0.42f);
            return root;
        }

        public static void Starfield(Transform parent, int count, float radius)
        {
            // sparse bright points for parallax while moving
            var go = new GameObject("Dust"); go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.startLifetime = 9999; main.startSpeed = 0; main.startSize = 0.03f; main.maxParticles = count;
            main.startColor = new Color(0.7f, 0.85f, 1f, 0.7f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = Mats.Additive(new Color(2, 2, 2, 1));
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++) { ep.position = UnityEngine.Random.insideUnitSphere * radius; ep.startSize = UnityEngine.Random.Range(0.02f, 0.07f); ep.startLifetime = 99999; ep.velocity = Vector3.zero; ep.startColor = new Color(0.7f, 0.85f, 1f, UnityEngine.Random.Range(0.3f, 0.9f)); ps.Emit(ep, 1); }
            ps.Play();
        }
    }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate() { if (Camera.main != null) transform.rotation = Camera.main.transform.rotation; }
    }
}
