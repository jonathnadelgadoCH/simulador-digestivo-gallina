using UnityEngine;

namespace DigestiveSimulator.Presentation
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
        private Transform target;
        private float yaw = 18f;
        private float pitch = 5f;
        private float distance = 7f;
        private float desiredDistance = 7f;
        private Vector3 focusPoint;
        private Bounds framedBounds;
        private bool interactionEnabled;
        private float leftUiBoundary;
        private float rightUiBoundary;
        private float topUiBoundary;
        private float bottomUiBoundary;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                if (target != null) Frame(target);
            }
        }

        public void Frame(Transform root)
        {
            if (root == null) return;
            var bounds = CalculateBounds(root);
            framedBounds = bounds;
            focusPoint = bounds.center;
            desiredDistance = Mathf.Clamp(bounds.size.magnitude * 1.65f, 5f, 14f);
        }

        public void ResetView()
        {
            if (target == null) return;
            yaw = 18f;
            pitch = 5f;
            Frame(target);
        }

        public void Focus(Transform selection)
        {
            if (selection == null) return;
            var bounds = CalculateBounds(selection);
            focusPoint = bounds.center;
            desiredDistance = Mathf.Clamp(bounds.size.magnitude * 4f, 2.5f, 7f);
        }

        public void ConfigureInteraction(bool enabled, float leftBoundary, float rightBoundary, float topBoundary, float bottomBoundary)
        {
            interactionEnabled = enabled;
            leftUiBoundary = Mathf.Max(0f, leftBoundary);
            rightUiBoundary = Mathf.Max(0f, rightBoundary);
            topUiBoundary = Mathf.Max(0f, topBoundary);
            bottomUiBoundary = Mathf.Max(0f, bottomBoundary);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var pointer = Input.mousePosition;
            var pointerInViewport = interactionEnabled
                && pointer.x > leftUiBoundary
                && pointer.x < Screen.width - rightUiBoundary
                && pointer.y > bottomUiBoundary
                && pointer.y < Screen.height - topUiBoundary;
            var shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var panRequested = pointerInViewport && (Input.GetMouseButton(2) || (shiftPressed && (Input.GetMouseButton(0) || Input.GetMouseButton(1))));
            var rotateRequested = pointerInViewport && !shiftPressed && (Input.GetMouseButton(0) || Input.GetMouseButton(1));
            if (panRequested)
            {
                var panScale = Mathf.Max(0.0025f, distance * 0.0025f);
                focusPoint -= transform.right * (Input.GetAxis("Mouse X") * panScale);
                focusPoint -= transform.up * (Input.GetAxis("Mouse Y") * panScale);
                ClampFocusPoint();
            }
            else if (rotateRequested)
            {
                yaw += Input.GetAxis("Mouse X") * 4f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 4f, -70f, 70f);
            }
            if (pointerInViewport && Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f)
                desiredDistance = Mathf.Clamp(desiredDistance - Input.mouseScrollDelta.y * 0.8f, 2.5f, 22f);
            distance = Mathf.Lerp(distance, desiredDistance, 1f - Mathf.Exp(-7f * Time.deltaTime));
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focusPoint + rotation * new Vector3(0f, 0f, -distance);
            transform.rotation = rotation;
        }

        private void ClampFocusPoint()
        {
            if (framedBounds.size == Vector3.zero) return;
            var allowance = Mathf.Max(1f, framedBounds.extents.magnitude * 1.5f);
            focusPoint.x = Mathf.Clamp(focusPoint.x, framedBounds.center.x - allowance, framedBounds.center.x + allowance);
            focusPoint.y = Mathf.Clamp(focusPoint.y, framedBounds.center.y - allowance, framedBounds.center.y + allowance);
            focusPoint.z = Mathf.Clamp(focusPoint.z, framedBounds.center.z - allowance, framedBounds.center.z + allowance);
        }

        private static Bounds CalculateBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }
    }
}
