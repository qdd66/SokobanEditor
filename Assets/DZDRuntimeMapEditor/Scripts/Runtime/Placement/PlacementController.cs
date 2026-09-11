using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    [DefaultExecutionOrder(-50)]
    [InfoBox("世界拾取：准心对准已放物体后左键拿起。仓库：滚轮切换，F 选定。B 打开背包，点击图标取出并关闭。V 高度调节（预览时鼠标上下改高度，锁定落点，不跟已放物品碰撞）。C 贴合表面（随落点法线倾斜）。CapsLock 旋转调节（预览时鼠标转物体）。默认打开连续摆放和网格放置；X 开关连续摆放（保留旋转缩放高度），Y 开关网格放置（水平吸到格子中心，网格线画在最底层，一格一件；按住 Ctrl 时不切换）。触发方式（点按或长按）在放置配置里选。左键放下；拿起已放物品时右键删除，从仓库拿出时右键取消。Q/E 旋转，Shift+Q/E 缩放（长按或单击在放置设置里选）。")]
    public sealed class PlacementController : MonoBehaviour
    {
        [LabelText("放置配置")]
        [Tooltip("射线、幽灵、旋转缩放和快捷键。")]
        [SerializeField, Required("请指定放置配置"), AssetsOnly, InlineEditor]
        PlacementConfig config;

        [LabelText("放置相机")]
        [Tooltip("用于准心射线检测的相机，一般是第一人称相机。")]
        [SerializeField, Required("请指定放置相机")]
        Camera placementCamera;

        [LabelText("鼠标锁定")]
        [Tooltip("只有鼠标锁定时才响应世界里的放置和拾取。")]
        [SerializeField, Required("请指定鼠标锁定组件")]
        GameplayCursor gameplayCursor;

        [LabelText("悬停描边")]
        [Tooltip("准心对准已放物体时的描边表现。")]
        [SerializeField, Required("请指定悬停描边组件")]
        OutlineSelectionVisual outline;

        [LabelText("已放物体根节点")]
        [Tooltip("确认放置后物体挂到这个节点下。可空。")]
        [SerializeField]
        Transform placedItemsRoot;

        [LabelText("仓库热键栏")]
        [Tooltip("底部物品栏。可空则关闭仓库选取。")]
        [SerializeField]
        WarehouseHotbarView warehouseHotbar;

        [LabelText("背包面板")]
        [Tooltip("按分类浏览全部物品。可空则关闭背包。")]
        [SerializeField]
        BackpackPanelView backpack;

        [LabelText("放置模式")]
        [Tooltip("高度调节、贴合表面、旋转调节、连续摆放和网格放置的开关状态。")]
        [SerializeField, Required("请指定放置模式")]
        PlacementModeState modeState;

        [LabelText("网格显示")]
        [Tooltip("网格放置开启时画出准心附近的格子。可空则只吸附不画线。")]
        [SerializeField]
        PlacementGridVisual gridVisual;

        [LabelText("地图编辑会话")]
        [Tooltip("记录放置和删除，供撤回使用。可空则不记历史。")]
        [SerializeField]
        MapEditSession editSession;

        [Title("调试")]
        [LabelText("调试投放")]
        [InfoBox("Play 模式下把预制体或场景物体拖到这里即可拿起。")]
        [SerializeField]
        GameObject debugDrop;

        readonly KeyboardMousePlacementInput input = new KeyboardMousePlacementInput();
        readonly KeyboardMouseWarehouseInput warehouseInput = new KeyboardMouseWarehouseInput();
        readonly PlacementRaycaster raycaster = new PlacementRaycaster();
        readonly GhostPreview ghost = new GhostPreview();
        readonly PlacementGridOccupancy occupancy = new PlacementGridOccupancy();
        PlacementSession session;
        PlacementHit lastPlacementHit;

        void Awake()
        {
            session = new PlacementSession(ghost, outline);
            ApplyDefaultModes();
            if (backpack != null)
            {
                backpack.ItemPicked += HandleBackpackItemPicked;
                backpack.Closed += HandleBackpackClosed;
            }
        }

        void OnDestroy()
        {
            if (backpack == null)
                return;
            backpack.ItemPicked -= HandleBackpackItemPicked;
            backpack.Closed -= HandleBackpackClosed;
        }

        void OnDisable()
        {
            session?.Abort();
            if (gridVisual != null)
                gridVisual.Hide();
            if (modeState == null)
                return;

            modeState.SetPreviewing(false);
            if (config == null)
                return;
            if (config.HeightAdjustSwitchStyle == ModeSwitchStyle.Hold)
                modeState.SetHeightAdjust(false);
            if (config.ContinuousSwitchStyle == ModeSwitchStyle.Hold)
                modeState.SetContinuous(false);
            if (config.SnapToSurfaceSwitchStyle == ModeSwitchStyle.Hold)
                modeState.SetSnapToSurface(false);
            if (config.RotateAdjustSwitchStyle == ModeSwitchStyle.Hold)
                modeState.SetRotateAdjust(false);
            if (config.GridPlaceSwitchStyle == ModeSwitchStyle.Hold)
                modeState.SetGrid(false);
        }

        void Update()
        {
            ConsumeDebugDrop();
            if (warehouseHotbar != null)
                warehouseHotbar.gameObject.SetActive(true);

            if (config == null || placementCamera == null || session == null)
            {
                if (gridVisual != null)
                    gridVisual.Hide();
                return;
            }

            TickWarehouse();
            TickModes();

            var allowWorldButtons = gameplayCursor != null &&
                                    gameplayCursor.IsLocked &&
                                    !gameplayCursor.RelockedThisFrame &&
                                    !gameplayCursor.IsUiHold;
            var heightAdjust = modeState != null && modeState.HeightAdjust;
            var continuousPlace = modeState != null && modeState.Continuous;
            var gridPlace = modeState != null && modeState.Grid;
            var snapToSurface = modeState != null && modeState.SnapToSurface && !gridPlace;
            var rotateAdjust = modeState != null && modeState.RotateAdjust;
            var lockSurface = heightAdjust && session.IsPreviewing;
            var liveHit = raycaster.Cast(placementCamera, config);
            PlacementHit hit;
            if (lockSurface && lastPlacementHit.HasHit)
            {
                hit = lastPlacementHit;
            }
            else
            {
                hit = liveHit;
                if (liveHit.HasHit)
                    lastPlacementHit = liveHit;
            }

            var followHit = hit;
            var occupied = false;
            var blockReason = PlacementBlockReason.None;
            if (gridPlace && hit.HasHit)
                followHit = PlacementGrid.SnapHit(hit, config);
            if (session.IsPreviewing && hit.HasHit)
            {
                blockReason = occupancy.Evaluate(
                    placedItemsRoot,
                    PlacementGrid.WorldToCell(followHit.Point, config),
                    session.GhostTransform,
                    config,
                    session.CurrentDefinition,
                    session.IsRelocatingExisting,
                    gridPlace);
                occupied = blockReason != PlacementBlockReason.None;
            }

            var mutation = session.Tick(
                input.Read(config),
                followHit,
                config,
                placedItemsRoot,
                Time.deltaTime,
                allowWorldButtons,
                heightAdjust,
                continuousPlace,
                snapToSurface,
                rotateAdjust,
                gridPlace,
                occupied,
                out var rejectedOccupied);
            if (rejectedOccupied)
            {
                if (blockReason == PlacementBlockReason.InstanceCap)
                    RaiseToast(config.GridInstanceCapHint);
                else
                    RaiseToast(config.GridOccupiedHint);
            }
            if (editSession != null && mutation.HasValue)
                editSession.HandlePlacement(mutation);
            if (modeState != null)
                modeState.SetPreviewing(session.IsPreviewing);

            if (gridVisual == null)
                return;
            if (gridPlace && followHit.HasHit && (gameplayCursor == null || !gameplayCursor.IsUiHold))
                gridVisual.Show(config, followHit, occupied);
            else
                gridVisual.Hide();
        }

        public void AbortSession()
        {
            session?.Abort();
            if (gridVisual != null)
                gridVisual.Hide();
            if (modeState != null)
                modeState.SetPreviewing(false);
        }

        void ApplyDefaultModes()
        {
            if (modeState == null || config == null)
                return;

            if (config.ContinuousSwitchStyle != ModeSwitchStyle.Hold)
                modeState.SetContinuous(config.DefaultContinuousPlace);
            if (config.GridPlaceSwitchStyle != ModeSwitchStyle.Hold)
                modeState.SetGrid(config.DefaultGridPlace);
        }

        void TickWarehouse()
        {
            if (gameplayCursor != null && gameplayCursor.IsUiHold && (backpack == null || !backpack.IsOpen))
                return;

            var warehouse = warehouseInput.Read(config);
            if (warehouse.ToggleBackpackPressed)
                ToggleBackpack();

            if (backpack != null && backpack.IsOpen)
                return;

            if (warehouseHotbar == null)
                return;

            warehouseHotbar.ApplyScroll(warehouse.ScrollSteps);
            if (!warehouse.ConfirmPressed)
                return;

            BeginPlaceFromWarehouse(warehouseHotbar.Selected);
        }

        void TickModes()
        {
            if (modeState == null || config == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var blockPress = (backpack != null && backpack.IsOpen) ||
                             (gameplayCursor != null && gameplayCursor.IsUiHold);

            TickMode(
                keyboard,
                config.ToggleHeightAdjustKey,
                config.HeightAdjustSwitchStyle,
                modeState.HeightAdjust,
                modeState.SetHeightAdjust,
                config.HeightAdjustOnHint,
                config.HeightAdjustOffHint,
                blockPress,
                false);
            TickMode(
                keyboard,
                config.ToggleContinuousPlaceKey,
                config.ContinuousSwitchStyle,
                modeState.Continuous,
                modeState.SetContinuous,
                config.ContinuousOnHint,
                config.ContinuousOffHint,
                blockPress,
                false);
            TickMode(
                keyboard,
                config.ToggleSnapToSurfaceKey,
                config.SnapToSurfaceSwitchStyle,
                modeState.SnapToSurface,
                modeState.SetSnapToSurface,
                config.SnapToSurfaceOnHint,
                config.SnapToSurfaceOffHint,
                blockPress,
                false);
            TickMode(
                keyboard,
                config.ToggleRotateAdjustKey,
                config.RotateAdjustSwitchStyle,
                modeState.RotateAdjust,
                modeState.SetRotateAdjust,
                config.RotateAdjustOnHint,
                config.RotateAdjustOffHint,
                blockPress,
                false);
            TickMode(
                keyboard,
                config.ToggleGridPlaceKey,
                config.GridPlaceSwitchStyle,
                modeState.Grid,
                modeState.SetGrid,
                config.GridPlaceOnHint,
                config.GridPlaceOffHint,
                blockPress,
                true);
        }

        void TickMode(
            Keyboard keyboard,
            Key key,
            ModeSwitchStyle style,
            bool current,
            Action<bool> set,
            string onHint,
            string offHint,
            bool blockPress,
            bool ignoreWhenCtrl)
        {
            if (key == Key.None)
                return;

            var control = keyboard[key];
            if (control == null)
                return;

            var next = current;
            if (style == ModeSwitchStyle.Hold)
                next = !blockPress && control.isPressed;
            else if (!blockPress &&
                     control.wasPressedThisFrame &&
                     !(ignoreWhenCtrl && CtrlOrCommandHeld(keyboard)))
                next = !current;

            if (next == current)
                return;

            set(next);
            RaiseToast(next ? onHint : offHint);
        }

        static bool CtrlOrCommandHeld(Keyboard keyboard)
        {
            return keyboard.leftCtrlKey.isPressed ||
                   keyboard.rightCtrlKey.isPressed ||
                   keyboard.leftCommandKey.isPressed ||
                   keyboard.rightCommandKey.isPressed;
        }

        void RaiseToast(string message)
        {
            if (config != null && config.ToastChannel != null)
                config.ToastChannel.Raise(message);
        }

        void ToggleBackpack()
        {
            if (backpack == null)
                return;

            if (backpack.IsOpen)
                backpack.SetOpen(false);
            else
                OpenBackpack();
        }

        void OpenBackpack()
        {
            backpack.SetOpen(true);
            if (gameplayCursor != null)
                gameplayCursor.SetUiHold(true);
        }

        void HandleBackpackItemPicked(PlaceableItemDef def)
        {
            if (warehouseHotbar != null)
                warehouseHotbar.Select(def);
            BeginPlaceFromWarehouse(def);
        }

        void HandleBackpackClosed()
        {
            if (gameplayCursor != null)
                gameplayCursor.SetUiHold(false);
        }

        void BeginPlaceFromWarehouse(PlaceableItemDef selected)
        {
            if (selected != null && selected.Prefab != null)
                session.BeginNew(selected, config);
        }

        void ConsumeDebugDrop()
        {
            if (debugDrop == null || session == null || !Application.isPlaying)
                return;

            var dropped = debugDrop;
            debugDrop = null;

            if (dropped.transform == transform || dropped.transform.IsChildOf(transform))
                return;

            var inLoadedScene = dropped.scene.IsValid() && dropped.scene.isLoaded;
            if (inLoadedScene)
                session.BeginExisting(PlacedItem.Ensure(dropped, null), config);
            else
                session.BeginNew(dropped, config);

            if (gameplayCursor != null)
                gameplayCursor.ForceLock();
        }
    }
}
