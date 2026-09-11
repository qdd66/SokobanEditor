using System;
using UnityEngine.Scripting;

namespace DZDMapEditor
{
    [AttributeUsage(AttributeTargets.Field)]
    [Preserve]
    public sealed class RuntimeEditableAttribute : Attribute
    {
        public RuntimeEditableAttribute(
            SettingsCategory category,
            string label = null,
            string chineseLabel = null,
            float sliderMin = float.NaN,
            float sliderMax = float.NaN,
            string suffix = null,
            string suffixChinese = null)
        {
            Category = category;
            Label = label ?? string.Empty;
            ChineseLabel = chineseLabel ?? string.Empty;
            SliderMin = sliderMin;
            SliderMax = sliderMax;
            Suffix = suffix ?? string.Empty;
            SuffixChinese = suffixChinese ?? string.Empty;
        }

        public SettingsCategory Category { get; }

        public string Label { get; }

        public string ChineseLabel { get; }

        public string Suffix { get; }

        public string SuffixChinese { get; }

        public float SliderMin { get; }

        public float SliderMax { get; }

        public string DisplayLabel => MapEditorLocale.Pick(Label, ChineseLabel);

        public string DisplaySuffix => MapEditorLocale.Pick(Suffix, SuffixChinese);
    }
}
