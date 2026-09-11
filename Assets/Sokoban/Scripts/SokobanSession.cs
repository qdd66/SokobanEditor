using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Sokoban
{
    [DefaultExecutionOrder(-20)]
    [InfoBox("从棋盘根收集玩家、墙、箱子、目标点。关卡直接在场景里摆，不在运行时加载。")]
    public sealed class SokobanSession : MonoBehaviour
    {
        [Title("引用")]
        [LabelText("玩法配置")]
        [SerializeField, Required("请指定玩法配置"), AssetsOnly, InlineEditor]
        SokobanConfig config;

        [LabelText("过关频道")]
        [SerializeField, Required("请指定过关频道"), AssetsOnly]
        SokobanWinChannel winChannel;

        [LabelText("棋盘根")]
        [Tooltip("这一层底下的 SokobanPiece 构成关卡。")]
        [SerializeField, Required("请指定棋盘根"), SceneObjectsOnly]
        Transform boardRoot;

        readonly SokobanBoard board = new SokobanBoard();
        readonly SokobanKeyboardInput input = new SokobanKeyboardInput();
        readonly List<SokobanPiece> pieces = new List<SokobanPiece>(64);
        readonly List<Pose> initialPoses = new List<Pose>(64);
        readonly List<Pose[]> undoStack = new List<Pose[]>(64);
        readonly List<MoveAnim> anims = new List<MoveAnim>(2);

        SokobanPiece playerPiece;
        bool won;
        float animElapsed;
        float animDuration;

        public bool IsWon => won;

        void Start()
        {
            Rebuild();
            CaptureInitial();
            EvaluateWin();
        }

        void Update()
        {
            if (config == null)
                return;

            TickAnim(Time.deltaTime);
            var blocking = anims.Count > 0;
            var frame = input.Read(config, blocking);
            if (frame.Reset)
            {
                ResetLevel();
                return;
            }

            if (frame.Undo)
            {
                UndoStep();
                return;
            }

            if (won || blocking || frame.Move == Vector2Int.zero)
                return;

            if (!board.TryMove(frame.Move, out var move))
                return;

            PushUndo();
            ApplyMove(move);
            FacePlayer(frame.Move);
            EvaluateWin();
        }

        public void ResetLevel()
        {
            anims.Clear();
            animElapsed = 0f;
            undoStack.Clear();
            RestorePoses(initialPoses);
            Rebuild();
            EvaluateWin();
        }

        public void CommitEditedLayout()
        {
            anims.Clear();
            animElapsed = 0f;
            undoStack.Clear();
            Rebuild();
            CaptureInitial();
        }

        public SokobanPlayableStatus EvaluatePlayable()
        {
            CollectPieces();
            var players = 0;
            var goals = 0;
            var boxes = 0;
            for (var i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece == null)
                    continue;
                switch (piece.Role)
                {
                    case SokobanRole.Player:
                        players++;
                        break;
                    case SokobanRole.Goal:
                        goals++;
                        break;
                    case SokobanRole.Box:
                        boxes++;
                        break;
                }
            }

            if (players < 1)
                return SokobanPlayableStatus.MissingPlayer;
            if (goals < 1)
                return SokobanPlayableStatus.MissingGoal;
            if (boxes < goals)
                return SokobanPlayableStatus.TooFewBoxes;
            return SokobanPlayableStatus.Ok;
        }

        void UndoStep()
        {
            if (undoStack.Count == 0)
                return;

            anims.Clear();
            animElapsed = 0f;
            var snapshot = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            RestorePoses(snapshot);
            Rebuild();
            EvaluateWin();
        }

        public void EvaluateWin()
        {
            var nowWon = board.IsWon();
            if (nowWon)
            {
                if (!won && winChannel != null)
                    winChannel.RaiseWon();
                won = true;
                return;
            }

            if (won && winChannel != null)
                winChannel.RaiseReset();
            won = false;
        }

        void PushUndo()
        {
            var snapshot = CapturePoses();
            undoStack.Add(snapshot);
            var max = config != null ? Mathf.Max(1, config.MaxUndoSteps) : 64;
            while (undoStack.Count > max)
                undoStack.RemoveAt(0);
        }

        void CaptureInitial()
        {
            CollectPieces();
            initialPoses.Clear();
            initialPoses.AddRange(CapturePoses());
        }

        Pose[] CapturePoses()
        {
            var snapshot = new Pose[pieces.Count];
            for (var i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == null)
                    continue;
                snapshot[i] = new Pose(pieces[i].transform.position, pieces[i].transform.rotation);
            }

            return snapshot;
        }

        void RestorePoses(IList<Pose> poses)
        {
            if (poses == null)
                return;

            var count = Mathf.Min(pieces.Count, poses.Count);
            for (var i = 0; i < count; i++)
            {
                if (pieces[i] == null)
                    continue;
                pieces[i].transform.SetPositionAndRotation(poses[i].position, poses[i].rotation);
            }
        }

        void Rebuild()
        {
            board.Clear();
            playerPiece = null;
            CollectPieces();
            if (config == null)
                return;

            for (var i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece == null)
                    continue;

                SnapToCell(piece.transform);
                board.Add(piece.Role, SokobanGrid.WorldToCell(piece.transform.position, config));
                if (piece.Role == SokobanRole.Player && playerPiece == null)
                    playerPiece = piece;
            }
        }

        void CollectPieces()
        {
            pieces.Clear();
            if (boardRoot == null)
                return;
            boardRoot.GetComponentsInChildren(false, pieces);
        }

        void ApplyMove(in SokobanMoveResult move)
        {
            var duration = config != null ? Mathf.Max(0f, config.MoveDuration) : 0f;
            anims.Clear();
            animElapsed = 0f;
            animDuration = duration;

            QueueMove(playerPiece, move.PlayerTo, duration);
            if (move.Pushed)
                QueueMove(FindBox(move.BoxFrom), move.BoxTo, duration);
        }

        void QueueMove(SokobanPiece piece, Vector2Int cell, float duration)
        {
            if (piece == null)
                return;

            var target = SokobanGrid.CellCenter(cell, piece.transform.position.y, config);
            if (duration <= 0f)
            {
                piece.transform.position = target;
                return;
            }

            anims.Add(new MoveAnim(piece.transform, piece.transform.position, target));
        }

        void TickAnim(float deltaTime)
        {
            if (anims.Count == 0)
                return;

            animElapsed += deltaTime;
            var t = animDuration <= 0f ? 1f : Mathf.Clamp01(animElapsed / animDuration);
            for (var i = 0; i < anims.Count; i++)
            {
                var anim = anims[i];
                if (anim.Target == null)
                    continue;
                anim.Target.position = Vector3.Lerp(anim.From, anim.To, t);
            }

            if (t < 1f)
                return;

            anims.Clear();
        }

        SokobanPiece FindBox(Vector2Int cell)
        {
            for (var i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece == null || piece.Role != SokobanRole.Box)
                    continue;
                if (SokobanGrid.WorldToCell(piece.transform.position, config) == cell)
                    return piece;
            }

            return null;
        }

        void FacePlayer(Vector2Int direction)
        {
            if (playerPiece == null || direction == Vector2Int.zero)
                return;
            var yaw = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            playerPiece.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void SnapToCell(Transform target)
        {
            var cell = SokobanGrid.WorldToCell(target.position, config);
            target.position = SokobanGrid.CellCenter(cell, target.position.y, config);
        }

        readonly struct MoveAnim
        {
            public MoveAnim(Transform target, Vector3 from, Vector3 to)
            {
                Target = target;
                From = from;
                To = to;
            }

            public Transform Target { get; }
            public Vector3 From { get; }
            public Vector3 To { get; }
        }
    }
}
