using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "MapEditorLocaleSettings",
        menuName = MapEditorInfo.CreateMenu + "/Locale Settings")]
    public sealed class MapEditorLocaleSettings : ScriptableObject
    {
        [Title("语言")]
        [LabelText("界面语言")]
        [Tooltip("运行时设置里可改。默认英文。玩家选择写入 PlayerPrefs，不会改这个资源的默认值。")]
        [RuntimeEditable(SettingsCategory.Control, "Language", "语言")]
        [SerializeField]
        MapEditorLanguage language = MapEditorLanguage.English;

        public MapEditorLanguage Language => language;

        void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            if (MapEditorLocale.HasSavedPreference)
            {
                var saved = MapEditorLocale.ReadPreference();
                language = saved;
                MapEditorLocale.Set(saved, persist: false);
            }
            else
            {
                MapEditorLocale.Set(language, persist: false);
            }
        }

        void OnDisable()
        {
            language = MapEditorLanguage.English;
        }

        public void ApplyFromSerialized()
        {
            MapEditorLocale.Set(language, persist: true);
        }
    }
}
