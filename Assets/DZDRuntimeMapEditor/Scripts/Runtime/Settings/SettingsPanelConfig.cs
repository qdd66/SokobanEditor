using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "SettingsPanelConfig",
        menuName = MapEditorInfo.CreateMenu + "/Settings Panel Config")]
    public sealed class SettingsPanelConfig : ScriptableObject
    {
        [Title("数据")]
        [LabelText("飞行配置")]
        [SerializeField, Required("请指定飞行配置"), AssetsOnly]
        FirstPersonFlyConfig flyConfig;

        [LabelText("放置配置")]
        [SerializeField, Required("请指定放置配置"), AssetsOnly]
        PlacementConfig placementConfig;

        [LabelText("暂停配置")]
        [SerializeField, Required("请指定暂停配置"), AssetsOnly]
        PauseMenuConfig pauseMenuConfig;

        [LabelText("存档配置")]
        [SerializeField, Required("请指定存档配置"), AssetsOnly]
        MapPersistenceConfig persistenceConfig;

        [LabelText("语言配置")]
        [SerializeField, Required("请指定语言配置"), AssetsOnly]
        MapEditorLocaleSettings localeSettings;

        [LabelText("开始界面配置")]
        [Tooltip("返回开始界面时读取场景名。可空则加载 StartScene。")]
        [SerializeField, AssetsOnly]
        StartMenuConfig startMenuConfig;

        [Title("外观")]
        [LabelText("面板背景")]
        [Tooltip("设置窗口默认背景。")]
        [SerializeField, Required("请指定面板背景"), AssetsOnly]
        Sprite panelBackground;

        [Title("文案")]
        [LabelText("标题")]
        [SerializeField]
        LocalizedText title = new LocalizedText("Settings", "设置");

        [LabelText("提示")]
        [SerializeField]
        LocalizedText hint = new LocalizedText(
            "Changes write to the config immediately. Esc cancels a key bind or returns to pause.",
            "改动立刻写入配置。Esc 取消改键或返回暂停。");

        [LabelText("返回")]
        [SerializeField]
        LocalizedText backLabel = new LocalizedText("Back", "返回");

        [LabelText("返回开始界面")]
        [SerializeField]
        LocalizedText returnToStartLabel = new LocalizedText("Return to Start", "返回开始界面");

        [LabelText("开始场景缺失")]
        [Tooltip("Build Settings 里找不到开始场景时的提示。")]
        [SerializeField]
        LocalizedText missingStartSceneHint = new LocalizedText(
            "Start scene is not in Build Settings",
            "开始场景未加入 Build Settings");

        [LabelText("控制")]
        [SerializeField]
        LocalizedText controlTab = new LocalizedText("Controls", "控制");

        [LabelText("角色操控")]
        [SerializeField]
        LocalizedText characterTab = new LocalizedText("Character", "角色操控");

        [LabelText("物品摆放")]
        [SerializeField]
        LocalizedText placementTab = new LocalizedText("Placement", "物品摆放");

        [LabelText("推箱子")]
        [SerializeField]
        LocalizedText sokobanTab = new LocalizedText("Sokoban", "推箱子");

        [LabelText("等待按键")]
        [SerializeField]
        LocalizedText waitingForKey = new LocalizedText("Press a new key…", "按下新按键…");

        [LabelText("按键冲突")]
        [Tooltip("{0} 为其它功能名称。")]
        [SerializeField]
        LocalizedText keyConflictHint = new LocalizedText("Same as {0}", "与「{0}」相同");

        [Title("分组标题")]
        [LabelText("常规")]
        [SerializeField]
        LocalizedText generalGroup = new LocalizedText("General", "常规");

        [LabelText("角色飞行")]
        [SerializeField]
        LocalizedText flyGroup = new LocalizedText("Flight", "角色飞行");

        [LabelText("物品摆放分组")]
        [SerializeField]
        LocalizedText placementGroup = new LocalizedText("Placement", "物品摆放");

        [LabelText("暂停分组")]
        [SerializeField]
        LocalizedText pauseGroup = new LocalizedText("Pause", "暂停");

        [LabelText("存档分组")]
        [SerializeField]
        LocalizedText persistenceGroup = new LocalizedText("Saves", "存档");

        [LabelText("推箱子分组")]
        [SerializeField]
        LocalizedText sokobanGroup = new LocalizedText("Sokoban", "推箱子");

        [Title("额外数据")]
        [LabelText("额外设置源")]
        [Tooltip("会出现在设置面板里的其它配置，例如推箱子玩法配置。")]
        [SerializeField]
        List<ScriptableObject> extraSettingSources = new List<ScriptableObject>();

        [Title("提示")]
        [LabelText("提示频道")]
        [Tooltip("改键冲突时发短提示。可空则只在行内显示。")]
        [SerializeField, AssetsOnly]
        ToastChannel toastChannel;

        public FirstPersonFlyConfig FlyConfig => flyConfig;
        public PlacementConfig PlacementConfig => placementConfig;
        public PauseMenuConfig PauseMenuConfig => pauseMenuConfig;
        public MapPersistenceConfig PersistenceConfig => persistenceConfig;
        public MapEditorLocaleSettings LocaleSettings => localeSettings;
        public StartMenuConfig StartMenuConfig => startMenuConfig;
        public Sprite PanelBackground => panelBackground;
        public string Title => title.Get();
        public string Hint => hint.Get();
        public string BackLabel => backLabel.Get();
        public string ReturnToStartLabel => returnToStartLabel.Get();
        public string MissingStartSceneHint => missingStartSceneHint.Get();
        public string StartSceneName
        {
            get
            {
                if (startMenuConfig != null && !string.IsNullOrEmpty(startMenuConfig.StartSceneName))
                    return startMenuConfig.StartSceneName;
                return "StartScene";
            }
        }
        public string ControlTab => controlTab.Get();
        public string CharacterTab => characterTab.Get();
        public string PlacementTab => placementTab.Get();
        public string SokobanTab => sokobanTab.Get();
        public string WaitingForKey => waitingForKey.Get();
        public string KeyConflictHint => keyConflictHint.Get();
        public ToastChannel ToastChannel => toastChannel;

        public IReadOnlyList<ScriptableObject> CollectSources()
        {
            var list = new List<ScriptableObject>
            {
                localeSettings,
                flyConfig,
                placementConfig,
                pauseMenuConfig,
                persistenceConfig
            };

            if (extraSettingSources == null)
                return list;

            for (var i = 0; i < extraSettingSources.Count; i++)
            {
                var extra = extraSettingSources[i];
                if (extra == null || list.Contains(extra))
                    continue;
                list.Add(extra);
            }

            return list;
        }

        public string GetGroupLabel(ScriptableObject source)
        {
            if (source == localeSettings)
                return generalGroup.Get();
            if (source == flyConfig)
                return flyGroup.Get();
            if (source == placementConfig)
                return placementGroup.Get();
            if (source == pauseMenuConfig)
                return pauseGroup.Get();
            if (source == persistenceConfig)
                return persistenceGroup.Get();
            if (IsExtraSource(source))
                return sokobanGroup.Get();
            return source != null ? source.name : string.Empty;
        }

        public void EnsureExtraSource(ScriptableObject source)
        {
            if (source == null)
                return;
            if (extraSettingSources == null)
                extraSettingSources = new List<ScriptableObject>();
            if (extraSettingSources.Contains(source))
                return;
            extraSettingSources.Add(source);
        }

        public string GetTabLabel(SettingsCategory category)
        {
            switch (category)
            {
                case SettingsCategory.Character:
                    return characterTab.Get();
                case SettingsCategory.Placement:
                    return placementTab.Get();
                case SettingsCategory.Sokoban:
                    return sokobanTab.Get();
                default:
                    return controlTab.Get();
            }
        }

        public string FormatConflict(IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0)
                return string.Empty;
            return string.Format(keyConflictHint.Get(), MapEditorLocale.JoinList(names));
        }

        bool IsExtraSource(ScriptableObject source)
        {
            if (source == null || extraSettingSources == null)
                return false;
            return extraSettingSources.Contains(source);
        }
    }
}
