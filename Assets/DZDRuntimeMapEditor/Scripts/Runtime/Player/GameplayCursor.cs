using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameplayCursor : MonoBehaviour
    {
        [LabelText("启用时锁定")]
        [Tooltip("组件启用后立刻锁定并隐藏鼠标。")]
        [SerializeField]
        bool lockOnEnable = true;

        [LabelText("解锁键")]
        [Tooltip("没有暂停界面占用时，按这个键解除鼠标锁定。左键点击画面会重新锁定。")]
        [SerializeField]
        Key unlockKey = Key.Escape;

        bool uiHold;

        bool ignoreUnlockThisFrame;

        public bool IsLocked { get; private set; }
        public bool RelockedThisFrame { get; private set; }
        public bool IsUiHold => uiHold;

        void OnEnable()
        {
            if (lockOnEnable)
                SetLocked(true);
        }

        void OnDisable()
        {
            SetLocked(false);
        }

        void Update()
        {
            RelockedThisFrame = false;
            var ignoreUnlock = ignoreUnlockThisFrame;
            ignoreUnlockThisFrame = false;
            if (uiHold)
                return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (IsLocked &&
                !ignoreUnlock &&
                keyboard != null &&
                keyboard[unlockKey].wasPressedThisFrame)
            {
                SetLocked(false);
                return;
            }

            if (!IsLocked &&
                mouse != null &&
                mouse.leftButton.wasPressedThisFrame)
            {
                RelockedThisFrame = true;
                SetLocked(true);
            }
        }

        public void IgnoreUnlockThisFrame()
        {
            ignoreUnlockThisFrame = true;
        }

        public void ForceLock()
        {
            RelockedThisFrame = !IsLocked;
            SetLocked(true);
        }

        public void SetUiHold(bool hold)
        {
            uiHold = hold;
            if (hold)
            {
                SetLocked(false);
                return;
            }

            if (!IsLocked)
                RelockedThisFrame = true;
            SetLocked(true);
        }

        void SetLocked(bool locked)
        {
            IsLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
