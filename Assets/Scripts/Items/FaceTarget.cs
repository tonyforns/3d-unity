using UnityEngine;

namespace ThreeDUnity.Items
{
    /// <summary>
    /// Rota solo este transform (p. ej. el cartel de precio) hacia un objetivo. No afecta al padre ni a hermanos.
    /// </summary>
    public class FaceTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private bool lockYAxis = true;

        public Transform Target => target;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 pivot = transform.position;
            Vector3 toViewer = pivot - target.position;
            if (lockYAxis)
            {
                toViewer.y = 0f;
            }

            if (toViewer.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toViewer.normalized, Vector3.up);
        }
    }
}
