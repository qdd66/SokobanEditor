using DZDMapEditor;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sokoban.PlayEdit
{
    [DefaultExecutionOrder(-25)]
    [InfoBox("默认推箱子。F1 切到 DZD 飞行放置，再按 F1 回到玩法并重编棋盘。Esc 暂停仍可用。")]
    public sealed class SokobanPlayEditController : MonoBehaviour
    {
        [Title("玩法")]
        [LabelText("玩法配置")]
        [SerializeField, Required("请指定玩法配置"), AssetsOnly]
        SokobanConfig config;

        [LabelText("推箱子会话")]
        [SerializeField, Required("请指定推箱子会话"), SceneObjectsOnly]
        SokobanSession session;

        [LabelText("俯视相机")]
        [SerializeField, Required("请指定俯视相机"), SceneObjectsOnly]
        SokobanSceneCamera playCamera;

        [LabelText("玩法摄像机")]
        [SerializeField, Required("请指定玩法摄像机"), SceneObjectsOnly]
        Camera playView;

        [LabelText("玩法听者")]
        [SerializeField, SceneObjectsOnly]
        AudioListener playListener;

        [LabelText("HUD")]
        [SerializeField, SceneObjectsOnly]
        SokobanHud hud;

        [Title("编辑器")]
        [LabelText("飞行")]
        [SerializeField, Required("请指定飞行控制器"), SceneObjectsOnly]
        FirstPersonFlyController fly;

        [LabelText("编辑摄像机")]
        [SerializeField, Required("请指定编辑摄像机"), SceneObjectsOnly]
        Camera editView;

        [LabelText("编辑听者")]
        [SerializeField, SceneObjectsOnly]
        AudioListener editListener;

        [LabelText("放置")]
        [SerializeField, SceneObjectsOnly]
        PlacementController placement;

        [LabelText("鼠标锁定")]
        [SerializeField, SceneObjectsOnly]
        GameplayCursor gameplayCursor;

        [LabelText("准星")]
        [Tooltip("编辑模式才显示屏幕中心准星。玩法时关闭。")]
        [SerializeField, SceneObjectsOnly]
        ScreenCrosshair crosshair;

        [LabelText("仓库")]
        [SerializeField, SceneObjectsOnly]
        WarehouseHotbarView warehouse;

        [LabelText("背包")]
        [SerializeField, SceneObjectsOnly]
        BackpackPanelView backpack;

        [LabelText("暂停")]
        [SerializeField, SceneObjectsOnly]
        PauseMenuController pause;

        [LabelText("存档")]
        [SerializeField, SceneObjectsOnly]
        MapPersistenceController persistence;

        [Title("提示")]
        [LabelText("提示频道")]
        [SerializeField, AssetsOnly]
        ToastChannel toastChannel;

        [LabelText("缺少玩家")]
        [SerializeField]
        LocalizedText missingPlayerHint = new LocalizedText("Need a player", "至少需要一名玩家");

        [LabelText("缺少目标")]
        [SerializeField]
        LocalizedText missingGoalHint = new LocalizedText("Need a goal", "至少需要一个目标点");

        [LabelText("箱子不足")]
        [SerializeField]
        LocalizedText tooFewBoxesHint = new LocalizedText(
            "Fewer boxes than goals; cannot win",
            "箱子少于目标点，无法过关");

        bool editMode;

        public bool IsEditMode => editMode;

        void Awake()
        {
            ApplyMode(false);
        }

        void Start()
        {
            // Awake 已关掉存档组件，其 Start 不会执行；开始界面带来的读档在这里补上。
            if (persistence != null)
                persistence.ApplyPendingSession();
        }

        void OnEnable()
        {
            if (persistence != null)
                persistence.MapApplied += HandleMapApplied;
        }

        void OnDisable()
        {
            if (persistence != null)
                persistence.MapApplied -= HandleMapApplied;
        }

        void Update()
        {
            if (config == null)
                return;
            if (pause != null && pause.IsOpen)
                return;
            if (backpack != null && backpack.IsOpen)
                return;

            var keyboard = Keyboard.current;
            var key = config.ToggleEditKey;
            if (keyboard == null || key == Key.None || !keyboard[key].wasPressedThisFrame)
                return;

            if (editMode)
                TryEnterPlay();
            else
                EnterEdit();
        }

        void HandleMapApplied()
        {
            if (session == null)
                return;
            session.CommitEditedLayout();
            if (editMode)
                return;
            if (playCamera != null)
                playCamera.FrameBoard();
            session.EvaluateWin();
        }

        void EnterEdit()
        {
            if (session != null)
                session.ResetLevel();
            ApplyMode(true);
            if (gameplayCursor != null)
                gameplayCursor.ForceLock();
        }

        void TryEnterPlay()
        {
            var status = session != null
                ? session.EvaluatePlayable()
                : SokobanPlayableStatus.MissingPlayer;
            if (status == SokobanPlayableStatus.MissingPlayer)
            {
                Toast(missingPlayerHint.Get());
                return;
            }

            if (status == SokobanPlayableStatus.MissingGoal)
            {
                Toast(missingGoalHint.Get());
                return;
            }

            if (status == SokobanPlayableStatus.TooFewBoxes)
                Toast(tooFewBoxesHint.Get());

            if (session != null)
                session.CommitEditedLayout();
            ApplyMode(false);
            if (playCamera != null)
                playCamera.FrameBoard();
            if (session != null)
                session.EvaluateWin();
        }

        void ApplyMode(bool edit)
        {
            editMode = edit;
            if (!edit)
            {
                if (placement != null)
                    placement.AbortSession();
                if (backpack != null && backpack.IsOpen)
                    backpack.SetOpen(false);
                if (persistence != null && persistence.IsLoadPanelOpen)
                    persistence.HideLoadPanel();
            }

            SetEnabled(session, !edit);
            SetEnabled(playCamera, !edit);
            SetEnabled(playView, !edit);
            SetEnabled(playListener, !edit);
            if (hud != null)
                hud.gameObject.SetActive(!edit);

            SetEnabled(fly, edit);
            SetEnabled(editView, edit);
            SetEnabled(editListener, edit);
            SetEnabled(placement, edit);
            SetEnabled(gameplayCursor, edit);
            SetEnabled(ResolveCrosshair(), edit);
            SetEnabled(persistence, edit);
            if (warehouse != null)
                warehouse.gameObject.SetActive(edit);
        }

        void Toast(string message)
        {
            if (toastChannel != null)
                toastChannel.Raise(message);
        }

        ScreenCrosshair ResolveCrosshair()
        {
            if (crosshair != null)
                return crosshair;
            return fly != null ? fly.GetComponent<ScreenCrosshair>() : null;
        }

        static void SetEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }
}
