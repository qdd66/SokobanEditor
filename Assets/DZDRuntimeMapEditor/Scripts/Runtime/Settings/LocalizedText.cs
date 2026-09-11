using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace DZDMapEditor
{
    [Serializable]
    [Preserve]
    public struct LocalizedText
    {
        [LabelText("English")]
        public string english;

        [LabelText("中文")]
        public string chinese;

        public LocalizedText(string english, string chinese)
        {
            this.english = english ?? string.Empty;
            this.chinese = chinese ?? string.Empty;
        }

        public string Get()
        {
            return MapEditorLocale.Pick(english, chinese);
        }
    }
}
