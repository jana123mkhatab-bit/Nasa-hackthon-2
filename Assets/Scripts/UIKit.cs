using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TLKS
{
    public enum A { TL, T, TR, L, C, R, BL, B, BR }

    public class RiskGauge : MonoBehaviour
    {
        public Image arc; public RectTransform needle; public Text value, caption;
        public float target, shown; public bool unknown;
        float vel;
        void Update()
        {
            if (unknown)
            {
                float flick = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                float w = 0.5f + 0.42f * Mathf.Sin(Time.unscaledTime * 2.1f) * Mathf.Sin(Time.unscaledTime * 0.7f);
                arc.fillAmount = w; arc.color = Color.Lerp(Theme.Dim, Theme.Amber, flick);
                needle.localRotation = Quaternion.Euler(0, 0, 90f - 180f * w);
                value.text = "??%"; value.color = Color.Lerp(Theme.Dim, Theme.Amber, flick);
                return;
            }
            shown = Mathf.SmoothDamp(shown, target, ref vel, 0.25f, 5f, Time.unscaledDeltaTime);
            arc.fillAmount = shown; arc.color = Theme.Heat(Mathf.Clamp01(shown * 1.25f));
            needle.localRotation = Quaternion.Euler(0, 0, 90f - 180f * shown);
            value.text = Mathf.RoundToInt(shown * 100f) + "%"; value.color = arc.color;
        }
        public void Set(float p) { unknown = false; target = Mathf.Clamp01(p); }
        public void Snap(float p) { unknown = false; target = shown = Mathf.Clamp01(p); }
    }

    public class UIKit
    {
        public Canvas canvas;
        public RectTransform root, stage, overlay;
        public Font font;
        public Sprite round, ringHalf, circle, whiteSpr;
        Image fade, barTop, barBot;
        RectTransform dlg; Text dlgName, dlgText, dlgHint;
        public Sfx sfx;
        public bool dialogueBusy;

        public UIKit(Sfx s)
        {
            sfx = s;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Bahnschrift", "Segoe UI", "Arial" }, 32);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("UI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();
                UnityEngine.Object.DontDestroyOnLoad(es);
            }
            root = go.GetComponent<RectTransform>();
            BuildSprites();
            stage = Stretch(root, "Stage"); overlay = Stretch(root, "Overlay");
            barTop = Img(overlay, "BarTop", A.T, 0, 0, 0, 0, Color.black); barTop.rectTransform.anchorMin = new Vector2(0, 1); barTop.rectTransform.anchorMax = new Vector2(1, 1);
            barTop.rectTransform.sizeDelta = new Vector2(0, 0); barTop.raycastTarget = false;
            barBot = Img(overlay, "BarBot", A.B, 0, 0, 0, 0, Color.black); barBot.rectTransform.anchorMin = new Vector2(0, 0); barBot.rectTransform.anchorMax = new Vector2(1, 0);
            barBot.rectTransform.sizeDelta = new Vector2(0, 0); barBot.raycastTarget = false;
            BuildDialogue();
            fade = Img(overlay, "Fade", A.C, 0, 0, 10, 10, Color.black);
            fade.rectTransform.anchorMin = Vector2.zero; fade.rectTransform.anchorMax = Vector2.one; fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;
            fade.raycastTarget = false; fade.color = new Color(0, 0, 0, 1);
        }

        // ---------- sprites
        void BuildSprites()
        {
            int s = 64; var t = new Texture2D(s, s, TextureFormat.RGBA32, false); float r = 18;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - s / 2f) - (s / 2f - r), py = Mathf.Abs(y + 0.5f - s / 2f) - (s / 2f - r);
                    float d = Mathf.Sqrt(Mathf.Max(px, 0) * Mathf.Max(px, 0) + Mathf.Max(py, 0) * Mathf.Max(py, 0)) + Mathf.Min(Mathf.Max(px, py), 0) - r;
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
                }
            t.Apply(); t.wrapMode = TextureWrapMode.Clamp;
            round = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
            int w = 256, h = 128; var g = new Texture2D(w, h, TextureFormat.RGBA32, false); g.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x + 0.5f - w / 2f, dy = y + 0.5f; float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(Mathf.Min(d - 100f, 126f - d) * 0.8f + 0.5f);
                    g.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            g.Apply();
            ringHalf = Sprite.Create(g, new Rect(0, 0, w, h), new Vector2(.5f, 0f), 100);
            int c = 64; var ct = new Texture2D(c, c, TextureFormat.RGBA32, false); ct.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < c; y++) for (int x = 0; x < c; x++) { float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(c / 2f, c / 2f)); ct.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(c / 2f - d))); }
            ct.Apply(); circle = Sprite.Create(ct, new Rect(0, 0, c, c), new Vector2(.5f, .5f), 100);
            var wt = new Texture2D(2, 2); wt.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); wt.Apply();
            whiteSpr = Sprite.Create(wt, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 100);
        }

        // ---------- layout helpers
        static Vector2 Anc(A a)
        {
            switch (a)
            {
                case A.TL: return new Vector2(0, 1); case A.T: return new Vector2(.5f, 1); case A.TR: return new Vector2(1, 1);
                case A.L: return new Vector2(0, .5f); case A.C: return new Vector2(.5f, .5f); case A.R: return new Vector2(1, .5f);
                case A.BL: return new Vector2(0, 0); case A.B: return new Vector2(.5f, 0); default: return new Vector2(1, 0);
            }
        }
        public RectTransform Rt(Transform parent, string name, A a, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            var v = Anc(a); rt.anchorMin = rt.anchorMax = rt.pivot = v;
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }
        public RectTransform Stretch(Transform parent, string name, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }
        public Image Img(Transform parent, string name, A a, float x, float y, float w, float h, Color c, Sprite spr = null)
        {
            var rt = Rt(parent, name, a, x, y, w, h);
            var im = rt.gameObject.AddComponent<Image>(); im.color = c;
            im.sprite = spr != null ? spr : whiteSpr;
            if (spr == round) im.type = Image.Type.Sliced;
            return im;
        }
        public RectTransform Panel(Transform parent, A a, float x, float y, float w, float h, Color? c = null, bool outline = true)
        {
            var im = Img(parent, "Panel", a, x, y, w, h, c ?? Theme.Bg, round);
            if (outline) { var o = im.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.31f, 0.82f, 1f, 0.28f); o.effectDistance = new Vector2(1.5f, -1.5f); }
            return im.rectTransform;
        }
        public Text Label(Transform parent, string text, int size, Color? col, A a, float x, float y, float w, float h, TextAnchor al = TextAnchor.UpperLeft, FontStyle st = FontStyle.Normal)
        {
            var rt = Rt(parent, "Text", a, x, y, w, h);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = col ?? Theme.Text; t.alignment = al; t.fontStyle = st;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = true;
            return t;
        }
        public Button Btn(Transform parent, string label, A a, float x, float y, float w, float h, Action cb, Color? accent = null, int size = 22)
        {
            var ac = accent ?? Theme.Cyan;
            var im = Img(parent, "Btn_" + label, a, x, y, w, h, new Color(ac.r * 0.22f, ac.g * 0.22f, ac.b * 0.22f, 0.95f), round);
            var o = im.gameObject.AddComponent<Outline>(); o.effectColor = new Color(ac.r, ac.g, ac.b, 0.7f); o.effectDistance = new Vector2(1.5f, -1.5f);
            var b = im.gameObject.AddComponent<Button>(); b.targetGraphic = im;
            var cb2 = b.colors; cb2.normalColor = Color.white; cb2.highlightedColor = new Color(1.9f, 1.9f, 1.9f); cb2.pressedColor = new Color(0.8f, 0.8f, 0.8f); cb2.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f); cb2.fadeDuration = 0.08f; cb2.colorMultiplier = 1f;
            b.colors = cb2;
            var tx = Label(im.transform, label, size, ac, A.C, 0, 0, w - 12, h, TextAnchor.MiddleCenter, FontStyle.Bold);
            tx.rectTransform.anchorMin = Vector2.zero; tx.rectTransform.anchorMax = Vector2.one; tx.rectTransform.offsetMin = Vector2.zero; tx.rectTransform.offsetMax = Vector2.zero;
            b.onClick.AddListener(() => { if (sfx != null) sfx.Click(); if (cb != null) cb(); });
            return b;
        }
        public static void SetBtn(Button b, string label)
        {
            var t = b.GetComponentInChildren<Text>(); if (t != null) t.text = label;
        }
        public Slider Sld(Transform parent, A a, float x, float y, float w, float h, float min, float max, float val, bool whole, Action<float> cb)
        {
            var rt = Rt(parent, "Slider", a, x, y, w, h);
            var sl = rt.gameObject.AddComponent<Slider>();
            var bg = Img(rt, "BG", A.C, 0, 0, w, 8, new Color(1, 1, 1, 0.12f), round); bg.rectTransform.anchorMin = new Vector2(0, .5f); bg.rectTransform.anchorMax = new Vector2(1, .5f); bg.rectTransform.sizeDelta = new Vector2(0, 8);
            var fa = Stretch(rt, "FillArea", 0, 0, 0, 0); fa.anchorMin = new Vector2(0, .5f); fa.anchorMax = new Vector2(1, .5f); fa.offsetMin = new Vector2(0, -4); fa.offsetMax = new Vector2(0, 4);
            var fill = Img(fa, "Fill", A.L, 0, 0, 0, 8, Theme.Cyan, round);
            var ha = Stretch(rt, "HandleArea", 10, 0, 10, 0);
            var hd = Img(ha, "Handle", A.C, 0, 0, 22, 22, Color.white, circle);
            sl.fillRect = fill.rectTransform; sl.handleRect = hd.rectTransform; sl.targetGraphic = hd; sl.direction = Slider.Direction.LeftToRight;
            sl.minValue = min; sl.maxValue = max; sl.wholeNumbers = whole; sl.value = val;
            sl.onValueChanged.AddListener(v => { if (cb != null) cb(v); });
            return sl;
        }
        public RiskGauge Gauge(Transform parent, A a, float x, float y, float w, string caption)
        {
            float h = w / 2f;
            var rt = Rt(parent, "Gauge", a, x, y, w, h + 90);
            var bg = Img(rt, "ArcBG", A.T, 0, 0, w, h, new Color(1, 1, 1, 0.1f), ringHalf);
            var arc = Img(rt, "Arc", A.T, 0, 0, w, h, Theme.Amber, ringHalf);
            arc.type = Image.Type.Filled; arc.fillMethod = Image.FillMethod.Radial180; arc.fillOrigin = 0; arc.fillClockwise = true; arc.fillAmount = 0.5f;
            var needle = Img(rt, "Needle", A.T, 0, -h, 5, h * 0.82f, Color.white, whiteSpr);
            needle.rectTransform.pivot = new Vector2(0.5f, 0f); needle.rectTransform.anchoredPosition = new Vector2(0, -h);
            var hub = Img(rt, "Hub", A.T, 0, -h + 11, 22, 22, Color.white, circle);
            var val = Label(rt, "0%", (int)(w * 0.16f), Theme.Amber, A.T, 0, -h - 18, w, w * 0.2f, TextAnchor.UpperCenter, FontStyle.Bold);
            var cap = Label(rt, caption, 17, Theme.Dim, A.T, 0, -h - 18 - w * 0.19f, w, 30, TextAnchor.UpperCenter);
            var g = rt.gameObject.AddComponent<RiskGauge>();
            g.arc = arc; g.needle = needle.rectTransform; g.value = val; g.caption = cap;
            return g;
        }

        // ---------- stage handling
        public void ClearStage()
        {
            for (int i = stage.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(stage.GetChild(i).gameObject);
        }

        // ---------- fade / bars
        public IEnumerator Fade(float to, float dur)
        {
            float from = fade.color.a, t = 0;
            while (t < dur) { t += Time.unscaledDeltaTime; fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / dur))); yield return null; }
            fade.color = new Color(0, 0, 0, to);
        }
        public void FadeSnap(float a) { fade.color = new Color(0, 0, 0, a); }
        public IEnumerator Letterbox(bool on, float dur = 0.8f)
        {
            float from = barTop.rectTransform.sizeDelta.y, to = on ? 120f : 0f, t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime; float v = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / dur));
                barTop.rectTransform.sizeDelta = new Vector2(0, v); barBot.rectTransform.sizeDelta = new Vector2(0, v); yield return null;
            }
        }

        // ---------- dialogue
        void BuildDialogue()
        {
            dlg = Panel(overlay, A.B, 0, 60, 1300, 190, new Color(0.02f, 0.035f, 0.07f, 0.92f));
            dlgName = Label(dlg, "NAME", 24, Theme.Cyan, A.TL, 28, -18, 600, 34, TextAnchor.UpperLeft, FontStyle.Bold);
            dlgText = Label(dlg, "", 28, Theme.Text, A.TL, 28, -58, 1244, 120);
            dlgHint = Label(dlg, "CLICK / SPACE", 15, Theme.Dim, A.BR, -24, 12, 300, 24, TextAnchor.LowerRight);
            dlg.gameObject.SetActive(false);
        }
        public static Color VoiceColor(string who)
        {
            switch (who)
            {
                case "PIP": return new Color(0.4f, 1f, 0.85f);
                case "SON": return Theme.Amber;
                case "STRANGER": return Theme.Red;
                case "FATHER": return new Color(0.75f, 0.85f, 1f);
                case "NARRATOR": return Theme.Dim;
                default: return Theme.Cyan;
            }
        }
        static bool Advance() { return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return); }
        public IEnumerator Say(string who, string text, float auto = 0f)
        {
            if (Game.devAuto && auto <= 0f) auto = 1.5f;
            dlg.gameObject.SetActive(true); dialogueBusy = true;
            dlgName.text = who == "NARRATOR" ? "" : who; dlgName.color = VoiceColor(who);
            dlgText.color = who == "NARRATOR" ? Theme.Dim : Theme.Text;
            dlgText.fontStyle = who == "NARRATOR" ? FontStyle.Italic : FontStyle.Normal;
            dlgHint.gameObject.SetActive(auto <= 0f);
            dlgText.text = "";
            yield return null;
            float shown = 0; int n = text.Length; bool skipped = false;
            while (shown < n)
            {
                shown += Time.unscaledDeltaTime * 85f;
                int k = Mathf.Min(n, (int)shown);
                dlgText.text = text.Substring(0, k);
                if (k % 3 == 0 && k < n && text[k] != ' ' && sfx != null && UnityEngine.Random.value < 0.15f) sfx.Beep();
                if (Advance()) { skipped = true; break; }
                yield return null;
            }
            dlgText.text = text;
            if (skipped) yield return null;
            float t = 0;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                if (auto > 0f) { if (t >= auto) break; }
                else if (Advance()) break;
                yield return null;
            }
            dlg.gameObject.SetActive(false); dialogueBusy = false;
        }
        public void HideDialogue() { dlg.gameObject.SetActive(false); }

        // ---------- toast
        public void Toast(string text, Color? c = null, float life = 3f)
        {
            var p = Panel(overlay, A.T, 0, -40, 760, 56, new Color(0.02f, 0.04f, 0.08f, 0.94f));
            Label(p, text, 22, c ?? Theme.Cyan, A.C, 0, 0, 730, 50, TextAnchor.MiddleCenter, FontStyle.Bold);
            UnityEngine.Object.Destroy(p.gameObject, life);
            if (sfx != null) sfx.Beep();
        }

        // ---------- modal debrief card
        public IEnumerator Card(string tag, string title, string body, string realPractice, string source, string[] extra = null, string btn = "CONTINUE")
        {
            var host = Stretch(stage, "Card");
            var dim = Img(host, "Dim", A.C, 0, 0, 10, 10, new Color(0, 0, 0, 0.55f)); dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            var p = Panel(host, A.C, 0, 0, 1040, 720, Theme.Bg2);
            Label(p, tag, 18, Theme.Amber, A.TL, 44, -34, 900, 28, TextAnchor.UpperLeft, FontStyle.Bold);
            Label(p, title, 44, Theme.Cyan, A.TL, 44, -64, 960, 110, TextAnchor.UpperLeft, FontStyle.Bold);
            Label(p, body, 25, Theme.Text, A.TL, 44, -178, 950, 220);
            float y = -178 - 10 - Mathf.Ceil(body.Length / 60f) * 34f;
            y = Mathf.Min(y, -330);
            Label(p, "<color=#4FD1FF>REAL NASA PRACTICE</color>\n" + realPractice, 22, Theme.Text, A.TL, 44, y, 950, 160);
            float yy = y - 20 - Mathf.Ceil(realPractice.Length / 66f) * 30f - 30f;
            if (extra != null) { Label(p, string.Join("\n", extra), 22, Theme.Green, A.TL, 44, yy, 950, 120); }
            Label(p, "SOURCE: " + source, 16, Theme.Dim, A.BL, 44, 78, 950, 40, TextAnchor.LowerLeft);
            var w = new Wait();
            Btn(p, btn, A.BR, -40, 24, 260, 56, () => w.Done(), Theme.Green, 24);
            if (Game.devAuto) { yield return new WaitForSeconds(3f); w.Done(); }
            yield return w;
            UnityEngine.Object.Destroy(host.gameObject);
        }
    }
}
