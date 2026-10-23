using UnityEngine;
using UnityEngine.InputSystem;

namespace BrainrotWaves
{
    /// <summary>
    /// Управление от первого лица: WASD — ходьба, Shift — бег, Пробел — прыжок, мышь — обзор.
    /// Esc освобождает курсор, клик мышью снова его захватывает.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Camera playerCamera;
        public float moveSpeed = 6f;
        public float sprintSpeed = 9f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;
        [Tooltip("Чувствительность мыши")]
        public float mouseSensitivity = 0.1f;

        CharacterController controller;
        float verticalVelocity;
        float pitch;

        public static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        }

        void Start()
        {
            LockCursor(true);
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (!IsCursorLocked)
            {
                if (mouse.leftButton.wasPressedThisFrame) LockCursor(true);
                return;
            }

            // Обзор мышью
            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);

            // Движение
            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

            // Гравитация и прыжок
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }
    }
}
