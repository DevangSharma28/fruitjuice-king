using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// The chainsaw held by the player and the farmers: a rounded orange engine with cover, vents, starter, wrap-around
    /// front handle, red hand guard and rear handle, plus a stadium-shaped bar with a chain loop whose links scroll.
    /// The engine is baked into one multi-material mesh (a handful of draw calls); the blade stays separate so it can
    /// vibrate and its chain can run.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        // Bar: from just inside the engine to the rounded nose (local +Z is forward, the saw's pivot is the engine centre).
        const float SawBarZ0 = 0.12f, SawBarZ1 = 0.78f, SawBarR = 0.062f, SawBarT = 0.046f;
        const float SawChainIn = 0.004f, SawChainOut = 0.03f, SawChainW = 0.066f, SawLink = 0.05f;

        static Mesh _sawBody, _sawBar, _sawChain;
        static Material[] _sawBodyMats;

        static Chainsaw BuildChainsaw(Transform parent, Vector3 localPos, float volume)
        {
            if (_sawBody == null) BakeChainsawMeshes();
            var saw = B.Node("Chainsaw", parent, localPos);
            var st = saw.transform;
            var body = B.MeshObj("Engine", st, _sawBody, _sawBodyMats, Vector3.zero, Vector3.one, null, true);
            var blade = B.Node("Blade", st, new Vector3(0f, -0.03f, 0f));
            B.MeshObj("Bar", blade.transform, _sawBar, _mSteel, Vector3.zero, Vector3.one, null, false);
            var chain = B.MeshObj("Chain", blade.transform, _sawChain, _mChain, Vector3.zero, Vector3.one, null, false);
            var exhaust = B.Node("Exhaust", st, new Vector3(0.13f, -0.02f, -0.12f));

            var c = saw.AddComponent<Chainsaw>();
            c.bladeVisual = blade.transform;
            c.engine = body.transform;
            c.chainRenderer = chain.GetComponent<MeshRenderer>();
            c.exhaust = exhaust.transform;
            c.exhaustPuffs = volume >= 1f; // only the player's saw puffs smoke (keeps the particle count down)
            var src = saw.AddComponent<AudioSource>();
            src.playOnAwake = true;
            src.loop = true;
            src.spatialBlend = 0f;
            c.audioSource = src;
            c.volumeScale = volume;
            return c;
        }

        static void BakeChainsawMeshes()
        {
            // (TubeBetween / B.Cylinder: the built-in cylinder mesh has radius 1, so a "diameter" of d draws 2d wide.)
            // Build the engine from primitives under a temporary root, then merge per material into one mesh.
            var tmp = new GameObject("_SawTmp").transform;
            var orange = _mOrangeBody;
            var dark = _mDark;
            RB("Housing", tmp, orange, new Vector3(0f, 0f, -0.02f), new Vector3(0.24f, 0.2f, 0.34f), 0.07f);
            RB("Skirt", tmp, dark, new Vector3(0f, -0.075f, -0.02f), new Vector3(0.25f, 0.05f, 0.35f), 0.025f);
            RB("Cover", tmp, dark, new Vector3(0f, 0.11f, -0.05f), new Vector3(0.19f, 0.07f, 0.24f), 0.03f);
            RB("CoverStripe", tmp, orange, new Vector3(0f, 0.148f, -0.05f), new Vector3(0.07f, 0.012f, 0.2f), 0.005f);
            for (int i = 0; i < 3; i++)
                B.Box("Vent", tmp, dark, new Vector3(0.121f, -0.02f + i * 0.035f, -0.04f), new Vector3(0.01f, 0.018f, 0.15f));
            // Starter on the left: recoil housing, steel cap, pull grip.
            B.MeshObj("Starter", tmp, _disc, new[] { dark, dark }, new Vector3(-0.12f, 0.0f, -0.05f), new Vector3(0.17f, 0.03f, 0.17f), new Vector3(0f, 0f, 90f));
            B.MeshObj("StarterCap", tmp, _disc, new[] { _mSteel, _mSteel }, new Vector3(-0.15f, 0.0f, -0.05f), new Vector3(0.07f, 0.012f, 0.07f), new Vector3(0f, 0f, 90f));
            RB("PullGrip", tmp, dark, new Vector3(-0.15f, 0.09f, -0.12f), new Vector3(0.03f, 0.03f, 0.08f), 0.012f);
            // Clutch cover where the bar leaves the engine.
            RB("Clutch", tmp, dark, new Vector3(0.1f, -0.02f, 0.12f), new Vector3(0.06f, 0.13f, 0.12f), 0.025f);
            // Wrap-around front handle and the red hand guard in front of it.
            var arch = new[]
            {
                new Vector3(-0.15f, -0.05f, 0.1f), new Vector3(-0.155f, 0.15f, 0.1f), new Vector3(-0.1f, 0.235f, 0.1f),
                new Vector3(0.1f, 0.235f, 0.1f), new Vector3(0.155f, 0.15f, 0.1f), new Vector3(0.15f, -0.05f, 0.1f),
            };
            for (int i = 0; i < arch.Length - 1; i++) TubeBetween("FrontHandle", tmp, arch[i], arch[i + 1], 0.026f, dark);
            RB("Guard", tmp, _mRed, new Vector3(0f, 0.2f, 0.165f), new Vector3(0.21f, 0.15f, 0.022f), 0.02f).transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            // Rear handle loop with an orange trigger.
            var loop = new[]
            {
                new Vector3(0f, 0.08f, -0.18f), new Vector3(0f, 0.14f, -0.29f), new Vector3(0f, 0.11f, -0.41f),
                new Vector3(0f, -0.03f, -0.42f), new Vector3(0f, -0.07f, -0.2f),
            };
            for (int i = 0; i < loop.Length - 1; i++) TubeBetween("RearHandle", tmp, loop[i], loop[i + 1], 0.03f, dark);
            RB("Trigger", tmp, orange, new Vector3(0f, 0.075f, -0.3f), new Vector3(0.025f, 0.05f, 0.06f), 0.01f);
            // Bar studs and the gold nose rivet.
            B.MeshObj("Stud", tmp, _disc, new[] { _mSteel, _mSteel }, new Vector3(0.016f, -0.03f, 0.2f), new Vector3(0.04f, 0.01f, 0.04f), new Vector3(0f, 0f, 90f));

            var byMat = new Dictionary<Material, List<CombineInstance>>();
            var order = new List<Material>();
            var toLocal = tmp.worldToLocalMatrix;
            foreach (var mr in tmp.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mats = mr.sharedMaterials;
                for (int s = 0; s < mf.sharedMesh.subMeshCount && s < mats.Length; s++)
                {
                    if (!byMat.TryGetValue(mats[s], out var list))
                    {
                        byMat[mats[s]] = list = new List<CombineInstance>();
                        order.Add(mats[s]);
                    }
                    list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = toLocal * mr.transform.localToWorldMatrix });
                }
            }
            var parts = new CombineInstance[order.Count];
            for (int i = 0; i < order.Count; i++)
            {
                var m = new Mesh();
                m.CombineMeshes(byMat[order[i]].ToArray(), true, true);
                parts[i] = new CombineInstance { mesh = m, transform = Matrix4x4.identity };
            }
            var bodyMesh = new Mesh { name = "ChainsawBody" };
            bodyMesh.CombineMeshes(parts, false, false);
            bodyMesh.RecalculateBounds();
            foreach (var p in parts) Object.DestroyImmediate(p.mesh);
            Object.DestroyImmediate(tmp.gameObject);
            _sawBody = SaveMesh(bodyMesh, "ChainsawBody");
            _sawBodyMats = order.ToArray();
            _sawBar = SaveMesh(SawBarMesh(), "ChainsawBar");
            _sawChain = SaveMesh(SawChainMesh(), "ChainsawChain");
        }

        /// <summary>Stadium outline in the YZ plane (two half circles of radius <paramref name="r"/> at z0 and z1), clockwise from the rear top.</summary>
        static List<Vector2> SawOutline(float r, int seg)
        {
            var pts = new List<Vector2>();
            // Top edge rear to front, nose half circle, bottom edge front to rear, rear half circle.
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * 0.5f - Mathf.PI * i / seg;
                pts.Add(new Vector2(Mathf.Sin(a) * r, SawBarZ1 + Mathf.Cos(a) * r));
            }
            for (int i = 0; i <= seg; i++)
            {
                float a = -Mathf.PI * 0.5f - Mathf.PI * i / seg;
                pts.Add(new Vector2(Mathf.Sin(a) * r, SawBarZ0 + Mathf.Cos(a) * r));
            }
            return pts; // x = y-coordinate, y = z-coordinate
        }

        static Mesh SawBarMesh()
        {
            var o = SawOutline(SawBarR, 10);
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            float h = SawBarT * 0.5f;
            // Two flat faces (fans around the centre) and the rim.
            foreach (var side in new[] { 1f, -1f })
            {
                int c0 = v.Count;
                v.Add(new Vector3(side * h, 0f, (SawBarZ0 + SawBarZ1) * 0.5f));
                n.Add(new Vector3(side, 0f, 0f));
                uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i < o.Count; i++)
                {
                    v.Add(new Vector3(side * h, o[i].x, o[i].y));
                    n.Add(new Vector3(side, 0f, 0f));
                    uv.Add(new Vector2(o[i].y, o[i].x));
                }
                for (int i = 0; i < o.Count; i++)
                {
                    int a = c0 + 1 + i, b = c0 + 1 + (i + 1) % o.Count;
                    // Both windings: the culled copy never shows, so the face renders whichever way it was wound.
                    tri.Add(c0); tri.Add(a); tri.Add(b);
                    tri.Add(c0); tri.Add(b); tri.Add(a);
                }
            }
            AddRim(o, SawBarR, -h, h, v, n, uv, tri, 0f);
            var m = new Mesh { name = "ChainsawBar" };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Chain loop: an outer band plus both side rings, with U running along the loop in link units.</summary>
        static Mesh SawChainMesh()
        {
            var inner = SawOutline(SawBarR + SawChainIn, 10);
            var outer = SawOutline(SawBarR + SawChainOut, 10);
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            float h = SawChainW * 0.5f;
            AddRim(outer, SawBarR + SawChainOut, -h, h, v, n, uv, tri, 0f);
            // Side rings between the inner and outer outlines.
            foreach (var side in new[] { 1f, -1f })
            {
                int s0 = v.Count;
                float dist = 0f;
                for (int i = 0; i < inner.Count; i++)
                {
                    if (i > 0) dist += Vector2.Distance(outer[i], outer[i - 1]);
                    v.Add(new Vector3(side * h, inner[i].x, inner[i].y));
                    v.Add(new Vector3(side * h, outer[i].x, outer[i].y));
                    n.Add(new Vector3(side, 0f, 0f));
                    n.Add(new Vector3(side, 0f, 0f));
                    uv.Add(new Vector2(dist / (SawLink * 8f), 0.15f));
                    uv.Add(new Vector2(dist / (SawLink * 8f), 0.85f));
                }
                int cnt = inner.Count;
                for (int i = 0; i < cnt; i++)
                {
                    int a = s0 + i * 2, b = s0 + ((i + 1) % cnt) * 2;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b + 1); tri.Add(a); tri.Add(b + 1); tri.Add(b);
                    tri.Add(a); tri.Add(b + 1); tri.Add(a + 1); tri.Add(a); tri.Add(b); tri.Add(b + 1);
                }
            }
            var m = new Mesh { name = "ChainsawChain" };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Outward-facing band along an outline, between x0 and x1; U is the distance along the loop in link units.</summary>
        static void AddRim(List<Vector2> o, float r, float x0, float x1, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri, float u0)
        {
            int s0 = v.Count;
            float dist = u0;
            for (int i = 0; i < o.Count; i++)
            {
                if (i > 0) dist += Vector2.Distance(o[i], o[i - 1]);
                // Outward normal: from the nearest half-circle centre (straight edges point straight up / down).
                float cz = Mathf.Clamp(o[i].y, SawBarZ0, SawBarZ1);
                var nn = new Vector3(0f, o[i].x, o[i].y - cz).normalized;
                v.Add(new Vector3(x0, o[i].x, o[i].y));
                v.Add(new Vector3(x1, o[i].x, o[i].y));
                n.Add(nn);
                n.Add(nn);
                uv.Add(new Vector2(dist / (SawLink * 8f), 0f));
                uv.Add(new Vector2(dist / (SawLink * 8f), 1f));
            }
            int cnt = o.Count;
            for (int i = 0; i < cnt; i++)
            {
                int a = s0 + i * 2, b = s0 + ((i + 1) % cnt) * 2;
                tri.Add(a); tri.Add(b); tri.Add(a + 1);
                tri.Add(a + 1); tri.Add(b); tri.Add(b + 1);
                tri.Add(a); tri.Add(a + 1); tri.Add(b);
                tri.Add(a + 1); tri.Add(b + 1); tri.Add(b);
            }
        }
    }
}
