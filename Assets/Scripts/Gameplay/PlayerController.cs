using UnityEngine;
using UnityEngine.InputSystem;

namespace BrainrotWaves
{
    /// <summary>
    /// Герой от первого лица: WASD — ходьба, Shift — бег, Пробел — прыжок, мышь — обзор.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public Transform cameraPivot;
        public Camera cam;
        public float baseSpeed = 6.5f;
        public float sprintMultiplier = 1.45f;
        public float jumpHeight = 1.3f;
        public float gravity = -24f;
        public static float MouseSensitivity = 0.1f;

        public Health Health { get; private set; }
        public bool InputEnabled { get; set; } = true;
        public bool IsGrounded { get; private set; }
        public float MoveFraction { get; private set; }
        public bool IsDashing => dashTimer > 0f;

        CharacterController controller;
        float verticalVelocity;
        float pitch;
        int jumpsLeft;
        int maxJumps = 1;
        float slowMultiplier = 1f;
        float slowTimer;
        Vector3 dashVelocity;
        float dashTimer;
        float bobPhase;
        Vector3 cameraBase;
        float baseFov = 75f;
        float fovKick;
        float landDip;

        public static bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            Health = GetComponent<Health>();
            Health.invulnerabilityTime = 0.25f;
            Health.Damaged += (amount, crit) =>
            {
                CameraShake.Add(Mathf.Clamp(amount / 40f, 0.15f, 0.5f));
                PostFX.Hurt(Mathf.Clamp(amount / 30f, 0.3f, 0.8f));
                SoundFX.Play(SoundFX.Sfx.Hurt, 0.8f, 0.1f);
            };
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Setup(BrainrotInfo hero)
        {
            maxJumps = hero.id == BrainrotId.Chimpanzini ? 2 : 1;
            if (cam != null) baseFov = cam.fieldOfView;
            cameraBase = cam != null ? cam.transform.localPosition : Vector3.zero;
            ApplyStats(true);
        }

        /// <summary>Пересчитать здоровье и броню после улучшений.</summary>
        public void ApplyStats(bool refill)
        {
            var stats = RunState.stats;
            Health.SetMax((RunState.HeroInfo.heroMaxHealth + stats.bonusMaxHealth) * stats.maxHealthMultiplier, refill);
            Health.damageTakenMultiplier = stats.damageTakenMultiplier;
        }

        public void Teleport(Vector3 position, float yaw)
        {
            controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            pitch = 0f;
            verticalVelocity = 0f;
            controller.enabled = true;
        }

        public void Dash(Vector3 direction, float speed, float duration)
        {
            direction.y = 0f;
            dashVelocity = direction.normalized * speed;
            dashTimer = duration;
            fovKick = 12f;
        }

        public void ApplySlow(float multiplier, float duration)
        {
            slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        void Update()
        {
            if (cam != null && Time.timeScale == 0f) cam.fieldOfView = GameSettings.Fov;
            if (Health.IsDead || Time.timeScale == 0f) return;
            float dt = Time.deltaTime;
            var stats = RunState.stats;

            if (stats.regenPerSecond > 0f) Health.Heal(stats.regenPerSecond * dt);

            if (slowTimer > 0f)
            {
                slowTimer -= dt;
                if (slowTimer <= 0f) slowMultiplier = 1f;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool canControl = InputEnabled && IsCursorLocked && keyboard != null && mouse != null;

            Vector2 input = Vector2.zero;
            bool sprint = false;
            bool jumpPressed = false;

            if (canControl)
            {
                Vector2 look = mouse.delta.ReadValue() * MouseSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch + (GameSettings.InvertY ? look.y : -look.y), -85f, 85f);

                if (Controls.Held(GameAction.Forward)) input.y += 1f;
                if (Controls.Held(GameAction.Back)) input.y -= 1f;
                if (Controls.Held(GameAction.Right)) input.x += 1f;
                if (Controls.Held(GameAction.Left)) input.x -= 1f;
                input = Vector2.ClampMagnitude(input, 1f);
                sprint = Controls.Held(GameAction.Sprint) && input.y > 0.1f;
                jumpPressed = Controls.Down(GameAction.Jump);
            }

            float speed = baseSpeed * stats.moveSpeedMultiplier * slowMultiplier * (sprint ? sprintMultiplier : 1f);
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;
            MoveFraction = Mathf.MoveTowards(MoveFraction, input.magnitude * (sprint ? 1f : 0.7f), dt * 6f);

            if (dashTimer > 0f)
            {
                dashTimer -= dt;
                move = dashVelocity;
            }

            bool wasGrounded = IsGrounded;
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
                jumpsLeft = maxJumps;
            }

            if (jumpPressed && (IsGrounded || jumpsLeft > 0))
            {
                if (!IsGrounded && jumpsLeft == maxJumps) jumpsLeft--; // сошёл с края — первый прыжок «съеден»
                if (jumpsLeft > 0)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    jumpsLeft--;
                    SoundFX.Play(SoundFX.Sfx.Jump, 0.4f, 0.1f);
                }
            }

            verticalVelocity += gravity * dt;
            move.y = verticalVelocity;
            controller.Move(move * dt);
            IsGrounded = controller.isGrounded;
            if (IsGrounded && !wasGrounded && verticalVelocity < -8f)
            {
                landDip = 0.12f;
                Particles.Dust(transform.position, 0.6f, 6, new Color(0.75f, 0.7f, 0.62f));
            }

            UpdateCamera(dt);
        }

        void UpdateCamera(float dt)
        {
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            if (cam == null) return;

            // Покачивание при ходьбе
            if (IsGrounded) bobPhase += dt * (6f + MoveFraction * 6f);
            float bobAmount = IsGrounded ? MoveFraction * 0.05f : 0f;
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase * 0.5f) * bobAmount, Mathf.Abs(Mathf.Sin(bobPhase * 0.5f)) * bobAmount * 1.4f, 0f);

            landDip = Mathf.MoveTowards(landDip, 0f, dt * 0.6f);

            // Тряска
            float shake = CameraShake.trauma * CameraShake.trauma;
            CameraShake.trauma = Mathf.Max(0f, CameraShake.trauma - dt * 1.6f);
            float t = Time.time * 25f;
            Vector3 shakeOffset = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * shake * 0.5f;
            Vector3 shakeRotation = new Vector3(Mathf.PerlinNoise(t, 10f) - 0.5f, Mathf.PerlinNoise(10f, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f) * shake * 8f;

            cam.transform.localPosition = cameraBase + bob + shakeOffset - new Vector3(0f, landDip, 0f);
            cam.transform.localRotation = Quaternion.Euler(shakeRotation);

            fovKick = Mathf.MoveTowards(fovKick, 0f, dt * 30f);
            baseFov = GameSettings.Fov;
            cam.fieldOfView = baseFov + fovKick + (MoveFraction > 0.85f ? 4f : 0f);
        }
    }
}
