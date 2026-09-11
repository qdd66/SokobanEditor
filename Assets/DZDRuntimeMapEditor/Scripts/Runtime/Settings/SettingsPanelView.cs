using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [LabelText("标题")]
        [SerializeField]
        TMP_Text title;

        [LabelText("提示")]
        [SerializeField]
        TMP_Text hint;

        [LabelText("返回")]
        [SerializeField]
        Button backButton;

        [LabelText("返回文字")]
        [SerializeField]
        TMP_Text backLabel;

        [LabelText("返回开始界面")]
        [SerializeField]
        Button returnToStartButton;

        [LabelText("返回开始界面文字")]
        [SerializeField]
        TMP_Text returnToStartLabel;

        [LabelText("控制页签")]
        [SerializeField]
        Button controlTab;

        [LabelText("角色页签")]
        [SerializeField]
        Button characterTab;

        [LabelText("放置页签")]
        [SerializeField]
        Button placementTab;

        [LabelText("推箱子页签")]
        [SerializeField]
        Button sokobanTab;

        [LabelText("控制页签文字")]
        [SerializeField]
        TMP_Text controlTabLabel;

        [LabelText("角色页签文字")]
        [SerializeField]
        TMP_Text characterTabLabel;

        [LabelText("放置页签文字")]
        [SerializeField]
        TMP_Text placementTabLabel;

        [LabelText("推箱子页签文字")]
        [SerializeField]
        TMP_Text sokobanTabLabel;

        [LabelText("内容根节点")]
        [SerializeField, Required("请指定内容根节点")]
        RectTransform contentRoot;

        [LabelText("行模板")]
        [SerializeField, Required("请指定行模板")]
        SettingsSettingRowView rowTemplate;

        [LabelText("分组模板")]
        [SerializeField]
        TMP_Text groupTemplate;

        SettingsPanelConfig config;
        SettingsCategory category;
        readonly List<GameObject> spawned = new List<GameObject>();
        readonly List<SettingsSettingRowView> rows = new List<SettingsSettingRowView>();
        List<RuntimeSettingField> keyFields = new List<RuntimeSettingField>();
        SettingsSettingRowView listeningRow;
        readonly Color tabOn = new Color(0.28f, 0.28f, 0.32f, 1f);
        readonly Color tabOff = new Color(0.16f, 0.16f, 0.18f, 1f);
        bool rebuildQueued;

        public event Action BackClicked;
        public event Action ReturnToStartClicked;

        public bool IsListening => listeningRow != null;

        public string WaitingForKeyText =>
            config != null ? config.WaitingForKey : MapEditorLocale.Pick("Press a new key…", "按下新按键…");

        public IReadOnlyList<RuntimeSettingField> AllKeyFields => keyFields;

        public void ApplyConfig(SettingsPanelConfig panelConfig)
        {
            config = panelConfig;
            if (config == null)
                return;
            if (title != null)
                title.text = config.Title;
            if (hint != null)
                hint.text = config.Hint;
            if (backLabel != null)
                backLabel.text = config.BackLabel;
            if (returnToStartLabel != null)
                returnToStartLabel.text = config.ReturnToStartLabel;
            if (controlTabLabel != null)
                controlTabLabel.text = config.ControlTab;
            if (characterTabLabel != null)
                characterTabLabel.text = config.CharacterTab;
            if (placementTabLabel != null)
                placementTabLabel.text = config.PlacementTab;
            if (sokobanTabLabel != null)
                sokobanTabLabel.text = config.SokobanTab;
        }

        public void Show(SettingsPanelConfig panelConfig)
        {
            ApplyConfig(panelConfig);
            category = SettingsCategory.Control;
            Rebuild();
        }

        public void BeginKeyListen(SettingsSettingRowView row)
        {
            if (row == null)
                return;
            CancelListen();
            listeningRow = row;
            row.Refresh(true);
        }

        public void CancelListen()
        {
            var row = listeningRow;
            listeningRow = null;
            if (row != null)
                row.Refresh(false);
        }

        public string FormatConflict(IReadOnlyList<string> names)
        {
            return config != null ? config.FormatConflict(names) : string.Empty;
        }

        void OnEnable()
        {
            Bind(backButton, HandleBack);
            Bind(returnToStartButton, HandleReturnToStart);
            Bind(controlTab, () => SelectTab(SettingsCategory.Control));
            Bind(characterTab, () => SelectTab(SettingsCategory.Character));
            Bind(placementTab, () => SelectTab(SettingsCategory.Placement));
            Bind(sokobanTab, () => SelectTab(SettingsCategory.Sokoban));
            MapEditorLocale.Changed += HandleLocaleChanged;
        }

        void OnDisable()
        {
            MapEditorLocale.Changed -= HandleLocaleChanged;
            rebuildQueued = false;
            StopAllCoroutines();
            CancelListen();
        }

        void HandleLocaleChanged()
        {
            if (config == null || !isActiveAndEnabled)
                return;
            ApplyConfig(config);
            if (!rebuildQueued)
            {
                rebuildQueued = true;
                StartCoroutine(RebuildAfterLanguageChange());
            }
        }

        IEnumerator RebuildAfterLanguageChange()
        {
            yield return null;
            rebuildQueued = false;
            if (config != null && isActiveAndEnabled)
                Rebuild();
        }

        void Update()
        {
            if (listeningRow == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            for (var i = 0; i < keyboard.allKeys.Count; i++)
            {
                var control = keyboard.allKeys[i];
                if (control == null || !control.wasPressedThisFrame)
                    continue;

                var key = control.keyCode;
                if (key == Key.None || key == Key.Escape)
                    continue;

                ApplyKey(key);
                return;
            }
        }

        void SelectTab(SettingsCategory next)
        {
            if (category == next && spawned.Count > 0)
                return;
            category = next;
            Rebuild();
        }

        void Rebuild()
        {
            CancelListen();
            ClearSpawned();
            if (config == null || contentRoot == null || rowTemplate == null)
            {
                RefreshTabs();
                return;
            }

            var sources = config.CollectSources();
            keyFields = RuntimeSettingBinder.CollectKeys(sources);
            var fields = RuntimeSettingBinder.Collect(sources, category);
            ScriptableObject lastTarget = null;
            var showGroups = category == SettingsCategory.Control || category == SettingsCategory.Sokoban;
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (showGroups && field.Target != lastTarget)
                {
                    SpawnGroup(config.GetGroupLabel(field.Target));
                    lastTarget = field.Target;
                }

                SpawnRow(field);
            }

            RefreshTabs();
        }

        void SpawnGroup(string text)
        {
            if (groupTemplate == null || string.IsNullOrEmpty(text))
                return;
            var instance = Instantiate(groupTemplate, contentRoot);
            instance.gameObject.SetActive(true);
            instance.text = text;
            spawned.Add(instance.gameObject);
        }

        void SpawnRow(RuntimeSettingField field)
        {
            var instance = Instantiate(rowTemplate, contentRoot);
            instance.gameObject.SetActive(true);
            instance.Bind(field, this);
            spawned.Add(instance.gameObject);
            rows.Add(instance);
        }

        void ApplyKey(Key key)
        {
            var row = listeningRow;
            if (row == null)
                return;

            var field = row.Field;
            var conflicts = RuntimeSettingBinder.FindKeyConflicts(keyFields, field, key);
            field.SetValue(key);
            listeningRow = null;
            for (var i = 0; i < rows.Count; i++)
                rows[i].Refresh(false);

            if (conflicts.Count > 0 && config != null && config.ToastChannel != null)
                config.ToastChannel.Raise(config.FormatConflict(conflicts));
        }

        void ClearSpawned()
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                    Destroy(spawned[i]);
            }

            spawned.Clear();
            rows.Clear();
        }

        void RefreshTabs()
        {
            Tint(controlTab, category == SettingsCategory.Control);
            Tint(characterTab, category == SettingsCategory.Character);
            Tint(placementTab, category == SettingsCategory.Placement);
            Tint(sokobanTab, category == SettingsCategory.Sokoban);
        }

        void Tint(Button button, bool on)
        {
            if (button == null)
                return;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = on ? tabOn : tabOff;
        }

        void HandleBack()
        {
            CancelListen();
            BackClicked?.Invoke();
        }

        void HandleReturnToStart()
        {
            CancelListen();
            ReturnToStartClicked?.Invoke();
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
