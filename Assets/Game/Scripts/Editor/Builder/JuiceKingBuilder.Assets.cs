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

        static readonly string[] FruitKey = { "Orange", "Watermelon", "Pineapple", "Coconut", "Mango", "Banana", "Papaya", "Strawberry", "Raspberry", "Blueberry", "Cranberry" };
        const int FruitN = ItemTypes.FruitCount;
        static readonly string[] FruitModel = { "Food/orange", "Food/watermelon", "Food/pineapple" };

        // Shared assets created in BuildAssets()
        static Mesh _disc, _cup, _funnel, _crown, _arrow, _quadXZ;
        static Material _mWhite, _mDark, _mSteel, _mGold, _mWood, _mGlass, _mBill, _mRed;
        static Material[] _mJuice = new Material[FruitN], _mSliceTop = new Material[FruitN], _mSliceRind = new Material[FruitN], _mFruitBody = new Material[FruitN];
        static Material _mPadSolid, _mPadDashed, _mPadFill, _mPointer, _mParticle, _mBlob, _mWhiteSprite;
        static Material _mGrass, _mTiles, _mSoil, _mRoad, _mSidewalk, _mAwning, _mChain, _mOrangeBody;
        static Sprite _sRound, _sButton, _sCircle, _sRing, _sWhite, _sMoney, _sJuice, _sSaw, _sBag, _sSpeed, _sWorker, _sStar, _sHeart, _sSoundOn, _sSoundOff;
        static Sprite _sCoin, _sCash2x, _sTurbo, _sMoneyBag, _sTrash, _sAd, _sPlay, _sCrown, _sPrice;
        static Sprite _sPanel, _sPill, _sCircleBtn, _sRingThick, _sShine, _sRays, _sGlow, _sSneaker;
        // Sprites cut from the UI atlases (see UIKit).
        static Sprite _uCoin, _uCoins, _uMoneyBag, _uLightning, _uCrown, _uStar, _uChef, _uFarmer, _uWaiter, _uBackpack, _uChainsaw,
            _uMachine, _uTable, _uTools, _uRecycle, _uLock, _uGift, _uHeartBubble, _uExclBubble, _uX, _uCheck,
            _uBtnGreen, _uBtnYellow, _uBtnBlue, _uBtnOrange, _uBtnRed, _uBtnGrey, _uWatchAd, _uGear, _uRibbon, _uPlank, _uNote, _uCard,
            _uPanelAwning, _uPanelPlain, _uToggleOn, _uToggleOff, _uHeroCoins, _uHeroMoneyBag, _uHeroCoinBox, _uArrowUp,
            _uRocket, _uWarning, _uBtnCream, _uBadgeAd, _uSqWood, _uSqBlue, _uSqGreen, _uSqOrange, _uRibbonYellow, _uBubbleWhite, _uPanelGold,
            _uPanelRope, _uPanelWin;
        static Material[] _mFx = new Material[(int)FxShape.Count];
        static Material _mWater, _mPath, _mCloud, _mHedge, _mBinGreen, _mBinDark, _mAnimal, _mDuckBody, _mDuckBeak, _mLily, _mStone, _mHay, _mRoof, _mWhiteWall, _mPink, _mBlue, _mButterfly;
        static Sprite[] _sFruit = new Sprite[FruitN];
        static Sprite[] _sJuiceIcons = new Sprite[FruitN];
        static Sprite _sParcel;
        static Sprite _sTruck, _sClock, _sSprout, _sMoon, _sCups, _sGlowRing, _sRingProgress;
        static Material _mShell, _mCream, _mUmbrella, _mCherry;
        static RuntimeAnimatorController _controller;

        static StackItem[] _slicePrefabs = new StackItem[FruitN], _juicePrefabs = new StackItem[FruitN];
        static StackItem[] _batterPrefabs = new StackItem[FruitN], _cakePrefabs = new StackItem[FruitN];
        static List<GameObject> _cakeCustomerPrefabs = new List<GameObject>();
        // Berry Blast materials and icons.
        static Material[] _mBerry = new Material[FruitN];
        static Material _mBerryLeaf, _mSponge, _mCreamWhite, _mCrust, _mTin, _mPunnet, _mPunnetPink, _mPunnetBlue, _mIce, _mLime, _mGoldLid;
        static Sprite[] _sCakeIcons = new Sprite[FruitN];
        static Sprite _sApple, _sFox, _sOven, _sMixer, _sCakeShop, _sTicket, _sNoAds;
        static StackItem _moneyPrefab;
        static FloatingText _floatingText;
        static List<GameObject> _customerPrefabs = new List<GameObject>();

        static readonly Color[] SliceRind =
        {
            new Color(1f, 0.52f, 0.05f), new Color(0.2f, 0.52f, 0.22f), new Color(0.72f, 0.48f, 0.14f),
            new Color(0.45f, 0.28f, 0.14f), new Color(1f, 0.62f, 0.15f), new Color(0.98f, 0.9f, 0.55f), new Color(1f, 0.55f, 0.25f),
            new Color(0.9f, 0.12f, 0.2f), new Color(0.85f, 0.15f, 0.4f), new Color(0.25f, 0.3f, 0.75f), new Color(0.7f, 0.05f, 0.15f)
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
            _mTiles = MatLib.Lit("Ground_Tiles", Color.white, 0.12f, MatLib.Tex("ground_tiles.png"), new Vector2(6f, 4.1f));
            _mSoil = MatLib.Lit("Ground_Soil", Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(2f, 2f));
            _mRoad = MatLib.Lit("Ground_Road", Color.white, 0.1f, MatLib.Tex("ground_road.png"), new Vector2(30f, 2f));
            _mSidewalk = MatLib.Lit("Ground_Sidewalk", new Color(0.86f, 0.86f, 0.86f), 0.1f, MatLib.Tex("ground_tiles.png"), new Vector2(30f, 0.8f));

            string[] sliceTex = { "slice_orange.png", "slice_watermelon.png", "slice_pineapple.png", "slice_coconut.png", "slice_mango.png", "slice_banana.png", "slice_papaya.png",
                "skin_strawberry.png", "", "", "" };
            for (int i = 0; i < FruitN; i++)
            {
                _mJuice[i] = MatLib.Lit("Juice_" + FruitKey[i], Balance.JuiceColors[i], 0.75f);
                _mSliceTop[i] = MatLib.Lit("SliceTop_" + FruitKey[i], Color.white, 0.35f, MatLib.Tex(sliceTex[i]));
                _mSliceRind[i] = MatLib.Lit("SliceRind_" + FruitKey[i], SliceRind[i], 0.25f);
                _mFruitBody[i] = MatLib.Lit("MachineBody_" + FruitKey[i], Color.Lerp(Balance.FruitColors[i], Color.white, 0.15f), 0.35f);
            }

            _mPadSolid = MatLib.Sprite("Pad_Solid", MatLib.Tex("pad_solid.png"), Color.white);
            _mPadDashed = MatLib.Sprite("Pad_Dashed", MatLib.Tex("pad_dashed.png"), Color.white);
            _mPadFill = MatLib.Sprite("Pad_Fill", Texture2D.whiteTexture, new Color(0.35f, 0.95f, 0.45f, 0.55f));
            _mPointer = MatLib.Sprite("Pointer", MatLib.Tex("pointer.png"), Color.white);
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
            _sCoin = UIKit.Get("icon_coin") ?? MatLib.Spr("icon_coin.png");
            _sCash2x = MatLib.Spr("icon_cash2x.png");
            _sTurbo = MatLib.Spr("icon_turbo.png");
            _sMoneyBag = MatLib.Spr("icon_moneybag.png");
            _sTrash = MatLib.Spr("icon_trash.png");
            _sAd = MatLib.Spr("icon_ad.png");
            _sPlay = MatLib.Spr("icon_play.png");
            _sCrown = UIKit.Get("icon_crown") ?? MatLib.Spr("icon_crown.png");
            _sPrice = MatLib.Spr("icon_price.png");
            _sPanel = MatLib.Spr("ui_panel.png");
            _sPill = MatLib.Spr("ui_pill.png");
            _sCircleBtn = MatLib.Spr("ui_circle_btn.png");
            _sRingThick = MatLib.Spr("ui_ring_thick.png");
            _sShine = MatLib.Spr("ui_shine.png");
            _sRays = MatLib.Spr("ui_rays.png");
            _sGlow = MatLib.Spr("ui_glow.png");
            _sSneaker = MatLib.Spr("icon_sneaker.png");

            _uCoin = UIKit.Get("icon_coin");
            _uCoins = UIKit.Get("icon_coins");
            _uMoneyBag = UIKit.Get("icon_coinsack");
            _uLightning = UIKit.Get("icon_lightning_orange");
            _uRocket = UIKit.Get("icon_rocket");
            _uCrown = UIKit.Get("icon_crown");
            _uStar = UIKit.Get("icon_star");
            _uChef = UIKit.Get("icon_recipe");
            _uFarmer = UIKit.Get("icon_farmer");
            _uWaiter = UIKit.Get("icon_girl");
            _uBackpack = UIKit.Get("icon_backpack");
            _uChainsaw = UIKit.Get("icon_chainsaw");
            _uMachine = UIKit.Get("icon_machine");
            _uTable = UIKit.Get("icon_table");
            _uTools = UIKit.Get("icon_tools");
            _uRecycle = UIKit.Get("icon_recycle");
            _uLock = UIKit.Get("icon_lock");
            _uGift = UIKit.Get("icon_gift");
            _uArrowUp = UIKit.Get("icon_arrow_up");
            _uWarning = UIKit.Get("icon_warning");
            _uHeartBubble = null;
            _uExclBubble = UIKit.Get("icon_excl");
            _uX = UIKit.Get("btn_x");
            _uCheck = UIKit.Get("btn_check");
            _uBtnGreen = UIKit.Get("btn_green");
            _uBtnYellow = UIKit.Get("btn_orange");
            _uBtnBlue = UIKit.Get("btn_blue");
            _uBtnOrange = UIKit.Get("btn_orange");
            _uBtnRed = UIKit.Get("btn_red");
            _uBtnGrey = UIKit.Get("btn_dark");
            _uBtnCream = UIKit.Get("btn_cream");
            _uWatchAd = UIKit.Get("icon_videoad");
            _uBadgeAd = UIKit.Get("badge_ad");
            _uGear = UIKit.Get("icon_gear");
            _uSqWood = UIKit.Get("sq_wood");
            _uSqBlue = UIKit.Get("sq_blue");
            _uSqGreen = UIKit.Get("sq_green");
            _uSqOrange = UIKit.Get("sq_orange");
            _uRibbon = UIKit.Get("ribbon_red");
            _uRibbonYellow = UIKit.Get("ribbon_yellow");
            _uPlank = UIKit.Get("plank_sign");
            _uNote = UIKit.Get("pill_cream");
            _uCard = UIKit.Get("bubble_cream");
            _uBubbleWhite = UIKit.Get("bubble_white");
            _uPanelAwning = UIKit.Get("panel_awning");
            _uPanelPlain = UIKit.Get("panel_plain");
            _uPanelGold = UIKit.Get("panel_gold");
            // Hi-res background panels (Atlas5) replace the older panels, cards and planks everywhere when cut.
            Sprite A5(string n, Sprite fallback)
            {
                var s = UIKit.Get(n);
                return s != null ? s : fallback;
            }
            _uPanelAwning = A5("p_awning", _uPanelAwning);
            _uPanelPlain = A5("p_plank", _uPanelPlain);
            _uPanelGold = A5("p_red", _uPanelGold);
            _uPanelRope = A5("p_rope", _uPanelPlain);
            _uPanelWin = A5("p_gold", _uPanelGold);
            _uPlank = A5("p_sign_wood", _uPlank);
            _uCard = A5("p_card_wood", _uCard);
            _uNote = A5("p_card", _uNote);
            _uToggleOn = UIKit.Get("toggle_on");
            _uToggleOff = UIKit.Get("toggle_off");
            _uHeroCoins = UIKit.Get("hero_coins_big");
            _uHeroMoneyBag = UIKit.Get("hero_moneybag");
            _uHeroCoinBox = UIKit.Get("hero_coinbox");

            string[] fxTex = { "fx_circle.png", "fx_star.png", "fx_heart.png", "fx_ring.png", "fx_drop.png", "fx_leaf.png", "fx_sparkle.png", "fx_coin.png", "fx_splat.png" };
            for (int i = 0; i < fxTex.Length; i++)
                _mFx[i] = MatLib.Sprite("Fx_" + ((FxShape)i), MatLib.Tex(fxTex[i]), Color.white);

            // Pond: stylized water shader (waves, caustics, sparkles).
            _mWater = MatLib.Water("Water", new Color(0.46f, 0.86f, 0.95f), new Color(0.16f, 0.52f, 0.78f), 0.35f, 0.004f, 0.7f);
            _mPath = MatLib.Lit("Ground_Path", Color.white, 0.05f, MatLib.Tex("ground_path.png"), new Vector2(1f, 3f));
            _mCloud = MatLib.Sprite("CloudShadow", MatLib.Tex("cloud_shadow.png"), new Color(1f, 1f, 1f, 0.4f));
            _mStraw = MatLib.Lit("StrawHat", new Color(0.95f, 0.8f, 0.4f), 0.1f);
            _mHedge = MatLib.Lit("Hedge", new Color(0.33f, 0.66f, 0.3f), 0.15f);
            _mBinGreen = MatLib.Lit("BinGreen", new Color(0.32f, 0.72f, 0.45f), 0.35f);
            _mBinDark = MatLib.Lit("BinDark", new Color(0.22f, 0.52f, 0.33f), 0.35f);
            _mAnimal = MatLib.Lit("Animals", Color.white, 0.15f, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/ithappy/Textures/Texture.png"));
            _mDuckBody = MatLib.Lit("DuckBody", new Color(1f, 0.93f, 0.45f), 0.3f);
            _mDuckBeak = MatLib.Lit("DuckBeak", new Color(1f, 0.55f, 0.15f), 0.3f);
            _mLily = MatLib.Lit("LilyPad", new Color(0.35f, 0.72f, 0.3f), 0.3f);
            _mStone = MatLib.Lit("Stone", new Color(0.72f, 0.72f, 0.74f), 0.15f);
            _mHay = MatLib.Lit("Hay", new Color(0.95f, 0.8f, 0.42f), 0.05f);
            _mRoof = MatLib.Lit("Roof", new Color(0.86f, 0.33f, 0.3f), 0.2f);
            _mWhiteWall = MatLib.Lit("WhiteWall", new Color(0.98f, 0.95f, 0.9f), 0.1f);
            _mPink = MatLib.Lit("Pink", new Color(1f, 0.55f, 0.7f), 0.3f);
            _mBlue = MatLib.Lit("Blue", new Color(0.35f, 0.62f, 1f), 0.3f);
            _mButterfly = MatLib.Sprite("Butterfly", MatLib.Tex("fx_heart.png"), Color.white);

            MatLib.StylizeAll();
            KenneyFoliageWind();

            _sFruit[0] = MatLib.Spr("icon_orange.png");
            _sFruit[1] = MatLib.Spr("icon_watermelon.png");
            _sFruit[2] = MatLib.Spr("icon_pineapple.png");
            for (int i = 0; i < ArtGen.TropicalFruitIcons.Length; i++) _sFruit[3 + i] = MatLib.Spr(ArtGen.TropicalFruitIcons[i]);
            for (int i = 0; i < FruitN; i++) _sJuiceIcons[i] = MatLib.Spr(ArtGen.JuiceIconFile(i));
            _sTruck = MatLib.Spr("icon_truck.png");
            _sParcel = MatLib.Spr("icon_parcel.png");
            _sClock = MatLib.Spr("icon_clock.png");
            _sSprout = MatLib.Spr("icon_sprout.png");
            _sMoon = MatLib.Spr("icon_moon.png");
            _sCups = MatLib.Spr("icon_cups.png");
            _sGlowRing = MatLib.Spr("glow_ring.png");
            _sRingProgress = MatLib.Spr("ring_progress.png");
            _mShell = MatLib.Lit("CoconutShell", new Color(0.5f, 0.31f, 0.16f), 0.15f);
            _mCream = MatLib.Lit("Cream", new Color(1f, 0.98f, 0.93f), 0.35f);
            _mUmbrella = MatLib.Lit("Umbrella", new Color(1f, 0.4f, 0.55f), 0.3f);
            _mCherry = MatLib.Lit("Cherry", new Color(0.9f, 0.1f, 0.18f), 0.7f);

            // ---- Berry Blast
            // Berry icons come from the Berry Blast UI atlas (Atlas4) when it has been cut; the painted ones are the fallback.
            string[] berryUi = { "b_icon_strawberry", "b_icon_raspberry", "b_icon_blueberry", "b_icon_cranberry" };
            for (int i = 0; i < ArtGen.BerryFruitIcons.Length; i++)
            {
                var u = i < berryUi.Length ? UIKit.Get(berryUi[i]) : null;
                _sFruit[7 + i] = u != null ? u : MatLib.Spr(ArtGen.BerryFruitIcons[i]);
            }
            for (int i = 7; i < FruitN; i++) _sCakeIcons[i] = MatLib.Spr(ArtGen.CakeIconFile(i));
            var atlasApple = UIKit.Get("b_icon_apple");
            _sApple = atlasApple != null ? atlasApple : MatLib.Spr("icon_golden_apple.png");
            _sTicket = MatLib.Spr(ArtGen.TicketIcon);
            _sNoAds = MatLib.Spr(ArtGen.NoAdsIcon);
            _sFox = MatLib.Spr("icon_fox.png");
            _sOven = MatLib.Spr("icon_oven.png");
            _sMixer = MatLib.Spr("icon_mixer.png");
            _sCakeShop = MatLib.Spr("icon_cakeshop.png");
            _mBerry[7] = MatLib.Lit("Berry_Strawberry", Color.white, 0.55f, MatLib.Tex("skin_strawberry.png"));
            _mBerry[8] = MatLib.Lit("Berry_Raspberry", new Color(0.88f, 0.2f, 0.42f), 0.5f);
            _mBerry[9] = MatLib.Lit("Berry_Blueberry", new Color(0.28f, 0.33f, 0.72f), 0.35f);
            _mBerry[10] = MatLib.Lit("Berry_Cranberry", new Color(0.75f, 0.06f, 0.16f), 0.8f);
            _mBerryLeaf = MatLib.Lit("BerryLeaf", new Color(0.3f, 0.66f, 0.28f), 0.25f);
            _mSponge = MatLib.Lit("CakeSponge", new Color(1f, 0.86f, 0.58f), 0.1f);
            _mCreamWhite = MatLib.Lit("CakeCream", new Color(1f, 0.98f, 0.95f), 0.45f);
            _mCrust = MatLib.Lit("TartCrust", new Color(0.9f, 0.62f, 0.3f), 0.2f);
            _mTin = MatLib.Lit("BakingTin", new Color(0.82f, 0.84f, 0.88f), 0.7f, null, null, 0.6f);
            _mPunnet = MatLib.Lit("Punnet", new Color(0.96f, 0.92f, 0.82f), 0.1f);
            _mPunnetPink = MatLib.Lit("PunnetPink", new Color(1f, 0.7f, 0.8f), 0.15f);
            _mPunnetBlue = MatLib.Lit("PunnetBlue", Color.white, 0.15f, MatLib.Tex("gingham.png"), new Vector2(2f, 1f));
            _mIce = MatLib.Glass("IceCube", new Color(0.9f, 0.97f, 1f, 0.55f));
            _mLime = MatLib.Lit("Lime", new Color(0.55f, 0.85f, 0.3f), 0.4f);
            _mGoldLid = MatLib.Lit("GoldLid", new Color(0.95f, 0.78f, 0.35f), 0.7f, null, null, 0.6f);
        }

        /// <summary>Kenney leaf and grass materials sway in the wind (the stylized shader weights sway by height).</summary>
        static void KenneyFoliageWind()
        {
            foreach (var (name, wind) in new[] { ("Nature_leafsGreen", 0.8f), ("Nature_grass", 1.6f), ("Furniture_plant", 0.6f) })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(MatLib.Dir + "Kenney/" + name + ".mat");
                if (m == null || !m.HasProperty("_Wind")) continue;
                m.SetFloat("_Wind", wind);
                EditorUtility.SetDirty(m);
            }
        }

        /// <summary>Upgrade icon lookup shared by both worlds (keys match UpgradeDef.icon).</summary>
        static void UpgradeIconTable(out string[] keys, out Sprite[] sprites)
        {
            var map = new (string, Sprite)[]
            {
                ("saw", _uChainsaw ? _uChainsaw : _sSaw), ("bag", _uBackpack ? _uBackpack : _sBag), ("speed", _uLightning ? _uLightning : _sSpeed),
                ("recipe", _uChef ? _uChef : _sPrice), ("counter", _uTable ? _uTable : _sStar), ("yield", _sFruit[4]), ("berryyield", _sFruit[7]), ("regrow", _sSprout),
                ("mixspeed", _uRocket ? _uRocket : _sTurbo), ("mixout", _sCups), ("mixcap", _uMachine ? _uMachine : _sJuice),
                ("truckreward", _uCoins ? _uCoins : _sCash2x), ("trucksize", _sTruck), ("truckfreq", _sClock), ("premium", _uCrown ? _uCrown : _sCrown),
                ("carryspeed", _sSneaker), ("wspeed", _uFarmer ? _uFarmer : _sWorker), ("wcarry", _uWaiter ? _uWaiter : _sWorker), ("offline", _sMoon),
            };
            keys = new string[map.Length];
            sprites = new Sprite[map.Length];
            for (int i = 0; i < map.Length; i++)
            {
                keys[i] = map[i].Item1;
                sprites[i] = map[i].Item2;
            }
        }

        // ------------------------------------------------------------------ item prefabs

        static GameObject SliceVisual(int f, Transform parent, Vector3 pos, float diameter, float thickness, Vector3? euler = null)
        {
            return B.MeshObj("Slice", parent, _disc, new[] { _mSliceTop[f], _mSliceRind[f] }, pos,
                new Vector3(diameter, thickness, diameter), euler);
        }

        static void BuildItemPrefabs()
        {
            for (int f = 0; f < FruitN; f++)
            {
                // Fruit slice: a chunky round slice that stacks like coins.
                var root = new GameObject("Slice_" + FruitKey[f]);
                var si = root.AddComponent<StackItem>();
                si.type = ItemTypes.Slice((FruitKind)f);
                si.height = 0.13f;
                // Tropical pieces differ in shape as well as colour: chunky coconut, flat mango cheek, small banana coin.
                float dia = f == 3 ? 0.44f : f == 5 ? 0.36f : 0.46f;
                float thick = f == 3 ? 0.12f : f == 5 ? 0.11f : 0.12f;
                if (f >= 7)
                {
                    si.height = 0.15f;
                    BerryPieceVisual(f, root.transform);
                }
                else SliceVisual(f, root.transform, Vector3.zero, dia, thick);
                // Shell dome kept inside the piece's own height so stacked pieces don't poke into each other.
                if (f == 3) B.MeshObj("Shell", root.transform, B.Sphere, _mShell, new Vector3(0f, 0.035f, 0f), new Vector3(0.42f, 0.07f, 0.42f));
                _slicePrefabs[f] = B.SavePrefab(root, Prefabs + "Items/Slice_" + FruitKey[f] + ".prefab").GetComponent<StackItem>();

                // Juice cup: tapered cup + lid + straw + fruit garnish.
                root = new GameObject("Juice_" + FruitKey[f]);
                si = root.AddComponent<StackItem>();
                si.type = ItemTypes.Juice((FruitKind)f);
                si.height = 0.37f;
                var v = B.Node("Visual", root.transform, Vector3.zero).transform;
                if (f < 3) TakeawayCupVisual(f, v);
                else if (f < 7) TropicalCupVisual(f, v);
                else BerryCupVisual(f, v);
                _juicePrefabs[f] = B.SavePrefab(root, Prefabs + "Items/Juice_" + FruitKey[f] + ".prefab").GetComponent<StackItem>();

                if (f < 7) continue;
                // Berry Cake Shop: batter in a baking tin, and the finished cake.
                root = new GameObject("Batter_" + FruitKey[f]);
                si = root.AddComponent<StackItem>();
                si.type = ItemTypes.Batter((FruitKind)f);
                si.height = 0.16f;
                BatterVisual(f, B.Node("Visual", root.transform, Vector3.zero).transform);
                _batterPrefabs[f] = B.SavePrefab(root, Prefabs + "Items/Batter_" + FruitKey[f] + ".prefab").GetComponent<StackItem>();

                root = new GameObject("Cake_" + FruitKey[f]);
                si = root.AddComponent<StackItem>();
                si.type = ItemTypes.Cake((FruitKind)f);
                si.height = 0.32f;
                CakeVisual(f, B.Node("Visual", root.transform, Vector3.zero).transform);
                _cakePrefabs[f] = B.SavePrefab(root, Prefabs + "Items/Cake_" + FruitKey[f] + ".prefab").GetComponent<StackItem>();
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

        /// <summary>Juice-coloured cup (top reads clearly from the high camera), white band, straw and a fruit garnish.</summary>
        static void TakeawayCupVisual(int f, Transform v)
        {
            B.MeshObj("Cup", v, _cup, _mJuice[f], Vector3.zero, new Vector3(0.28f, 0.3f, 0.28f));
            B.MeshObj("Band", v, _disc, new[] { _mWhite, _mWhite }, new Vector3(0f, 0.1f, 0f), new Vector3(0.264f, 0.08f, 0.264f));
            B.MeshObj("Rim", v, _disc, new[] { _mWhite, _mWhite }, new Vector3(0f, 0.292f, 0f), new Vector3(0.3f, 0.018f, 0.3f));
            B.MeshObj("Top", v, _disc, new[] { _mJuice[f], _mJuice[f] }, new Vector3(0f, 0.3f, 0f), new Vector3(0.26f, 0.012f, 0.26f));
            B.MeshObj("Straw", v, _disc, new[] { _mRed, _mRed }, new Vector3(0.04f, 0.26f, 0.02f), new Vector3(0.035f, 0.22f, 0.035f), new Vector3(0f, 0f, -16f));
            SliceVisual(f, v, new Vector3(-0.02f, 0.28f, -0.12f), 0.17f, 0.035f, new Vector3(-70f, 0f, 0f));
        }

        /// <summary>
        /// Each tropical drink has its own vessel so stacks read at a glance: shell, tumbler, shake glass, goblet.
        /// Every vessel stays within the stacking slot (about 0.3 m wide, 0.37 m tall) so trays and counters stack cleanly.
        /// </summary>
        static void TropicalCupVisual(int f, Transform v)
        {
            var juice = _mJuice[f];
            switch ((FruitKind)f)
            {
                case FruitKind.Coconut:
                    // Half coconut shell with milk, a straw and a tiny umbrella.
                    // Coconut-shell cup: a tapered brown shell with a rounded base, a white flesh rim and milk on top.
                    // Cup-shaped (not a ball) so a tray of them stacks into neat rows with white tops.
                    B.MeshObj("Base", v, B.Sphere, _mShell, new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.12f, 0.22f));
                    B.MeshObj("Shell", v, _cup, _mShell, new Vector3(0f, 0.02f, 0f), new Vector3(0.26f, 0.24f, 0.26f));
                    B.MeshObj("Flesh", v, _disc, new[] { _mCream, _mCream }, new Vector3(0f, 0.25f, 0f), new Vector3(0.275f, 0.03f, 0.275f));
                    B.MeshObj("Milk", v, _disc, new[] { juice, juice }, new Vector3(0f, 0.262f, 0f), new Vector3(0.22f, 0.01f, 0.22f));
                    B.MeshObj("Straw", v, _disc, new[] { _mBlue, _mBlue }, new Vector3(0.04f, 0.25f, 0.02f), new Vector3(0.03f, 0.1f, 0.03f), new Vector3(0f, 0f, -18f));
                    B.MeshObj("Umbrella", v, MakeUmbrella(), _mUmbrella, new Vector3(-0.05f, 0.32f, -0.03f), new Vector3(0.12f, 0.04f, 0.12f), new Vector3(0f, 0f, 12f));
                    break;
                case FruitKind.Mango:
                    // Tall tumbler: juice column inside clear glass, mango wedge on the rim.
                    B.MeshObj("Juice", v, _cup, juice, new Vector3(0f, 0.01f, 0f), new Vector3(0.23f, 0.29f, 0.23f));
                    B.MeshObj("Glass", v, _cup, _mGlass, Vector3.zero, new Vector3(0.27f, 0.34f, 0.27f), null, false);
                    B.MeshObj("Top", v, _disc, new[] { juice, juice }, new Vector3(0f, 0.3f, 0f), new Vector3(0.28f, 0.012f, 0.28f));
                    B.MeshObj("Straw", v, _disc, new[] { _mYellowStraw(), _mYellowStraw() }, new Vector3(0.04f, 0.28f, 0.02f), new Vector3(0.035f, 0.22f, 0.035f), new Vector3(0f, 0f, -14f));
                    SliceVisual(f, v, new Vector3(-0.1f, 0.31f, -0.06f), 0.15f, 0.05f, new Vector3(-60f, 20f, 0f));
                    break;
                case FruitKind.Banana:
                    // Milkshake: pale glass, cream dome, cherry and a banana coin.
                    B.MeshObj("Cup", v, _cup, juice, Vector3.zero, new Vector3(0.28f, 0.26f, 0.28f));
                    B.MeshObj("Band", v, _disc, new[] { _mPink, _mPink }, new Vector3(0f, 0.08f, 0f), new Vector3(0.262f, 0.06f, 0.262f));
                    B.MeshObj("Cream", v, B.Sphere, _mCream, new Vector3(0f, 0.26f, 0f), new Vector3(0.28f, 0.13f, 0.28f));
                    B.MeshObj("Cherry", v, B.Sphere, _mCherry, new Vector3(0f, 0.33f, 0f), Vector3.one * 0.06f);
                    B.MeshObj("Straw", v, _disc, new[] { _mRed, _mRed }, new Vector3(0.06f, 0.26f, 0.03f), new Vector3(0.035f, 0.2f, 0.035f), new Vector3(0f, 0f, -20f));
                    SliceVisual(f, v, new Vector3(-0.09f, 0.3f, -0.06f), 0.12f, 0.04f, new Vector3(-55f, 0f, 0f));
                    break;
                default:
                    // Papaya goblet: stem + bowl, green rim, papaya slice garnish.
                    B.MeshObj("Foot", v, _disc, new[] { _mGlass, _mGlass }, Vector3.zero, new Vector3(0.2f, 0.02f, 0.2f), null, false);
                    B.MeshObj("Stem", v, _disc, new[] { _mGlass, _mGlass }, new Vector3(0f, 0.02f, 0f), new Vector3(0.05f, 0.1f, 0.05f), null, false);
                    B.MeshObj("Bowl", v, _cup, juice, new Vector3(0f, 0.11f, 0f), new Vector3(0.28f, 0.19f, 0.28f));
                    B.MeshObj("Rim", v, _disc, new[] { _mLeafTrim(), _mLeafTrim() }, new Vector3(0f, 0.29f, 0f), new Vector3(0.3f, 0.02f, 0.3f));
                    B.MeshObj("Top", v, _disc, new[] { juice, juice }, new Vector3(0f, 0.3f, 0f), new Vector3(0.27f, 0.012f, 0.27f));
                    B.MeshObj("Straw", v, _disc, new[] { _mBinGreen, _mBinGreen }, new Vector3(0.04f, 0.28f, 0.02f), new Vector3(0.035f, 0.2f, 0.035f), new Vector3(0f, 0f, -16f));
                    SliceVisual(f, v, new Vector3(-0.02f, 0.3f, -0.13f), 0.17f, 0.04f, new Vector3(-70f, 0f, 0f));
                    break;
            }
        }

        // ------------------------------------------------------------------ berry items

        static void Berry(Transform parent, int f, Vector3 pos, float size, float yaw = 0f)
        {
            var sphere = LowSphere();
            switch ((FruitKind)f)
            {
                case FruitKind.Strawberry:
                    // Plump cone: squashed sphere tipped forward, with a green crown.
                    B.MeshObj("Berry", parent, sphere, _mBerry[f], pos, new Vector3(size, size * 0.9f, size * 1.15f), new Vector3(-90f + 20f, yaw, 0f), false);
                    B.MeshObj("Crown", parent, _disc, new[] { _mBerryLeaf, _mBerryLeaf }, pos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, size * 0.25f, -size * 0.45f),
                        new Vector3(size * 0.7f, 0.02f, size * 0.7f), new Vector3(-70f, yaw, 0f), false);
                    break;
                case FruitKind.Raspberry:
                    B.MeshObj("Berry", parent, sphere, _mBerry[f], pos, new Vector3(size, size * 1.05f, size), null, false);
                    B.MeshObj("Hole", parent, _disc, new[] { _mDark, _mDark }, pos + Vector3.up * size * 0.5f, new Vector3(size * 0.3f, 0.01f, size * 0.3f), null, false);
                    break;
                case FruitKind.Blueberry:
                    B.MeshObj("Berry", parent, sphere, _mBerry[f], pos, new Vector3(size, size * 0.85f, size), null, false);
                    B.MeshObj("Crown", parent, _disc, new[] { _mDark, _mDark }, pos + Vector3.up * size * 0.42f, new Vector3(size * 0.32f, 0.012f, size * 0.32f), null, false);
                    break;
                default:
                    B.MeshObj("Berry", parent, sphere, _mBerry[f], pos, new Vector3(size * 0.9f, size, size * 0.9f), new Vector3(0f, yaw, 12f), false);
                    break;
            }
        }

        /// <summary>
        /// Harvested berries, each kind packed its own way so a pile reads at a glance: strawberries on a leaf, raspberries
        /// in a pink punnet, blueberries in a gingham punnet, cranberries in a wooden scoop. Stays within 0.46 m x 0.15 m.
        /// </summary>
        static void BerryPieceVisual(int f, Transform root)
        {
            var t = B.Node("Visual", root, Vector3.zero).transform;
            switch ((FruitKind)f)
            {
                case FruitKind.Strawberry:
                    B.MeshObj("Leaf", t, _disc, new[] { _mBerryLeaf, _mBerryLeaf }, Vector3.zero, new Vector3(0.44f, 0.02f, 0.36f), null, false);
                    Berry(t, f, new Vector3(-0.09f, 0.07f, -0.04f), 0.16f, 20f);
                    Berry(t, f, new Vector3(0.1f, 0.07f, -0.02f), 0.15f, -30f);
                    Berry(t, f, new Vector3(0f, 0.08f, 0.09f), 0.15f, 170f);
                    break;
                case FruitKind.Raspberry:
                    B.MeshObj("Punnet", t, _cup, _mPunnetPink, Vector3.zero, new Vector3(0.4f, 0.09f, 0.4f), null, false);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Mathf.PI * 2f;
                        Berry(t, f, new Vector3(Mathf.Cos(a) * 0.1f, 0.1f, Mathf.Sin(a) * 0.1f), 0.11f);
                    }
                    break;
                case FruitKind.Blueberry:
                    B.MeshObj("Punnet", t, _cup, _mPunnetBlue, Vector3.zero, new Vector3(0.4f, 0.09f, 0.4f), null, false);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        var p = i == 6 ? new Vector3(0f, 0.11f, 0f) : new Vector3(Mathf.Cos(a) * 0.11f, 0.095f, Mathf.Sin(a) * 0.11f);
                        Berry(t, f, p, 0.09f);
                    }
                    break;
                default:
                    B.MeshObj("Scoop", t, _cup, _mWood, Vector3.zero, new Vector3(0.38f, 0.08f, 0.38f), null, false);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f + 0.3f;
                        Berry(t, f, new Vector3(Mathf.Cos(a) * 0.1f, 0.09f, Mathf.Sin(a) * 0.1f), 0.1f, i * 40f);
                    }
                    Berry(t, f, new Vector3(0f, 0.11f, 0f), 0.1f);
                    break;
            }
        }

        /// <summary>Four berry drinks, four vessels: smoothie glass, mason jar, milk bottle, tumbler. Within 0.3 m x 0.37 m.</summary>
        static void BerryCupVisual(int f, Transform v)
        {
            var juice = _mJuice[f];
            var sphere = LowSphere();
            switch ((FruitKind)f)
            {
                case FruitKind.Strawberry:
                    B.MeshObj("Juice", v, _cup, juice, new Vector3(0f, 0.01f, 0f), new Vector3(0.24f, 0.25f, 0.24f));
                    B.MeshObj("Glass", v, _cup, _mGlass, Vector3.zero, new Vector3(0.27f, 0.28f, 0.27f), null, false);
                    B.MeshObj("Cream", v, sphere, _mCreamWhite, new Vector3(0f, 0.28f, 0f), new Vector3(0.27f, 0.11f, 0.27f));
                    B.MeshObj("Swirl", v, sphere, _mCreamWhite, new Vector3(0f, 0.33f, 0f), new Vector3(0.14f, 0.07f, 0.14f), null, false);
                    Berry(v, f, new Vector3(0.04f, 0.35f, 0f), 0.09f, 40f);
                    B.MeshObj("Straw", v, _disc, new[] { _mWhite, _mWhite }, new Vector3(-0.05f, 0.27f, 0.03f), new Vector3(0.03f, 0.16f, 0.03f), new Vector3(0f, 0f, 16f), false);
                    break;
                case FruitKind.Raspberry:
                    B.MeshObj("Juice", v, _disc, new[] { juice, juice }, new Vector3(0f, 0.01f, 0f), new Vector3(0.23f, 0.26f, 0.23f));
                    B.MeshObj("Jar", v, _disc, new[] { _mGlass, _mGlass }, Vector3.zero, new Vector3(0.26f, 0.29f, 0.26f), null, false);
                    B.MeshObj("Lid", v, _disc, new[] { _mGoldLid, _mGoldLid }, new Vector3(0f, 0.29f, 0f), new Vector3(0.27f, 0.035f, 0.27f));
                    Berry(v, f, new Vector3(0.05f, 0.2f, -0.1f), 0.06f);
                    Berry(v, f, new Vector3(-0.06f, 0.12f, -0.1f), 0.06f);
                    B.MeshObj("Straw", v, _disc, new[] { _mPink, _mPink }, new Vector3(0.05f, 0.3f, 0.02f), new Vector3(0.03f, 0.1f, 0.03f), new Vector3(0f, 0f, -14f), false);
                    break;
                case FruitKind.Blueberry:
                    B.MeshObj("Body", v, _disc, new[] { juice, juice }, Vector3.zero, new Vector3(0.25f, 0.22f, 0.25f));
                    B.MeshObj("Shoulder", v, sphere, juice, new Vector3(0f, 0.22f, 0f), new Vector3(0.25f, 0.1f, 0.25f), null, false);
                    B.MeshObj("Neck", v, _disc, new[] { juice, juice }, new Vector3(0f, 0.24f, 0f), new Vector3(0.14f, 0.08f, 0.14f), null, false);
                    B.MeshObj("Cap", v, _disc, new[] { _mPunnetBlue, _mPunnetBlue }, new Vector3(0f, 0.32f, 0f), new Vector3(0.17f, 0.045f, 0.17f));
                    B.MeshObj("Label", v, _disc, new[] { _mWhite, _mWhite }, new Vector3(0f, 0.07f, 0f), new Vector3(0.256f, 0.08f, 0.256f), null, false);
                    Berry(v, f, new Vector3(0f, 0.11f, -0.13f), 0.05f);
                    break;
                default:
                    B.MeshObj("Juice", v, _cup, juice, new Vector3(0f, 0.01f, 0f), new Vector3(0.25f, 0.27f, 0.25f));
                    B.MeshObj("Glass", v, _cup, _mGlass, Vector3.zero, new Vector3(0.28f, 0.3f, 0.28f), null, false);
                    B.MeshObj("Ice1", v, B.Cube, _mIce, new Vector3(-0.05f, 0.26f, 0.02f), Vector3.one * 0.08f, new Vector3(20f, 30f, 10f), false);
                    B.MeshObj("Ice2", v, B.Cube, _mIce, new Vector3(0.05f, 0.27f, -0.03f), Vector3.one * 0.07f, new Vector3(-15f, 60f, 25f), false);
                    B.MeshObj("Lime", v, _disc, new[] { _mLime, _mLime }, new Vector3(0.1f, 0.29f, -0.05f), new Vector3(0.13f, 0.025f, 0.13f), new Vector3(-70f, 30f, 0f), false);
                    Berry(v, f, new Vector3(-0.03f, 0.3f, 0.06f), 0.06f);
                    B.MeshObj("Straw", v, _disc, new[] { _mBinGreen, _mBinGreen }, new Vector3(-0.05f, 0.28f, 0.03f), new Vector3(0.03f, 0.18f, 0.03f), new Vector3(0f, 0f, 14f), false);
                    break;
            }
        }

        /// <summary>Round baking tin filled with berry batter and a swirl of fruit.</summary>
        static void BatterVisual(int f, Transform v)
        {
            var batter = MatLib.Lit("Batter_" + FruitKey[f], Color.Lerp(Balance.JuiceColors[f], new Color(1f, 0.95f, 0.88f), 0.55f), 0.35f);
            B.MeshObj("Tin", v, _cup, _mTin, Vector3.zero, new Vector3(0.36f, 0.14f, 0.36f));
            B.MeshObj("Batter", v, _disc, new[] { batter, batter }, new Vector3(0f, 0.1f, 0f), new Vector3(0.33f, 0.035f, 0.33f), null, false);
            B.MeshObj("Swirl", v, LowSphere(), _mJuice[f], new Vector3(0.04f, 0.135f, -0.02f), new Vector3(0.1f, 0.02f, 0.14f), new Vector3(0f, 30f, 0f), false);
            B.MeshObj("Swirl2", v, LowSphere(), _mJuice[f], new Vector3(-0.07f, 0.135f, 0.05f), new Vector3(0.07f, 0.02f, 0.1f), new Vector3(0f, -40f, 0f), false);
        }

        /// <summary>Four cakes, each its own recipe: shortcake, velvet layer cake, cheesecake, tart. Within 0.36 m x 0.32 m.</summary>
        static void CakeVisual(int f, Transform v)
        {
            var sphere = LowSphere();
            switch ((FruitKind)f)
            {
                case FruitKind.Strawberry:
                    B.MeshObj("Plate", v, _disc, new[] { _mWhite, _mWhite }, Vector3.zero, new Vector3(0.38f, 0.02f, 0.38f), null, false);
                    B.MeshObj("Sponge1", v, _disc, new[] { _mSponge, _mSponge }, new Vector3(0f, 0.02f, 0f), new Vector3(0.32f, 0.08f, 0.32f));
                    B.MeshObj("Jam", v, _disc, new[] { _mJuice[f], _mJuice[f] }, new Vector3(0f, 0.1f, 0f), new Vector3(0.33f, 0.025f, 0.33f), null, false);
                    B.MeshObj("Cream1", v, _disc, new[] { _mCreamWhite, _mCreamWhite }, new Vector3(0f, 0.125f, 0f), new Vector3(0.325f, 0.025f, 0.325f), null, false);
                    B.MeshObj("Sponge2", v, _disc, new[] { _mSponge, _mSponge }, new Vector3(0f, 0.15f, 0f), new Vector3(0.32f, 0.07f, 0.32f));
                    B.MeshObj("Top", v, sphere, _mCreamWhite, new Vector3(0f, 0.22f, 0f), new Vector3(0.33f, 0.07f, 0.33f));
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i / 3f * Mathf.PI * 2f + 0.4f;
                        Berry(v, f, new Vector3(Mathf.Cos(a) * 0.08f, 0.28f, Mathf.Sin(a) * 0.08f), 0.09f, i * 120f);
                    }
                    break;
                case FruitKind.Raspberry:
                    var velvet = MatLib.Lit("CakeVelvet", new Color(0.88f, 0.3f, 0.45f), 0.25f);
                    var pinkFrost = MatLib.Lit("FrostingPink", new Color(1f, 0.66f, 0.78f), 0.45f);
                    B.MeshObj("Plate", v, _disc, new[] { _mWhite, _mWhite }, Vector3.zero, new Vector3(0.38f, 0.02f, 0.38f), null, false);
                    B.MeshObj("Body", v, _disc, new[] { velvet, velvet }, new Vector3(0f, 0.02f, 0f), new Vector3(0.31f, 0.2f, 0.31f));
                    B.MeshObj("Layer", v, _disc, new[] { _mCreamWhite, _mCreamWhite }, new Vector3(0f, 0.1f, 0f), new Vector3(0.315f, 0.03f, 0.315f), null, false);
                    B.MeshObj("Frosting", v, _disc, new[] { pinkFrost, pinkFrost }, new Vector3(0f, 0.2f, 0f), new Vector3(0.33f, 0.04f, 0.33f));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        B.MeshObj("Drip", v, sphere, pinkFrost, new Vector3(Mathf.Cos(a) * 0.16f, 0.2f, Mathf.Sin(a) * 0.16f), new Vector3(0.05f, 0.08f, 0.05f), null, false);
                        if (i % 2 == 0) Berry(v, f, new Vector3(Mathf.Cos(a) * 0.09f, 0.27f, Mathf.Sin(a) * 0.09f), 0.075f);
                    }
                    Berry(v, f, new Vector3(0f, 0.28f, 0f), 0.08f);
                    break;
                case FruitKind.Blueberry:
                    var cheese = MatLib.Lit("Cheesecake", new Color(1f, 0.95f, 0.8f), 0.25f);
                    var topping = MatLib.Lit("BlueTopping", new Color(0.36f, 0.22f, 0.6f), 0.7f);
                    B.MeshObj("Plate", v, _disc, new[] { _mWhite, _mWhite }, Vector3.zero, new Vector3(0.38f, 0.02f, 0.38f), null, false);
                    B.MeshObj("Crust", v, _disc, new[] { _mCrust, _mCrust }, new Vector3(0f, 0.02f, 0f), new Vector3(0.33f, 0.05f, 0.33f));
                    B.MeshObj("Cheese", v, _disc, new[] { cheese, cheese }, new Vector3(0f, 0.07f, 0f), new Vector3(0.32f, 0.14f, 0.32f));
                    B.MeshObj("Topping", v, _disc, new[] { topping, topping }, new Vector3(0f, 0.21f, 0f), new Vector3(0.325f, 0.035f, 0.325f));
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Mathf.PI * 2f + 0.2f;
                        B.MeshObj("Drip", v, sphere, topping, new Vector3(Mathf.Cos(a) * 0.158f, 0.19f, Mathf.Sin(a) * 0.158f), new Vector3(0.05f, 0.1f, 0.05f), null, false);
                    }
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        var p = i == 6 ? Vector3.zero : new Vector3(Mathf.Cos(a) * 0.08f, 0f, Mathf.Sin(a) * 0.08f);
                        Berry(v, f, p + Vector3.up * 0.26f, 0.07f);
                    }
                    break;
                default:
                    B.MeshObj("Shell", v, _cup, _mCrust, Vector3.zero, new Vector3(0.36f, 0.14f, 0.36f));
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i / 12f * Mathf.PI * 2f;
                        B.MeshObj("Flute", v, sphere, _mCrust, new Vector3(Mathf.Cos(a) * 0.178f, 0.12f, Mathf.Sin(a) * 0.178f), new Vector3(0.05f, 0.05f, 0.05f), null, false);
                    }
                    var filling = MatLib.Lit("TartFilling", new Color(0.62f, 0.04f, 0.12f), 0.85f);
                    B.MeshObj("Filling", v, _disc, new[] { filling, filling }, new Vector3(0f, 0.12f, 0f), new Vector3(0.32f, 0.02f, 0.32f), null, false);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2f + 0.2f;
                        float r = i % 2 == 0 ? 0.1f : 0.05f;
                        Berry(v, f, new Vector3(Mathf.Cos(a) * r, 0.16f, Mathf.Sin(a) * r), 0.075f, i * 45f);
                    }
                    B.MeshObj("Leaf", v, sphere, _mBerryLeaf, new Vector3(0.02f, 0.19f, 0.02f), new Vector3(0.1f, 0.02f, 0.05f), new Vector3(0f, 35f, 0f), false);
                    break;
            }
        }

        static Material _mYellowStraw() => MatLib.Lit("StrawYellow", new Color(1f, 0.85f, 0.2f), 0.4f);
        static Material _mLeafTrim() => MatLib.Lit("LeafTrim", new Color(0.4f, 0.75f, 0.3f), 0.3f);

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
            B.Decal("Blob", root.transform, _mBlob, new Vector3(0f, 0.075f, 0f), new Vector2(1.1f, 1.1f)).GetComponent<MeshRenderer>().sortingOrder = -2;
            return root;
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
            B.MeshObj("Gem", crown.transform, B.Sphere, _mRed, new Vector3(0f, 0.22f, -0.52f), Vector3.one * 0.2f);

            // Chainsaw held in front-right, attached to the torso so it bobs with the body.
            var torso = B.Find(model, "torso");
            var saw = BuildChainsaw(root.transform, new Vector3(0.3f, 0.62f, 0.42f), 1f);
            if (torso != null) saw.transform.SetParent(torso, true);
            saw.carrier = carrier;
            saw.owner = root.transform;
            saw.range = 1.4f;

            var ca = root.AddComponent<CharacterAnim>();
            ca.animator = anim;
            ca.alwaysHoldRight = true;
            ca.animSpeedRef = 5.2f;
            ca.footsteps = true;

            var pc = root.AddComponent<PlayerController>();
            pc.cc = cc;
            pc.carrier = carrier;
            pc.saw = saw;
            pc.anim = ca;
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
                ca.lean = 4f;
                cust.anim = ca;

                AddAccessory(root.transform, anim.transform, _customerPrefabs.Count);

                _customerPrefabs.Add(B.SavePrefab(root, Prefabs + "Customers/" + root.name + ".prefab"));
            }

            // Berry Cake Shop customers: party-goers in paper hats with a pink bubble, so the cake queue reads apart.
            _cakeCustomerPrefabs.Clear();
            string[] cakeModels = { "character-female-a", "character-female-c", "character-female-e", "character-male-b", "character-male-c", "character-male-e", "character-female-f" };
            Material[] hats = { MatLib.Lit("PartyHatPink", new Color(1f, 0.55f, 0.72f), 0.3f), MatLib.Lit("PartyHatBlue", new Color(0.45f, 0.7f, 1f), 0.3f),
                MatLib.Lit("PartyHatYellow", new Color(1f, 0.85f, 0.3f), 0.3f), MatLib.Lit("PartyHatMint", new Color(0.5f, 0.92f, 0.7f), 0.3f) };
            for (int i = 0; i < cakeModels.Length; i++)
            {
                var mdl = cakeModels[i];
                var root = CharacterBase("CakeCustomer_" + mdl.Replace("character-", ""), "Characters/" + mdl, out var anim, out _);
                var hands = B.Node("Hands", root.transform, new Vector3(0f, 0.62f, 0.42f));
                var carrier = root.AddComponent<Carrier>();
                carrier.stackRoot = hands.transform;
                carrier.capacity = 3;
                carrier.columns = 1;
                carrier.canCollectLoose = false;
                carrier.interactsWithZones = false;
                carrier.swayAmount = 0.03f;

                var bubble = BuildBubble(root.transform, new Vector3(0f, 2.2f, 0f), new Color(1f, 0.86f, 0.93f));
                var cust = root.AddComponent<Customer>();
                cust.hands = carrier;
                cust.bubble = bubble;

                var ca = root.AddComponent<CharacterAnim>();
                ca.animator = anim;
                ca.hands = carrier;
                ca.animSpeedRef = 3.2f;
                ca.lean = 4f;
                cust.anim = ca;

                // Striped paper party hat with a pom-pom.
                var head = B.Find(anim.transform, "head");
                var acc = B.Node("PartyHat", root.transform, new Vector3(0f, 1.44f, 0f), new Vector3(-8f, 0f, 10f));
                var hat = hats[i % hats.Length];
                B.MeshObj("Cone", acc.transform, _crownCone(), hat, Vector3.zero, new Vector3(0.26f, 0.34f, 0.26f));
                B.MeshObj("Band", acc.transform, _disc, new[] { _mWhite, _mWhite }, new Vector3(0f, 0.08f, 0f), new Vector3(0.2f, 0.04f, 0.2f), null, false);
                B.MeshObj("Pom", acc.transform, LowSphere(), _mWhite, new Vector3(0f, 0.35f, 0f), Vector3.one * 0.09f, null, false);
                acc.transform.SetParent(head != null ? head : anim.transform, true);

                _cakeCustomerPrefabs.Add(B.SavePrefab(root, Prefabs + "Customers/" + root.name + ".prefab"));
            }
        }

        static Mesh _partyCone;

        /// <summary>Unit cone (radius 0.5, height 1, pivot at the base).</summary>
        static Mesh _crownCone()
        {
            if (_partyCone != null) return _partyCone;
            var g = new MeshGen();
            g.Cone(0.5f, 0f, 1f, 16, 0, true);
            _partyCone = SaveMesh(g.ToMesh("PartyCone"), "PartyCone");
            return _partyCone;
        }

        /// <summary>Small hats / glasses so the crowd looks varied and cute.</summary>
        static void AddAccessory(Transform root, Transform model, int index)
        {
            var head = B.Find(model, "head");
            Transform parent = head != null ? head : model;
            Material[] tints = { _mRed, _mBlue, _mPink, _mStraw, _mBinGreen };
            var mat = tints[index % tints.Length];
            // Built in world units under the unscaled root, then re-parented to the (scaled) head bone keeping world size.
            var acc = B.Node("Accessory", root, new Vector3(0f, 1.42f, 0f));
            var a = acc.transform;
            switch (index % 4)
            {
                case 0: // baseball cap
                    B.MeshObj("Crown", a, B.Sphere, mat, new Vector3(0f, 0.06f, 0f), new Vector3(0.44f, 0.24f, 0.44f));
                    B.MeshObj("Brim", a, _disc, new[] { mat, mat }, new Vector3(0f, 0.03f, 0.2f), new Vector3(0.3f, 0.025f, 0.28f));
                    break;
                case 1: // sun hat
                    B.MeshObj("Brim", a, _disc, new[] { _mStraw, _mStraw }, new Vector3(0f, 0.06f, 0f), new Vector3(0.56f, 0.025f, 0.56f));
                    B.MeshObj("Top", a, _disc, new[] { _mStraw, _mStraw }, new Vector3(0f, 0.06f, 0f), new Vector3(0.32f, 0.15f, 0.32f));
                    B.MeshObj("Band", a, _disc, new[] { mat, mat }, new Vector3(0f, 0.08f, 0f), new Vector3(0.33f, 0.045f, 0.33f));
                    break;
                case 2: // sunglasses
                    a.localPosition = new Vector3(0f, 1.2f, 0.27f);
                    B.Box("L", a, _mDark, new Vector3(-0.1f, 0f, 0f), new Vector3(0.15f, 0.08f, 0.03f));
                    B.Box("R", a, _mDark, new Vector3(0.1f, 0f, 0f), new Vector3(0.15f, 0.08f, 0.03f));
                    B.Box("Bridge", a, _mDark, new Vector3(0f, 0.015f, 0f), new Vector3(0.07f, 0.02f, 0.025f));
                    break;
                default: // bow
                    a.localPosition = new Vector3(0.2f, 1.45f, 0f);
                    B.MeshObj("L", a, B.Sphere, _mPink, new Vector3(-0.07f, 0f, 0f), new Vector3(0.13f, 0.1f, 0.07f));
                    B.MeshObj("R", a, B.Sphere, _mPink, new Vector3(0.07f, 0f, 0f), new Vector3(0.13f, 0.1f, 0.07f));
                    B.MeshObj("K", a, B.Sphere, _mRed, Vector3.zero, Vector3.one * 0.055f);
                    break;
            }
            a.SetParent(parent, true);
        }

        static OrderBubble BuildBubble(Transform parent, Vector3 pos, Color? tint = null)
        {
            var go = B.Node("OrderBubble", parent, pos, null, Vector3.one * 0.8f);
            go.AddComponent<Billboard>();
            SpriteRenderer bg;
            if (_uBubbleWhite != null)
                bg = B.Sprite("Bg", go.transform, _uBubbleWhite, new Vector3(0f, -0.06f, 0f), SpriteScale(_uBubbleWhite, 1.45f), false, 10, tint ?? Color.white);
            else
            {
                bg = B.Sprite("Bg", go.transform, _sRound, Vector3.zero, 1f, false, 10, Color.white);
                bg.drawMode = SpriteDrawMode.Sliced;
                bg.size = new Vector2(1.25f, 0.72f);
            }
            var icon = B.Sprite("Icon", go.transform, _sFruit[0], new Vector3(-0.25f, 0.02f, -0.01f), 0.22f, false, 11);
            var count = B.Text("Count", go.transform, "1", 5f, new Color(0.25f, 0.2f, 0.2f), new Vector3(0.3f, 0.02f, -0.01f), false);
            count.GetComponent<MeshRenderer>().sortingOrder = 12;
            // Faces replace the order inside the bubble, so they are sized to the bubble (~0.6 m), not to the sprite's pixels.
            var heartSprite = _uHeartBubble ? _uHeartBubble : _sHeart;
            var sadSprite = _uExclBubble ? _uExclBubble : _sHeart;
            var happy = B.Sprite("Happy", go.transform, heartSprite, new Vector3(0f, 0.04f, -0.02f), SpriteScale(heartSprite, 0.62f), false, 13);
            happy.gameObject.SetActive(false);
            var sad = B.Sprite("Sad", go.transform, sadSprite, new Vector3(0f, 0.04f, -0.02f), SpriteScale(sadSprite, 0.6f), false, 13);
            sad.gameObject.SetActive(false);

            // Patience bar under the order.
            var bar = B.Node("Patience", go.transform, new Vector3(0f, -0.25f, -0.012f));
            var barBg = B.Sprite("Bg", bar.transform, _sWhite, Vector3.zero, 1f, false, 11, new Color(0.25f, 0.2f, 0.2f, 0.5f));
            barBg.transform.localScale = new Vector3(0.9f, 0.09f, 1f);
            var barFill = B.Sprite("Fill", bar.transform, _sWhite, new Vector3(0f, 0f, -0.002f), 1f, false, 12, new Color(0.45f, 0.9f, 0.4f));
            barFill.transform.localScale = new Vector3(0.86f, 0.06f, 1f);

            var ob = go.AddComponent<OrderBubble>();
            ob.icon = icon;
            ob.countText = count;
            ob.happy = happy;
            ob.sad = sad;
            ob.background = bg;
            ob.patienceBar = bar;
            ob.patienceFill = barFill.transform;
            ob.patienceFillRenderer = barFill;
            return ob;
        }
    }
}
