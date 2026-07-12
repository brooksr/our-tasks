using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Showroom-style orbit camera: drag to orbit, scroll/pinch to zoom.
    /// Yaw is clamped so the user can't swing behind the walls, and pitch is
    /// clamped between eye level and a gentle top-down view. The room
    /// controller adjusts target/distance when the room size changes.
    /// </summary>
    public class CameraOrbitController : MonoBehaviour
    {
        public enum ControlMode
        {
            Orbit,
            FirstPersonWalk
        }

        public ControlMode controlMode = ControlMode.Orbit;

        public Vector3 target = new Vector3(0f, 0.75f, -0.4f);
        public float distance = 6.6f;
        public float minDistance = 3.2f;
        public float maxDistance = 11f;

        public float yaw = 205f;
        public float pitch = 20f;
        public float minYaw = 130f;
        public float maxYaw = 280f;
        public float minPitch = 8f;
        public float maxPitch = 65f;

        public float orbitSensitivity = 0.25f;
        public float zoomSensitivity = 3.5f;
        public float touchZoomSensitivity = 0.01f;
        public float panSensitivity = 0.015f;
        public float keyboardOrbitSpeed = 75f;
        public float keyboardZoomSpeed = 10f;

        public Vector3 walkStartPosition = new Vector3(0f, 5.4f, -3f);
        public float walkStartYaw = 0f;
        public float walkStartPitch = 4f;
        public float walkSpeed = 8.5f;
        public float walkSprintMultiplier = 1.8f;
        public float walkLookSensitivity = 0.18f;
        public float minWalkY = 1.5f;
        public float maxWalkY = 18f;

        Vector3 _defaultTarget;
        float _defaultYaw;
        float _defaultPitch;
        float _defaultDistance;
        Vector3 _defaultWalkPosition;
        float _defaultWalkYaw;
        float _defaultWalkPitch;
        float _walkYaw;
        float _walkPitch;
        Vector3 _lastMouse;
        bool _dragging;
        bool _panning;
        bool _walkDragging;

        void Start()
        {
            _defaultTarget = target;
            _defaultYaw = yaw;
            _defaultPitch = pitch;
            _defaultDistance = distance;
            _defaultWalkPosition = walkStartPosition;
            _defaultWalkYaw = walkStartYaw;
            _defaultWalkPitch = walkStartPitch;
            _walkYaw = walkStartYaw;
            _walkPitch = walkStartPitch;
            if (controlMode == ControlMode.FirstPersonWalk)
            {
                transform.position = walkStartPosition;
                ApplyWalkRotation();
            }
            else
            {
                ApplyOrbit();
            }
        }

        void LateUpdate()
        {
            if (controlMode == ControlMode.FirstPersonWalk)
            {
                HandleWalkMode();
                ApplyWalkRotation();
                return;
            }

            if (Input.touchCount > 0) HandleTouch();
            else HandleMouse();
            HandleKeyboardOrbit();
            ApplyOrbit();
        }

        void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragging = true;
                _lastMouse = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
            {
                _dragging = false;
            }
            if (Input.GetMouseButtonDown(2) || (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftShift)))
            {
                _panning = true;
                _lastMouse = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(1))
            {
                _panning = false;
            }
            if (_dragging && Input.GetMouseButton(0))
            {
                Vector3 delta = Input.mousePosition - _lastMouse;
                _lastMouse = Input.mousePosition;
                yaw += delta.x * orbitSensitivity;
                pitch -= delta.y * orbitSensitivity;
            }
            if (_panning)
            {
                Vector3 delta = Input.mousePosition - _lastMouse;
                _lastMouse = Input.mousePosition;
                Pan(delta);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                distance -= scroll * zoomSensitivity;
            }
        }

        void HandleKeyboardOrbit()
        {
            float dt = Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.LeftArrow)) yaw -= keyboardOrbitSpeed * dt;
            if (Input.GetKey(KeyCode.RightArrow)) yaw += keyboardOrbitSpeed * dt;
            if (Input.GetKey(KeyCode.UpArrow)) pitch += keyboardOrbitSpeed * dt;
            if (Input.GetKey(KeyCode.DownArrow)) pitch -= keyboardOrbitSpeed * dt;
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) distance -= keyboardZoomSpeed * dt;
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) distance += keyboardZoomSpeed * dt;
        }

        void HandleTouch()
        {
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved)
                {
                    yaw += t.deltaPosition.x * orbitSensitivity * 0.5f;
                    pitch -= t.deltaPosition.y * orbitSensitivity * 0.5f;
                }
            }
            else if (Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                float previous = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                float current = (a.position - b.position).magnitude;
                distance -= (current - previous) * touchZoomSensitivity;
            }
        }

        void HandleWalkMode()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                _walkDragging = true;
                _lastMouse = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            {
                _walkDragging = false;
            }
            if (_walkDragging && (Input.GetMouseButton(0) || Input.GetMouseButton(1)))
            {
                Vector3 delta = Input.mousePosition - _lastMouse;
                _lastMouse = Input.mousePosition;
                _walkYaw += delta.x * walkLookSensitivity;
                _walkPitch -= delta.y * walkLookSensitivity;
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= forward;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += right;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.C)) move -= Vector3.up;

            if (move.sqrMagnitude > 0.0001f)
            {
                float speed = walkSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? walkSprintMultiplier : 1f);
                transform.position += move.normalized * speed * Time.unscaledDeltaTime;
                var pos = transform.position;
                pos.y = Mathf.Clamp(pos.y, minWalkY, maxWalkY);
                transform.position = pos;
            }
        }

        void ApplyOrbit()
        {
            yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = target - rotation * Vector3.forward * distance;
            transform.rotation = rotation;
        }

        void ApplyWalkRotation()
        {
            _walkPitch = Mathf.Clamp(_walkPitch, -78f, 78f);
            transform.rotation = Quaternion.Euler(_walkPitch, _walkYaw, 0f);
        }

        void Pan(Vector3 screenDelta)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            target -= (right * screenDelta.x + up * screenDelta.y) * panSensitivity * Mathf.Max(1f, distance * 0.08f);
        }

        public void SetControlMode(ControlMode mode)
        {
            controlMode = mode;
            _dragging = false;
            _panning = false;
            _walkDragging = false;
            if (controlMode == ControlMode.FirstPersonWalk)
            {
                transform.position = walkStartPosition;
                _walkYaw = walkStartYaw;
                _walkPitch = walkStartPitch;
                ApplyWalkRotation();
            }
            else
            {
                ApplyOrbit();
            }
        }

        public void SetWalkStart(Vector3 position, float startYaw, float startPitch)
        {
            walkStartPosition = position;
            walkStartYaw = startYaw;
            walkStartPitch = startPitch;
            _defaultWalkPosition = position;
            _defaultWalkYaw = startYaw;
            _defaultWalkPitch = startPitch;
            if (controlMode == ControlMode.FirstPersonWalk)
            {
                transform.position = walkStartPosition;
                _walkYaw = walkStartYaw;
                _walkPitch = walkStartPitch;
                ApplyWalkRotation();
            }
        }

        /// <summary>Reframe for a new room size while keeping the user's current angle.</summary>
        public void SetFraming(Vector3 newTarget, float newDistance)
        {
            target = newTarget;
            distance = newDistance;
            _defaultTarget = newTarget;
            _defaultDistance = newDistance;
            ApplyOrbit();
        }

        public void ResetView()
        {
            controlMode = ControlMode.Orbit;
            target = _defaultTarget;
            yaw = _defaultYaw;
            pitch = _defaultPitch;
            distance = _defaultDistance;
            walkStartPosition = _defaultWalkPosition;
            walkStartYaw = _defaultWalkYaw;
            walkStartPitch = _defaultWalkPitch;
            _walkYaw = _defaultWalkYaw;
            _walkPitch = _defaultWalkPitch;
            ApplyOrbit();
        }
    }
}
