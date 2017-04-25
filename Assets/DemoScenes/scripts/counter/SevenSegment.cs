using UnityEngine;
using Unity8051Emu.Wrapper;

namespace Uemu.Demo.Counter
{
    public class SevenSegment : MonoBehaviour
    {
        public MeshRenderer[] Segments;
        public Emu8051 Controller;

        private void Update()
        {
            for (int i = 0; i < 8; i++)
                Segments[i].material.color = Controller[0, i] == 1 ? Color.blue : Color.gray;
        }
    }
}