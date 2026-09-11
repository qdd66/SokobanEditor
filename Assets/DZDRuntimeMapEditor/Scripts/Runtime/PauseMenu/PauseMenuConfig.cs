using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "PauseMenuConfig",
        menuName = MapEditorInfo.CreateMenu + "/Pause Menu Config")]
    public sealed class PauseMenuConfig : ScriptableObject
    {
        [Title("快捷键")]
        [LabelText("暂停")]
        [Tooltip("打开或关闭暂停界面。保存/读取子页打开时先关掉子页。")]
        [RuntimeEditable(SettingsCategory.Control, "Pause", "暂停")]
        [SerializeField]
        Key pauseKey = Key.Escape;

        [Title("暂停")]
        [LabelText("暂停游戏时间")]
        [Tooltip("勾选后暂停时 Time.timeScale 为 0，飞行和塑造都会停。")]
        [SerializeField]
        bool pauseTimeScale = true;

        [Title("文案")]
        [LabelText("标题")]
        [SerializeField]
        LocalizedText title = new LocalizedText("Paused", "暂停");

        [LabelText("提示")]
        [SerializeField]
        LocalizedText hint = new LocalizedText("Esc to keep editing", "Esc 继续编辑");

        [LabelText("回到游戏")]
        [SerializeField]
        LocalizedText resumeLabel = new LocalizedText("Resume", "回到游戏");

        [LabelText("保存场景")]
        [SerializeField]
        LocalizedText saveLabel = new LocalizedText("Save Map", "保存场景");

        [LabelText("加载场景")]
        [SerializeField]
        LocalizedText loadLabel = new LocalizedText("Load Map", "加载场景");

        [LabelText("设置")]
        [SerializeField]
        LocalizedText settingsLabel = new LocalizedText("Settings", "设置");

        [Title("保存页")]
        [LabelText("保存标题")]
        [SerializeField]
        LocalizedText saveTitle = new LocalizedText("Save Map", "保存场景");

        [LabelText("保存提示")]
        [SerializeField]
        LocalizedText saveHint = new LocalizedText(
            "Click a save to overwrite · Rename or delete on the right · Type a name to create a new save",
            "点击已有存档覆盖 · 右侧可改名或删除 · 输入名称后建立新存档");

        [LabelText("名称占位")]
        [SerializeField]
        LocalizedText namePlaceholder = new LocalizedText("Save name", "输入存档名称");

        [LabelText("建立新存档")]
        [SerializeField]
        LocalizedText createLabel = new LocalizedText("Create New Save", "建立新存档");

        [LabelText("返回")]
        [SerializeField]
        LocalizedText backLabel = new LocalizedText("Back", "返回");

        [LabelText("名称为空")]
        [SerializeField]
        LocalizedText emptyNameHint = new LocalizedText("Please enter a save name", "请输入存档名称");

        public Key PauseKey => pauseKey;
        public bool PauseTimeScale => pauseTimeScale;
        public string Title => title.Get();
        public string Hint => hint.Get();
        public string ResumeLabel => resumeLabel.Get();
        public string SaveLabel => saveLabel.Get();
        public string LoadLabel => loadLabel.Get();
        public string SettingsLabel => settingsLabel.Get();
        public string SaveTitle => saveTitle.Get();
        public string SaveHint => saveHint.Get();
        public string NamePlaceholder => namePlaceholder.Get();
        public string CreateLabel => createLabel.Get();
        public string BackLabel => backLabel.Get();
        public string EmptyNameHint => emptyNameHint.Get();
    }
}
