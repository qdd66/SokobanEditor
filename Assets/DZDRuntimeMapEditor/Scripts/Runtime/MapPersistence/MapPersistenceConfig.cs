using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "MapPersistenceConfig",
        menuName = MapEditorInfo.CreateMenu + "/Map Persistence Config")]
    public sealed class MapPersistenceConfig : ScriptableObject
    {
        public const int CurrentFormatVersion = 1;
        public const string FileExtension = ".tabsmap";

        [Title("文件")]
        [LabelText("默认文件名")]
        [Tooltip("还没打开过地图时，保存使用这个名字。")]
        [SerializeField]
        string defaultFileName = "Map";

        [LabelText("编辑器写到工程 Maps 目录")]
        [Tooltip("勾选后在 Editor 里把文件写到工程根目录的 Maps 文件夹，方便打开查看。打包后仍写到 persistentDataPath。")]
        [SerializeField]
        bool useProjectMapsFolderInEditor = true;

        [Title("撤回")]
        [LabelText("最大步数")]
        [Tooltip("超过后丢掉最早的操作。")]
        [SerializeField, MinValue(1)]
        int maxUndoSteps = 32;

        [Title("内容")]
        [LabelText("保存相机位置")]
        [Tooltip("加载后把飞行视角放到保存时的位置。")]
        [SerializeField]
        bool saveCamera = true;

        [Title("快捷键")]
        [LabelText("保存")]
        [RuntimeEditable(SettingsCategory.Control, "Save", "保存")]
        [SerializeField]
        Key saveKey = Key.S;

        [LabelText("保存需要 Ctrl")]
        [RuntimeEditable(SettingsCategory.Control, "Save Requires Ctrl", "保存需要 Ctrl")]
        [SerializeField]
        bool saveRequiresCtrl = true;

        [LabelText("读取列表")]
        [Tooltip("打开本地地图列表。")]
        [RuntimeEditable(SettingsCategory.Control, "Load List", "读取列表")]
        [SerializeField]
        Key loadKey = Key.O;

        [LabelText("读取需要 Ctrl")]
        [RuntimeEditable(SettingsCategory.Control, "Load Requires Ctrl", "读取需要 Ctrl")]
        [SerializeField]
        bool loadRequiresCtrl = true;

        [LabelText("撤回")]
        [RuntimeEditable(SettingsCategory.Control, "Undo", "撤回")]
        [SerializeField]
        Key undoKey = Key.Z;

        [LabelText("撤回需要 Ctrl")]
        [RuntimeEditable(SettingsCategory.Control, "Undo Requires Ctrl", "撤回需要 Ctrl")]
        [SerializeField]
        bool undoRequiresCtrl = true;

        [LabelText("重做")]
        [RuntimeEditable(SettingsCategory.Control, "Redo", "重做")]
        [SerializeField]
        Key redoKey = Key.Y;

        [LabelText("重做需要 Ctrl")]
        [RuntimeEditable(SettingsCategory.Control, "Redo Requires Ctrl", "重做需要 Ctrl")]
        [SerializeField]
        bool redoRequiresCtrl = true;

        [LabelText("关闭读取面板")]
        [RuntimeEditable(SettingsCategory.Control, "Close Load Panel", "关闭读取面板")]
        [SerializeField]
        Key closeLoadPanelKey = Key.Escape;

        [Title("提示")]
        [LabelText("提示频道")]
        [SerializeField, AssetsOnly]
        ToastChannel toastChannel;

        [LabelText("已保存")]
        [SerializeField]
        LocalizedText savedHint = new LocalizedText("Saved {0}", "已保存 {0}");

        [LabelText("已加载")]
        [SerializeField]
        LocalizedText loadedHint = new LocalizedText("Loaded {0}", "已加载 {0}");

        [LabelText("保存失败")]
        [SerializeField]
        LocalizedText saveFailedHint = new LocalizedText("Save failed", "保存失败");

        [LabelText("加载失败")]
        [SerializeField]
        LocalizedText loadFailedHint = new LocalizedText("Load failed", "加载失败");

        [LabelText("没有可撤回的操作")]
        [SerializeField]
        LocalizedText nothingToUndoHint = new LocalizedText("Nothing to undo", "没有可撤回的操作");

        [LabelText("已撤回")]
        [SerializeField]
        LocalizedText undoneHint = new LocalizedText("Undone", "已撤回");

        [LabelText("已重做")]
        [SerializeField]
        LocalizedText redoneHint = new LocalizedText("Redone", "已重做");

        [LabelText("没有地图文件")]
        [SerializeField]
        LocalizedText noMapsHint = new LocalizedText("No saved maps yet", "还没有保存过的地图");

        [LabelText("跳过缺失物品")]
        [SerializeField]
        LocalizedText missingItemsHint = new LocalizedText(
            "{0} objects were skipped because their definitions are missing",
            "有 {0} 个物体找不到定义，已跳过");

        [Title("改名与删除")]
        [LabelText("列表提示")]
        [Tooltip("读取列表窗口上的说明。")]
        [SerializeField]
        LocalizedText loadListHint = new LocalizedText(
            "Click a row to load · Rename or delete on the right · Esc to close",
            "点击一项加载 · 右侧可改名或删除 · Esc 关闭");

        [LabelText("改名")]
        [SerializeField]
        LocalizedText renameLabel = new LocalizedText("Rename", "改名");

        [LabelText("删除")]
        [SerializeField]
        LocalizedText deleteLabel = new LocalizedText("Delete", "删除");

        [LabelText("改名占位")]
        [SerializeField]
        LocalizedText renamePlaceholder = new LocalizedText("New name", "输入新名称");

        [LabelText("确认删除标题")]
        [SerializeField]
        LocalizedText confirmDeleteTitle = new LocalizedText("Delete Save", "删除存档");

        [LabelText("确认删除说明")]
        [Tooltip("{0} 是存档显示名。")]
        [SerializeField]
        LocalizedText confirmDeleteHint = new LocalizedText(
            "Delete \"{0}\"? The file is removed from disk and cannot be undone.",
            "确定删除「{0}」？文件会从磁盘去掉，无法撤回。");

        [LabelText("确认删除按钮")]
        [SerializeField]
        LocalizedText confirmDeleteLabel = new LocalizedText("Delete", "删除");

        [LabelText("取消")]
        [SerializeField]
        LocalizedText cancelLabel = new LocalizedText("Cancel", "取消");

        [LabelText("已改名")]
        [SerializeField]
        LocalizedText renamedHint = new LocalizedText("Renamed to {0}", "已改名为 {0}");

        [LabelText("改名失败")]
        [SerializeField]
        LocalizedText renameFailedHint = new LocalizedText("Rename failed", "改名失败");

        [LabelText("名称为空")]
        [SerializeField]
        LocalizedText emptyRenameHint = new LocalizedText("Please enter a new name", "请输入新名称");

        [LabelText("名称已被占用")]
        [SerializeField]
        LocalizedText nameTakenHint = new LocalizedText("A save with that name already exists", "已有同名存档");

        [LabelText("已删除")]
        [SerializeField]
        LocalizedText deletedHint = new LocalizedText("Deleted {0}", "已删除 {0}");

        [LabelText("删除失败")]
        [SerializeField]
        LocalizedText deleteFailedHint = new LocalizedText("Delete failed", "删除失败");

        public string DefaultFileName => defaultFileName;
        public bool UseProjectMapsFolderInEditor => useProjectMapsFolderInEditor;
        public int MaxUndoSteps => Mathf.Max(1, maxUndoSteps);
        public bool SaveCamera => saveCamera;
        public Key SaveKey => saveKey;
        public bool SaveRequiresCtrl => saveRequiresCtrl;
        public Key LoadKey => loadKey;
        public bool LoadRequiresCtrl => loadRequiresCtrl;
        public Key UndoKey => undoKey;
        public bool UndoRequiresCtrl => undoRequiresCtrl;
        public Key RedoKey => redoKey;
        public bool RedoRequiresCtrl => redoRequiresCtrl;
        public Key CloseLoadPanelKey => closeLoadPanelKey;
        public ToastChannel ToastChannel => toastChannel;
        public string SavedHint => savedHint.Get();
        public string LoadedHint => loadedHint.Get();
        public string SaveFailedHint => saveFailedHint.Get();
        public string LoadFailedHint => loadFailedHint.Get();
        public string NothingToUndoHint => nothingToUndoHint.Get();
        public string UndoneHint => undoneHint.Get();
        public string RedoneHint => redoneHint.Get();
        public string NoMapsHint => noMapsHint.Get();
        public string MissingItemsHint => missingItemsHint.Get();
        public string LoadListHint => loadListHint.Get();
        public string RenameLabel => renameLabel.Get();
        public string DeleteLabel => deleteLabel.Get();
        public string RenamePlaceholder => renamePlaceholder.Get();
        public string ConfirmDeleteTitle => confirmDeleteTitle.Get();
        public string ConfirmDeleteHint => confirmDeleteHint.Get();
        public string ConfirmDeleteLabel => confirmDeleteLabel.Get();
        public string CancelLabel => cancelLabel.Get();
        public string RenamedHint => renamedHint.Get();
        public string RenameFailedHint => renameFailedHint.Get();
        public string EmptyRenameHint => emptyRenameHint.Get();
        public string NameTakenHint => nameTakenHint.Get();
        public string DeletedHint => deletedHint.Get();
        public string DeleteFailedHint => deleteFailedHint.Get();
    }
}
