using UnityEngine;

namespace ImmersivePortals.Entities
{
    public class PortalCamera : MonoBehaviour
    {
        public Transform playerCamera;
        public Transform portal;
        public Transform otherPortal;
        public Camera camera;

        public GameObject renderTarget;

        // Update is called once per frame
        public void Update()
        {
            if (playerCamera == null) 
                playerCamera = GameCamera.instance.transform;

            if (portal == null || otherPortal == null || camera == null)
                return;

            Vector3 playerOffsetFromPortal = playerCamera.position - otherPortal.position;
            camera.transform.position = portal.position + playerOffsetFromPortal;

            float angularDifferenceBetweenPortalRotations = Quaternion.Angle(portal.rotation, otherPortal.rotation);

            Quaternion portalRotationalDifference = Quaternion.AngleAxis(angularDifferenceBetweenPortalRotations, Vector3.up);
            Vector3 newCameraDirection = portalRotationalDifference * playerCamera.forward;
            camera.transform.rotation = Quaternion.LookRotation(newCameraDirection, Vector3.up);
        }

        public void OnDestroy()
        {
            GameObject.DestroyImmediate(renderTarget);
        }
    }
}
