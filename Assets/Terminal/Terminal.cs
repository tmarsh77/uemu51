using UnityEngine;
using UTerm;
using Uterm.Tools;

namespace Unity8051Emu.Terminal
{
    public class Terminal : MonoBehaviour, ITerminal
    {
        public TextMeshOutputDevice OutDev;
        public GameObject PlayerCamera;
        public Camera TerminalCamera;
        public bool Interact = false;
        public bool InputEnabled { get; set; }
        public int _lastInputData = -1;

        private UTermCore _terminal;
        private UTermIO _tio;
        private bool _releaseReturnAfterInteract = true;
        private float _inpTimeStamp;

        public bool HasInput
        {
            get
            {
                return _lastInputData != -1;
            }
        }

        private void Awake()
        {
            _terminal = new UTermCore();
            _tio = new UTermIO(_terminal, OutDev);
            //_tio.Print("Hello world!\n");

            if (Interact)
            {
                ActivateTerminal(true);
            }
        }

        public byte ReadLastInput()
        {
            byte data = (byte)_lastInputData;
            _lastInputData = -1;
            return data;
        }

        public void PlaceCaret(CaretCoord coord)
        {
            _terminal.PlaceCaret(coord);
        }

        public void Print(char c)
        {
            _terminal.PutChar(ASCIICoDec.CodeASCII(c));
        }

        public void WriteLine(string line)
        {
            _tio.Print(line);
        }

        private bool CheckDelay(float d = 0.1f)
        {
            if (Time.time - _inpTimeStamp < d)
                return false;

            _inpTimeStamp = Time.time;
            return true;
        }

        public void HandleInput()
        {
            if (!Interact)
                return;

            byte inpChr = 0x00;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                _tio.Print(ASCIICoDec.ControlCharacters.STX);
            }
            else if (Input.GetKey(KeyCode.Return))
            {
                _tio.Print(ASCIICoDec.ControlCharacters.LF);
            }


            else if (Input.GetKey(KeyCode.LeftArrow) && CheckDelay())
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.DC1;
            }
            else if (Input.GetKey(KeyCode.RightArrow) && CheckDelay())
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.DC2;
            }
            else if (Input.GetKey(KeyCode.UpArrow) && CheckDelay())
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.DC3;
            }
            else if (Input.GetKey(KeyCode.DownArrow) && CheckDelay())
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.DC4;
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.TAB;
            }
            else if (Input.GetKey(KeyCode.Space) && CheckDelay(0.5f))
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.SPC;
            }
            else if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.FS;
            }
            else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                inpChr = (byte)ASCIICoDec.ControlCharacters.RS;
            }


            else if (Input.anyKeyDown && Input.inputString.Length > 0)
            {
                inpChr = ASCIICoDec.CodeASCII(Input.inputString[0]);
            }

            if (inpChr != 0x00)
            {
                if (InputEnabled)
                    _tio.Print(ASCIICoDec.DecodeASCII(inpChr));

                _lastInputData = inpChr;
            }
        }

        private void HandleUserInteract()
        {
            if (!Interact && Vector3.Distance(
                PlayerCamera.transform.position, transform.position) < 2)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    _releaseReturnAfterInteract = false;
                    ActivateTerminal(true);
                    return;
                }
            }

            if (!_releaseReturnAfterInteract)
            {
                if (Input.GetKeyUp(KeyCode.Return))
                    _releaseReturnAfterInteract = true;
                else
                    return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ActivateTerminal(false);
                return;
            }
        }

        private void ActivateTerminal(bool activate)
        {
            if (activate)
            {
                TerminalCamera.gameObject.SetActive(true);
                PlayerCamera.gameObject.SetActive(false);
                Interact = true;
            }
            else
            {
                Interact = false;
                PlayerCamera.gameObject.SetActive(true);
                TerminalCamera.gameObject.SetActive(false);
            }
        }

        public void Tick() // Update
        {
            HandleInput();
            _tio.Tick(Time.time);
        }

        private void Update()
        {
            HandleUserInteract();
        }
    }
}