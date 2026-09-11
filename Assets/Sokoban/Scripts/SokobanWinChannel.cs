using System;
using UnityEngine;

namespace Sokoban
{
    [CreateAssetMenu(fileName = "SokobanWinChannel", menuName = "推箱子/过关频道")]
    public sealed class SokobanWinChannel : ScriptableObject
    {
        public event Action Won;
        public event Action Reset;

        public void RaiseWon()
        {
            Won?.Invoke();
        }

        public void RaiseReset()
        {
            Reset?.Invoke();
        }
    }
}
