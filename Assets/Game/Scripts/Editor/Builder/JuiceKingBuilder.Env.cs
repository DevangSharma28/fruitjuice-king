using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>Charm pass: trash bin, pond, windmill, chicken coop, critters, plaza dressing, wind and clouds.</summary>
    public static partial class JuiceKingBuilder
    {
        const string AnimalRoot = "Assets/ThirdParty/ithappy/";

        static Ambient _ambient;
        static Transform _nature;
        static Mesh _tower, _cone, _lidMesh;

        // Keep-out areas for random decor (x, z, radius).
        static readonly Vector3[] Reserved =
        {
            new Vector3(-8.0f, 16.3f, 3.3f),   // pond
            new Vector3(10.2f, 16.6f, 3.4f),   // coop
            new Vector3(-16.5f, 9f, 4.5f),     // windmill
            new Vector3(-9.2f, -1.2f, 2.2f),   // trash
            new Vector3(8.8f, -10.4f, 3f),     // doghouse & flower bed
        };

        static bool InReserved(Vector3 p)
        {
            foreach (var r in Reserved)
                if ((new Vector2(p.x, p.z) - new Vector2(r.x, r.y)).sqrMagnitude < r.z * r.z) return true;
            return false;
        }

        static void Sway(Transform t, float amount, float speed)
        {
            if (_ambient == null || t == null) return;
            t.gameObject.isStatic = false;
            _ambient.swayers.Add(new Ambient.Swayer { t = t, amount = amount, speed = speed, phase = (float)_rnd.NextDouble() * 10f });
        }

        static Material Emissive(string name, Color c, float intensity)
        {
            var m = MatLib.Lit(name, c, 0.4f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void BuildEnvMeshes()
        {
            var g = new MeshGen();
            g.Side(0.5f, 0.36f, 0f, 1f, 20, 0);
            g.Cap(0.36f, 1f, 20, 0, true);
            _tower = SaveMesh(g.ToMesh("Tower"), "Tower");

            g = new MeshGen();
            g.Cone(0.5f, 0f, 1f, 20, 0, true);
            _cone = SaveMesh(g.ToMesh("Cone"), "Cone");

            // Bin lid: a shallow dome, hinged at its back edge (pivot at z = 0, lid extends to +z).
            g = new MeshGen();
            g.Side(0.5f, 0.42f, 0f, 0.08f, 24, 0);
            g.Cone(0.42f, 0.08f, 0.2f, 24, 0, false);
            g.Cap(0.5f, 0f, 24, 0, false);
            var lm = g.ToMesh("BinLid");
            var v = lm.vertices;
            for (int i = 0; i < v.Length; i++) v[i].z += 0.5f;
            lm.vertices = v;
            lm.RecalculateBounds();
            _lidMesh = SaveMesh(lm, "BinLid");
        }

        // ================================================================== trash bin

        static TrashBin BuildTrashBin(Vector3 binPos, Vector3 zonePos)
        {
            var root = B.Node("TrashBin", _stations, binPos);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero);
            var bt = body.transform;
            // Tapered green bin with a darker rim, vertical ridges and a trash icon on the front.
            B.MeshObj("Can", bt, _cup, _mBinGreen, Vector3.zero, new Vector3(1.05f, 1.15f, 1.05f));
            B.MeshObj("Rim", bt, _disc, new[] { _mBinDark, _mBinDark }, new Vector3(0f, 1.1f, 0f), new Vector3(1.12f, 0.1f, 1.12f));
            B.MeshObj("Foot", bt, _disc, new[] { _mBinDark, _mBinDark }, Vector3.zero, new Vector3(0.9f, 0.08f, 0.9f));
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * 360f;
                var rib = B.Box("Rib", bt, _mBinDark, Vector3.zero, new Vector3(0.06f, 0.8f, 0.04f));
                rib.transform.localRotation = Quaternion.Euler(0f, a, 0f);
                rib.transform.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.55f, 0.49f);
            }
            var binIcon = _uRecycle ? _uRecycle : _sTrash;
            var icon = B.Sprite("Icon", bt, binIcon, new Vector3(0.54f, 0.62f, 0f), SpriteScale(binIcon, 0.42f), false, 2);
            icon.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            var icon2 = B.Sprite("Icon2", bt, binIcon, new Vector3(0f, 0.62f, -0.54f), SpriteScale(binIcon, 0.42f), false, 2);
            icon2.transform.localRotation = Quaternion.identity;

            // Hinge on the far (west) side, lid swings up toward the pad (+x).
            var hinge = B.Node("Hinge", bt, new Vector3(-0.56f, 1.15f, 0f), new Vector3(0f, 90f, 0f));
            var lid = B.Node("Lid", hinge.transform, Vector3.zero);
            B.MeshObj("LidMesh", lid.transform, _lidMesh, _mBinGreen, Vector3.zero, new Vector3(1.12f, 1f, 1.12f));
            B.Box("Handle", lid.transform, _mBinDark, new Vector3(0f, 0.24f, 0.56f), new Vector3(0.36f, 0.06f, 0.1f));

            var mouth = B.Node("Mouth", t, new Vector3(0f, 1.2f, 0f));

            // Little signpost so it reads as "the bin".
            var sign = B.Node("Sign", t, new Vector3(0.1f, 0f, 0.85f));
            B.Cyl("Post", sign.transform, _mDark, new Vector3(0f, 0.8f, 0f), 0.07f, 1.6f);
            var board = B.Node("Board", sign.transform, new Vector3(0f, 1.55f, 0f));
            board.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", board.transform, _sRound, Vector3.zero, 1f, false, 3, new Color(0.22f, 0.52f, 0.33f, 0.95f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(1.5f, 0.5f);
            B.Text("Text", board.transform, "TRASH", 3.6f, Color.white, new Vector3(0f, 0.02f, -0.01f));

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.radius = 0.55f;
            col.height = 1.3f;

            var bin = root.AddComponent<TrashBin>();
            bin.body = bt;
            bin.lid = lid.transform;
            bin.mouth = mouth.transform;
            bin.openAngle = -115f;

            var padMat = PadMat("Trash", new Color(0.55f, 0.9f, 0.6f, 1f));
            var zone = MakeZone<TrashZone>("TrashZone", t, zonePos, new Vector2(1.9f, 1.9f), padMat, _uRecycle ? _uRecycle : _sTrash);
            zone.bin = bin;
            bin.zone = zone;
            return bin;
        }

        // ================================================================== plaza dressing

        static void BuildPlazaDressing()
        {
            var root = B.Node("PlazaDressing", _decor, Vector3.zero).transform;

            // Low stone curb framing the plaza (gaps where the fields and path open up).
            void Curb(Vector3 a, Vector3 b)
            {
                Vector3 d = b - a;
                var c = B.Box("Curb", root, _mStone, (a + b) * 0.5f + Vector3.up * 0.05f, new Vector3(0.28f, 0.1f, d.magnitude), new Vector3(0f, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0f), false);
                c.GetComponent<MeshRenderer>().receiveShadows = true;
            }
            Curb(new Vector3(-12f, 0, -12.7f), new Vector3(-12f, 0, 3.5f));
            Curb(new Vector3(12f, 0, -12.7f), new Vector3(12f, 0, 3.5f));

            // Hedge planters along the street edge, dotted with flowers.
            Color[] petals = { new Color(1f, 0.45f, 0.55f), new Color(1f, 0.9f, 0.3f), Color.white, new Color(0.75f, 0.55f, 1f) };
            var petalMats = new Material[petals.Length];
            for (int i = 0; i < petals.Length; i++) petalMats[i] = MatLib.Lit("Petal" + i, petals[i], 0.3f);
            void Hedge(float x0, float x1, float z)
            {
                var box = B.Box("Planter", root, _mWood, new Vector3((x0 + x1) * 0.5f, 0.18f, z), new Vector3(x1 - x0, 0.36f, 0.62f));
                var hedge = B.Box("Hedge", root, _mHedge, new Vector3((x0 + x1) * 0.5f, 0.52f, z), new Vector3(x1 - x0 - 0.12f, 0.36f, 0.5f));
                for (float x = x0 + 0.3f; x < x1 - 0.2f; x += 0.45f)
                {
                    var m = petalMats[_rnd.Next(petalMats.Length)];
                    B.MeshObj("Flower", root, B.Sphere, m, new Vector3(x + (float)_rnd.NextDouble() * 0.2f, 0.72f, z + ((float)_rnd.NextDouble() - 0.5f) * 0.3f), Vector3.one * 0.16f, null, false);
                }
                var c = box.AddComponent<BoxCollider>();
                c.center = new Vector3(0f, 0.5f, 0f);
                c.size = new Vector3(1f, 2f, 1f);
            }
            Hedge(-12.2f, -3.6f, -13.1f);
            Hedge(3.8f, 12.2f, -13.1f);

            // Warm lamp posts at the plaza corners.
            var glow = Emissive("LampGlow", new Color(1f, 0.9f, 0.62f), 1.6f);
            void Lamp(Vector3 p)
            {
                var l = B.Node("Lamp", root, p).transform;
                B.Cyl("Pole", l, _mDark, new Vector3(0f, 1.3f, 0f), 0.1f, 2.6f);
                B.MeshObj("Base", l, _disc, new[] { _mDark, _mDark }, Vector3.zero, new Vector3(0.32f, 0.2f, 0.32f));
                B.MeshObj("Cap", l, _cone, _mDark, new Vector3(0f, 2.9f, 0f), new Vector3(0.5f, 0.25f, 0.5f));
                B.MeshObj("Bulb", l, B.Sphere, glow, new Vector3(0f, 2.72f, 0f), Vector3.one * 0.38f, null, false);
                var c = l.gameObject.AddComponent<CapsuleCollider>();
                c.center = new Vector3(0f, 1f, 0f);
                c.radius = 0.15f;
                c.height = 2f;
            }
            Lamp(new Vector3(-11.6f, 0f, 3.1f));
            Lamp(new Vector3(11.6f, 0f, 3.1f));
            Lamp(new Vector3(-11.6f, 0f, -12.2f));
            Lamp(new Vector3(11.6f, 0f, -12.2f));

            // Benches and pots along the sides.
            B.Prop("Furniture/bench", root, new Vector3(-11.4f, 0f, -3.4f), 0.26f, 90f);
            B.Prop("Furniture/bench", root, new Vector3(11.4f, 0f, -8.6f), 0.26f, -90f);
            B.Prop("Furniture/pottedPlant", root, new Vector3(11.5f, 0f, -1.4f), 0.3f);
            B.Prop("Furniture/pottedPlant", root, new Vector3(-11.5f, 0f, -6.2f), 0.3f);

            // Fruit stands and crates near the juicers.
            B.Prop("Market/display-fruit", root, new Vector3(8.2f, 0f, 0.4f), 2.4f, -90f);
            B.Prop("Survival/barrel", root, new Vector3(7.6f, 0f, -1.7f), 2.4f, 0f);
            var crate = B.Prop("Survival/box-open", root, new Vector3(-6.4f, 0f, 1.6f), 3f, 15f);
            for (int i = 0; i < 3; i++)
                B.Prop("Food/orange", crate.transform, new Vector3(-0.15f + i * 0.15f, 0.72f, (i % 2) * 0.1f), 2.4f, i * 50f);

            // Bunting over the path to the pineapple field.
            Bunting(root, new Vector3(-2.7f, 0f, 3.95f), new Vector3(2.7f, 0f, 3.95f), 3.3f, 13);

            // "JUICE KING" brand on the stand's skirt.
            var stand = _stations.Find("JuiceStand");
            if (stand != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var ic = stand.Find("Icon" + i);
                    if (ic != null) Object.DestroyImmediate(ic.gameObject);
                }
                var brand = B.Text("Brand", stand, "JUICE KING", 4.4f, new Color(1f, 0.85f, 0.25f), new Vector3(0f, 0.6f, -0.6f));
                brand.rectTransform.sizeDelta = new Vector2(3.6f, 0.8f);
                B.Sprite("CrownL", stand, _sCrown, new Vector3(-1.75f, 0.62f, -0.6f), 0.13f, false, 6);
                B.Sprite("CrownR", stand, _sCrown, new Vector3(1.75f, 0.62f, -0.6f), 0.13f, false, 6);
            }
        }

        static void Bunting(Transform parent, Vector3 a, Vector3 b, float height, int flags)
        {
            var root = B.Node("Bunting", parent, Vector3.zero).transform;
            foreach (var p in new[] { a, b })
            {
                B.Cyl("Pole", root, _mWood, p + Vector3.up * height * 0.5f, 0.12f, height);
                B.MeshObj("Knob", root, B.Sphere, _mRed, p + Vector3.up * height, Vector3.one * 0.2f);
                var c = B.Node("PoleCol", root, p + Vector3.up * 1f).AddComponent<CapsuleCollider>();
                c.radius = 0.12f;
                c.height = 2f;
            }
            Material[] mats = { _mRed, MatLib.Lit("FlagYellow", new Color(1f, 0.85f, 0.25f), 0.2f), _mBlue, _mBinGreen, _mPink };
            var flagMesh = FlagMesh();
            for (int i = 0; i < flags; i++)
            {
                float k = (i + 0.5f) / flags;
                Vector3 p = Vector3.Lerp(a, b, k) + Vector3.up * (height - 0.1f - Mathf.Sin(k * Mathf.PI) * 0.7f);
                var f = B.MeshObj("Flag", root, flagMesh, mats[i % mats.Length], p, Vector3.one * 0.42f,
                    new Vector3(0f, Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg - 90f, 0f), false);
                Sway(f.transform, 9f, 2.4f);
            }
        }

        static Mesh _flag;

        static Mesh FlagMesh()
        {
            if (_flag != null) return _flag;
            var m = new Mesh { name = "Flag" };
            // Double-sided triangle hanging down from its top edge.
            m.vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, -0.9f, 0f), new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0f, -0.9f, 0f) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.forward, Vector3.forward, Vector3.forward };
            m.uv = new[] { Vector2.zero, Vector2.right, new Vector2(0.5f, 1f), Vector2.zero, Vector2.right, new Vector2(0.5f, 1f) };
            m.triangles = new[] { 0, 1, 2, 3, 5, 4 };
            m.RecalculateBounds();
            _flag = SaveMesh(m, "Flag");
            return _flag;
        }

        // ================================================================== pond

        static void BuildPond(Vector3 c, float r)
        {
            var root = B.Node("Pond", _decor, c).transform;
            B.MeshObj("Sand", root, _disc, new[] { _mPath, _mPath }, new Vector3(0f, 0.005f, 0f), new Vector3(r * 2.35f, 0.02f, r * 2.2f), null, false);
            var water = B.MeshObj("Water", root, _disc, new[] { _mWater, _mWater }, new Vector3(0f, 0.012f, 0f), new Vector3(r * 2f, 0.03f, r * 1.85f), null, false);
            water.GetComponent<MeshRenderer>().receiveShadows = true;
            var bed = MatLib.Lit("PondBed", new Color(0.2f, 0.45f, 0.62f), 0.3f);
            B.MeshObj("Deep", root, _disc, new[] { bed, bed }, new Vector3(0f, 0.008f, 0f), new Vector3(r * 1.9f, 0.01f, r * 1.75f), null, false);

            string[] rocks = { "Nature/rock_smallA", "Nature/rock_smallB", "Survival/rock-a", "Survival/rock-c" };
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f + (float)_rnd.NextDouble() * 0.2f;
                var p = new Vector3(Mathf.Cos(a) * r * 1.05f, 0f, Mathf.Sin(a) * r * 0.98f);
                B.Prop(rocks[_rnd.Next(rocks.Length)], root, p, 1.2f + (float)_rnd.NextDouble() * 0.8f, (float)_rnd.NextDouble() * 360f);
            }

            var flower = MatLib.Lit("LilyFlower", new Color(1f, 0.7f, 0.85f), 0.3f);
            Vector3[] pads = { new Vector3(-0.9f, 0f, 0.6f), new Vector3(1.1f, 0f, -0.4f), new Vector3(0.3f, 0f, 1.2f), new Vector3(-0.4f, 0f, -1.1f) };
            foreach (var p in pads)
            {
                var pad = B.MeshObj("Lily", root, _disc, new[] { _mLily, _mLily }, p + Vector3.up * 0.045f, new Vector3(0.62f, 0.02f, 0.62f), new Vector3(0f, (float)_rnd.NextDouble() * 360f, 0f), false);
                Sway(pad.transform, 3f, 0.8f);
            }
            B.MeshObj("LilyBloom", root, B.Sphere, flower, pads[0] + new Vector3(0.05f, 0.1f, 0f), new Vector3(0.2f, 0.14f, 0.2f));

            // Cattails on one bank.
            var reed = MatLib.Lit("Reed", new Color(0.36f, 0.62f, 0.28f), 0.1f);
            var cat = MatLib.Lit("Cattail", new Color(0.52f, 0.32f, 0.18f), 0.1f);
            for (int i = 0; i < 7; i++)
            {
                var p = new Vector3(r * 0.85f + (float)_rnd.NextDouble() * 0.5f, 0f, -0.9f + i * 0.28f);
                float h = 0.9f + (float)_rnd.NextDouble() * 0.5f;
                var st = B.Node("Reed", root, p).transform;
                B.Cyl("Stem", st, reed, new Vector3(0f, h * 0.5f, 0f), 0.04f, h);
                B.MeshObj("Head", st, B.Sphere, cat, new Vector3(0f, h, 0f), new Vector3(0.09f, 0.24f, 0.09f));
                Sway(st, 5f, 1.6f);
            }

            // Two ducks paddling around.
            for (int i = 0; i < 2; i++)
            {
                var duck = BuildDuck(root, new Vector3(i == 0 ? -0.6f : 0.7f, 0.03f, i == 0 ? -0.3f : 0.5f));
                var w = duck.AddComponent<Wanderer>();
                w.areaCenter = c;
                w.areaSize = new Vector2(r * 1.3f, r * 1.2f);
                w.speed = 0.45f;
                w.pause = new Vector2(0.5f, 2.5f);
                w.skittish = 0f;
                w.floats = true;
                w.bobTarget = duck.transform.GetChild(0);
            }

            var col = root.gameObject.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.4f, 0f);
            col.radius = r * 1.02f;
        }

        static GameObject BuildDuck(Transform parent, Vector3 localPos)
        {
            var root = B.Node("Duck", parent, localPos);
            var body = B.Node("Body", root.transform, Vector3.zero).transform;
            B.MeshObj("Torso", body, B.Sphere, _mDuckBody, new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.3f, 0.56f));
            B.MeshObj("Tail", body, B.Sphere, _mDuckBody, new Vector3(0f, 0.2f, -0.26f), new Vector3(0.18f, 0.16f, 0.2f), new Vector3(-30f, 0f, 0f));
            B.MeshObj("Head", body, B.Sphere, _mDuckBody, new Vector3(0f, 0.38f, 0.2f), Vector3.one * 0.26f);
            B.MeshObj("Beak", body, B.Sphere, _mDuckBeak, new Vector3(0f, 0.36f, 0.35f), new Vector3(0.14f, 0.06f, 0.14f));
            B.MeshObj("EyeL", body, B.Sphere, _mDark, new Vector3(-0.09f, 0.42f, 0.3f), Vector3.one * 0.045f);
            B.MeshObj("EyeR", body, B.Sphere, _mDark, new Vector3(0.09f, 0.42f, 0.3f), Vector3.one * 0.045f);
            return root;
        }

        // ================================================================== windmill

        static void BuildWindmill(Vector3 pos)
        {
            var root = B.Node("Windmill", _decor, pos, new Vector3(0f, 25f, 0f)).transform;
            B.MeshObj("Base", root, _disc, new[] { _mStone, _mStone }, Vector3.zero, new Vector3(3.3f, 0.4f, 3.3f));
            B.MeshObj("Tower", root, _tower, _mWhiteWall, new Vector3(0f, 0.35f, 0f), new Vector3(3f, 5.2f, 3f));
            B.MeshObj("Trim", root, _disc, new[] { _mWood, _mWood }, new Vector3(0f, 5.4f, 0f), new Vector3(2.35f, 0.25f, 2.35f));
            B.MeshObj("Roof", root, _cone, _mRoof, new Vector3(0f, 5.6f, 0f), new Vector3(2.6f, 2f, 2.6f));
            B.Box("Door", root, _mWood, new Vector3(0f, 0.95f, -1.35f), new Vector3(0.75f, 1.2f, 0.12f), new Vector3(-5f, 0f, 0f));
            var glass = MatLib.Lit("WindowBlue", new Color(0.55f, 0.8f, 1f), 0.8f);
            B.Box("Window", root, glass, new Vector3(0f, 3.1f, -1.1f), new Vector3(0.5f, 0.6f, 0.1f), new Vector3(-5f, 0f, 0f));
            B.Box("WindowFrame", root, _mWood, new Vector3(0f, 3.1f, -1.08f), new Vector3(0.62f, 0.72f, 0.08f), new Vector3(-5f, 0f, 0f));

            var hub = B.Node("Hub", root, new Vector3(0f, 4.9f, -1.35f)).transform;
            B.MeshObj("Axle", hub, B.Cylinder, _mDark, Vector3.zero, new Vector3(0.35f, 0.2f, 0.35f), new Vector3(90f, 0f, 0f));
            var sail = MatLib.Lit("Sail", new Color(1f, 0.97f, 0.9f), 0.1f);
            for (int i = 0; i < 4; i++)
            {
                var arm = B.Node("Arm", hub, Vector3.zero, new Vector3(0f, 0f, i * 90f + 20f)).transform;
                B.Box("Spar", arm, _mWood, new Vector3(0f, 1.65f, -0.1f), new Vector3(0.14f, 3.3f, 0.1f));
                B.Box("Sail", arm, sail, new Vector3(0.36f, 2f, -0.08f), new Vector3(0.62f, 2.3f, 0.04f));
                B.Box("SailEdge", arm, _mRed, new Vector3(0.68f, 2f, -0.08f), new Vector3(0.05f, 2.3f, 0.05f));
            }
            _ambient.spinners.Add(new Ambient.Spinner { t = hub, axis = Vector3.forward, speed = 38f });

            // Hay bales at the foot.
            for (int i = 0; i < 3; i++)
                B.MeshObj("Hay", root, B.Cylinder, _mHay, new Vector3(1.7f + i * 0.05f, 0.35f + (i == 2 ? 0.62f : 0f), -1.4f + (i == 1 ? 0.8f : i == 2 ? 0.4f : 0f)),
                    new Vector3(0.7f, 0.45f, 0.7f), new Vector3(0f, 0f, 90f));
        }

        // ================================================================== chicken coop

        static void BuildCoop(Vector3 c)
        {
            var root = B.Node("Coop", _decor, c).transform;
            var house = B.Node("House", root, new Vector3(1.2f, 0f, 1.3f), new Vector3(0f, -20f, 0f)).transform;
            B.Box("Stilts", house, _mWood, new Vector3(0f, 0.25f, 0f), new Vector3(1.7f, 0.5f, 1.3f));
            B.Box("Walls", house, _mWhiteWall, new Vector3(0f, 0.95f, 0f), new Vector3(1.8f, 0.9f, 1.4f));
            B.Box("RoofL", house, _mRoof, new Vector3(-0.5f, 1.62f, 0f), new Vector3(1.15f, 0.1f, 1.7f), new Vector3(0f, 0f, 35f));
            B.Box("RoofR", house, _mRoof, new Vector3(0.5f, 1.62f, 0f), new Vector3(1.15f, 0.1f, 1.7f), new Vector3(0f, 0f, -35f));
            B.Box("Gable", house, _mWhiteWall, new Vector3(0f, 1.52f, 0f), new Vector3(1.2f, 0.4f, 1.36f), new Vector3(0f, 0f, 45f));
            B.Box("Door", house, _mDark, new Vector3(0f, 0.8f, -0.71f), new Vector3(0.45f, 0.55f, 0.04f));
            B.Box("Ramp", house, _mWood, new Vector3(0f, 0.3f, -1.1f), new Vector3(0.45f, 0.05f, 0.9f), new Vector3(-28f, 0f, 0f));
            B.MeshObj("Hay", root, B.Cylinder, _mHay, new Vector3(-1.4f, 0.35f, 1.6f), new Vector3(0.7f, 0.45f, 0.7f), new Vector3(0f, 30f, 90f));
            B.Box("Trough", root, _mWood, new Vector3(-0.8f, 0.15f, -0.8f), new Vector3(1.2f, 0.3f, 0.4f));
            B.Box("Feed", root, _mHay, new Vector3(-0.8f, 0.28f, -0.8f), new Vector3(1.05f, 0.05f, 0.28f));
            B.Prop("Survival/bucket", root, new Vector3(0.2f, 0f, -1.4f), 3f);
            var houseCol = B.Node("HouseCol", root, new Vector3(1.2f, 0.8f, 1.3f)).AddComponent<BoxCollider>();
            houseCol.size = new Vector3(1.9f, 1.6f, 1.5f);

            // FenceLine takes world positions, so parent it to a root at the origin.
            var fence = B.Node("CoopFence", _decor, Vector3.zero).transform;
            FenceLine(c + new Vector3(-2.6f, 0f, -2.4f), c + new Vector3(-2.6f, 0f, 3f), fence);
            FenceLine(c + new Vector3(-2.6f, 0f, -2.4f), c + new Vector3(2.6f, 0f, -2.4f), fence);

            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimalRoot + "Animations/Animation_Controllers/Chicken.controller");
            for (int i = 0; i < 4; i++)
            {
                var p = c + new Vector3(-1.2f + i * 0.7f, 0f, -1.2f + (i % 2) * 0.9f);
                var w = Animal("Chicken_001", ctrl, root, p, 1.35f);
                if (w == null) continue;
                w.areaCenter = c + new Vector3(-0.4f, 0f, -0.2f);
                w.areaSize = new Vector2(3.6f, 3.6f);
                w.speed = 0.8f;
                w.pause = new Vector2(0.8f, 3f);
                w.skittish = 2f;
            }
        }

        static Wanderer Animal(string mesh, RuntimeAnimatorController ctrl, Transform parent, Vector3 worldPos, float scale)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalRoot + "Meshes/" + mesh + ".fbx");
            if (fbx == null)
            {
                Debug.LogWarning("[JuiceKing] Missing animal " + mesh);
                return null;
            }
            var root = B.Node(mesh.Replace("_001", ""), parent, Vector3.zero);
            root.transform.position = worldPos;
            root.transform.rotation = Quaternion.Euler(0f, (float)_rnd.NextDouble() * 360f, 0f);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            m.transform.SetParent(root.transform, false);
            m.transform.localScale = Vector3.one * scale;
            foreach (var r in m.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = _mAnimal;
                r.sharedMaterials = mats;
            }
            var anim = m.GetComponent<Animator>();
            if (anim == null) anim = m.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            B.Decal("Blob", root.transform, _mBlob, new Vector3(0f, 0.075f, 0f), Vector2.one * (0.6f * scale)).GetComponent<MeshRenderer>().sortingOrder = -2;
            var w = root.AddComponent<Wanderer>();
            w.animator = anim;
            w.areaCenter = worldPos;
            return w;
        }

        static void BuildCritters()
        {
            var root = B.Node("Critters", _decor, Vector3.zero).transform;
            // Doghouse and a round flower bed fill the empty south-east corner of the plaza.
            var dh = B.Node("DogHouse", _decor, new Vector3(10.6f, 0f, -11.3f), new Vector3(0f, -35f, 0f)).transform;
            B.Box("Walls", dh, _mWood, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 1.2f));
            B.Box("RoofL", dh, _mRoof, new Vector3(-0.3f, 1.05f, 0f), new Vector3(0.75f, 0.08f, 1.35f), new Vector3(0f, 0f, 38f));
            B.Box("RoofR", dh, _mRoof, new Vector3(0.3f, 1.05f, 0f), new Vector3(0.75f, 0.08f, 1.35f), new Vector3(0f, 0f, -38f));
            B.Box("Gable", dh, _mWood, new Vector3(0f, 0.95f, 0f), new Vector3(0.7f, 0.3f, 1.16f), new Vector3(0f, 0f, 45f));
            B.Box("Door", dh, _mDark, new Vector3(0f, 0.35f, -0.61f), new Vector3(0.45f, 0.55f, 0.03f));
            B.MeshObj("Bowl", dh, _disc, new[] { _mRed, _mRed }, new Vector3(0.75f, 0f, -0.7f), new Vector3(0.36f, 0.1f, 0.36f));
            B.Node("Col", dh, new Vector3(0f, 0.5f, 0f)).AddComponent<BoxCollider>().size = new Vector3(1.15f, 1f, 1.25f);

            var bed = B.Node("FlowerBed", _decor, new Vector3(7.6f, 0f, -10f)).transform;
            B.MeshObj("Rim", bed, _disc, new[] { _mStone, _mStone }, Vector3.zero, new Vector3(2.4f, 0.2f, 2.4f));
            B.MeshObj("Soil", bed, _disc, new[] { MatLib.Lit("BedSoil", new Color(0.45f, 0.3f, 0.18f), 0.05f), MatLib.Lit("BedSoil", new Color(0.45f, 0.3f, 0.18f), 0.05f) },
                new Vector3(0f, 0.05f, 0f), new Vector3(2.1f, 0.17f, 2.1f));
            var tree = B.Prop("Nature/tree_default", bed, new Vector3(0f, 0.2f, 0f), 2.4f, 30f);
            Sway(tree.transform, 1.5f, 1f);
            string[] fl = { "Nature/flower_redA", "Nature/flower_yellowA", "Nature/flower_purpleA", "Nature/flower_redB", "Nature/flower_yellowB" };
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                var f = B.Prop(fl[i % fl.Length], bed, new Vector3(Mathf.Cos(a) * 0.75f, 0.2f, Mathf.Sin(a) * 0.75f), 2f, i * 40f);
                Sway(f.transform, 7f, 2.2f);
            }
            B.Node("Col", bed, new Vector3(0f, 0.5f, 0f)).AddComponent<SphereCollider>().radius = 1.2f;

            var dogCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimalRoot + "Animations/Animation_Controllers/Dog.controller");
            var dog = Animal("Dog_001", dogCtrl, root, new Vector3(9.8f, 0f, -9.6f), 1.25f);
            if (dog != null)
            {
                dog.areaCenter = new Vector3(10.2f, 0f, -8.8f);
                dog.areaSize = new Vector2(2.6f, 2.4f);
                dog.speed = 1.3f;
                dog.pause = new Vector2(2f, 5f);
                dog.skittish = 1.6f;
            }
            var catCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimalRoot + "Animations/Animation_Controllers/Kitty.controller");
            var cat = Animal("Kitty_001", catCtrl, root, new Vector3(-7.2f, 0f, -9.4f), 1.6f);
            if (cat != null)
            {
                cat.areaCenter = new Vector3(-7.6f, 0f, -10.2f);
                cat.areaSize = new Vector2(3.4f, 3f);
                cat.speed = 0.7f;
                cat.pause = new Vector2(3f, 7f);
                cat.skittish = 1.5f;
            }
        }

        // ================================================================== butterflies & clouds

        static void BuildButterflies()
        {
            var root = B.Node("Butterflies", _decor, Vector3.zero);
            var bf = root.AddComponent<Butterflies>();
            Color[] cols = { new Color(1f, 0.75f, 0.9f), new Color(1f, 0.92f, 0.45f), new Color(0.65f, 0.85f, 1f), Color.white };
            var mats = new Material[cols.Length];
            for (int i = 0; i < cols.Length; i++)
            {
                mats[i] = MatLib.Sprite("Butterfly" + i, MatLib.Tex("fx_leaf.png"), cols[i]);
                mats[i].renderQueue = 3000;
            }
            Vector3[] homes =
            {
                new Vector3(-11f, 0f, -2f), new Vector3(11f, 0f, -4f), new Vector3(-3.4f, 0f, 12.4f), new Vector3(3.6f, 0f, 12.8f),
                new Vector3(-6.5f, 0f, 14.6f), new Vector3(8.5f, 0f, -11.5f), new Vector3(-10.5f, 0f, 9f), new Vector3(10.8f, 0f, 9.5f)
            };
            var flies = new List<Butterflies.Fly>();
            for (int i = 0; i < homes.Length; i++)
            {
                var body = B.Node("Butterfly", root.transform, homes[i] + Vector3.up).transform;
                B.MeshObj("Body", body, B.Sphere, _mDark, Vector3.zero, new Vector3(0.05f, 0.05f, 0.18f), null, false);
                Transform Wing(float side)
                {
                    var w = B.Node(side < 0 ? "WingL" : "WingR", body, Vector3.zero).transform;
                    B.MeshObj("Quad", w, B.Quad, mats[i % mats.Length], new Vector3(side * 0.13f, 0f, 0f), new Vector3(0.26f, 0.26f, 1f), new Vector3(90f, side * 35f, 0f), false);
                    return w;
                }
                flies.Add(new Butterflies.Fly { body = body, wingL = Wing(-1f), wingR = Wing(1f), home = homes[i], radius = 1.6f });
            }
            bf.flies = flies.ToArray();
        }

        static void BuildClouds()
        {
            _mCloud.renderQueue = 2999;
            EditorUtility.SetDirty(_mCloud);
            var root = B.Node("CloudShadows", _decor, Vector3.zero).transform;
            var list = new List<Transform>();
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(-36f + i * 13f + (float)_rnd.NextDouble() * 4f, 0.07f, -14f + (float)_rnd.NextDouble() * 34f);
                float s = 13f + (float)_rnd.NextDouble() * 9f;
                var d = B.Decal("Cloud", root, _mCloud, p, new Vector2(s, s * 0.7f), (float)_rnd.NextDouble() * 360f);
                list.Add(d.transform);
            }
            _ambient.clouds = list.ToArray();
            _ambient.cloudAreaX = new Vector2(-40f, 40f);
            _ambient.cloudSpeed = 1.1f;
        }

        // ================================================================== ground cover

        static void BuildGroundCover()
        {
            // Nature kit only: its palette is re-tinted to match; the Survival kit greens read as teal.
            string[] tufts = { "Nature/grass", "Nature/grass_large" };
            string[] extras = { "Nature/mushroom_red", "Nature/stump_round", "Survival/tree-log-small", "Nature/rock_smallA" };
            var root = B.Node("GroundCover", _nature, Vector3.zero).transform;
            for (int i = 0; i < 120; i++)
            {
                Vector3 p;
                int guard = 0;
                do
                {
                    p = new Vector3(((float)_rnd.NextDouble() - 0.5f) * 30f, 0f, -12.5f + (float)_rnd.NextDouble() * 38f);
                } while ((IsBusy(p) || InReserved(p)) && ++guard < 30);
                if (guard >= 30) continue;
                var t = B.Prop(tufts[_rnd.Next(tufts.Length)], root, p, 2.2f + (float)_rnd.NextDouble() * 1.4f, (float)_rnd.NextDouble() * 360f);
                B.NoShadows(t);
            }
            for (int i = 0; i < 16; i++)
            {
                Vector3 p;
                int guard = 0;
                do
                {
                    p = new Vector3((_rnd.Next(2) == 0 ? -1 : 1) * (13.5f + (float)_rnd.NextDouble() * 4f), 0f, -11f + (float)_rnd.NextDouble() * 32f);
                } while (InReserved(p) && ++guard < 20);
                string m = extras[_rnd.Next(extras.Length)];
                float s = m.Contains("rock") ? 2f : m.Contains("log") ? 3f : 3.2f;
                B.NoShadows(B.Prop(m, root, p, s, (float)_rnd.NextDouble() * 360f));
            }
        }
    }
}
