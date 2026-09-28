using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.LowLevel;

namespace JuiceKing.EditorTools
{
    public static partial class JuiceKingBuilder
    {
        const string Gen = "Assets/Game/Generated/";
        const string Prefabs = "Assets/Game/Prefabs/";
        public const float CharScale = 2.2f;

        static readonly string[] FruitKey = { "Orange", "Watermelon", "Pineapple" };
        static readonly string[] FruitModel = { "Food/orange", "Food/watermelon", "Food/pineapple" };

        // Shared assets created in BuildAssets()
        static Mesh _disc, _cup, _funnel, _crown, _arrow, _quadXZ;
        static Material _mWhite, _mDark, _mSteel, _mGold, _mWood, _mGlass, _mBill, _mRed;
        static Material[] _mJuice = new Material[3], _mSliceTop = new Material[3], _mSliceRind = new Material[3], _mFruitBody = new Material[3];
        static Material _mPadSolid, _mPadDashed, _mPadFill, _mPointer, _mParticle, _mBlob, _mWhiteSprite;
        static Material _mGrass, _mTiles, _mSoil, _mRoad, _mSidewalk, _mAwning, _mChain, _mOrangeBody;
        static Sprite _sRound, _sButton, _sCircle, _sRing, _sWhite, _sMoney, _sJuice, _sSaw, _sBag, _sSpeed, _sWorker, _sStar, _sHeart, _sSoundOn, _sSoundOff;
        static Sprite[] _sFruit = new Sprite[3];
        static RuntimeAnimatorController _controller;

        static StackItem[] _slicePrefabs = new StackItem[3], _juicePrefabs = new StackItem[3];
        static StackItem _moneyPrefab;
        static FloatingText _floatingText;
        static List<GameObject> _customerPrefabs = new List<GameObject>();

        static readonly Color[] SliceRind =
        {
            new Color(1f, 0.52f, 0.05f), new Color(0.2f, 0.52f, 0.22f), new Color(0.72f, 0.48f, 0.14f)
        };

        // ------------------------------------------------------------------ fonts

        static void BuildFont()
        {
            const string ttf = "Assets/Game/Art/Fonts/LilitaOne-Regular.ttf";
            const string path = Gen + "Fonts/LilitaOne SDF.asset";
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fontAsset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fontAsset.name = "LilitaOne SDF";
                Directory.CreateDirectory(Gen + "Fonts");
                AssetDatabase.CreateAsset(fontAsset, path);
                fontAsset.atlasTextures[0].name = "LilitaOne Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
                fontAsset.material.name = "LilitaOne Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                fontAsset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789$+-!?.,:%/'&x ");
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
            }

            const string outlinePath = Gen + "Fonts/LilitaOne Outline.mat";
            var outline = AssetDatabase.LoadAssetAtPath<Material>(outlinePath);
            if (outline == null)
            {
                outline = new Material(fontAsset.material) { name = "LilitaOne Outline" };
                AssetDatabase.CreateAsset(outline, outlinePath);
            }
            outline.shader = fontAsset.material.shader;
            outline.CopyPropertiesFromMaterial(fontAsset.material);
            outline.EnableKeyword("OUTLINE_ON");
            outline.SetFloat("_OutlineWidth", 0.22f);
            outline.SetColor("_OutlineColor", new Color(0.12f, 0.08f, 0.1f, 1f));
            outline.SetFloat("_FaceDilate", 0.18f);
            outline.SetFloat("_UnderlayOffsetY", -0.6f);
            EditorUtility.SetDirty(outline);

            B.Font = fontAsset;
            B.FontOutlineMat = outline;
        }

        // ------------------------------------------------------------------ meshes

        static Mesh SaveMesh(Mesh m, string name) => B.SaveAsset(m, Gen + "Meshes/" + name + ".asset");

        static void BuildMeshes()
        {
            // Unit disc: radius 0.5, height 1, pivot at bottom. Sub 0 = caps (textured), sub 1 = side.
            var g = new MeshGen();
            g.Cap(0.5f, 1f, 32, 0, true);
            g.Cap(0.5f, 0f, 32, 0, false);
            g.Side(0.5f, 0.5f, 0f, 1f, 32, 1);
            _disc = SaveMesh(g.ToMesh("Disc"), "Disc");

            // Cup: tapered, pivot at bottom.
            g = new MeshGen();
            g.Side(0.4f, 0.5f, 0f, 1f, 24, 0);
            g.Cap(0.4f, 0f, 24, 0, false);
            g.Cap(0.5f, 1f, 24, 0, true);
            _cup = SaveMesh(g.ToMesh("Cup"), "Cup");

            // Funnel (double sided, open both ends).
            g = new MeshGen();
            g.Side(0.35f, 1f, 0f, 1f, 24, 0);
            g.Side(0.35f, 1f, 0f, 1f, 24, 0, true);
            _funnel = SaveMesh(g.ToMesh("Funnel"), "Funnel");

            _crown = SaveMesh(CrownMesh(), "Crown");

            // Guide arrow pointing down: shaft + cone.
            g = new MeshGen();
            g.Side(0.18f, 0.18f, 0.55f, 1.25f, 16, 0);
            g.Cap(0.18f, 1.25f, 16, 0, true);
            g.Cone(0.45f, 0.6f, 0f, 20, 0, true);
            _arrow = SaveMesh(g.ToMesh("Arrow"), "Arrow");

            // Flat unit quad on XZ (for fill bars on pads).
            g = new MeshGen();
            g.Box(Vector3.zero, new Vector3(1f, 0.0001f, 1f), 0);
            _quadXZ = SaveMesh(g.ToMesh("QuadXZ"), "QuadXZ");
        }

        static Mesh CrownMesh()
        {
            var g = new MeshGen();
            g.Side(0.5f, 0.55f, 0f, 0.4f, 20, 0);
            g.Side(0.5f, 0.55f, 0f, 0.4f, 20, 0, true);
            var m = g.ToMesh("CrownBand");
            // Spikes merged via CombineMeshes.
            var combine = new List<CombineInstance> { new CombineInstance { mesh = m, transform = Matrix4x4.identity } };
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                var cg = new MeshGen();
                cg.Cone(0.16f, 0f, 0.45f, 10, 0, false);
                var cm = cg.ToMesh("spike");
                combine.Add(new CombineInstance
                {
                    mesh = cm,
                    transform = Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * 0.5f, 0.35f, Mathf.Sin(a) * 0.5f), Quaternion.identity, Vector3.one)
                });
            }
            var result = new Mesh { name = "Crown" };
            result.CombineMeshes(combine.ToArray(), true, true);
            result.RecalculateBounds();
            return result;
        }

        // ------------------------------------------------------------------ materials & sprites

        static void BuildMaterials()
        {
            _mWhite = MatLib.Lit("White", new Color(0.96f, 0.96f, 0.96f), 0.2f);
            _mDark = MatLib.Lit("Dark", new Color(0.2f, 0.2f, 0.24f), 0.2f);
            _mSteel = MatLib.Lit("Steel", new Color(0.75f, 0.78f, 0.82f), 0.55f, null, null, 0.4f);
            _mGold = MatLib.Lit("Gold", new Color(1f, 0.78f, 0.15f), 0.65f, null, null, 0.5f);
            _mRed = MatLib.Lit("Red", new Color(0.95f, 0.3f, 0.32f), 0.3f);
            _mOrangeBody = MatLib.Lit("SawOrange", new Color(1f, 0.5f, 0.1f), 0.35f);
            _mWood = MatLib.Lit("Wood", Color.white, 0.1f, MatLib.Tex("wood.png"));
            _mGlass = MatLib.Glass("Glass", new Color(0.8f, 0.93f, 1f, 0.28f));
            _mBill = MatLib.Lit("Bill", Color.white, 0.1f, MatLib.Tex("bill.png"));
            _mChain = MatLib.Lit("Chain", Color.white, 0.4f, MatLib.Tex("chain.png"), new Vector2(3f, 1f));
            _mAwning = MatLib.Lit("Awning", Color.white, 0.1f, MatLib.Tex("awning.png"), new Vector2(3f, 1f));

            _mGrass = MatLib.Lit("Ground_Grass", Color.white, 0.05f, MatLib.Tex("ground_grass.png"), new Vector2(14f, 14f));
            _mTiles = MatLib.Lit("Ground_Tiles", Color.white, 0.12f, MatLib.Tex("ground_tiles.png"), new Vector2(10f, 8f));
            _mSoil = MatLib.Lit("Ground_Soil", Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(2f, 2f));
            _mRoad = MatLib.Lit("Ground_Road", Color.white, 0.1f, MatLib.Tex("ground_road.png"), new Vector2(30f, 2f));
            _mSidewalk = MatLib.Lit("Ground_Sidewalk", new Color(0.85f, 0.85f, 0.83f), 0.1f, MatLib.Tex("ground_tiles.png"), new Vector2(40f, 1f));

            string[] sliceTex = { "slice_orange.png", "slice_watermelon.png", "slice_pineapple.png" };
            for (int i = 0; i < 3; i++)
            {
                _mJuice[i] = MatLib.Lit("Juice_" + FruitKey[i], Balance.JuiceColors[i], 0.75f);
                _mSliceTop[i] = MatLib.Lit("SliceTop_" + FruitKey[i], Color.white, 0.35f, MatLib.Tex(sliceTex[i]));
                _mSliceRind[i] = MatLib.Lit("SliceRind_" + FruitKey[i], SliceRind[i], 0.25f);
                _mFruitBody[i] = MatLib.Lit("MachineBody_" + FruitKey[i], Color.Lerp(Balance.FruitColors[i], Color.white, 0.15f), 0.35f);
            }

            _mPadSolid = MatLib.Sprite("Pad_Solid", MatLib.Tex("pad_solid.png"), Color.white);
            _mPadDashed = MatLib.Sprite("Pad_Dashed", MatLib.Tex("pad_dashed.png"), Color.white);
            _mPadFill = MatLib.Sprite("Pad_Fill", Texture2D.whiteTexture, new Color(0.35f, 0.95f, 0.45f, 0.55f));
            _mPointer = MatLib.Sprite("Pointer", MatLib.Tex("pointer.png"), new Color(1f, 1f, 1f, 0.9f));
            _mParticle = MatLib.Sprite("Particle", MatLib.Tex("fx_circle.png"), Color.white);
            _mBlob = MatLib.Sprite("BlobShadow", MatLib.Tex("blob_shadow.png"), Color.white);
            // URP's own sprite material batches per texture correctly (Sprites/Default does not under URP 17).
            _mWhiteSprite = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            B.SpriteMat = _mWhiteSprite;

            _sRound = MatLib.Spr("ui_round.png");
            _sButton = MatLib.Spr("ui_button.png");
            _sCircle = MatLib.Spr("ui_circle.png");
            _sRing = MatLib.Spr("ui_ring.png");
            _sWhite = MatLib.Spr("white.png");
            _sMoney = MatLib.Spr("icon_money.png");
            _sJuice = MatLib.Spr("icon_juice.png");
            _sSaw = MatLib.Spr("icon_saw.png");
            _sBag = MatLib.Spr("icon_bag.png");
            _sSpeed = MatLib.Spr("icon_speed.png");
            _sWorker = MatLib.Spr("icon_worker.png");
            _sStar = MatLib.Spr("icon_star.png");
            _sHeart = MatLib.Spr("icon_heart.png");
            _sSoundOn = MatLib.Spr("icon_sound_on.png");
            _sSoundOff = MatLib.Spr("icon_sound_off.png");
            _sFruit[0] = MatLib.Spr("icon_orange.png");
            _sFruit[1] = MatLib.Spr("icon_watermelon.png");
            _sFruit[2] = MatLib.Spr("icon_pineapple.png");
        }

        // ------------------------------------------------------------------ item prefabs

        static GameObject SliceVisual(int f, Transform parent, Vector3 pos, float diameter, float thickness, Vector3? euler = null)
        {
            return B.MeshObj("Slice", parent, _disc, new[] { _mSliceTop[f], _mSliceRind[f] }, pos,
                new Vector3(diameter, thickness, diameter), euler);
        }

        static void BuildItemPrefabs()
        {
            for (int f = 0; f < 3; f++)
            {
                // Fruit slice: a chunky round slice that stacks like coins.
                var root = new GameObject("Slice_" + FruitKey[f]);
                var si = root.AddComponent<StackItem>();
                si.type = ItemTypes.Slice((FruitKind)f);
                si.height = 0.13f;
                SliceVisual(f, root.transform, Vector3.zero, 0.46f, 0.12f);
                _slicePrefabs[f] = B.SavePrefab(root, Prefabs + "Items/Slice_" + FruitKey[f] + ".prefab").GetComponent<StackItem>();

                // Juice cup: tapered cup + lid + straw + fruit garnish.
                root = new GameObject("Juice_" + FruitKey[f]);
                si = root.AddComponent<StackItem>();
                si.type = ItemTypes.Juice((FruitKind)f);
                si.height = 0.37f;
                var v = B.Node("Visual", root.transform, Vector3.zero).transform;
                // Juice-coloured cup (top reads clearly from the high camera), white band, straw and a fruit garnish.
                B.MeshObj("Cup", v, _cup, _mJuice[f], Vector3.zero, new Vector3(0.28f, 0.3f, 0.28f));
                B.MeshObj("Band", v, _disc, new[] { _mWhite, _mWhite }, new Vector3(0f, 0.1f, 0f), new Vector3(0.264f, 0.08f, 0.264f));
                B.MeshObj("Rim", v, _disc, new[] { _mWhite, _mWhite }, new Vector3(0f, 0.292f, 0f), new Vector3(0.3f, 0.018f, 0.3f));
                B.MeshObj("Top", v, _disc, new[] { _mJuice[f], _mJuice[f] }, new Vector3(0f, 0.3f, 0f), new Vector3(0.26f, 0.012f, 0.26f));
                B.MeshObj("Straw", v, _disc, new[] { _mRed, _mRed }, new Vector3(0.04f, 0.26f, 0.02f), new Vector3(0.035f, 0.22f, 0.035f), new Vector3(0f, 0f, -16f));
                SliceVisual(f, v, new Vector3(-0.02f, 0.28f, -0.12f), 0.17f, 0.035f, new Vector3(-70f, 0f, 0f));
                _juicePrefabs[f] = B.SavePrefab(root, Prefabs + "Items/Juice_" + FruitKey[f] + ".prefab").GetComponent<StackItem>();
            }

            // Money bill.
            var m = new GameObject("Money");
            var ms = m.AddComponent<StackItem>();
            ms.type = ItemType.Money;
            ms.height = 0.07f;
            ms.value = 5;
            B.MeshObj("Bill", m.transform, B.Cube, _mBill, new Vector3(0f, 0.035f, 0f), new Vector3(0.52f, 0.06f, 0.27f));
            _moneyPrefab = B.SavePrefab(m, Prefabs + "Items/Money.prefab").GetComponent<StackItem>();

            // Floating text.
            var ftGo = new GameObject("FloatingText");
            var tmp = B.Text("Text", ftGo.transform, "+$10", 6f, Color.white, Vector3.zero);
            var ft = ftGo.AddComponent<FloatingText>();
            ft.text = tmp;
            _floatingText = B.SavePrefab(ftGo, Prefabs + "FX/FloatingText.prefab").GetComponent<FloatingText>();
        }

        // ------------------------------------------------------------------ characters

        /// <summary>Character root with a scaled, animated Kenney model.</summary>
        static GameObject CharacterBase(string name, string model, out Animator anim, out Transform modelRoot)
        {
            var root = new GameObject(name);
            var mdl = B.Model(model, root.transform, Vector3.zero, CharScale, 0f, "Model");
            modelRoot = mdl.transform;
            anim = mdl.GetComponent<Animator>();
            if (anim == null) anim = mdl.AddComponent<Animator>();
            anim.runtimeAnimatorController = _controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // Soft blob shadow for readability.
            B.Decal("Blob", root.transform, _mBlob, new Vector3(0f, 0.02f, 0f), new Vector2(1.1f, 1.1f));
            return root;
        }

        static Chainsaw BuildChainsaw(Transform parent, Vector3 localPos, float volume)
        {
            var saw = B.Node("Chainsaw", parent, localPos);
            var body = B.Box("Body", saw.transform, _mOrangeBody, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.24f, 0.36f));
            B.Box("Handle", saw.transform, _mDark, new Vector3(0f, 0.17f, -0.02f), new Vector3(0.05f, 0.05f, 0.26f));
            B.Box("HandleL", saw.transform, _mDark, new Vector3(0f, 0.1f, 0.1f), new Vector3(0.05f, 0.14f, 0.04f));
            B.Box("HandleR", saw.transform, _mDark, new Vector3(0f, 0.1f, -0.13f), new Vector3(0.05f, 0.14f, 0.04f));
            B.Box("Stripe", saw.transform, _mDark, new Vector3(0f, -0.04f, 0f), new Vector3(0.225f, 0.05f, 0.365f));
            var blade = B.Node("Blade", saw.transform, new Vector3(0f, -0.02f, 0.5f));
            B.Box("Bar", blade.transform, _mSteel, Vector3.zero, new Vector3(0.05f, 0.12f, 0.68f));
            B.MeshObj("Tip", blade.transform, B.Cylinder, _mSteel, new Vector3(0f, 0f, 0.34f), new Vector3(0.12f, 0.025f, 0.12f), new Vector3(0f, 0f, 90f));
            var chain = B.Box("Chain", blade.transform, _mChain, Vector3.zero, new Vector3(0.035f, 0.16f, 0.72f));
            var c = saw.AddComponent<Chainsaw>();
            c.bladeVisual = blade.transform;
            c.chainRenderer = chain.GetComponent<MeshRenderer>();
            var src = saw.AddComponent<AudioSource>();
            src.playOnAwake = true;
            src.loop = true;
            src.spatialBlend = 0f;
            c.audioSource = src;
            c.volumeScale = volume;
            return c;
        }

        static GameObject BuildPlayerObject()
        {
            var root = CharacterBase("Player", "Characters/character-male-a", out var anim, out var model);
            root.tag = "Player";

            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.75f, 0f);
            cc.height = 1.5f;
            cc.radius = 0.38f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.04f;

            // Back stack (in a basket).
            var stack = B.Node("StackRoot", root.transform, new Vector3(0f, 0.72f, -0.52f));
            var basket = B.Model("Market/shopping-basket", root.transform, new Vector3(0f, 0.5f, -0.5f), 1.5f, 90f, "Basket");
            var carrier = root.AddComponent<Carrier>();
            carrier.stackRoot = stack.transform;
            carrier.isPlayer = true;
            carrier.capacity = Balance.BagCapacity(0);
            carrier.columns = 2;
            carrier.columnSpacing = 0.44f;

            // Crown for the Juice King.
            var head = B.Find(model, "head");
            var crownParent = head != null ? head : model;
            var crown = B.MeshObj("Crown", crownParent, _crown, _mGold, Vector3.zero, Vector3.one * 0.19f);
            crown.transform.position = root.transform.position + new Vector3(0f, 1.47f, 0f);
            crown.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

            // Chainsaw held in front-right, attached to the torso so it bobs with the body.
            var torso = B.Find(model, "torso");
            var saw = BuildChainsaw(root.transform, new Vector3(0.3f, 0.62f, 0.42f), 1f);
            if (torso != null) saw.transform.SetParent(torso, true);
            saw.carrier = carrier;
            saw.owner = root.transform;
            saw.range = 1.4f;

            var pc = root.AddComponent<PlayerController>();
            pc.cc = cc;
            pc.carrier = carrier;
            pc.saw = saw;

            var ca = root.AddComponent<CharacterAnim>();
            ca.animator = anim;
            ca.alwaysHoldRight = true;
            ca.animSpeedRef = 5.2f;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return root;
        }

        static void BuildCustomerPrefabs()
        {
            _customerPrefabs.Clear();
            string[] models =
            {
                "character-female-a", "character-female-b", "character-female-c", "character-female-d", "character-female-e", "character-female-f",
                "character-male-b", "character-male-c", "character-male-d", "character-male-e", "character-male-f"
            };
            foreach (var mdl in models)
            {
                var root = CharacterBase("Customer_" + mdl.Replace("character-", ""), "Characters/" + mdl, out var anim, out _);
                var hands = B.Node("Hands", root.transform, new Vector3(0f, 0.62f, 0.42f));
                var carrier = root.AddComponent<Carrier>();
                carrier.stackRoot = hands.transform;
                carrier.capacity = 4;
                carrier.columns = 1;
                carrier.canCollectLoose = false;
                carrier.interactsWithZones = false;
                carrier.swayAmount = 0.03f;

                var bubble = BuildBubble(root.transform, new Vector3(0f, 2.15f, 0f));
                var cust = root.AddComponent<Customer>();
                cust.hands = carrier;
                cust.bubble = bubble;
                cust.speed = Random.Range(2.6f, 3.1f);

                var ca = root.AddComponent<CharacterAnim>();
                ca.animator = anim;
                ca.hands = carrier;
                ca.animSpeedRef = 3.2f;

                _customerPrefabs.Add(B.SavePrefab(root, Prefabs + "Customers/" + root.name + ".prefab"));
            }
        }

        static OrderBubble BuildBubble(Transform parent, Vector3 pos)
        {
            var go = B.Node("OrderBubble", parent, pos, null, Vector3.one * 0.8f);
            go.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", go.transform, _sRound, Vector3.zero, 1f, false, 10, Color.white);
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(1.25f, 0.72f);
            var tail = B.Sprite("Tail", go.transform, _sWhite, new Vector3(0f, -0.36f, 0.001f), 0.2f, false, 10);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var icon = B.Sprite("Icon", go.transform, _sFruit[0], new Vector3(-0.25f, 0.02f, -0.01f), 0.22f, false, 11);
            var count = B.Text("Count", go.transform, "1", 5f, new Color(0.25f, 0.2f, 0.2f), new Vector3(0.3f, 0.02f, -0.01f), false);
            count.GetComponent<MeshRenderer>().sortingOrder = 12;
            var happy = B.Sprite("Happy", go.transform, _sHeart, new Vector3(0f, 0.02f, -0.01f), 0.22f, false, 11);
            happy.gameObject.SetActive(false);
            var ob = go.AddComponent<OrderBubble>();
            ob.icon = icon;
            ob.countText = count;
            ob.happy = happy;
            ob.background = bg;
            return ob;
        }
    }
}
