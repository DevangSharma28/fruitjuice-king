using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Visual polish shared by both worlds: soft contact shadows under props, vertex-coloured swaying foliage, ambient
    /// particles (pollen motes, drifting leaves, waterfall mist) and ProBuilder-made props (striped swim rings).
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        // ================================================================== entry points

        /// <summary>World 0 polish. Runs after the scene is dressed and before <see cref="OptimizeScene"/>.</summary>
        static void PolishClassic()
        {
            ScanGround();
            AutoContacts(_stations);
            AutoContacts(_decor, "PlazaDressing", "Patio", "Coop");
            AutoContacts(_nature, "Trees", "Plants");
            FruitContacts();
            SeeThroughTallDecor(_decor, _nature);
            BuildAmbientLife(new Color(1f, 0.95f, 0.7f, 0.8f), new Color(0.5f, 0.78f, 0.3f), new Color(0.98f, 0.72f, 0.3f));
        }

        /// <summary>Tropical polish. Runs after the scene is dressed and before <see cref="OptimizeScene"/>.</summary>
        static void PolishTropical()
        {
            ApplyFoliage(_stations, _decor, _nature);
            ScanGround();
            AutoContacts(_stations);
            foreach (var field in _stations.GetComponentsInChildren<FruitField>(true))
                AutoContacts(field.transform.parent.Find("Decor"));
            AutoContacts(_decor, "MixerDecor_*", "Beach");
            AutoContacts(_nature, "Trees", "Small");
            AutoContacts(_world.Find("Delivery/Bay"));
            foreach (var truck in _world.GetComponentsInChildren<DeliveryTruck>(true)) TruckContact(truck);
            FruitContacts();

            var lagoon = _decor.Find("Lagoon");
            if (lagoon != null)
            {
                BuildMist(lagoon, lagoon.TransformPoint(new Vector3(0f, 0.12f, 2.75f)));
                SwimRing(lagoon, new Vector3(-1.9f, 0.05f, -0.4f), new Color(1f, 0.36f, 0.34f), 0, true, 20f);
                SwimRing(lagoon, new Vector3(1.6f, 0.05f, 0.9f), new Color(0.3f, 0.72f, 1f), 1, true, -35f);
            }
            var beach = _decor.Find("Beach");
            if (beach != null)
            {
                SwimRing(beach, new Vector3(-14.1f, 0.15f, -21.2f), new Color(1f, 0.8f, 0.25f), 2, false, 10f);
                SwimRing(beach, new Vector3(10.4f, 0.15f, -23.6f), new Color(1f, 0.36f, 0.34f), 0, false, 60f);
            }
            SeeThroughTallDecor(_decor, _nature, _world.Find("Delivery"));
            BuildAmbientLife(new Color(1f, 0.98f, 0.82f, 0.8f), new Color(0.35f, 0.8f, 0.35f), new Color(0.75f, 0.9f, 0.3f));
        }

        /// <summary>Berry Blast polish. Runs after the scene is dressed and before <see cref="OptimizeScene"/>.</summary>
        static void PolishBerry()
        {
            ApplyFoliage(_stations, _decor, _nature);
            ScanGround();
            AutoContacts(_stations);
            AutoContacts(_decor, "Village", "Garden", "CakeShop", "FoxDenArea", "Picnic");
            AutoContacts(_nature, "Trees", "Small");
            var delivery = _world.Find("Delivery");
            if (delivery != null) foreach (Transform desk in delivery) AutoContacts(desk);
            foreach (var truck in _world.GetComponentsInChildren<DeliveryTruck>(true)) TruckContact(truck);
            foreach (var truck in _actors.GetComponentsInChildren<DeliveryTruck>(true)) TruckContact(truck);
            FruitContacts();
            SeeThroughTallDecor(_decor, _nature, delivery);
            // Blossom petals drift down instead of leaves.
            BuildAmbientLife(new Color(1f, 0.95f, 0.85f, 0.8f), new Color(1f, 0.72f, 0.84f), new Color(1f, 0.96f, 0.98f));
        }

        // ================================================================== contact shadows

        static Material _mContact;
        static readonly List<Bounds> _groundTops = new List<Bounds>();
        static readonly Dictionary<Mesh, Vector3[]> _vertCache = new Dictionary<Mesh, Vector3[]>();

        /// <summary>Never shadowed: water, moving or flying things, fences, ground cover, the pier.</summary>
        static readonly string[] NoContact =
        {
            "Pond", "Lagoon", "Critters", "Butterflies", "CloudShadows", "Gulls", "Parrots", "Sailboat", "GroundCover", "Pier", "Contact",
            "Birds", "Starfish", "PathBlob", "GrassPatch"
        };

        /// <summary>Small decor attached to a bigger prop gets its own shadow instead of widening the prop's.</summary>
        static readonly HashSet<string> SeparateContact = new HashSet<string>
            { "TikiTorch", "Fern", "Hibiscus", "FruitCrate", "Surfboard", "BeachBall", "Rocks", "Crate", "Barrel" };

        static Material ContactMat()
        {
            if (_mContact == null)
                _mContact = MatLib.Sprite("ContactShadow", MatLib.Tex("contact_shadow.png"), new Color(0.16f, 0.1f, 0.26f, 0.5f));
            return _mContact;
        }

        /// <summary>Remember the flat walkable surfaces (plaza, paths, deck, road) so shadows sit just above them.</summary>
        static void ScanGround()
        {
            _groundTops.Clear();
            foreach (var mr in _world.GetComponentsInChildren<MeshRenderer>(true))
            {
                var t = mr.transform;
                if (t.IsChildOf(_stations) || t.IsChildOf(_decor) || t.IsChildOf(_nature)) continue;
                var b = WorldBounds(mr);
                if (b.size == Vector3.zero || b.max.y > 0.2f || b.max.y < -0.02f || b.size.y > 0.3f) continue;
                _groundTops.Add(b);
            }
        }

        static float GroundY(Vector3 p)
        {
            float top = 0f;
            foreach (var b in _groundTops)
                if (p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z && b.max.y > top) top = b.max.y;
            return top + 0.014f;
        }

        static Vector3[] Verts(Mesh m)
        {
            if (!_vertCache.TryGetValue(m, out var v) || v == null) _vertCache[m] = v = m.vertices;
            return v;
        }

        /// <summary>Bounds from the mesh (works for inactive objects, unlike Renderer.bounds).</summary>
        static Bounds WorldBounds(MeshRenderer mr)
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return default;
            var lb = mf.sharedMesh.bounds;
            var m = mr.transform.localToWorldMatrix;
            var b = new Bounds(m.MultiplyPoint3x4(lb.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
                b.Encapsulate(m.MultiplyPoint3x4(lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return b;
        }

        /// <summary>Opaque 3D parts of a prop (no pads, piles, labels, decals or transparent bits).</summary>
        static bool Solid(MeshRenderer mr)
        {
            if (mr.GetComponent<TMPro.TextMeshPro>() != null) return false;
            if (mr.GetComponentInParent<Zone>(true) != null || mr.GetComponentInParent<ItemPile>(true) != null) return false;
            var mat = mr.sharedMaterial;
            return mat != null && mat.renderQueue < 2450;
        }

        static bool UnderSeparate(Transform t, Transform root)
        {
            for (var p = t; p != null && p != root; p = p.parent)
                if (SeparateContact.Contains(p.name)) return true;
            return false;
        }

        /// <summary>
        /// Footprint of a prop where it meets the ground: the XZ extent (in the prop's yaw frame) of the vertices near its
        /// lowest point, so a tree gets a trunk-sized shadow and a hut or bench its whole base.
        /// </summary>
        static bool Footprint(Transform root, out Vector3 center, out Vector2 size)
        {
            center = default;
            size = default;
            float minY = float.MaxValue, maxY = float.MinValue;
            var parts = new List<(Matrix4x4 m, Vector3[] v)>();
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!Solid(mr) || UnderSeparate(mr.transform, root)) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var b = WorldBounds(mr);
                minY = Mathf.Min(minY, b.min.y);
                maxY = Mathf.Max(maxY, b.max.y);
                parts.Add((mr.transform.localToWorldMatrix, Verts(mf.sharedMesh)));
            }
            if (parts.Count == 0 || minY > 0.35f || maxY - minY < 0.15f) return false;

            float band = minY + Mathf.Clamp((maxY - minY) * 0.3f, 0.12f, 0.4f);
            var rot = Quaternion.Euler(0f, root.eulerAngles.y, 0f);
            var inv = Quaternion.Inverse(rot);
            float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var (m, verts) in parts)
                foreach (var v in verts)
                {
                    var w = m.MultiplyPoint3x4(v);
                    if (w.y > band) continue;
                    var l = inv * (w - root.position);
                    x0 = Mathf.Min(x0, l.x);
                    x1 = Mathf.Max(x1, l.x);
                    z0 = Mathf.Min(z0, l.z);
                    z1 = Mathf.Max(z1, l.z);
                }
            if (x0 > x1) return false;
            center = root.position + rot * new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            size = new Vector2(x1 - x0, z1 - z0);
            return true;
        }

        /// <summary>Soft oval shadow on the ground under a prop, parented to it so it hides and reveals with it.</summary>
        static void ContactAt(Transform parent, Vector3 worldCenter, Vector2 size, float y, float yaw)
        {
            var d = B.Decal("Contact", null, ContactMat(), new Vector3(worldCenter.x, y, worldCenter.z), size, yaw);
            d.GetComponent<MeshRenderer>().sortingOrder = -6;
            d.transform.SetParent(parent, true);
            d.layer = parent.gameObject.layer;
        }

        static void ContactFor(Transform prop)
        {
            if (Footprint(prop, out var c, out var s))
            {
                float big = Mathf.Max(s.x, s.y), small = Mathf.Min(s.x, s.y);
                if (big >= 0.28f && big <= 8f && big <= small * 6f)
                {
                    // Pad the footprint: the blob fades out towards its edge.
                    var size = new Vector2(s.x * 1.25f + 0.35f, s.y * 1.25f + 0.35f);
                    ContactAt(prop, c, size, GroundY(c), prop.eulerAngles.y);
                }
            }
            foreach (var d in prop.GetComponentsInChildren<Transform>(true))
                if (d != prop && SeparateContact.Contains(d.name) && !UnderSeparate(d.parent, prop)) ContactFor(d);
        }

        static bool IsGroup(string name, string[] groups)
        {
            foreach (var g in groups)
                if (g.EndsWith("*") ? name.StartsWith(g.Substring(0, g.Length - 1)) : name == g) return true;
            return false;
        }

        /// <summary>
        /// Contact shadow under every direct child of <paramref name="container"/> that stands on the ground; children
        /// named in <paramref name="groups"/> (a trailing * matches a prefix) are containers themselves.
        /// </summary>
        static void AutoContacts(Transform container, params string[] groups)
        {
            if (container == null) return;
            var kids = new List<Transform>();
            foreach (Transform c in container) kids.Add(c);
            foreach (var c in kids)
            {
                if (System.Array.IndexOf(NoContact, c.name) >= 0 || c.name.Contains("Fence")) continue;
                if (IsGroup(c.name, groups))
                {
                    AutoContacts(c, groups);
                    continue;
                }
                if (c.GetComponent<Zone>() != null || c.GetComponentInChildren<FruitField>(true) != null) continue;
                if (c.GetComponentInChildren<Wanderer>(true) != null || c.GetComponentInChildren<DeliveryTruck>(true) != null) continue;
                ContactFor(c);
            }
        }

        /// <summary>Shadows under the harvestable plants. Classic fruit carries its own (it shrinks and hides with the fruit).</summary>
        static void FruitContacts()
        {
            foreach (var node in _stations.GetComponentsInChildren<FruitNode>(true))
            {
                var p = node.transform.position;
                if (node is TropicalFruitNode) ContactAt(node.transform, p, new Vector2(1.9f, 1.7f), p.y + 0.095f, 0f);
                else if (node is BerryBushNode) ContactAt(node.transform, p, new Vector2(1.5f, 1.35f), p.y + 0.075f, 0f);
                else ContactAt(node.visual, p, new Vector2(node.radius * 2.3f, node.radius * 2.1f), p.y + 0.11f, 0f);
            }
        }

        /// <summary>Delivery trucks carry a shadow along the road.</summary>
        static void TruckContact(DeliveryTruck truck)
        {
            var t = truck.transform;
            if (!Footprint(t, out var c, out var s)) return;
            ContactAt(t, c, new Vector2(s.x * 1.15f + 0.3f, s.y * 1.1f + 0.3f), t.position.y + 0.05f, t.eulerAngles.y);
        }

        // ================================================================== see-through

        /// <summary>
        /// Tall decor (trees, umbrellas, windmill, huts, signs) dithers out when it stands between the camera and the
        /// player (JuiceKing/Stylized <c>_SEE_THROUGH</c>). Only these materials pay for the fragment clip.
        /// </summary>
        static void SeeThroughTallDecor(params Transform[] roots)
        {
            var done = new HashSet<Material>();
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (WorldBounds(mr).max.y < 1.8f) continue;
                    foreach (var m in mr.sharedMaterials)
                    {
                        if (m == null || !m.HasProperty("_SeeThrough") || !done.Add(m)) continue;
                        m.SetFloat("_SeeThrough", 1f);
                        m.EnableKeyword("_SEE_THROUGH");
                        EditorUtility.SetDirty(m);
                    }
                }
            }
        }

        // ================================================================== foliage

        static readonly Dictionary<string, Material> _foliage = new Dictionary<string, Material>();

        /// <summary>Vertex-coloured (+ optional wind) variant of a foliage material: darker base, sunlit tips, sway.</summary>
        static Material Foliage(Material m, float wind)
        {
            if (m == null || !m.HasProperty("_VertexColor") || m.GetFloat("_VertexColor") > 0.5f) return m;
            string key = m.name + "_V" + Mathf.RoundToInt(wind * 10f);
            if (_foliage.TryGetValue(key, out var v) && v != null) return v;
            var col = m.GetColor("_BaseColor");
            v = MatLib.Lit(key, new Color(Mathf.Min(1f, col.r * 1.1f), Mathf.Min(1f, col.g * 1.1f), Mathf.Min(1f, col.b * 1.1f), 1f),
                m.GetFloat("_Smoothness"), m.GetTexture("_BaseMap"), m.GetTextureScale("_BaseMap"));
            v.SetFloat("_VertexColor", 1f);
            v.SetFloat("_Wind", wind);
            v.SetFloat("_RimStrength", 0.24f);
            v.SetFloat("_Cull", m.GetFloat("_Cull"));
            EditorUtility.SetDirty(v);
            _foliage[key] = v;
            return v;
        }

        /// <summary>Swap leaves, canopies and trunks built from the vertex-coloured meshes to foliage materials.</summary>
        static void ApplyFoliage(params Transform[] roots)
        {
            var leaf = new HashSet<Mesh> { _shFrond, _shBroadLeaf, _shPapayaLeaf };
            var canopy = new HashSet<Mesh>(_shLeafBall);
            var trunk = new HashSet<Mesh> { _shPalmTrunk, _shShortTrunk, _shThinTrunk, _shStem, _shBanana };
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    var mesh = mf.sharedMesh;
                    if (mr == null || mesh == null) continue;
                    float wind = leaf.Contains(mesh) ? 1.3f : canopy.Contains(mesh) ? 0.6f : trunk.Contains(mesh) ? 0f : -1f;
                    if (wind < 0f) continue;
                    var mats = mr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = Foliage(mats[i], wind);
                    mr.sharedMaterials = mats;
                }
            }
        }

        // ================================================================== ambient particles

        static ParticleSystem NewParticles(string name, Transform parent, Vector3 localPos, Material mat, int max)
        {
            var go = B.Node(name, parent, localPos);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingOrder = 8;
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            return ps;
        }

        static void FadeInOut(ParticleSystem ps, float peak = 1f)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.2f), new GradientAlphaKey(peak, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        /// <summary>Floating pollen / light motes and drifting leaves in the area around the player.</summary>
        static void BuildAmbientLife(Color mote, Color leafA, Color leafB)
        {
            var root = B.Node("AmbientLife", null, Vector3.zero);
            // The camera looks north from the south: keep most of the area ahead of the player.
            root.AddComponent<FollowTarget>().offset = new Vector3(0f, 0f, 3f);

            var motes = NewParticles("Motes", root.transform, new Vector3(0f, 1.6f, 0f), MatLib.Sprite("Fx_Mote", MatLib.Tex("fx_circle.png"), Color.white), 80);
            var mm = motes.main;
            mm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            mm.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.15f);
            mm.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);
            mm.startColor = new ParticleSystem.MinMaxGradient(mote, new Color(1f, 1f, 1f, mote.a));
            var me = motes.emission;
            me.rateOverTime = 9f;
            var msh = motes.shape;
            msh.shapeType = ParticleSystemShapeType.Box;
            msh.scale = new Vector3(24f, 2.6f, 28f);
            var mn = motes.noise;
            mn.enabled = true;
            mn.strength = 0.3f;
            mn.frequency = 0.25f;
            mn.scrollSpeed = 0.15f;
            var ms = motes.sizeOverLifetime;
            ms.enabled = true;
            ms.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.4f)));
            FadeInOut(motes);

            var leaves = NewParticles("Leaves", root.transform, new Vector3(0f, 5.5f, -1f), MatLib.Sprite("Fx_LeafFall", MatLib.Tex("fx_leaf.png"), Color.white), 20);
            var lm = leaves.main;
            lm.startLifetime = new ParticleSystem.MinMaxCurve(8f, 11f);
            lm.startSpeed = 0f;
            lm.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
            lm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            lm.startColor = new ParticleSystem.MinMaxGradient(leafA, leafB);
            var le = leaves.emission;
            le.rateOverTime = 0.8f;
            var lsh = leaves.shape;
            lsh.shapeType = ParticleSystemShapeType.Box;
            lsh.scale = new Vector3(22f, 1f, 24f);
            var lv = leaves.velocityOverLifetime;
            lv.enabled = true;
            lv.space = ParticleSystemSimulationSpace.World;
            lv.x = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            lv.y = new ParticleSystem.MinMaxCurve(-0.6f, -0.45f);
            lv.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            var lr = leaves.rotationOverLifetime;
            lr.enabled = true;
            lr.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
            var ln = leaves.noise;
            ln.enabled = true;
            ln.strength = 0.6f;
            ln.frequency = 0.35f;
            FadeInOut(leaves);
        }

        /// <summary>Soft white spray rising where a waterfall lands.</summary>
        static void BuildMist(Transform parent, Vector3 worldPos)
        {
            var ps = NewParticles("Mist", parent, Vector3.zero, MatLib.Sprite("Fx_Mist", MatLib.Tex("soft_blob.png"), Color.white), 50);
            ps.transform.position = worldPos;
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            m.startColor = new Color(1f, 1f, 1f, 0.5f);
            m.gravityModifier = -0.02f;
            var e = ps.emission;
            e.rateOverTime = 14f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 40f;
            sh.radius = 0.4f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            var so = ps.sizeOverLifetime;
            so.enabled = true;
            so.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.5f)));
            FadeInOut(ps, 0.75f);
        }

        // ================================================================== waterfall

        static Mesh _waterfall;

        /// <summary>
        /// Curved sheet that pours over a lip and falls (parabola), widening as it drops. Pivot at the lip's foot;
        /// faces -Z. V runs down the flow in metres / 1.1, so a scrolling texture streams downwards.
        /// </summary>
        static Mesh WaterfallMesh()
        {
            if (_waterfall != null) return _waterfall;
            const int cols = 6, rows = 14;
            const float top = 2.3f, bottom = 0.02f, reach = 0.6f;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float len = 0f;
            var prev = new Vector3(0f, top, 0f);
            for (int i = 0; i <= rows; i++)
            {
                float t = i / (float)rows;
                var c = new Vector3(0f, Mathf.Lerp(top, bottom, t * t), -reach * t);
                len += (c - prev).magnitude;
                prev = c;
                float w = Mathf.Lerp(0.95f, 1.3f, t);
                for (int j = 0; j <= cols; j++)
                {
                    float u = j / (float)cols;
                    // A slight outward bow across the sheet so it catches the light unevenly.
                    float bow = Mathf.Sin(u * Mathf.PI) * 0.06f;
                    verts.Add(c + new Vector3((u - 0.5f) * w, 0f, -bow));
                    uvs.Add(new Vector2(u, -len / 1.1f));
                }
            }
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    int a = i * (cols + 1) + j, b = a + 1, c = a + cols + 1, d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            var m = new Mesh { name = "T_Waterfall" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            _waterfall = SaveMesh(m, "T_Waterfall");
            return _waterfall;
        }

        // ================================================================== striped umbrella

        static readonly Dictionary<string, Mesh> _canopies = new Dictionary<string, Mesh>();

        /// <summary>
        /// Umbrella canopy with 8 panels alternating two colours, scalloped between the ribs, a little valance under
        /// the rim and a darker underside. Same unit size as <see cref="MakeUmbrella"/> (radius 0.5, height 0.5, pivot
        /// at the rim), colours in the mesh (use <see cref="VertexLit"/>).
        /// </summary>
        static Mesh StripedCanopy(Color a, Color b, string key)
        {
            if (_canopies.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            const int panels = 8, sub = 4;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();
            var apex = new Vector3(0f, 0.5f, 0f);

            Vector3 Rim(float ang, float f) =>
                new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (0.5f * (1f - 0.05f * Mathf.Sin(f * Mathf.PI))) + Vector3.up * (0.05f * Mathf.Sin(f * Mathf.PI));
            Vector3 Normal(float ang) => new Vector3(Mathf.Cos(ang), 1f, Mathf.Sin(ang)).normalized;
            void Tri(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 n0, Vector3 n1, Vector3 n2, Color col)
            {
                int i = v.Count;
                v.Add(p0); v.Add(p1); v.Add(p2);
                n.Add(n0); n.Add(n1); n.Add(n2);
                c.Add(col); c.Add(col); c.Add(col);
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
            }

            for (int p = 0; p < panels; p++)
            {
                // Vertex colours are not converted to linear space like material colours are: do it here.
                var col = (p % 2 == 0 ? a : b).linear;
                var under = col * 0.5f;
                under.a = 1f;
                float mid = (p + 0.5f) / panels * Mathf.PI * 2f;
                for (int s = 0; s < sub; s++)
                {
                    float f0 = s / (float)sub, f1 = (s + 1) / (float)sub;
                    float a0 = (p + f0) / panels * Mathf.PI * 2f, a1 = (p + f1) / panels * Mathf.PI * 2f;
                    Vector3 r0 = Rim(a0, f0), r1 = Rim(a1, f1);
                    // Top (clockwise seen from above), then the same panel seen from below.
                    Tri(apex, r1, r0, Normal(mid), Normal(a1), Normal(a0), col);
                    Tri(apex, r0, r1, Vector3.down, Vector3.down, Vector3.down, under);
                    // Valance: a flap hanging under the rim with a rounded bottom edge.
                    float d0 = 0.05f + 0.07f * Mathf.Sin(f0 * Mathf.PI), d1 = 0.05f + 0.07f * Mathf.Sin(f1 * Mathf.PI);
                    Vector3 b0 = r0 + Vector3.down * d0, b1 = r1 + Vector3.down * d1;
                    Vector3 o0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), o1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                    Tri(r0, r1, b0, o0, o1, o0, col);
                    Tri(r1, b1, b0, o1, o1, o0, col);
                }
            }
            mesh = new Mesh { name = "Canopy_" + key };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, "Canopy_" + key);
            _canopies[key] = mesh;
            return mesh;
        }

        /// <summary>Striped canopy in the colour of <paramref name="mat"/> and cream, plus a finial on top.</summary>
        static void StripedUmbrella(Transform parent, Material mat, Vector3 pos, Vector3 scale)
        {
            var col = mat.GetColor("_BaseColor");
            B.MeshObj("Canopy", parent, StripedCanopy(col, new Color(1f, 0.97f, 0.9f), mat.name), VertexLit(), pos, scale);
            B.MeshObj("Finial", parent, LowSphere(), _mWhite, pos + Vector3.up * scale.y * 0.5f, Vector3.one * 0.14f, null, false);
        }

        /// <summary>The 10x7 low-poly sphere (world 0 is built before the tropical meshes exist).</summary>
        static Mesh LowSphere() => _lowSphere != null ? _lowSphere : _lowSphere = UVSphere("T_LowSphere", 10, 7);

        // ================================================================== ProBuilder props

        static readonly Dictionary<int, Mesh> _swimRings = new Dictionary<int, Mesh>();
        static Material _mVertexLit;

        /// <summary>White stylized material that takes its colours from the mesh (ProBuilder face colours).</summary>
        static Material VertexLit()
        {
            if (_mVertexLit == null)
            {
                _mVertexLit = MatLib.Lit("VertexColorLit", Color.white, 0.45f);
                _mVertexLit.SetFloat("_VertexColor", 1f);
                _mVertexLit.SetFloat("_RimStrength", 0.25f);
                EditorUtility.SetDirty(_mVertexLit);
            }
            return _mVertexLit;
        }

        /// <summary>Bake a ProBuilder shape to a plain mesh asset (the game does not need ProBuilder at runtime).</summary>
        static Mesh BakeProBuilder(ProBuilderMesh pb, string name)
        {
            pb.ToMesh();
            pb.Refresh();
            var mesh = Object.Instantiate(pb.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = name;
            mesh.RecalculateBounds();
            Object.DestroyImmediate(pb.gameObject);
            return SaveMesh(mesh, name);
        }

        /// <summary>Inflatable ring (ProBuilder torus, 0.84 m across) with six alternating colour/white panels.</summary>
        static Mesh SwimRingMesh(Color stripe, int key)
        {
            if (_swimRings.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            var pb = ShapeGenerator.GenerateTorus(PivotLocation.Center, 10, 24, 0.42f, 0.15f, true, 360f, 360f, false);
            var pos = pb.positions;
            var white = new Color(1f, 0.98f, 0.94f).linear;
            stripe = stripe.linear;
            foreach (var f in pb.faces)
            {
                var c = Vector3.zero;
                foreach (var i in f.distinctIndexes) c += pos[i];
                float a = Mathf.Atan2(c.z, c.x) + Mathf.PI;
                pb.SetFaceColor(f, Mathf.FloorToInt(a / (Mathf.PI * 2f) * 6f) % 2 == 0 ? stripe : white);
            }
            mesh = BakeProBuilder(pb, "PB_SwimRing" + key);
            _swimRings[key] = mesh;
            return mesh;
        }

        /// <summary>Striped swim ring: bobbing on water, or lying on the sand.</summary>
        static GameObject SwimRing(Transform parent, Vector3 localPos, Color stripe, int key, bool floating, float yaw)
        {
            var go = B.MeshObj("SwimRing", parent, SwimRingMesh(stripe, key), VertexLit(), localPos, Vector3.one,
                new Vector3(floating ? 0f : 6f, yaw, 0f));
            if (floating) Sway(go.transform, 5f, 1.2f);
            return go;
        }
    }
}
