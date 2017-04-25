using UnityEngine;

namespace Uemu.Demo.Machine
{
    public class TextDisplay : MonoBehaviour
    {
        private TextMesh _textMesh;

        private byte[] _chars = new byte[3];
        private int _char;

        private void Start()
        {
            _textMesh = GetComponentInChildren<TextMesh>();
        }

        public void PushChar(byte ch)
        {
            if (_char == -1 || (_char > 0 && _chars[_char - 1] == ch))
                return;

            _chars[_char] = ch;
            _char = _char < _chars.Length - 1 ? _char + 1 : -1;
        }

        private void Update()
        {
            if (_char == -1)
            {
                _char = 0;
                Redraw();
            }
        }

        private void Redraw()
        {
            string msg = string.Empty;

            for (int i = 0; i < _chars.Length; i++)
            {
                msg += Uterm.Tools.ASCIICoDec.DecodeASCII(_chars[i]);
                _chars[i] = 0x00;
            }

            _textMesh.text = msg;
        }
    }
}