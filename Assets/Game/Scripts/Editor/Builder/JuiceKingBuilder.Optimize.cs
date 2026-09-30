using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Draw-call reduction for mobile: merges the non-animated pieces of composite props (juicers, counter, bin...) into
    /// one mesh per material, and flags static scenery for static batching.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        static int _combineSerial;
        static string _combineDir = "Combined/";

        /// <summary>
        /// Merge every plain MeshRenderer under <paramref name="parent"/> (except inside <paramref name="keep"/> subtrees)
        /// into one child mesh per material. Transforms stay relative to the parent, so parent animation (wobble, punch) still works.
        /// </summary>
        static void CombineUnder(Transform parent, params Transform[] keep)
        {
            var keepSet = new HashSet<Transform>();
            foreach (var k in keep)
                if (k != null) keepSet.Add(k);

            bool Kept(Transform t)
            {
                for (var p = t; p != null && p != parent; p = p.parent)
                    if (keepSet.Contains(p)) return true;
                return false;
            }

            var byMat = new Dictionary<Material, List<CombineInstance>>();
            var shadow = new Dictionary<Material, bool>();
            var consumed = new List<MeshRenderer>();
            var toLocal = parent.worldToLocalMatrix;
            foreach (var mr in parent.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (Kept(mr.transform) || !mr.gameObject.activeSelf) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                // Leave prefab instances (Kenney props) and text alone.
                if (PrefabUtility.IsPartOfPrefabInstance(mr.gameObject) || mr.GetComponent<TMPro.TextMeshPro>() != null) continue;
                var mesh = mf.sharedMesh;
                var mats = mr.sharedMaterials;
                var m = toLocal * mr.transform.localToWorldMatrix;
                for (int s = 0; s < mesh.subMeshCount && s < mats.Length; s++)
                {
                    var mat = mats[s];
                    if (mat == null) continue;
                    if (!byMat.TryGetValue(mat, out var list)) byMat[mat] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh = mesh, subMeshIndex = s, transform = m });
                    shadow[mat] = (shadow.TryGetValue(mat, out var sh) && sh) || mr.shadowCastingMode != ShadowCastingMode.Off;
                }
                consumed.Add(mr);
            }
            if (consumed.Count < 2) return;

            foreach (var kv in byMat)
            {
                var combined = new Mesh { name = parent.name + "_" + kv.Key.name, indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(kv.Value.ToArray(), true, true);
                combined.RecalculateBounds();
                var saved = SaveMesh(combined, _combineDir + parent.name + "_" + kv.Key.name.Replace('/', '_') + "_" + (_combineSerial++));
                var go = B.MeshObj("Combined_" + kv.Key.name, parent, saved, kv.Key, Vector3.zero, Vector3.one, null, shadow[kv.Key]);
                go.layer = parent.gameObject.layer;
            }

            // Remove the source renderers; delete objects that end up empty.
            foreach (var mr in consumed)
            {
                if (mr == null) continue;
                var go = mr.gameObject;
                var mf = go.GetComponent<MeshFilter>();
                Object.DestroyImmediate(mr);
                if (mf != null) Object.DestroyImmediate(mf);
                if (go.transform.childCount == 0 && go.GetComponents<Component>().Length == 1 && !keepSet.Contains(go.transform)) Object.DestroyImmediate(go);
            }
        }

        static void SetBatchingStatic(GameObject go, bool recursive = true)
        {
            var list = recursive ? go.GetComponentsInChildren<Transform>(true) : new[] { go.transform };
            foreach (var t in list)
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        /// <summary>Run after the scene is assembled.</summary>
        static void OptimizeScene(string sceneKey)
        {
            // Each scene owns its folder of merged meshes, so rebuilding one world never deletes the other's.
            _combineDir = "Combined/" + sceneKey + "/";
            string dir = Gen + "Meshes/" + _combineDir.TrimEnd('/');
            System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            foreach (var old in AssetDatabase.FindAssets("t:Mesh", new[] { dir }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(old));
            // Meshes from before per-scene folders sit directly in Combined/: no scene uses them after this rebuild.
            string legacy = Gen + "Meshes/Combined";
            if (sceneKey == "JuiceKing")
            foreach (var old in AssetDatabase.FindAssets("t:Mesh", new[] { legacy }))
            {
                var path = AssetDatabase.GUIDToAssetPath(old);
                if (System.IO.Path.GetDirectoryName(path).Replace('\\', '/') == legacy) AssetDatabase.DeleteAsset(path);
            }
            _combineSerial = 0;

            // Juicers: everything except the animated bits.
            foreach (var j in _stations.GetComponentsInChildren<Juicer>(true))
            {
                if (j is BerryPress bp)
                {
                    // Keep every animated part of the press separate.
                    var keep = new List<Transform> { bp.drum, bp.piston, bp.gaugeNeedle, bp.statusLight != null ? bp.statusLight.transform : null };
                    if (bp.pipeBlobs != null) keep.AddRange(bp.pipeBlobs);
                    if (bp.ringLights != null) foreach (var r in bp.ringLights) if (r != null) keep.Add(r.transform);
                    CombineUnder(j.body, keep.ToArray());
                }
                else CombineUnder(j.body, j.blades, j.liquid, j.statusLight != null ? j.statusLight.transform : null);
                var tray = j.transform.Find("Tray");
                if (tray != null) CombineUnder(tray, tray.Find("OutputPile"));
                if (j.hopper != null) CombineUnder(j.hopper);
                if (j.sign != null) CombineUnder(j.sign, (j as BerryPress)?.crown);
            }

            // Cake mixers and ovens: merge the static shell, keep what moves or swaps material.
            foreach (var m in _stations.GetComponentsInChildren<CakeMixer>(true))
                CombineUnder(m.transform.Find("Body"), m.head, m.bowl, m.flourSack, m.statusLight != null ? m.statusLight.transform : null);
            foreach (var o in _stations.GetComponentsInChildren<Oven>(true))
                CombineUnder(o.body, o.door, o.glow != null ? o.glow.transform : null, o.dial, o.timerFill, o.chimney);

            // Counter body (punches as one piece on upgrade), bin body, sign board.
            foreach (var c in _stations.GetComponentsInChildren<Counter>(true))
            {
                if (c.body != null) CombineUnder(c.body);
                var sign = c.transform.Find("Sign");
                if (sign != null) CombineUnder(sign);
            }
            foreach (var b in _stations.GetComponentsInChildren<TrashBin>(true))
                if (b.body != null) CombineUnder(b.body, b.lid != null ? b.lid.parent : null);

            // Field beds, mounds and leaves never move: static-batch them (fruit visuals and HP bars stay dynamic).
            foreach (var field in _stations.GetComponentsInChildren<Transform>(true))
            {
                string n = field.name;
                if (n == "Soil" || n.StartsWith("Frame") || n == "Mound" || n == "Leaf")
                    SetBatchingStatic(field.gameObject, false);
            }

            // Scenery on the plaza and around it.
            foreach (var n in new[] { "UpgradeStation", "CashPile" })
            {
                var t = _stations.Find(n);
                if (t == null) continue;
                foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.GetComponentInParent<ItemPile>() == null && r.GetComponentInParent<Zone>() == null) SetBatchingStatic(r.gameObject, false);
            }
        }
    }
}
