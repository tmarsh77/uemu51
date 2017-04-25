using System.Collections;
using UnityEngine;

namespace Uemu.Demo.Machine
{
    public class Lamp : MonoBehaviour
    {
        public Color ColorOff;
        public Color ColorOn;

        private Material _mat;
        private Light _light;
        private AudioSource _asrc;

        private void Start()
        {
            _mat = GetComponent<MeshRenderer>().material;
            _light = GetComponentInChildren<Light>();
            _asrc = GetComponent<AudioSource>();
            Switch(false);
        }

        public void Alert()
        {
            StartCoroutine(Anim());
        }

        private void Switch(bool state)
        {
            _light.gameObject.SetActive(state);
            _mat.color = state ? ColorOn : ColorOff;
        }

        private IEnumerator Anim()
        {
            Switch(true);
            _asrc.Play();
            while (_asrc.isPlaying)
                yield return null;
            Switch(false);
        }
    }
}