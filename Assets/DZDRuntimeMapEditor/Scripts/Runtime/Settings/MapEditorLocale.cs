using System;
using UnityEngine;

namespace DZDMapEditor
{
    public static class MapEditorLocale
    {
        public const string PrefsKey = "DZDMapEditor.Language";

        public static MapEditorLanguage Current { get; private set; } = MapEditorLanguage.English;

        public static event Action Changed;

        public static bool HasSavedPreference => PlayerPrefs.HasKey(PrefsKey);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPlayModeState()
        {
            Changed = null;
            Current = MapEditorLanguage.English;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadBeforeScene()
        {
            Current = ReadPreference();
        }

        public static MapEditorLanguage ReadPreference()
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
                return MapEditorLanguage.English;
            return PlayerPrefs.GetInt(PrefsKey, 0) == (int)MapEditorLanguage.Chinese
                ? MapEditorLanguage.Chinese
                : MapEditorLanguage.English;
        }

        public static string Pick(string english, string chinese)
        {
            if (Current == MapEditorLanguage.Chinese)
            {
                if (!string.IsNullOrEmpty(chinese))
                    return chinese;
                return english ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(english))
                return english;
            return chinese ?? string.Empty;
        }

        public static string JoinList(System.Collections.Generic.IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0)
                return string.Empty;
            var separator = Current == MapEditorLanguage.Chinese ? "、" : ", ";
            return string.Join(separator, names);
        }

        public static void Set(MapEditorLanguage language, bool persist)
        {
            if (persist)
            {
                PlayerPrefs.SetInt(PrefsKey, (int)language);
                PlayerPrefs.Save();
            }

            if (Current == language)
                return;

            Current = language;
            Changed?.Invoke();
        }
    }
}
