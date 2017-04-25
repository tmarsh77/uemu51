using UnityEngine;
using Unity8051Emu.Debugger;
using Unity8051Emu.Terminal;
using Unity8051Emu.Wrapper;

namespace Uemu.Demo.Hello
{
    public class Hello : MonoBehaviour
    {
        public TextAsset Prog;
        public UClock Clock;
        public Terminal Term;
        public SetPinBtn[] Buttons;

        private Emu8051 _controller;
        private Emu8051Dbg _debugger;

        private void Start()
        {
            foreach (SetPinBtn btn in Buttons)
            {                
                btn.OnActivate = (n) => _controller[0, n] = 1;
            }

            Clock.Init();
            _controller = new Emu8051(65536, 65536, 128);
            _controller.LoadIhex(Prog.text);
            _controller.Clock = Clock;
            _debugger = new Emu8051Dbg(_controller, Term);
            _debugger.Clock = Clock;
        }

        private void Update()
        {
            _debugger.Tick();
        }
    }
}