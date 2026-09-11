using UnityEditor;
using UnityEngine;

namespace DZDMapEditor
{
    public static class MapEditorPaths
    {
        public const string SampleRoot = "Assets/DZDRuntimeMapEditor";
        public const string UserDataRoot = SampleRoot + "/Data";
        public const string UserPrefabRoot = SampleRoot + "/Prefabs";
        public const string UserItemsPrefabRoot = UserPrefabRoot + "/Items";
        public const string UserItemsDefRoot = UserDataRoot + "/Placement/Items";
        public const string UserCategoriesRoot = UserDataRoot + "/Placement/Categories";
        public const string UserIconFolder = SampleRoot + "/Art/Placement/Icons";
        public const string UserMaterialsRoot = SampleRoot + "/Materials";
        public const string PlacementGridMaterial = UserMaterialsRoot + "/PlacementGrid.mat";

        public const string StartMenuConfig = UserDataRoot + "/StartMenu/StartMenuConfig.asset";
        public const string MapSessionRequest = UserDataRoot + "/StartMenu/MapSessionRequest.asset";
        public const string PersistenceConfig = UserDataRoot + "/MapPersistence/MapPersistenceConfig.asset";
        public const string ToastChannel = UserDataRoot + "/Toast/ToastChannel.asset";
        public const string ToastHudConfig = UserDataRoot + "/Toast/ToastHudConfig.asset";
        public const string PlacementConfig = UserDataRoot + "/Placement/PlacementConfig.asset";
        public const string PlaceableCatalog = UserDataRoot + "/Placement/PlaceableCatalog.asset";
        public const string FlyConfig = UserDataRoot + "/Player/FirstPersonFlyConfig.asset";
        public const string PauseMenuConfig = UserDataRoot + "/PauseMenu/PauseMenuConfig.asset";
        public const string SettingsPanelConfig = UserDataRoot + "/Settings/SettingsPanelConfig.asset";
        public const string LocaleSettings = UserDataRoot + "/Settings/MapEditorLocaleSettings.asset";
        public const string SampleScenesRoot = SampleRoot + "/Scenes";
        public const string EmbeddedPackageRoot = SampleRoot + "/Package";
        public const string StartScene = SampleScenesRoot + "/StartScene.unity";
        public const string EditorScene = SampleScenesRoot + "/SampleScene.unity";
        public const string TestScene = SampleScenesRoot + "/TestScene.unity";
        public const string OverlaySettings = UserDataRoot + "/Placement/MapEditorOverlaySettings.asset";
        public const string UrpRenderer = "Assets/Settings/PC_Renderer.asset";
        public const string OptionalPauseBackground = SampleRoot + "/Resources/UI/BG.png";
        public const string LegacyChineseTmpFont = SampleRoot + "/Resources/Fonts/字制区喜脉体 SDF.asset";
        public const string LiberationSansTmpFont =
            "Assets/Packages/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        public static string PackageRoot
        {
            get
            {
                if (AssetDatabase.IsValidFolder(EmbeddedPackageRoot))
                    return EmbeddedPackageRoot;

                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MapEditorInfo).Assembly);
                if (info != null &&
                    !string.IsNullOrEmpty(info.assetPath) &&
                    AssetDatabase.IsValidFolder(info.assetPath))
                    return info.assetPath;

                return SampleRoot;
            }
        }

        public static string FlyerPrefab => PackageFile("Prefabs/Player/FirstPersonFlyer.prefab");
        public static string ToastHudPrefab => PackageFile("Prefabs/UI/ToastHud.prefab");
        public static string MapLoadPanelPrefab => PackageFile("Prefabs/UI/MapLoadPanel.prefab");
        public static string StartMenuPrefab => PackageFile("Prefabs/UI/StartMenu.prefab");
        public static string PauseMenuPrefab => PackageFile("Prefabs/UI/PauseMenu.prefab");
        public static string WarehouseSlotPrefab => PackageFile("Prefabs/Placement/WarehouseSlot.prefab");
        public static string WarehouseHotbarPrefab => PackageFile("Prefabs/Placement/WarehouseHotbar.prefab");
        public static string BackpackPanelPrefab => PackageFile("Prefabs/Placement/BackpackPanel.prefab");

        public static string PackageFile(string relative)
        {
            return PackageRoot + "/" + relative.Replace('\\', '/');
        }

        public static T LoadPackage<T>(string relative) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(PackageFile(relative));
        }
    }
}
