using System.Collections;
using Unity8051Emu.Debugger;
using Unity8051Emu.Terminal;
using Unity8051Emu.Wrapper;
using UnityEngine;

namespace Uemu.Demo.Machine
{
    public class Machine : MonoBehaviour
    {
        public Terminal UTerm;
        public Transform Bucket;
        public Transform FillPoint;
        public TextDisplay Display;

        public TextAsset Program;
        public Lamp Alert;
        public Emu8051Dbg _debugger;
        public UClock Clock;

        private Emu8051 _controller;
        private bool _fill, _move, _rotate, _cycle;
        private bool _locked;

        private void Start()
        {
            Clock.Init();
            _controller = new Emu8051(65536, 65536, 128);
            _controller.PortsUpdated += Tick;
            _controller.LoadIhex(Program.text);
            _controller.Clock = Clock;
            _debugger = new Emu8051Dbg(_controller, UTerm);
            _debugger.Clock = Clock; // to be able to control clock speed
        }
        
        private void Update()
        {
            _debugger.Tick();

            if (_fill)
            {
                _fill = false;
                StartCoroutine(Fill());
            }

            if (_move)
            {
                _move = false;
                StartCoroutine(Move(new Vector3(12, 7, 4)));
            }

            if (_rotate)
            {
                _rotate = false;
                StartCoroutine(Rotate());
            }

            if (_cycle)
            {
                _cycle = false;
                StartCoroutine(Move(new Vector3(-12, 7, 4)));
            }
        }

        private void Tick(object sender, System.EventArgs e)
        {
            if (_controller[0, 7] == 1 && _controller[0, 6] == 0)
            {
                // display current op

                if (_controller[1] != 0x00 && _controller[1] != 0xFF)
                {
                    Display.PushChar(_controller[1]);
                }
                
                // --------------

                if (_controller[0, 4] == 1)
                {
                    _controller[0, 4] = _locked ? 0 : 1;
                }
                else if (!_locked && _controller[0, 5] == 0)
                {
                    if (_controller[0, 0] == 1)
                    {
                        _locked = true;
                        _fill = true;
                    }
                    else if (_controller[0, 1] == 1)
                    {
                        _locked = true;
                        _move = true;
                    }
                    else if (_controller[0, 2] == 1)
                    {
                        _locked = true;
                        _rotate = true;
                    }
                    else if (_controller[0, 3] == 1)
                    {
                        _locked = true;
                        _cycle = true;
                    }
                }

            }
        }

        private IEnumerator Fill()
        {
            Alert.Alert();
            for (int i = 0; i < 25; i++)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.AddComponent<Rigidbody>();
                Vector3 rp = Random.insideUnitSphere;
                rp.y = 0;
                obj.transform.position = FillPoint.position + rp;
                obj.transform.localScale = Vector3.one / 2;
                yield return new WaitForSeconds(0.2f);
            }
            yield return new WaitForSeconds(1);
            _locked = false;
        }

        private IEnumerator Move(Vector3 target)
        {
            while (Bucket.position != target)
            {
                Bucket.position = Vector3.MoveTowards(Bucket.position, target, Time.deltaTime * 5);
                yield return null;
            }
            _locked = false;
        }

        private IEnumerator Rotate()
        {
            while (Mathf.Abs(180 - Mathf.Abs(Bucket.eulerAngles.z)) > 10)
            {
                Bucket.Rotate(-Bucket.forward, Time.deltaTime * 45);
                yield return null;
            }
            yield return new WaitForSeconds(1);
            while (Mathf.Abs(Bucket.eulerAngles.z) > 5)
            {
                Bucket.Rotate(Bucket.forward, Time.deltaTime * 45);
                yield return null;
            }
            Bucket.eulerAngles = Vector3.zero;
            _locked = false;
        }
    }
}