using Uterm.Tools;

namespace UTerm
{
    class UTermIO
    {
        enum Mode
        {
            NONE,
            READ_STR
        }

        private UTermCore _terminal;
        private IOutputDevice _output;
        private Mode _mode;
        private string _buf;
        private string _inputBuf;
        private bool _hasInput;
        private bool _cursor;

        public UTermIO(UTermCore terminal, IOutputDevice output)
        {
            _buf = string.Empty;
            _terminal = terminal;
            _output = output;
        }

        public void Print(string s)
        {
            foreach (var c in s)
                _terminal.PutChar(ASCIICoDec.CodeASCII(c));
        }

        public void Print(char c)
        {
            if (_mode == Mode.READ_STR)
                _inputBuf += c;

            _terminal.PutChar(ASCIICoDec.CodeASCII(c));
        }

        public void Print(ASCIICoDec.ControlCharacters c)
        {
            if (c == ASCIICoDec.ControlCharacters.STX)
            {
                _inputBuf = string.Empty;
                _mode = Mode.READ_STR;
            }

            if (c == ASCIICoDec.ControlCharacters.LF)
            {
                _mode = Mode.NONE;
                if (!string.IsNullOrEmpty(_inputBuf))
                    _hasInput = true;
            }

            _terminal.PutChar((byte)c);
        }

        public string ReadInputBuffer()
        {
            if (_hasInput)
            {
                _hasInput = false;
                string input = _inputBuf;
                _inputBuf = null;
                return input;
            }
            return null;
        }

        private void RefreshDisplay()
        {
            _buf = string.Empty;

            foreach (var c in _terminal.ReadBuffer())
                _buf += c;

            _output.SetText(_buf);
        }

        private void TimedTick()
        {
            if (!(_terminal.Full && _terminal.ActiveChar))
            {
                _cursor = !_cursor;

                _terminal.ShowCursor = _cursor;
            }
        }

        private float _cTimer;

        public void Tick(float time)
        {
            if (time - _cTimer > .5f)
            {
                TimedTick();
                _cTimer = time;
            }

            RefreshDisplay();
        }
    }
}