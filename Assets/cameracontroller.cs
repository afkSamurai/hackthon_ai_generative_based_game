using UnityEngine;

namespace TMPro.Examples {
    public class CameraController : MonoBehaviour {
        public Transform Player;

        public float Speed = 5f;

        private Controls controls;
        private Vector2 movementInput;

        private void Awake() {
            controls = new Controls();
            controls.Player.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
            controls.Player.Movement.canceled += ctx => movementInput = Vector2.zero;
        }

        private void OnEnable() {
            controls.Player.Enable();
        }

        private void OnDisable() {
            controls.Player.Disable();
        }

        private void LateUpdate() {
            if (Player == null) return;

            float horizontalInput = movementInput.x;
            float verticalInput = movementInput.y;

            // Translate camera based on input
            Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * Speed * Time.deltaTime;
            transform.Translate(movement, Space.Self);
        }
    }
}