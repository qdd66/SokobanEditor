using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

namespace DZDMapEditor
{
    [Preserve]
    public readonly struct RuntimeSettingField
    {
        public RuntimeSettingField(
            ScriptableObject target,
            FieldInfo field,
            RuntimeEditableAttribute attribute)
        {
            Target = target;
            Field = field;
            Attribute = attribute;
        }

        public ScriptableObject Target { get; }

        public FieldInfo Field { get; }

        public RuntimeEditableAttribute Attribute { get; }

        public string Label
        {
            get
            {
                if (Attribute != null &&
                    (!string.IsNullOrWhiteSpace(Attribute.Label) ||
                     !string.IsNullOrWhiteSpace(Attribute.ChineseLabel)))
                    return Attribute.DisplayLabel;
#if UNITY_EDITOR
                var label = Field.GetCustomAttribute<LabelTextAttribute>();
                if (label != null && !string.IsNullOrWhiteSpace(label.Text))
                    return label.Text;
#endif
                return Field.Name;
            }
        }

        public string Suffix
        {
            get
            {
                if (Attribute != null &&
                    (!string.IsNullOrWhiteSpace(Attribute.Suffix) ||
                     !string.IsNullOrWhiteSpace(Attribute.SuffixChinese)))
                    return Attribute.DisplaySuffix;
#if UNITY_EDITOR
                var suffix = Field.GetCustomAttribute<SuffixLabelAttribute>();
                if (suffix != null && !string.IsNullOrWhiteSpace(suffix.Label))
                    return suffix.Label;
#endif
                return string.Empty;
            }
        }

        public object GetValue()
        {
            return Field.GetValue(Target);
        }

        public void SetValue(object value)
        {
            Field.SetValue(Target, value);
            if (Target is MapEditorLocaleSettings localeSettings)
            {
                localeSettings.ApplyFromSerialized();
                return;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(Target);
#endif
        }
    }

    [Preserve]
    public static class RuntimeSettingBinder
    {
        public static List<RuntimeSettingField> Collect(
            IReadOnlyList<ScriptableObject> sources,
            SettingsCategory category)
        {
            var list = new List<RuntimeSettingField>();
            if (sources == null)
                return list;

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null)
                    continue;

                var fields = source.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (var f = 0; f < fields.Length; f++)
                {
                    var attr = fields[f].GetCustomAttribute<RuntimeEditableAttribute>();
                    if (attr == null || attr.Category != category)
                        continue;
                    if (!IsSupported(fields[f].FieldType))
                        continue;
                    list.Add(new RuntimeSettingField(source, fields[f], attr));
                }
            }

            return list;
        }

        public static List<RuntimeSettingField> CollectKeys(IReadOnlyList<ScriptableObject> sources)
        {
            var list = new List<RuntimeSettingField>();
            if (sources == null)
                return list;

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null)
                    continue;

                var fields = source.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (var f = 0; f < fields.Length; f++)
                {
                    var field = fields[f];
                    if (field.FieldType != typeof(Key))
                        continue;
                    var attr = field.GetCustomAttribute<RuntimeEditableAttribute>();
                    if (attr == null)
                        continue;
                    list.Add(new RuntimeSettingField(source, field, attr));
                }
            }

            return list;
        }

        public static bool IsSupported(Type type)
        {
            if (type == typeof(float) || type == typeof(int) || type == typeof(bool) || type == typeof(Key))
                return true;
            return type != null && type.IsEnum && type != typeof(Key);
        }

        public static void GetSliderRange(RuntimeSettingField field, float current, out float min, out float max)
        {
            min = 0f;
            max = Mathf.Max(1f, Mathf.Abs(current) * 4f);
#if UNITY_EDITOR
            var range = field.Field.GetCustomAttribute<PropertyRangeAttribute>();
            if (range != null)
            {
                min = (float)range.Min;
                max = (float)range.Max;
                ApplyAttributeRange(field, ref min, ref max);
                return;
            }

            var minAttr = field.Field.GetCustomAttribute<MinValueAttribute>();
            var maxAttr = field.Field.GetCustomAttribute<MaxValueAttribute>();
            if (minAttr != null)
                min = (float)minAttr.MinValue;
            if (maxAttr != null)
                max = (float)maxAttr.MaxValue;
#endif
            ApplyAttributeRange(field, ref min, ref max);
            if (max < min)
                max = min;
        }

        static void ApplyAttributeRange(RuntimeSettingField field, ref float min, ref float max)
        {
            if (field.Attribute == null)
                return;
            if (!float.IsNaN(field.Attribute.SliderMin))
                min = field.Attribute.SliderMin;
            if (!float.IsNaN(field.Attribute.SliderMax))
                max = field.Attribute.SliderMax;
        }

        public static string FormatValue(RuntimeSettingField field, object value)
        {
            if (field.Field.FieldType == typeof(int))
                return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

            if (field.Field.FieldType == typeof(float))
            {
                var number = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                var abs = Mathf.Abs(number);
                if (abs >= 100f)
                    return number.ToString("0", CultureInfo.InvariantCulture);
                if (abs >= 10f)
                    return number.ToString("0.#", CultureInfo.InvariantCulture);
                return number.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return value != null ? value.ToString() : string.Empty;
        }

        public static string FormatKey(Key key)
        {
            return MapEditorLocale.Current == MapEditorLanguage.Chinese
                ? FormatKeyChinese(key)
                : FormatKeyEnglish(key);
        }

        static string FormatKeyEnglish(Key key)
        {
            if (key == Key.None)
                return "None";

            switch (key)
            {
                case Key.Space: return "Space";
                case Key.Escape: return "Esc";
                case Key.Enter: return "Enter";
                case Key.Tab: return "Tab";
                case Key.Backspace: return "Backspace";
                case Key.Delete: return "Delete";
                case Key.Insert: return "Insert";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "Page Up";
                case Key.PageDown: return "Page Down";
                case Key.LeftShift: return "Left Shift";
                case Key.RightShift: return "Right Shift";
                case Key.LeftCtrl: return "Left Ctrl";
                case Key.RightCtrl: return "Right Ctrl";
                case Key.LeftAlt: return "Left Alt";
                case Key.RightAlt: return "Right Alt";
                case Key.LeftArrow: return "Left Arrow";
                case Key.RightArrow: return "Right Arrow";
                case Key.UpArrow: return "Up Arrow";
                case Key.DownArrow: return "Down Arrow";
                case Key.CapsLock: return "Caps Lock";
                case Key.NumLock: return "Num Lock";
                case Key.PrintScreen: return "Print Screen";
                case Key.Pause: return "Pause";
                case Key.ContextMenu: return "Menu";
            }

            return SplitKeyName(key);
        }

        static string FormatKeyChinese(Key key)
        {
            if (key == Key.None)
                return "无";

            switch (key)
            {
                case Key.Space: return "空格";
                case Key.Escape: return "Esc";
                case Key.Enter: return "回车";
                case Key.Tab: return "Tab";
                case Key.Backspace: return "退格";
                case Key.Delete: return "删除";
                case Key.Insert: return "插入";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "上一页";
                case Key.PageDown: return "下一页";
                case Key.LeftShift: return "左 Shift";
                case Key.RightShift: return "右 Shift";
                case Key.LeftCtrl: return "左 Ctrl";
                case Key.RightCtrl: return "右 Ctrl";
                case Key.LeftAlt: return "左 Alt";
                case Key.RightAlt: return "右 Alt";
                case Key.LeftArrow: return "左方向";
                case Key.RightArrow: return "右方向";
                case Key.UpArrow: return "上方向";
                case Key.DownArrow: return "下方向";
                case Key.CapsLock: return "大写锁定";
                case Key.NumLock: return "数字锁定";
                case Key.PrintScreen: return "截屏";
                case Key.Pause: return "Pause";
                case Key.ContextMenu: return "菜单键";
            }

            var raw = key.ToString();
            if (raw.StartsWith("Digit", StringComparison.Ordinal) && raw.Length == 6)
                return raw.Substring(5);
            if (raw.StartsWith("Numpad", StringComparison.Ordinal) && raw.Length > 6)
                return "小键盘 " + TranslateKeyToken(raw.Substring(6));

            var parts = SplitKeyName(key).Split(' ');
            var builder = new StringBuilder();
            for (var i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                    builder.Append(' ');
                builder.Append(TranslateKeyToken(parts[i]));
            }

            return builder.ToString();
        }

        static string SplitKeyName(Key key)
        {
            var raw = key.ToString();
            if (raw.StartsWith("Digit", StringComparison.Ordinal) && raw.Length == 6)
                return raw.Substring(5);
            if (raw.StartsWith("Numpad", StringComparison.Ordinal) && raw.Length > 6)
                return "Numpad " + raw.Substring(6);

            var builder = new StringBuilder(raw.Length + 8);
            for (var i = 0; i < raw.Length; i++)
            {
                var c = raw[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(raw[i - 1]))
                    builder.Append(' ');
                builder.Append(c);
            }

            return builder.ToString();
        }

        static string TranslateKeyToken(string token)
        {
            switch (token)
            {
                case "Left": return "左";
                case "Right": return "右";
                case "Up": return "上";
                case "Down": return "下";
                case "Space": return "空格";
                case "Escape": return "Esc";
                case "Enter": return "回车";
                case "Backspace": return "退格";
                case "Delete": return "删除";
                case "Insert": return "插入";
                case "Arrow": return "方向";
                case "Shift": return "Shift";
                case "Ctrl": return "Ctrl";
                case "Alt": return "Alt";
                case "Command": return "Command";
                case "Numpad": return "小键盘";
                default: return token;
            }
        }

        public static string FormatEnum(Type enumType, object value)
        {
            var name = Enum.GetName(enumType, value);
            if (string.IsNullOrEmpty(name))
                return value != null ? value.ToString() : string.Empty;

            var member = enumType.GetField(name);
            if (member == null)
                return name;

            var runtime = member.GetCustomAttribute<RuntimeLabelAttribute>();
            if (runtime != null &&
                (!string.IsNullOrWhiteSpace(runtime.Text) ||
                 !string.IsNullOrWhiteSpace(runtime.ChineseText)))
                return runtime.Display;
#if UNITY_EDITOR
            var label = member.GetCustomAttribute<LabelTextAttribute>();
            if (label != null && !string.IsNullOrWhiteSpace(label.Text))
                return label.Text;
#endif
            return name;
        }

        public static List<string> FindKeyConflicts(
            IReadOnlyList<RuntimeSettingField> keyFields,
            RuntimeSettingField current,
            Key key)
        {
            var names = new List<string>();
            if (key == Key.None || keyFields == null)
                return names;

            for (var i = 0; i < keyFields.Count; i++)
            {
                var other = keyFields[i];
                if (other.Target == current.Target && other.Field == current.Field)
                    continue;
                if (other.Field.FieldType != typeof(Key))
                    continue;
                if (current.Attribute != null &&
                    other.Attribute != null &&
                    current.Attribute.Category != other.Attribute.Category)
                    continue;
                if (!other.GetValue().Equals(key))
                    continue;
                if (KeyRequiresCtrl(current) != KeyRequiresCtrl(other))
                    continue;
                names.Add(other.Label);
            }

            return names;
        }

        static bool KeyRequiresCtrl(RuntimeSettingField field)
        {
            if (field.Target == null || field.Field == null)
                return false;

            var fieldName = field.Field.Name;
            if (!fieldName.EndsWith("Key", StringComparison.Ordinal))
                return false;

            var prefix = fieldName.Substring(0, fieldName.Length - 3);
            var ctrlField = field.Target.GetType().GetField(
                prefix + "RequiresCtrl",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (ctrlField == null || ctrlField.FieldType != typeof(bool))
                return false;

            var value = ctrlField.GetValue(field.Target);
            return value is bool required && required;
        }
    }
}
