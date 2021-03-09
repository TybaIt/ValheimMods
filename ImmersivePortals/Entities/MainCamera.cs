using UnityEngine;

namespace ImmersivePortals.Entities
{
    public class MainCamera : MonoBehaviour {

        void OnPreCull () {
            var portals = FindObjectsOfType<Portal> ();
        
            for (int i = 0; i < portals.Length; i++) {
                portals[i].PrePortalRender ();
            }

            for (int i = 0; i < portals.Length; i++) {
                portals[i].Render ();
            }

            for (int i = 0; i < portals.Length; i++) {
                portals[i].PostPortalRender ();
            }
        }

    }
}
