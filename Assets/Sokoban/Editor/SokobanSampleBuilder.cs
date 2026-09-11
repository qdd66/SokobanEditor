using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Sokoban
{
    public static class SokobanSampleBuilder
    {
        public const string Root = "Assets/Sokoban";
        public const string DataPath = Root + "/Data";
        public const string MaterialsPath = Root + "/Materials";
        public const string PrefabsPath = Root + "/Prefabs";
        public const string ScenesPath = Root + "/Scenes";
        public const string ConfigPath = DataPath + "/SokobanConfig.asset";
        public const string ChannelPath = DataPath + "/SokobanWinChannel.asset";
        public const string ScenePath = ScenesPath + "/SokobanSample.unity";
        public const string PlayerPrefabPath = PrefabsPath + "/SokobanPlayer.prefab";
        public const string WallPrefabPath = PrefabsPath + "/SokobanWall.prefab";
        public const string BoxPrefabPath = PrefabsPath + "/SokobanBox.prefab";
        public const string GoalPrefabPath = PrefabsPath + "/SokobanGoal.prefab";

        const string ChineseFontPath = "Assets/DZDRuntimeMapEditor/Resources/Fonts/字制区喜脉体 SDF.asset";
        const string FallbackFontPath =
            "Assets/Packages/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string UrpLitName = "Universal Render Pipeline/Lit";

        [MenuItem("Tools/推箱子/生成示例场景与预制体", false, 10)]
        public static void BuildFromMenu()
        {
            EditorUtility.DisplayDialog("推箱子", Build(), "确定");
        }

        public static string Build()
        {
            EnsureFolder(Root);
            EnsureFolder(DataPath);
            EnsureFolder(MaterialsPath);
            EnsureFolder(PrefabsPath);
            EnsureFolder(ScenesPath);

            var config = LoadOrCreate<SokobanConfig>(ConfigPath);
            var channel = LoadOrCreate<SokobanWinChannel>(ChannelPath);
            AssetDatabase.SaveAssets();
            config = AssetDatabase.LoadAssetAtPath<SokobanConfig>(ConfigPath);
            channel = AssetDatabase.LoadAssetAtPath<SokobanWinChannel>(ChannelPath);

            var playerMat = CreateLitMaterial(MaterialsPath + "/SokobanPlayer.mat", new Color(0.25f, 0.55f, 0.95f));
            var wallMat = CreateLitMaterial(MaterialsPath + "/SokobanWall.mat", new Color(0.34f, 0.37f, 0.42f));
            var boxMat = CreateLitMaterial(MaterialsPath + "/SokobanBox.mat", new Color(0.82f, 0.48f, 0.18f));
            var goalMat = CreateLitMaterial(MaterialsPath + "/SokobanGoal.mat", new Color(0.32f, 0.82f, 0.38f));
            var groundMat = CreateLitMaterial(MaterialsPath + "/SokobanGround.mat", new Color(0.18f, 0.2f, 0.19f));

            var playerPrefab = CreatePiecePrefab(
                PlayerPrefabPath, "SokobanPlayer", PrimitiveType.Capsule, playerMat,
                SokobanRole.Player, new Vector3(0.7f, 0.55f, 0.7f));
            var wallPrefab = CreatePiecePrefab(
                WallPrefabPath, "SokobanWall", PrimitiveType.Cube, wallMat,
                SokobanRole.Wall, Vector3.one);
            var boxPrefab = CreatePiecePrefab(
                BoxPrefabPath, "SokobanBox", PrimitiveType.Cube, boxMat,
                SokobanRole.Box, new Vector3(0.82f, 0.82f, 0.82f));
            var goalPrefab = CreatePiecePrefab(
                GoalPrefabPath, "SokobanGoal", PrimitiveType.Cube, goalMat,
                SokobanRole.Goal, new Vector3(0.88f, 0.06f, 0.88f));

            CreateSampleScene(config, channel, playerPrefab, wallPrefab, boxPrefab, goalPrefab, groundMat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "已生成四个预制体、配置和示例场景：\n" + ScenePath;
        }

        static void CreateSampleScene(
            SokobanConfig config,
            SokobanWinChannel channel,
            GameObject playerPrefab,
            GameObject wallPrefab,
            GameObject boxPrefab,
            GameObject goalPrefab,
            Material groundMat)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Directional Light");
            var lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(3.5f, 0f, 3f);
            ground.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            ApplyMaterial(ground, groundMat);

            var board = new GameObject("Board");
            PlaceLevel(board.transform, config, playerPrefab, wallPrefab, boxPrefab, goalPrefab);

            var systems = new GameObject("SokobanSession");
            var session = systems.AddComponent<SokobanSession>();
            var sessionSo = new SerializedObject(session);
            sessionSo.FindProperty("config").objectReferenceValue = config;
            sessionSo.FindProperty("winChannel").objectReferenceValue = channel;
            sessionSo.FindProperty("boardRoot").objectReferenceValue = board.transform;
            sessionSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(session);

            var cameraGo = new GameObject("SokobanCamera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.15f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            var sceneCam = cameraGo.AddComponent<SokobanSceneCamera>();
            var camSo = new SerializedObject(sceneCam);
            camSo.FindProperty("boardRoot").objectReferenceValue = board.transform;
            camSo.FindProperty("config").objectReferenceValue = config;
            camSo.FindProperty("pitch").floatValue = 62f;
            camSo.FindProperty("yaw").floatValue = 0f;
            camSo.FindProperty("fieldOfView").floatValue = 40f;
            camSo.FindProperty("padding").floatValue = 1.6f;
            camSo.FindProperty("minDistance").floatValue = 5f;
            camSo.ApplyModifiedPropertiesWithoutUndo();
            sceneCam.FrameBoard();

            CreateHud(config, channel);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void PlaceLevel(
            Transform board,
            SokobanConfig config,
            GameObject playerPrefab,
            GameObject wallPrefab,
            GameObject boxPrefab,
            GameObject goalPrefab)
        {
            const int width = 7;
            const int depth = 6;
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    var edge = x == 0 || z == 0 || x == width - 1 || z == depth - 1;
                    if (edge)
                        Place(wallPrefab, board, new Vector2Int(x, z), PieceHeight(SokobanRole.Wall), config);
                }
            }

            Place(playerPrefab, board, new Vector2Int(2, 3), PieceHeight(SokobanRole.Player), config);
            Place(boxPrefab, board, new Vector2Int(3, 3), PieceHeight(SokobanRole.Box), config);
            Place(boxPrefab, board, new Vector2Int(4, 3), PieceHeight(SokobanRole.Box), config);
            Place(goalPrefab, board, new Vector2Int(3, 2), PieceHeight(SokobanRole.Goal), config);
            Place(goalPrefab, board, new Vector2Int(4, 2), PieceHeight(SokobanRole.Goal), config);
        }

        static float PieceHeight(SokobanRole role)
        {
            switch (role)
            {
                case SokobanRole.Player:
                    return 0.55f;
                case SokobanRole.Box:
                    return 0.41f;
                case SokobanRole.Goal:
                    return 0.03f;
                default:
                    return 0.5f;
            }
        }

        static void Place(GameObject prefab, Transform parent, Vector2Int cell, float y, SokobanConfig config)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = SokobanGrid.CellCenter(cell, y, config);
            instance.transform.rotation = Quaternion.identity;
        }

        static GameObject CreatePiecePrefab(
            string path,
            string objectName,
            PrimitiveType primitive,
            Material material,
            SokobanRole role,
            Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = objectName;
            go.transform.localScale = scale;
            ApplyMaterial(go, material);
            var piece = go.AddComponent<SokobanPiece>();
            piece.BindRole(role);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void CreateHud(SokobanConfig config, SokobanWinChannel channel)
        {
            var canvasGo = new GameObject("SokobanHud");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = LoadHudFont();
            var hint = CreateLabel(canvasGo.transform, "Hint", new Vector2(0f, 48f), 36f, TextAlignmentOptions.Center, font);
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(-80f, 64f);

            var win = CreateLabel(canvasGo.transform, "Win", new Vector2(0f, 80f), 72f, TextAlignmentOptions.Center, font);
            var winRt = win.rectTransform;
            winRt.anchorMin = new Vector2(0f, 0.5f);
            winRt.anchorMax = new Vector2(1f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(-80f, 120f);
            win.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<SokobanHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("winChannel").objectReferenceValue = channel;
            so.FindProperty("hintLabel").objectReferenceValue = hint;
            so.FindProperty("winLabel").objectReferenceValue = win;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            Vector2 anchored,
            float fontSize,
            TextAlignmentOptions align,
            TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.raycastTarget = false;
            if (font != null)
                text.font = font;
            var rt = text.rectTransform;
            rt.anchoredPosition = anchored;
            return text;
        }

        static TMP_FontAsset LoadHudFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            if (font != null)
                return font;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
        }

        static Material CreateLitMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                ApplyColor(existing, color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find(UrpLitName);
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            ApplyColor(mat, color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void ApplyColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }

        static void ApplyMaterial(GameObject go, Material material)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
