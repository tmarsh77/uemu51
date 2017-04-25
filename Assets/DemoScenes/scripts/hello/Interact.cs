using UnityEngine;

namespace Uemu.Demo.Hello
{
    public class Interact : MonoBehaviour
    {
        public Camera Cam;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;

                if (Physics.Raycast(Cam.ViewportPointToRay(new Vector3(.5f, .5f, 0)), out hit))
                {
                    SetPinBtn btn = hit.transform.gameObject.GetComponent<SetPinBtn>();
                    if (btn != null)
                    {
                        btn.Activate();
                    }
                }
            }
        }
    }
}