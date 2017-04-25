using System;
using System.Collections;
using UnityEngine;

namespace Uemu.Demo.Hello
{
    public class SetPinBtn : MonoBehaviour
    {
        public Action<int> OnActivate;
        public int PinNumber;

        private MeshRenderer _renderer;
        private Coroutine _anim;

        private void Start()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        public void Activate()
        {
            if (_anim != null)
                return;

            _anim = StartCoroutine(Anim());
            OnActivate(PinNumber);
        }

        private IEnumerator Anim()
        {
            _renderer.material.color = Color.blue;
            yield return new WaitForSeconds(.5f);
            _renderer.material.color = Color.white;
            _anim = null;
        }
    }
}