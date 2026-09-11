using System;
using UnityEngine.Scripting;

namespace DZDMapEditor
{
    [AttributeUsage(AttributeTargets.Field)]
    [Preserve]
    public sealed class RuntimeLabelAttribute : Attribute
    {
        public RuntimeLabelAttribute(string text, string chineseText = null)
        {
            Text = text ?? string.Empty;
            ChineseText = chineseText ?? string.Empty;
        }

        public string Text { get; }

        public string ChineseText { get; }

        public string Display => MapEditorLocale.Pick(Text, ChineseText);
    }
}
