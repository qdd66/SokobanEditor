#if !ODIN_INSPECTOR
using System;

namespace Sirenix.OdinInspector
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TitleAttribute : Attribute
    {
        public string Title;
        public string Subtitle;

        public TitleAttribute(string title)
        {
            Title = title;
        }

        public TitleAttribute(string title, string subtitle)
        {
            Title = title;
            Subtitle = subtitle;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class LabelTextAttribute : Attribute
    {
        public string Text;

        public LabelTextAttribute(string text)
        {
            Text = text;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class SuffixLabelAttribute : Attribute
    {
        public string Label;
        public bool Overlay;

        public SuffixLabelAttribute(string label)
        {
            Label = label;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class RequiredAttribute : Attribute
    {
        public string ErrorMessage;

        public RequiredAttribute()
        {
        }

        public RequiredAttribute(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class AssetsOnlyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class SceneObjectsOnlyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class ChildGameObjectsOnlyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class PreviewFieldAttribute : Attribute
    {
        public PreviewFieldAttribute()
        {
        }

        public PreviewFieldAttribute(int height)
        {
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class MinValueAttribute : Attribute
    {
        public double MinValue;

        public MinValueAttribute(double min)
        {
            MinValue = min;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class MaxValueAttribute : Attribute
    {
        public double MaxValue;

        public MaxValueAttribute(double max)
        {
            MaxValue = max;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class PropertyRangeAttribute : Attribute
    {
        public double Min;
        public double Max;

        public PropertyRangeAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class EnumToggleButtonsAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class InfoBoxAttribute : Attribute
    {
        public string Message;

        public InfoBoxAttribute(string message)
        {
            Message = message;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class InlineEditorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class ListDrawerSettingsAttribute : Attribute
    {
        public bool DraggableItems;
        public bool ShowFoldout;
        public bool ShowItemCount;
        public int NumberOfItemsPerPage;
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class ShowInInspectorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class ReadOnlyAttribute : Attribute
    {
    }
}
#endif
