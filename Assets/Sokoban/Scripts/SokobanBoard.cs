using System.Collections.Generic;
using UnityEngine;

namespace Sokoban
{
    public readonly struct SokobanMoveResult
    {
        public SokobanMoveResult(Vector2Int playerFrom, Vector2Int playerTo, bool pushed, Vector2Int boxFrom, Vector2Int boxTo)
        {
            PlayerFrom = playerFrom;
            PlayerTo = playerTo;
            Pushed = pushed;
            BoxFrom = boxFrom;
            BoxTo = boxTo;
        }

        public Vector2Int PlayerFrom { get; }
        public Vector2Int PlayerTo { get; }
        public bool Pushed { get; }
        public Vector2Int BoxFrom { get; }
        public Vector2Int BoxTo { get; }
    }

    public sealed class SokobanBoard
    {
        readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> goals = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> boxes = new HashSet<Vector2Int>();
        Vector2Int player;
        bool hasPlayer;

        public void Clear()
        {
            walls.Clear();
            goals.Clear();
            boxes.Clear();
            player = default;
            hasPlayer = false;
        }

        public void Add(SokobanRole role, Vector2Int cell)
        {
            switch (role)
            {
                case SokobanRole.Wall:
                    walls.Add(cell);
                    break;
                case SokobanRole.Goal:
                    goals.Add(cell);
                    break;
                case SokobanRole.Box:
                    boxes.Add(cell);
                    break;
                case SokobanRole.Player:
                    player = cell;
                    hasPlayer = true;
                    break;
            }
        }

        public bool TryMove(Vector2Int direction, out SokobanMoveResult result)
        {
            result = default;
            if (!hasPlayer || direction == Vector2Int.zero)
                return false;

            var next = player + direction;
            if (walls.Contains(next))
                return false;

            if (boxes.Contains(next))
            {
                var beyond = next + direction;
                if (walls.Contains(beyond) || boxes.Contains(beyond))
                    return false;

                boxes.Remove(next);
                boxes.Add(beyond);
                var from = player;
                player = next;
                result = new SokobanMoveResult(from, player, true, next, beyond);
                return true;
            }

            var walkFrom = player;
            player = next;
            result = new SokobanMoveResult(walkFrom, player, false, default, default);
            return true;
        }

        public bool IsWon()
        {
            if (goals.Count == 0)
                return false;

            foreach (var goal in goals)
            {
                if (!boxes.Contains(goal))
                    return false;
            }

            return true;
        }
    }
}
