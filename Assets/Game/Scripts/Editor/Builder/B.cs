using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace JuiceKing.EditorTools
{
    /// <summary>Scene/prefab construction helpers.</summary>
    public static class B
    {
        static Mesh _cube, _cyl, _sphere, _quad;
        public static Mesh Cube => _cube != null ? _cube : _cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        public static Mesh Cylinder => _cyl != null ? _cyl : _cyl = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
        public static Mesh Sphere => _sphere != null ? _sphere : _sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        public static Mesh Quad => _quad != null ? _quad : _quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        public static Material SpriteMat;
        public static TMP_FontAsset Font;
        public static Material FontOutlineMat;

        public static GameObject Node(string name, Transform parent, Vector3 pos, Vector3? euler = null, Vector3? scale = null)
        {
            var go = new GameObject(name);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            t.localScale = scale ?? Vector3.one;
            return go;
        }

        public static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale,
            Vector3? euler = null, bool shadows = true)
            => MeshObj(name, parent, mesh, new[] { mat }, pos, scale, euler, shadows);

        public static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material[] mats, Vector3 pos, Vector3 scale,
            Vector3? euler = null, bool shadows = true)
        {
            var go = Node(name, parent, pos, euler, scale);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Material mat, Vector3 pos, Vector3 size, Vector3? euler = null, bool shadows = true)
            => MeshObj(name, parent, Cube, mat, pos, size, euler, shadows);

        public static GameObject Cyl(string name, Transform parent, Material mat, Vector3 pos, float diameter, float height, Vector3? euler = null)
            => MeshObj(name, parent, Cylinder, mat, pos, new Vector3(diameter, height * 0.5f, diameter), euler);

        /// <summary>Instantiate a Kenney model (kept as a prefab instance) scaled uniformly.</summary>
        public static GameObject Model(string relPath, Transform parent, Vector3 pos, float scale, float yRot = 0f, string name = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyImport.Root + "/" + relPath + ".fbx");
            if (asset == null)
            {
                Debug.LogWarning("Missing model " + relPath);
                return Node(name ?? relPath, parent, pos);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            if (name != null) go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>
        /// Static prop: wrapper node at pos/yaw with the model re-centred so its bounds sit bottom-centre on the wrapper
        /// (Kenney kits use different pivots and unit scales).
        /// </summary>
        public static GameObject Prop(string relPath, Transform parent, Vector3 pos, float scale, float yRot = 0f, string name = null)
        {
            var wrap = Node(name ?? System.IO.Path.GetFileName(relPath), parent, pos, new Vector3(0f, yRot, 0f));
            var m = Model(relPath, wrap.transform, Vector3.zero, scale);
            var rs = m.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                bool first = true;
                var lb = new Bounds();
                foreach (var r in rs)
                {
                    var b = r.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                        var l = wrap.transform.InverseTransformPoint(c);
                        if (first)
                        {
                            lb = new Bounds(l, Vector3.zero);
                            first = false;
                        }
                        else lb.Encapsulate(l);
                    }
                }
                m.transform.localPosition = new Vector3(-lb.center.x, -lb.min.y, -lb.center.z);
            }
            wrap.isStatic = true;
            return wrap;
        }

        public static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Vector3 pos, float scale, bool flat = false,
            int order = 0, Color? color = null)
        {
            var go = Node(name, parent, pos, flat ? new Vector3(90f, 0f, 0f) : Vector3.zero, Vector3.one * scale);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = SpriteMat;
            sr.sortingOrder = order;
            sr.color = color ?? Color.white;
            return sr;
        }

        /// <summary>Flat textured quad lying on the ground (XZ), unlit alpha-blended.</summary>
        public static GameObject Decal(string name, Transform parent, Material mat, Vector3 pos, Vector2 size, float yRot = 0f)
        {
            var go = MeshObj(name, parent, Quad, mat, pos, new Vector3(size.x, size.y, 1f), new Vector3(90f, yRot, 0f), false);
            go.GetComponent<MeshRenderer>().receiveShadows = false;
            return go;
        }

        public static TextMeshPro Text(string name, Transform parent, string text, float size, Color color, Vector3 pos, bool outline = true)
        {
            var go = Node(name, parent, pos);
            var t = go.AddComponent<TextMeshPro>();
            t.font = Font;
            if (outline && FontOutlineMat != null) t.fontSharedMaterial = FontOutlineMat;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(8f, 2f);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.sortingOrder = 5;
            return t;
        }

        public static void NoShadows(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        public static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        public static Bounds RenderBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        public static T SaveAsset<T>(T obj, string path) where T : Object
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(obj, existing);
                // CopySerialized does not mark the asset dirty: without this SaveAssets can skip writing it to disk.
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }
            AssetDatabase.CreateAsset(obj, path);
            return obj;
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
