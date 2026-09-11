using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "StartMenuConfig",
        menuName = MapEditorInfo.CreateMenu + "/Start Menu Config")]
    public sealed class StartMenuConfig : ScriptableObject
    {
        [Title("场景")]
        [LabelText("编辑场景名")]
        [Tooltip("Build Settings 里的场景名，新建和读档都会加载它。")]
        [SerializeField]
        string editorSceneName = "SampleScene";

        [Title("快捷键")]
        [LabelText("关闭存档列表")]
        [SerializeField]
        Key closeLoadPanelKey = Key.Escape;

        [Title("文案")]
        [LabelText("标题")]
        [SerializeField]
        LocalizedText title = new LocalizedText("Map Editor", "地图编辑器");

        [LabelText("提示")]
        [SerializeField]
        LocalizedText hint = new LocalizedText("Open a saved map, or start a new one", "打开已有存档，或从默认场景新建");

        [LabelText("打开地图存档")]
        [SerializeField]
        LocalizedText openSaveLabel = new LocalizedText("Open Map", "打开地图存档");

        [LabelText("新建地图")]
        [SerializeField]
        LocalizedText newMapLabel = new LocalizedText("New Map", "新建地图");

        public string EditorSceneName => editorSceneName;
        public Key CloseLoadPanelKey => closeLoadPanelKey;
        public string Title => title.Get();
        public string Hint => hint.Get();
        public string OpenSaveLabel => openSaveLabel.Get();
        public string NewMapLabel => newMapLabel.Get();
    }
}
