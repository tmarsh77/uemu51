using UnityEngine;
using Unity8051Emu.Debugger;
using Unity8051Emu.Terminal;
using Unity8051Emu.Wrapper;

namespace Uemu.Demo.Counter
{
    public class Counter : MonoBehaviour
    {
        public TextAsset Prog;
        public SevenSegment LedDisplay;
        public UClock Clock;
        public Terminal Term;

        private Emu8051 _controller;
        private Emu8051Dbg _debugger;

        private void Start()
        {
            Clock.Init();
            _controller = new Emu8051(65536, 65536, 128);
            _controller.LoadIhex(Prog.text);
            _controller.Clock = Clock;
            LedDisplay.Controller = _controller;
            _debugger = new Emu8051Dbg(_controller, Term);
            _debugger.Clock = Clock;
        }

        private void Update()
        {
            _debugger.Tick();
        }
    }
}