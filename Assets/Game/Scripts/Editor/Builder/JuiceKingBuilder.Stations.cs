using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>Juicers, the juice counter, floor pads and unlock tiles.</summary>
    public static partial class JuiceKingBuilder
    {
        static readonly Dictionary<string, Mesh> _rounded = new Dictionary<string, Mesh>();
        static Material _mPadDash, _mPadHazard, _mLedOn, _mLedOff, _mChrome, _mMarble, _mPanelDark, _mSlabDark;

        /// <summary>Cached rounded-edge box mesh of a given size (pivot at centre).</summary>
        static Mesh RoundedBox(string key, Vector3 size, float radius, int seg = 5)
        {
            if (_rounded.TryGetValue(key, out var m) && m != null) return m;
            var g = new MeshGen();
            g.RoundedBox(Vector3.zero, size, radius, seg, 0);
            m = SaveMesh(g.ToMesh("RB_" + key), "RB_" + key);
            _rounded[key] = m;
            return m;
        }

        static GameObject RB(string name, Transform parent, Material mat, Vector3 pos, Vector3 size, float radius, bool shadows = true)
        {
            string key = $"{size.x:0.00}_{size.y:0.00}_{size.z:0.00}_{radius:0.00}";
            return B.MeshObj(name, parent, RoundedBox(key, size, radius), mat, pos, Vector3.one, null, shadows);
        }

        static void BuildStationMaterials()
        {
            _mPadDash = MatLib.Sprite("Pad_Dash", MatLib.Tex("pad_dash.png"), Color.white);
            _mPadHazard = MatLib.Sprite("Pad_Hazard", MatLib.Tex("pad_hazard.png"), Color.white);
            _mLedOn = Emissive("LedOn", new Color(0.35f, 1f, 0.45f), 2.2f);
            _mLedOff = MatLib.Lit("LedOff", new Color(0.25f, 0.35f, 0.28f), 0.6f);
            _mChrome = MatLib.Lit("Chrome", new Color(0.86f, 0.88f, 0.92f), 0.85f, null, null, 0.8f);
            _mMarble = MatLib.Lit("Marble", new Color(0.97f, 0.96f, 0.93f), 0.55f);
            _mPanelDark = MatLib.Lit("PanelDark", new Color(0.17f, 0.19f, 0.24f), 0.45f);
            _mSlabDark = MatLib.Lit("SlabDark", new Color(0.2f, 0.17f, 0.14f), 0.2f);
        }

        /// <summary>Scale that makes a sprite <paramref name="size"/> world units wide.</summary>
        static float SpriteScale(Sprite s, float size) => s == null ? 1f : size / (s.rect.width / s.pixelsPerUnit);

        // ================================================================== floor pads

        static T MakeZone<T>(string name, Transform parent, Vector3 worldPos, Vector2 size, Material mat, Sprite icon) where T : Zone
        {
            var go = B.Node(name, parent, Vector3.zero);
            go.transform.position = worldPos;
            var pad = B.Node("Pad", go.transform, Vector3.zero, null, new Vector3(size.x, 1f, size.y));
            // Raised slab (darker shade of the pad colour) gives the tile an edge and a shadow line on the plaza.
            RB("Slab", pad.transform, SlabMat(mat), new Vector3(0f, 0.03f, 0f), new Vector3(1.03f, 0.06f, 1.03f), 0.16f, false);
            var frame = B.Decal("Frame", pad.transform, mat, new Vector3(0f, 0.066f, 0f), Vector2.one);
            frame.GetComponent<MeshRenderer>().sortingOrder = 0;
            if (icon != null)
            {
                float s = SpriteScale(icon, Mathf.Min(size.x, size.y) * 0.5f);
                B.Sprite("Icon", go.transform, icon, new Vector3(0f, 0.075f, 0f), s, true, 1, Color.white);
            }
            var z = go.AddComponent<T>();
            z.size = size;
            z.padVisual = pad.transform;
            return z;
        }

        /// <summary>Tinted copy of the pad tile material.</summary>
        static Material PadMat(string name, Color c)
        {
            var m = MatLib.Sprite("Pad_" + name, MatLib.Tex("pad_tile.png"), c);
            return m;
        }

        static Material SlabMat(Material pad)
        {
            var c = pad != null ? pad.color : Color.grey;
            return MatLib.Lit("Slab_" + (pad != null ? pad.name : "Grey"), Color.Lerp(c, new Color(0.12f, 0.1f, 0.1f), 0.55f), 0.25f);
        }

        // ================================================================== juicer

        static Juicer BuildJuicer(int f, Vector3 pos)
        {
            var root = B.Node("Juicer_" + FruitKey[f], _stations, pos);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero);
            var bt = body.transform;
            var shell = _mFruitBody[f];

            // Plinth + chrome trim.
            RB("Plinth", bt, _mPanelDark, new Vector3(0f, 0.11f, 0f), new Vector3(1.95f, 0.22f, 1.75f), 0.22f);
            RB("Trim", bt, _mChrome, new Vector3(0f, 0.245f, 0f), new Vector3(1.99f, 0.05f, 1.79f), 0.24f, false);
            // Rounded fruit-coloured cabinet with a white band.
            RB("Cabinet", bt, shell, new Vector3(0f, 0.8f, 0f), new Vector3(1.62f, 1.08f, 1.38f), 0.3f);
            RB("Band", bt, _mWhite, new Vector3(0f, 1.2f, 0f), new Vector3(1.67f, 0.14f, 1.43f), 0.32f, false);
            RB("Lip", bt, _mChrome, new Vector3(0f, 1.36f, 0f), new Vector3(1.3f, 0.06f, 1.1f), 0.3f, false);

            // Front control panel (faces the camera / tray side).
            var panel = B.Node("Panel", bt, new Vector3(0f, 0.6f, -0.7f)).transform;
            RB("Face", panel, _mPanelDark, Vector3.zero, new Vector3(1.02f, 0.5f, 0.07f), 0.03f, false);
            var ic = B.Sprite("Icon", panel, _sFruit[f], new Vector3(-0.18f, 0.03f, -0.045f), SpriteScale(_sFruit[f], 0.42f), false, 2);
            ic.transform.localRotation = Quaternion.identity;
            Material[] btn = { _mRed, MatLib.Lit("BtnGreen", new Color(0.35f, 0.85f, 0.4f), 0.5f), MatLib.Lit("BtnYellow", new Color(1f, 0.85f, 0.2f), 0.5f) };
            for (int i = 0; i < 3; i++)
                B.MeshObj("Button" + i, panel, B.Sphere, btn[i], new Vector3(0.18f + i * 0.13f, -0.14f, -0.04f), new Vector3(0.09f, 0.09f, 0.05f), null, false);
            var led = B.MeshObj("Led", panel, B.Sphere, _mLedOff, new Vector3(0.31f, 0.13f, -0.045f), new Vector3(0.13f, 0.13f, 0.06f), null, false);
            // Side vents.
            for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < 3; i++)
                B.Box("Vent", bt, _mPanelDark, new Vector3(s * 0.815f, 0.55f + i * 0.14f, 0f), new Vector3(0.02f, 0.05f, 0.7f), null, false);

            // Glass jug: collar, juice, spinning blades with fruit chunks, lid and hopper funnel.
            B.MeshObj("Collar", bt, _disc, new[] { _mChrome, _mChrome }, new Vector3(0f, 1.38f, 0f), new Vector3(1.02f, 0.12f, 1.02f));
            var liquid = B.MeshObj("Liquid", bt, _disc, new[] { _mJuice[f], _mJuice[f] }, new Vector3(0f, 1.5f, 0f), new Vector3(0.76f, 0.9f, 0.76f), null, false);
            var blades = B.Node("Blades", bt, new Vector3(0f, 1.56f, 0f));
            B.Box("B1", blades.transform, _mSteel, Vector3.zero, new Vector3(0.62f, 0.03f, 0.09f));
            B.Box("B2", blades.transform, _mSteel, Vector3.zero, new Vector3(0.09f, 0.03f, 0.62f));
            var chunk = MatLib.Lit("Chunk_" + FruitKey[f], Color.Lerp(Balance.FruitColors[f], Color.white, 0.25f), 0.5f);
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                B.MeshObj("Chunk", blades.transform, B.Sphere, chunk, new Vector3(Mathf.Cos(a) * 0.24f, 0.12f + (i % 2) * 0.18f, Mathf.Sin(a) * 0.24f), Vector3.one * 0.13f, null, false);
            }
            B.MeshObj("Jar", bt, _cup, _mGlass, new Vector3(0f, 1.44f, 0f), new Vector3(0.98f, 1.02f, 0.98f), null, false);
            B.MeshObj("Rim", bt, _disc, new[] { _mChrome, _mChrome }, new Vector3(0f, 2.44f, 0f), new Vector3(1.04f, 0.07f, 1.04f), null, false);
            B.MeshObj("Funnel", bt, _funnel, shell, new Vector3(0f, 2.5f, 0f), new Vector3(0.62f, 0.46f, 0.62f));
            var intake = B.Node("Intake", bt, new Vector3(0f, 2.95f, 0f));

            // Round fruit sign on a post so every machine reads at a glance.
            var sign = B.Node("Sign", t, new Vector3(0.72f, 0f, 0.5f)).transform;
            B.Cyl("Post", sign, _mChrome, new Vector3(0f, 1.9f, 0f), 0.07f, 1.1f);
            var disc = B.MeshObj("Disc", sign, _disc, new[] { _mWhite, shell }, new Vector3(0f, 2.72f, 0f), new Vector3(0.78f, 0.07f, 0.78f), new Vector3(-90f, 0f, 0f));
            var sIcon = B.Sprite("Icon", sign, _sFruit[f], new Vector3(0f, 2.72f, -0.085f), SpriteScale(_sFruit[f], 0.56f), false, 2);
            sIcon.transform.localRotation = Quaternion.identity;

            // Chrome spout with a downturned tip over the tray.
            B.MeshObj("Spout", bt, B.Cylinder, _mChrome, new Vector3(0f, 1.02f, -0.83f), new Vector3(0.13f, 0.14f, 0.13f), new Vector3(90f, 0f, 0f));
            B.MeshObj("SpoutTip", bt, B.Cylinder, _mChrome, new Vector3(0f, 0.95f, -0.96f), new Vector3(0.12f, 0.07f, 0.12f));
            var spout = B.Node("SpoutPoint", bt, new Vector3(0f, 0.85f, -0.96f));

            // Stainless output tray.
            var tray = B.Node("Tray", t, new Vector3(0f, 0f, -1.62f));
            var trt = tray.transform;
            RB("Top", trt, _mSteel, new Vector3(0f, 0.5f, 0f), new Vector3(1.55f, 0.07f, 1f), 0.1f);
            B.Box("RailF", trt, _mChrome, new Vector3(0f, 0.56f, -0.48f), new Vector3(1.5f, 0.05f, 0.04f), null, false);
            B.Box("RailB", trt, _mChrome, new Vector3(0f, 0.56f, 0.48f), new Vector3(1.5f, 0.05f, 0.04f), null, false);
            foreach (var lx in new[] { -0.66f, 0.66f })
            foreach (var lz in new[] { -0.4f, 0.4f })
                B.Cyl("Leg", trt, _mPanelDark, new Vector3(lx, 0.235f, lz), 0.07f, 0.47f);
            var outPile = B.Node("OutputPile", trt, new Vector3(0f, 0.54f, 0f)).AddComponent<ItemPile>();
            outPile.columns = 4;
            outPile.rows = 2;
            outPile.layers = 3;
            outPile.spacing = new Vector2(0.34f, 0.4f);
            outPile.layerHeight = 0.37f;

            // Slatted wooden crate for incoming slices.
            var crate = B.Node("Crate", t, new Vector3(0f, 0f, 1.45f));
            var ct = crate.transform;
            B.Box("Floor", ct, _mWood, new Vector3(0f, 0.08f, 0f), new Vector3(1.12f, 0.16f, 1.02f));
            var slat = MatLib.Lit("CrateSlat", new Color(0.86f, 0.62f, 0.38f), 0.1f);
            var post = MatLib.Lit("CratePost", new Color(0.55f, 0.36f, 0.22f), 0.1f);
            for (int i = 0; i < 2; i++)
            {
                float y = 0.24f + i * 0.17f;
                B.Box("SlatL", ct, slat, new Vector3(-0.54f, y, 0f), new Vector3(0.05f, 0.11f, 1.02f));
                B.Box("SlatR", ct, slat, new Vector3(0.54f, y, 0f), new Vector3(0.05f, 0.11f, 1.02f));
                B.Box("SlatB", ct, slat, new Vector3(0f, y, 0.49f), new Vector3(1.12f, 0.11f, 0.05f));
                B.Box("SlatF", ct, slat, new Vector3(0f, y, -0.49f), new Vector3(1.12f, 0.11f, 0.05f));
            }
            foreach (var cx in new[] { -0.54f, 0.54f })
            foreach (var cz in new[] { -0.49f, 0.49f })
                B.Box("Post", ct, post, new Vector3(cx, 0.26f, cz), new Vector3(0.09f, 0.52f, 0.09f));
            var inPile = B.Node("InputPile", t, new Vector3(0f, 0.36f, 1.45f)).AddComponent<ItemPile>();
            inPile.columns = 2;
            inPile.rows = 2;
            inPile.layers = 6;
            inPile.spacing = new Vector2(0.42f, 0.42f);
            inPile.layerHeight = 0.13f;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.2f, -0.1f);
            col.size = new Vector3(1.9f, 2.4f, 4.1f);

            var hum = root.AddComponent<AudioSource>();
            hum.playOnAwake = true;
            hum.loop = true;

            var j = root.AddComponent<Juicer>();
            j.kind = (FruitKind)f;
            j.inputPile = inPile;
            j.outputPile = outPile;
            j.intakePoint = intake.transform;
            j.spoutPoint = spout.transform;
            j.body = bt;
            j.blades = blades.transform;
            j.liquid = liquid.transform;
            j.hum = hum;
            j.hopper = ct;
            j.statusLight = led.GetComponent<Renderer>();
            j.ledOn = _mLedOn;
            j.ledOff = _mLedOff;
            j.sign = sign;

            var dz = MakeZone<DropZone>("InputZone", t, pos + new Vector3(0f, 0f, 3.0f), new Vector2(2.3f, 1.5f), _mPadIn, _sFruit[f]);
            dz.receiverBehaviour = j;
            var pz = MakeZone<PickupZone>("OutputZone", t, pos + new Vector3(0f, 0f, -3.0f), new Vector2(2.3f, 1.4f), _mPadOut, _sJuice);
            pz.sourceBehaviour = j;
            j.inputZone = dz;
            j.outputZone = pz;
            return j;
        }

        // ================================================================== counter

        static Counter BuildCounter(Vector3 pos)
        {
            var root = B.Node("JuiceStand", _stations, pos);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero).transform;

            RB("Base", body, _mWood, new Vector3(0f, 0.5f, 0f), new Vector3(4.5f, 0.95f, 1.15f), 0.16f);
            RB("Kick", body, _mPanelDark, new Vector3(0f, 0.06f, 0f), new Vector3(4.58f, 0.12f, 1.22f), 0.2f);
            RB("Top", body, _mMarble, new Vector3(0f, 1.03f, 0f), new Vector3(4.82f, 0.1f, 1.42f), 0.22f);
            RB("TopEdge", body, _mWood, new Vector3(0f, 0.965f, 0f), new Vector3(4.74f, 0.04f, 1.36f), 0.2f, false);

            // Striped skirt facing the customers, with the brand.
            B.Box("Skirt", body, _mAwning, new Vector3(0f, 0.6f, -0.605f), new Vector3(4.2f, 0.6f, 0.04f));
            var brand = B.Text("Brand", body, "JUICE KING", 4.4f, new Color(1f, 0.85f, 0.25f), new Vector3(0f, 0.6f, -0.635f));
            brand.rectTransform.sizeDelta = new Vector2(3.6f, 0.8f);
            B.Sprite("CrownL", body, _sCrown, new Vector3(-1.75f, 0.62f, -0.635f), SpriteScale(_sCrown, 0.34f), false, 6);
            B.Sprite("CrownR", body, _sCrown, new Vector3(1.75f, 0.62f, -0.635f), SpriteScale(_sCrown, 0.34f), false, 6);

            // End posts with a fruit bowl and juice jars.
            foreach (var sx in new[] { -1f, 1f })
                RB("Post", body, _mRed, new Vector3(sx * 2.33f, 0.62f, 0f), new Vector3(0.24f, 1.24f, 1.3f), 0.1f);
            for (int i = 0; i < 2; i++)
            {
                int f = i == 0 ? 0 : 1;
                var jar = B.Node("Jar" + i, body, new Vector3(-2.0f + i * 0.32f, 1.08f, 0.22f - i * 0.1f)).transform;
                B.MeshObj("Juice", jar, _disc, new[] { _mJuice[f], _mJuice[f] }, new Vector3(0f, 0.02f, 0f), new Vector3(0.24f, 0.3f, 0.24f), null, false);
                B.MeshObj("Glass", jar, _cup, _mGlass, Vector3.zero, new Vector3(0.3f, 0.42f, 0.3f), null, false);
                B.MeshObj("Lid", jar, _disc, new[] { _mChrome, _mChrome }, new Vector3(0f, 0.42f, 0f), new Vector3(0.32f, 0.04f, 0.32f), null, false);
            }
            B.Prop("Market/cash-register", body, new Vector3(1.75f, 1.08f, 0.1f), 0.9f, 180f);

            // Sandwich-board sign beside the stand, facing the street.
            var sign = B.Node("Sign", t, new Vector3(-3.45f, 0f, -0.9f), new Vector3(0f, 15f, 0f));
            B.Box("Board", sign.transform, _mWhite, new Vector3(0f, 1.05f, 0f), new Vector3(1.6f, 1.1f, 0.1f), new Vector3(-10f, 0f, 0f));
            B.Box("Frame", sign.transform, _mRed, new Vector3(0f, 1.05f, 0.03f), new Vector3(1.72f, 1.22f, 0.08f), new Vector3(-10f, 0f, 0f));
            B.Box("LegL", sign.transform, _mDark, new Vector3(-0.7f, 0.5f, 0.12f), new Vector3(0.08f, 1f, 0.08f));
            B.Box("LegR", sign.transform, _mDark, new Vector3(0.7f, 0.5f, 0.12f), new Vector3(0.08f, 1f, 0.08f));
            var txt = B.Text("Title", sign.transform, "FRESH\nJUICE", 3.6f, new Color(1f, 0.55f, 0.1f), new Vector3(0f, 1.08f, -0.08f));
            txt.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            txt.textWrappingMode = TMPro.TextWrappingModes.Normal;
            txt.lineSpacing = -20f;
            txt.rectTransform.sizeDelta = new Vector2(1.5f, 1f);

            var display = B.Node("Display", t, new Vector3(-0.3f, 1.08f, 0f)).AddComponent<ItemPile>();
            display.columns = 6;
            display.rows = 2;
            display.layers = Balance.CounterLayers(0);
            display.spacing = new Vector2(0.36f, 0.42f);
            display.layerHeight = 0.37f;

            // Stock readout over the jars.
            var label = B.Node("Capacity", t, new Vector3(-1.85f, 2.05f, 0.1f));
            label.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", label.transform, _sRound, new Vector3(0f, 0f, 0.02f), 1f, false, 4, new Color(0.1f, 0.08f, 0.12f, 0.65f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(1.2f, 0.52f);
            var cap = B.Text("Text", label.transform, "0/24", 4f, Color.white, new Vector3(0f, 0.01f, 0f));

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.size = new Vector3(4.8f, 1.2f, 1.4f);

            var c = root.AddComponent<Counter>();
            c.display = display;
            c.body = body;
            c.capacityText = cap;
            c.servePoint = B.Node("ServePoint", t, new Vector3(0f, 0f, -1.35f)).transform;
            var dz = MakeZone<DropZone>("StockZone", t, pos + new Vector3(0f, 0f, 1.7f), new Vector2(3.4f, 1.5f), _mPadCounter, _sJuice);
            dz.receiverBehaviour = c;
            c.dropZone = dz;
            return c;
        }

        // ================================================================== unlock tiles

        static UnlockZone Unlock(string id, string title, int price, Vector3 pos, Sprite icon, GameObject[] reveal, params UnlockZone[] next)
        {
            var go = B.Node("Unlock_" + id, _unlocks, pos);
            var size = new Vector2(2.7f, 2.7f);
            var pad = B.Node("Pad", go.transform, Vector3.zero, null, new Vector3(size.x, 1f, size.y));
            RB("Slab", pad.transform, _mSlabDark, new Vector3(0f, 0.035f, 0f), new Vector3(1.02f, 0.07f, 1.02f), 0.14f, false);
            B.Decal("Frame", pad.transform, _mPadHazard, new Vector3(0f, 0.075f, 0f), Vector2.one).GetComponent<MeshRenderer>().sortingOrder = 0;
            // Inner panel of the hazard texture is ~72% of the tile; the green fill grows inside it.
            const float inner = 0.72f;
            var fill = B.MeshObj("Fill", pad.transform, _quadXZ, _mPadFill, new Vector3(0f, 0.079f, -inner * 0.5f), new Vector3(inner, 1f, 0.0001f), null, false);
            fill.GetComponent<MeshRenderer>().sortingOrder = 1;

            // Big icon and price painted on the tile itself.
            var gIcon = B.Sprite("GroundIcon", go.transform, icon, new Vector3(0f, 0.085f, 0.3f), SpriteScale(icon, 1.05f), true, 2);
            var coin = B.Sprite("Coin", go.transform, _sCoin, new Vector3(-0.62f, 0.085f, -0.52f), SpriteScale(_sCoin, 0.46f), true, 3);
            var gPrice = B.Text("GroundPrice", go.transform, price.ToString(), 8.5f, Color.white, new Vector3(0.2f, 0.09f, -0.52f));
            gPrice.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gPrice.rectTransform.sizeDelta = new Vector2(1.7f, 0.6f);
            gPrice.alignment = TMPro.TextAlignmentOptions.Center;
            gPrice.GetComponent<MeshRenderer>().sortingOrder = 3;

            // Floating sign with the name (and price, still readable while standing on the tile).
            var label = B.Node("Label", go.transform, new Vector3(0f, 1.35f, 0f));
            label.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", label.transform, _sRound, new Vector3(0f, 0f, 0.02f), 1f, false, 4, new Color(0.1f, 0.08f, 0.12f, 0.72f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(2.6f, 0.72f);
            var ic = B.Sprite("Icon", label.transform, icon, new Vector3(-1.02f, 0.02f, 0f), SpriteScale(icon, 0.46f), false, 5);
            var titleT = B.Text("Title", label.transform, title, 4.4f, Color.white, new Vector3(0.16f, 0.02f, 0f));
            titleT.rectTransform.sizeDelta = new Vector2(2.1f, 0.6f);
            titleT.enableAutoSizing = true;
            titleT.fontSizeMin = 2.4f;
            titleT.fontSizeMax = 4.4f;
            var priceT = B.Text("Price", label.transform, "$" + price, 6.5f, new Color(1f, 0.9f, 0.35f), new Vector3(0.16f, 0.02f, 0f));
            priceT.gameObject.SetActive(false);

            var z = go.AddComponent<UnlockZone>();
            z.id = id;
            z.title = title;
            z.price = price;
            z.size = size;
            z.padVisual = pad.transform;
            z.reveal = reveal;
            z.next = next;
            z.priceText = priceT;
            z.groundPriceText = gPrice;
            z.titleText = titleT;
            z.fill = fill.transform;
            z.fillDepth = inner;
            z.icon = ic;
            z.label = label.transform;
            _zones.Add(z);
            return z;
        }
    }
}
