using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class SettingsSettingRowView : MonoBehaviour
    {
        [LabelText("名称")]
        [SerializeField]
        TMP_Text label;

        [LabelText("滑条")]
        [SerializeField]
        Slider slider;

        [LabelText("数值")]
        [SerializeField]
        TMP_Text valueLabel;

        [LabelText("开关")]
        [SerializeField]
        Toggle toggle;

        [LabelText("按键")]
        [SerializeField]
        Button keyButton;

        [LabelText("按键文字")]
        [SerializeField]
        TMP_Text keyLabel;

        [LabelText("冲突")]
        [SerializeField]
        TMP_Text conflictLabel;

        [LabelText("枚举")]
        [SerializeField]
        Button enumButton;

        [LabelText("枚举文字")]
        [SerializeField]
        TMP_Text enumLabel;

        RuntimeSettingField field;
        SettingsPanelView host;
        bool suppress;

        public RuntimeSettingField Field => field;

        public void Bind(RuntimeSettingField setting, SettingsPanelView panel)
        {
            field = setting;
            host = panel;
            if (label != null)
                label.text = setting.Label;

            var type = setting.Field.FieldType;
            var isKey = type == typeof(Key);
            var isBool = type == typeof(bool);
            var isNumber = type == typeof(float) || type == typeof(int);
            var isEnum = type.IsEnum && !isKey;

            SetActive(slider, isNumber);
            SetActive(valueLabel, isNumber);
            SetActive(toggle, isBool);
            SetActive(keyButton, isKey);
            SetActive(enumButton, isEnum);
            SetActive(conflictLabel, isKey);

            Unbind();
            if (isNumber && slider != null)
                slider.onValueChanged.AddListener(HandleSlider);
            if (isBool && toggle != null)
                toggle.onValueChanged.AddListener(HandleToggle);
            if (isKey && keyButton != null)
                keyButton.onClick.AddListener(HandleKeyClick);
            if (isEnum && enumButton != null)
                enumButton.onClick.AddListener(HandleEnumClick);

            Refresh(false);
        }

        public void Refresh(bool listening)
        {
            if (field.Field == null || field.Target == null)
                return;

            suppress = true;
            var type = field.Field.FieldType;
            var value = field.GetValue();
            if (type == typeof(float) || type == typeof(int))
            {
                var current = Convert.ToSingle(value);
                RuntimeSettingBinder.GetSliderRange(field, current, out var min, out var max);
                if (slider != null)
                {
                    slider.minValue = min;
                    slider.maxValue = max;
                    slider.wholeNumbers = type == typeof(int);
                    slider.SetValueWithoutNotify(Mathf.Clamp(current, min, max));
                }

                if (valueLabel != null)
                {
                    var text = RuntimeSettingBinder.FormatValue(field, value);
                    if (!string.IsNullOrEmpty(field.Suffix))
                        text += " " + field.Suffix;
                    valueLabel.text = text;
                }
            }
            else if (type == typeof(bool) && toggle != null)
            {
                toggle.SetIsOnWithoutNotify((bool)value);
            }
            else if (type == typeof(Key))
            {
                var key = (Key)value;
                if (keyLabel != null)
                    keyLabel.text = listening && host != null
                        ? host.WaitingForKeyText
                        : RuntimeSettingBinder.FormatKey(key);
                RefreshConflict(key);
            }
            else if (type.IsEnum && enumLabel != null)
            {
                enumLabel.text = RuntimeSettingBinder.FormatEnum(type, value);
            }

            suppress = false;
        }

        void OnDestroy()
        {
            Unbind();
        }

        void Unbind()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(HandleSlider);
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(HandleToggle);
            if (keyButton != null)
                keyButton.onClick.RemoveListener(HandleKeyClick);
            if (enumButton != null)
                enumButton.onClick.RemoveListener(HandleEnumClick);
        }

        void HandleSlider(float value)
        {
            if (suppress || field.Field == null)
                return;
            if (field.Field.FieldType == typeof(int))
                field.SetValue(Mathf.RoundToInt(value));
            else
                field.SetValue(value);
            Refresh(false);
        }

        void HandleToggle(bool value)
        {
            if (suppress || field.Field == null)
                return;
            field.SetValue(value);
        }

        void HandleEnumClick()
        {
            if (suppress || field.Field == null)
                return;

            var values = Enum.GetValues(field.Field.FieldType);
            if (values.Length == 0)
                return;

            var current = field.GetValue();
            var index = 0;
            for (var i = 0; i < values.Length; i++)
            {
                if (values.GetValue(i).Equals(current))
                {
                    index = i;
                    break;
                }
            }

            var next = values.GetValue((index + 1) % values.Length);
            field.SetValue(next);
            Refresh(false);
        }

        void HandleKeyClick()
        {
            if (host != null)
                host.BeginKeyListen(this);
        }

        void RefreshConflict(Key key)
        {
            if (conflictLabel == null || host == null)
                return;
            var names = RuntimeSettingBinder.FindKeyConflicts(host.AllKeyFields, field, key);
            var text = host.FormatConflict(names);
            conflictLabel.text = text;
            conflictLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }
    }
}
