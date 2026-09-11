using DZDMapEditor;
using Sokoban;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sokoban.PlayEdit
{
    public static class SokobanArtSwap
    {
        const string ScenePath = "Assets/Sokoban/Scenes/SokobanSample.unity";
        const string PrefabsRoot = "Assets/Sokoban/Prefabs";
        const string PlayerPrefab = PrefabsRoot + "/SokobanPlayer.prefab";
        const string WallPrefab = PrefabsRoot + "/SokobanWall.prefab";
        const string Wall2Prefab = PrefabsRoot + "/SokobanWall2.prefab";
        const string Wall3Prefab = PrefabsRoot + "/SokobanWall3.prefab";
        const string BoxPrefab = PrefabsRoot + "/SokobanBox.prefab";
        const string GoalPrefab = PrefabsRoot + "/SokobanGoal.prefab";
        const string FloorPrefab = PrefabsRoot + "/SokobanFloor.prefab";
        const string ArtWall1 = PrefabsRoot + "/墙1.prefab";
        const string ArtWall2 = PrefabsRoot + "/墙2.prefab";
        const string ArtWall3 = PrefabsRoot + "/墙3.prefab";
        const string ArtBox = PrefabsRoot + "/箱子.prefab";
        const string ArtGoal = PrefabsRoot + "/目标点.prefab";
        const string CategoryPath = "Assets/Sokoban/Data/Placement/SokobanPieceCategory.asset";
        const string CatalogPath = "Assets/Sokoban/Data/Placement/SokobanPlaceableCatalog.asset";
        const string PlayerDefPath = "Assets/Sokoban/Data/Placement/Items/SokobanPlayerDef.asset";
        const string WallDefPath = "Assets/Sokoban/Data/Placement/Items/SokobanWallDef.asset";
        const string Wall2DefPath = "Assets/Sokoban/Data/Placement/Items/SokobanWall2Def.asset";
        const string Wall3DefPath = "Assets/Sokoban/Data/Placement/Items/SokobanWall3Def.asset";
        const string BoxDefPath = "Assets/Sokoban/Data/Placement/Items/SokobanBoxDef.asset";
        const string GoalDefPath = "Assets/Sokoban/Data/Placement/Items/SokobanGoalDef.asset";
        const string FloorDefPath = "Assets/Sokoban/Data/Placement/Items/SokobanFloorDef.asset";
        const string ConfigPath = "Assets/Sokoban/Data/SokobanConfig.asset";
        const string UndoName = "替换推箱子素材";
        const string ArtChild = "Art";
        const string PrefMinX = "SokobanArtSwap.minX";
        const string PrefMinZ = "SokobanArtSwap.minZ";
        const string PrefMaxX = "SokobanArtSwap.maxX";
        const string PrefMaxZ = "SokobanArtSwap.maxZ";
        const string PrefFloorY = "SokobanArtSwap.floorY";

        [MenuItem("Tools/推箱子/替换关卡素材", false, 21)]
        public static void ApplyFromMenu()
        {
            EditorUtility.DisplayDialog("推箱子", Apply(), "确定");
        }

        public static string Apply()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var assets = ApplyPrefabs();
            if (assets.StartsWith("找不到"))
                return assets;
            return ApplyScene();
        }

        public static string ApplyPrefabs()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ArtWall1) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(ArtBox) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(ArtGoal) == null)
                return "找不到新素材：墙1 / 箱子 / 目标点。";

            EmbedArt(WallPrefab, ArtWall1, SokobanRole.Wall);
            CopyThenEmbed(WallPrefab, Wall2Prefab, ArtWall2, SokobanRole.Wall);
            CopyThenEmbed(WallPrefab, Wall3Prefab, ArtWall3, SokobanRole.Wall);
            EmbedArt(BoxPrefab, ArtBox, SokobanRole.Box);
            EmbedArt(GoalPrefab, ArtGoal, SokobanRole.Goal);
            CopyThenEmbed(WallPrefab, FloorPrefab, ArtWall2, SokobanRole.Floor);
            return BindDefsAndCatalog();
        }

        public static string EmbedWall()
        {
            EmbedArt(WallPrefab, ArtWall1, SokobanRole.Wall);
            return "墙1";
        }

        public static string EmbedBox()
        {
            EmbedArt(BoxPrefab, ArtBox, SokobanRole.Box);
            return "箱子";
        }

        public static string EmbedGoal()
        {
            EmbedArt(GoalPrefab, ArtGoal, SokobanRole.Goal);
            return "目标点";
        }

        public static string EmbedWall2()
        {
            CopyThenEmbed(WallPrefab, Wall2Prefab, ArtWall2, SokobanRole.Wall);
            return "墙2";
        }

        public static string EmbedWall3()
        {
            CopyThenEmbed(WallPrefab, Wall3Prefab, ArtWall3, SokobanRole.Wall);
            return "墙3";
        }

        public static string EmbedFloor()
        {
            CopyThenEmbed(WallPrefab, FloorPrefab, ArtWall2, SokobanRole.Floor);
            return "地面";
        }

        public static string BindDefsAndCatalog()
        {
            var category = AssetDatabase.LoadAssetAtPath<PlaceableCategoryDef>(CategoryPath);
            var playerDef = EnsureDef(
                PlayerDefPath, "sokoban-player", "玩家", PlayerPrefab, category,
                PlaceableOccupancyLayer.Solid, 1);
            var wallDef = EnsureDef(
                WallDefPath, "sokoban-wall", "墙1", WallPrefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var wall2Def = EnsureDef(
                Wall2DefPath, "sokoban-wall-2", "墙2", Wall2Prefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var wall3Def = EnsureDef(
                Wall3DefPath, "sokoban-wall-3", "墙3", Wall3Prefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var floorDef = EnsureDef(
                FloorDefPath, "sokoban-floor", "地面", FloorPrefab, category,
                PlaceableOccupancyLayer.Floor, 0);
            var boxDef = EnsureDef(
                BoxDefPath, "sokoban-box", "箱子", BoxPrefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var goalDef = EnsureDef(
                GoalDefPath, "sokoban-goal", "目标点", GoalPrefab, category,
                PlaceableOccupancyLayer.Overlay, 0);

            BindPlaced(PlayerPrefab, playerDef);
            BindPlaced(WallPrefab, wallDef);
            BindPlaced(Wall2Prefab, wall2Def);
            BindPlaced(Wall3Prefab, wall3Def);
            BindPlaced(FloorPrefab, floorDef);
            BindPlaced(BoxPrefab, boxDef);
            BindPlaced(GoalPrefab, goalDef);

            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlaceableCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            var items = so.FindProperty("items");
            var defs = new[] { playerDef, wallDef, wall2Def, wall3Def, floorDef, boxDef, goalDef };
            items.arraySize = defs.Length;
            for (var i = 0; i < defs.Length; i++)
                items.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return "关卡棋子已换成新素材。";
        }

        public static string BindCatalogOnly()
        {
            var category = AssetDatabase.LoadAssetAtPath<PlaceableCategoryDef>(CategoryPath);
            var playerDef = EnsureDef(
                PlayerDefPath, "sokoban-player", "玩家", PlayerPrefab, category,
                PlaceableOccupancyLayer.Solid, 1);
            var wallDef = EnsureDef(
                WallDefPath, "sokoban-wall", "墙1", WallPrefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var wall2Def = EnsureDef(
                Wall2DefPath, "sokoban-wall-2", "墙2", Wall2Prefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var wall3Def = EnsureDef(
                Wall3DefPath, "sokoban-wall-3", "墙3", Wall3Prefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var floorDef = EnsureDef(
                FloorDefPath, "sokoban-floor", "地面", FloorPrefab, category,
                PlaceableOccupancyLayer.Floor, 0);
            var boxDef = EnsureDef(
                BoxDefPath, "sokoban-box", "箱子", BoxPrefab, category,
                PlaceableOccupancyLayer.Solid, 0);
            var goalDef = EnsureDef(
                GoalDefPath, "sokoban-goal", "目标点", GoalPrefab, category,
                PlaceableOccupancyLayer.Overlay, 0);

            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlaceableCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            var items = so.FindProperty("items");
            var defs = new[] { playerDef, wallDef, wall2Def, wall3Def, floorDef, boxDef, goalDef };
            items.arraySize = defs.Length;
            for (var i = 0; i < defs.Length; i++)
                items.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return "Catalog 已更新。";
        }

        public static string BindPlacedPrefab(string which)
        {
            PlaceableItemDef def;
            string path;
            switch (which)
            {
                case "player":
                    path = PlayerPrefab;
                    def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(PlayerDefPath);
                    break;
                case "wall":
                    path = WallPrefab;
                    def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(WallDefPath);
                    break;
                case "wall2":
                    path = Wall2Prefab;
                    def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(Wall2DefPath);
                    break;
                case "wall3":
                    path = Wall3Prefab;
                    def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(Wall3DefPath);
                    break;
                case "floor":
                    path = FloorPrefab;
                    def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(FloorDefPath);
                    break;
                case "box":
                    path = BoxPrefab;
                    def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(BoxDefPath);
                    break;
                case "goal":
                    path = GoalPrefab;
                    def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(GoalDefPath);
                    break;
                default:
                    return "未知预制体：" + which;
            }

            BindPlaced(path, def);
            return which;
        }

        public static string NormalizePieceScales()
        {
            var paths = new[] { WallPrefab, Wall2Prefab, Wall3Prefab, FloorPrefab, BoxPrefab, GoalPrefab };
            var count = 0;
            for (var i = 0; i < paths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]) == null)
                    continue;
                var contents = PrefabUtility.LoadPrefabContents(paths[i]);
                try
                {
                    contents.transform.localScale = Vector3.one;
                    HidePrimitiveVisual(contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, paths[i]);
                    count++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            return "已把 " + count + " 个棋子缩放置为 1。";
        }

        public static string MeasureArt()
        {
            var paths = new[] { WallPrefab, FloorPrefab, BoxPrefab, GoalPrefab, ArtWall1, ArtBox, ArtGoal };
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < paths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                if (prefab == null)
                {
                    sb.Append(paths[i]).Append(" missing; ");
                    continue;
                }

                var contents = PrefabUtility.LoadPrefabContents(paths[i]);
                try
                {
                    var renderers = contents.GetComponentsInChildren<Renderer>(true);
                    if (renderers == null || renderers.Length == 0)
                    {
                        sb.Append(contents.name).Append(" no renderer; ");
                        continue;
                    }

                    var bounds = renderers[0].bounds;
                    for (var r = 1; r < renderers.Length; r++)
                        bounds.Encapsulate(renderers[r].bounds);
                    sb.Append(contents.name)
                        .Append(" size=")
                        .Append(bounds.size)
                        .Append(" center=")
                        .Append(bounds.center)
                        .Append(" scale=")
                        .Append(contents.transform.localScale)
                        .Append("; ");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            return sb.ToString();
        }

        public static string ApplySceneBind()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var config = AssetDatabase.LoadAssetAtPath<SokobanConfig>(ConfigPath);
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefab);
            var floorDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(FloorDefPath);
            var wallDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(WallDefPath);
            var boxDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(BoxDefPath);
            var goalDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(GoalDefPath);
            var playerDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(PlayerDefPath);
            if (config == null)
                return "找不到玩法配置。";

            var scene = EnsureSampleScene();
            var board = FindNamed(scene, "Board");
            if (board == null)
                return "场景里找不到 Board。";

            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            var pieces = board.GetComponentsInChildren<SokobanPiece>(true);
            var pieceCount = 0;
            for (var i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                if (piece == null || piece.Role == SokobanRole.Floor)
                    continue;
                var cell = SokobanGrid.WorldToCell(piece.transform.position, config);
                min.x = Mathf.Min(min.x, cell.x);
                min.y = Mathf.Min(min.y, cell.y);
                max.x = Mathf.Max(max.x, cell.x);
                max.y = Mathf.Max(max.y, cell.y);
                BindPieceDef(piece, playerDef, wallDef, boxDef, goalDef, floorDef);
                AlignPieceHeight(piece, config);
                pieceCount++;
            }

            if (min.x == int.MaxValue)
                return "棋盘上没有棋子。";

            EditorPrefs.SetInt(PrefMinX, min.x);
            EditorPrefs.SetInt(PrefMinZ, min.y);
            EditorPrefs.SetInt(PrefMaxX, max.x);
            EditorPrefs.SetInt(PrefMaxZ, max.y);
            if (floorPrefab != null)
                EditorPrefs.SetFloat(PrefFloorY, FloorHeight(floorPrefab));
            EditorSceneManager.MarkSceneDirty(scene);
            return "绑定 " + pieceCount + " 个棋子，范围 (" + min.x + "," + min.y + ")-(" + max.x + "," + max.y + ") floorY=" + EditorPrefs.GetFloat(PrefFloorY, -0.5f);
        }

        public static string ApplySceneFloors(int z)
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var config = AssetDatabase.LoadAssetAtPath<SokobanConfig>(ConfigPath);
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefab);
            var floorDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(FloorDefPath);
            if (config == null || floorPrefab == null || floorDef == null)
                return "请先替换关卡棋子资源。";

            var scene = EnsureSampleScene();
            var board = FindNamed(scene, "Board");
            if (board == null)
                return "场景里找不到 Board。";

            var minX = EditorPrefs.GetInt(PrefMinX, 0);
            var maxX = EditorPrefs.GetInt(PrefMaxX, 6);
            var minZ = EditorPrefs.GetInt(PrefMinZ, 0);
            var maxZ = EditorPrefs.GetInt(PrefMaxZ, 5);
            if (z < minZ || z > maxZ)
                return "skip z=" + z;

            var floorY = EditorPrefs.GetFloat(PrefFloorY, -0.5f);
            var existing = board.GetComponentsInChildren<SokobanPiece>(true);
            var created = 0;
            for (var x = minX; x <= maxX; x++)
            {
                var cell = new Vector2Int(x, z);
                if (HasFloor(existing, cell, config))
                    continue;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, board.transform);
                instance.transform.position = SokobanGrid.CellCenter(cell, floorY, config);
                instance.transform.rotation = Quaternion.identity;
                var item = PlacedItem.Ensure(instance, floorDef);
                item.EnsureInstanceId();
                EditorUtility.SetDirty(item);
                created++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            return "z=" + z + " 地面 +" + created + " y=" + floorY;
        }

        public static string ApplySceneFinish()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var scene = EnsureSampleScene();
            var ground = FindNamed(scene, "Ground");
            if (ground != null)
                Object.DestroyImmediate(ground);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return "地面已换成可编辑的墙体方块。";
        }

        public static string VerifyScene()
        {
            var scene = EnsureSampleScene();
            var board = FindNamed(scene, "Board");
            if (board == null)
                return "场景里找不到 Board。";

            var pieces = board.GetComponentsInChildren<SokobanPiece>(true);
            var floors = 0;
            var walls = 0;
            var boxes = 0;
            var goals = 0;
            var players = 0;
            var floorY = 0f;
            var wallY = 0f;
            var boxY = 0f;
            var goalY = 0f;
            var playerY = 0f;
            for (var i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                if (piece == null)
                    continue;
                var y = piece.transform.position.y;
                switch (piece.Role)
                {
                    case SokobanRole.Floor:
                        floors++;
                        floorY = y;
                        break;
                    case SokobanRole.Wall:
                        walls++;
                        wallY = y;
                        break;
                    case SokobanRole.Box:
                        boxes++;
                        boxY = y;
                        break;
                    case SokobanRole.Goal:
                        goals++;
                        goalY = y;
                        break;
                    case SokobanRole.Player:
                        players++;
                        playerY = y;
                        break;
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            var itemCount = catalog != null && catalog.Items != null ? catalog.Items.Count : -1;
            var ground = FindNamed(scene, "Ground");
            return "floors=" + floors +
                   " walls=" + walls +
                   " boxes=" + boxes +
                   " goals=" + goals +
                   " players=" + players +
                   " fy=" + floorY +
                   " wy=" + wallY +
                   " by=" + boxY +
                   " gy=" + goalY +
                   " py=" + playerY +
                   " ground=" + (ground != null) +
                   " catalog=" + itemCount +
                   " total=" + pieces.Length;
        }

        public static string ApplyScene()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var config = AssetDatabase.LoadAssetAtPath<SokobanConfig>(ConfigPath);
            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefab);
            var floorDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(FloorDefPath);
            var wallDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(WallDefPath);
            var boxDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(BoxDefPath);
            var goalDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(GoalDefPath);
            var playerDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(PlayerDefPath);
            if (config == null || catalog == null || floorPrefab == null)
                return "请先替换关卡棋子资源。";

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var board = FindNamed(scene, "Board");
            if (board == null)
                return "场景里找不到 Board。";

            var ground = FindNamed(scene, "Ground");
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            var pieces = board.GetComponentsInChildren<SokobanPiece>(true);
            for (var i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                if (piece == null || piece.Role == SokobanRole.Floor)
                    continue;
                var cell = SokobanGrid.WorldToCell(piece.transform.position, config);
                min.x = Mathf.Min(min.x, cell.x);
                min.y = Mathf.Min(min.y, cell.y);
                max.x = Mathf.Max(max.x, cell.x);
                max.y = Mathf.Max(max.y, cell.y);
                BindPieceDef(piece, playerDef, wallDef, boxDef, goalDef, floorDef);
            }

            if (min.x == int.MaxValue)
                return "棋盘上没有棋子。";

            var floorY = FloorHeight(floorPrefab);
            var existing = board.GetComponentsInChildren<SokobanPiece>(true);
            for (var z = min.y; z <= max.y; z++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var cell = new Vector2Int(x, z);
                    if (HasFloor(existing, cell, config))
                        continue;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, board.transform);
                    Undo.RegisterCreatedObjectUndo(instance, UndoName);
                    instance.transform.position = SokobanGrid.CellCenter(cell, floorY, config);
                    instance.transform.rotation = Quaternion.identity;
                    var item = PlacedItem.Ensure(instance, floorDef);
                    item.EnsureInstanceId();
                    EditorUtility.SetDirty(item);
                }
            }

            if (ground != null)
                Undo.DestroyObjectImmediate(ground);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return "地面已换成可编辑的墙体方块。";
        }

        static Scene EnsureSampleScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return scene;
        }

        static void AlignPieceHeight(SokobanPiece piece, SokobanConfig config)
        {
            if (piece == null || config == null)
                return;
            var cell = SokobanGrid.WorldToCell(piece.transform.position, config);
            var y = piece.transform.position.y;
            switch (piece.Role)
            {
                case SokobanRole.Wall:
                case SokobanRole.Box:
                case SokobanRole.Goal:
                    y = 0f;
                    break;
                case SokobanRole.Player:
                    y = 0.55f;
                    break;
                default:
                    return;
            }

            piece.transform.position = SokobanGrid.CellCenter(cell, y, config);
        }

        static void CopyThenEmbed(string source, string dest, string artPath, SokobanRole role)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(artPath) == null)
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dest) == null)
                AssetDatabase.CopyAsset(source, dest);
            EmbedArt(dest, artPath, role);
        }

        static void EmbedArt(string gameplayPath, string artPath, SokobanRole role)
        {
            var gameplay = AssetDatabase.LoadAssetAtPath<GameObject>(gameplayPath);
            var art = AssetDatabase.LoadAssetAtPath<GameObject>(artPath);
            if (gameplay == null || art == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(gameplayPath);
            try
            {
                HidePrimitiveVisual(contents);
                var old = contents.transform.Find(ArtChild);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var nested = (GameObject)PrefabUtility.InstantiatePrefab(art, contents.transform);
                PrefabUtility.UnpackPrefabInstance(nested, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                nested.name = ArtChild;
                nested.transform.localPosition = Vector3.zero;
                nested.transform.localRotation = Quaternion.identity;
                nested.transform.localScale = Vector3.one;
                contents.transform.localScale = Vector3.one;
                StripEditComponents(nested);

                var piece = contents.GetComponent<SokobanPiece>();
                if (piece == null)
                    piece = contents.AddComponent<SokobanPiece>();
                piece.BindRole(role);
                EditorUtility.SetDirty(piece);
                PrefabUtility.SaveAsPrefabAsset(contents, gameplayPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void HidePrimitiveVisual(GameObject root)
        {
            var renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;
            var filter = root.GetComponent<MeshFilter>();
            if (filter != null)
                filter.sharedMesh = null;
            var collider = root.GetComponent<BoxCollider>();
            if (collider != null)
                collider.enabled = false;
        }

        static void StripEditComponents(GameObject root)
        {
            var placed = root.GetComponentsInChildren<PlacedItem>(true);
            for (var i = 0; i < placed.Length; i++)
            {
                if (placed[i] != null)
                    Object.DestroyImmediate(placed[i]);
            }

            var pieces = root.GetComponentsInChildren<SokobanPiece>(true);
            for (var i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null && pieces[i].gameObject != root)
                    Object.DestroyImmediate(pieces[i]);
            }
        }

        static PlaceableItemDef EnsureDef(
            string path,
            string id,
            string displayName,
            string prefabPath,
            PlaceableCategoryDef category,
            PlaceableOccupancyLayer layer,
            int maxInstances)
        {
            var def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<PlaceableItemDef>();
                AssetDatabase.CreateAsset(def, path);
            }

            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("category").objectReferenceValue = category;
            so.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            so.FindProperty("occupancyLayer").intValue = (int)layer;
            so.FindProperty("maxInstances").intValue = maxInstances;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static void BindPlaced(string prefabPath, PlaceableItemDef def)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || def == null)
                return;
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PlacedItem.Ensure(contents, def);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void BindPieceDef(
            SokobanPiece piece,
            PlaceableItemDef playerDef,
            PlaceableItemDef wallDef,
            PlaceableItemDef boxDef,
            PlaceableItemDef goalDef,
            PlaceableItemDef floorDef)
        {
            PlaceableItemDef def;
            switch (piece.Role)
            {
                case SokobanRole.Player:
                    def = playerDef;
                    break;
                case SokobanRole.Box:
                    def = boxDef;
                    break;
                case SokobanRole.Goal:
                    def = goalDef;
                    break;
                case SokobanRole.Floor:
                    def = floorDef;
                    break;
                default:
                    def = wallDef;
                    break;
            }

            var item = PlacedItem.Ensure(piece.gameObject, def);
            item.EnsureInstanceId();
            EditorUtility.SetDirty(item);
        }

        static bool HasFloor(SokobanPiece[] pieces, Vector2Int cell, SokobanConfig config)
        {
            for (var i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                if (piece == null || piece.Role != SokobanRole.Floor)
                    continue;
                if (SokobanGrid.WorldToCell(piece.transform.position, config) == cell)
                    return true;
            }

            return false;
        }

        static float FloorHeight(GameObject prefab)
        {
            var contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
            try
            {
                var renderers = contents.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                    return -0.5f;
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                return -bounds.max.y;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static GameObject FindNamed(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindChildNamed(roots[i].transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        static Transform FindChildNamed(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildNamed(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
