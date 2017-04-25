using UTerm;
using UnityEngine;

namespace Unity8051Emu.Terminal
{
    [RequireComponent(typeof(TextMesh))]
    public class TextMeshOutputDevice : MonoBehaviour, IOutputDevice
    {
        private TextMesh _textMesh;

        private void Awake()
        {
            _textMesh = GetComponent<TextMesh>();
        }

        public TextMeshOutputDevice(TextMesh tmesh)
        {
            _textMesh = tmesh;
        }

        public void SetText(string text)
        {
            _textMesh.text = text;
        }
    }
}