using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

namespace TLKS
{
    public class Part { public string id, name, kind, source; public float mass, thrust, isp; }
    public class Body { public string name; public double rangeKm, lightTimeSec; public Vector3 pos; }
    public class Exo { public string name, host, method; public float radiusE, insol, teq, period, massE, starTeff, distPc; public int year; }
    public class DsnLink { public string dish, station, craft; public double rtlt; public bool down; }

    public static class Nasa
    {
        public const double C = 299792.458; // km/s
        public static List<Part> parts = new List<Part>();
        public static Dictionary<string, Body> bodies = new Dictionary<string, Body>();
        public static List<Exo> exo = new List<Exo>();
        public static List<DsnLink> dsn = new List<DsnLink>();
        public static bool dsnLive; public static string dsnStamp = "snapshot";
        static bool loaded;

        static double D(string s) { double v; double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v); return v; }
        static float F(string s) { return (float)D(s); }

        public static void Load()
        {
            if (loaded) return; loaded = true;
            var pc = Resources.Load<TextAsset>("NasaData/parts");
            if (pc != null)
            {
                var lines = pc.text.Split('\n');
                for (int i = 1; i < lines.Length; i++)
                {
                    var f = lines[i].Trim().Split(',');
                    if (f.Length < 7) continue;
                    parts.Add(new Part { id = f[0], name = f[1], kind = f[2], mass = F(f[3]), thrust = F(f[4]), isp = F(f[5]), source = f[6] });
                }
            }
            string[] ids = { "10", "301", "499", "599" }; string[] names = { "Sun", "Moon", "Mars", "Jupiter" };
            for (int k = 0; k < ids.Length; k++)
            {
                var ta = Resources.Load<TextAsset>("NasaData/h_" + ids[k]);
                if (ta == null) continue;
                bool inside = false;
                foreach (var ln in ta.text.Split('\n'))
                {
                    if (ln.StartsWith("$$SOE")) { inside = true; continue; }
                    if (ln.StartsWith("$$EOE")) break;
                    if (!inside) continue;
                    var f = ln.Split(',');
                    if (f.Length < 10) continue;
                    // JDTDB, date, X,Y,Z, VX,VY,VZ, LT, RG, RR
                    bodies[names[k]] = new Body { name = names[k], lightTimeSec = D(f[8]), rangeKm = D(f[9]), pos = new Vector3((float)D(f[2]), (float)D(f[3]), (float)D(f[4])) };
                    break;
                }
            }
            var ec = Resources.Load<TextAsset>("NasaData/exoplanets");
            if (ec != null)
            {
                var lines = ec.text.Split('\n');
                for (int i = 1; i < lines.Length; i++)
                {
                    var f = Csv(lines[i]);
                    if (f.Count < 11) continue;
                    exo.Add(new Exo { name = f[0], host = f[1], radiusE = F(f[2]), insol = F(f[3]), teq = F(f[4]), period = F(f[5]), massE = F(f[6]), starTeff = F(f[7]), distPc = F(f[8]), year = (int)D(f[9]), method = f[10] });
                }
            }
            var dx = Resources.Load<TextAsset>("NasaData/dsn");
            if (dx != null) ParseDsn(dx.text);
        }

        static List<string> Csv(string line)
        {
            var r = new List<string>(); bool q = false; var sb = new System.Text.StringBuilder();
            foreach (char c in line.TrimEnd('\r', '\n'))
            {
                if (c == '"') q = !q; else if (c == ',' && !q) { r.Add(sb.ToString()); sb.Length = 0; } else sb.Append(c);
            }
            r.Add(sb.ToString()); return r;
        }

        public static Exo Planet() { foreach (var e in exo) if (e.name == "Kepler-442 b") return e; return exo.Count > 0 ? exo[0] : new Exo { name = "Kepler-442 b", radiusE = 1.34f, insol = 0.66f, teq = 241, period = 112.3f, massE = 2.36f, distPc = 366 }; }
        public static Body Get(string n) { Body b; if (bodies.TryGetValue(n, out b)) return b; return new Body { name = n, rangeKm = 4e5, lightTimeSec = 1.3 }; }
        public static double LightDelay(double km) { return km / C; }
        public static string Dur(double s)
        {
            if (s < 120) return s.ToString("0.0") + " s";
            if (s < 7200) return (s / 60).ToString("0.0") + " min";
            if (s < 172800) return (s / 3600).ToString("0.0") + " h";
            if (s < 86400.0 * 730) return (s / 86400).ToString("0.0") + " days";
            return (s / 31557600.0).ToString("N0") + " years";
        }

        static string Station(string dish)
        {
            int n; int.TryParse(dish.Replace("DSS", ""), out n);
            if (n >= 10 && n < 30) return "Goldstone"; if (n >= 30 && n < 50) return "Canberra"; if (n >= 50 && n < 70) return "Madrid"; return "DSN";
        }

        public static void ParseDsn(string xml)
        {
            try
            {
                var d = new XmlDocument(); d.LoadXml(xml);
                var list = new List<DsnLink>();
                foreach (XmlNode dish in d.SelectNodes("//dish"))
                {
                    var t = dish.SelectSingleNode("target"); if (t == null) continue;
                    string n = t.Attributes["name"] != null ? t.Attributes["name"].Value : "";
                    if (n == "DSN" || n == "") continue;
                    double rt = D(t.Attributes["rtlt"] != null ? t.Attributes["rtlt"].Value : "0");
                    var down = dish.SelectSingleNode("downSignal[@active='true']");
                    string dn = dish.Attributes["name"].Value;
                    list.Add(new DsnLink { dish = dn, station = Station(dn), craft = n, rtlt = rt, down = down != null });
                }
                if (list.Count > 0) dsn = list;
            }
            catch (Exception e) { Debug.LogWarning("DSN parse: " + e.Message); }
        }

        /// <summary>Best-effort live refresh of the DSN Now feed; the bundled snapshot is the fallback.</summary>
        public static IEnumerator RefreshDsn()
        {
            using (var rq = UnityWebRequest.Get("https://eyes.nasa.gov/dsn/data/dsn.xml?r=" + (int)(Time.realtimeSinceStartup)))
            {
                rq.timeout = 5;
                yield return rq.SendWebRequest();
                if (rq.result == UnityWebRequest.Result.Success && rq.downloadHandler.text.Contains("<dsn"))
                { ParseDsn(rq.downloadHandler.text); dsnLive = true; dsnStamp = "LIVE " + DateTime.UtcNow.ToString("HH:mm") + " UTC"; }
            }
        }
    }
}
