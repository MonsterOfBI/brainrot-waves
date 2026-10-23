using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrainrotWaves
{
    /// <summary>
    /// Оружие с мгновенным попаданием (луч из центра экрана). Левая кнопка мыши — выстрел.
    /// Пока это пистолет; автомат и дробовик добавим через те же настройки.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        public Camera playerCamera;
        public string weaponName = "Пистолет";
        public float damage = 25f;
        [Tooltip("Выстрелов в секунду")]
        public float fireRate = 4f;
        [Tooltip("Стрелять, пока зажата кнопка")]
        public bool automatic = false;
        public float range = 100f;

        [Header("Внешний вид")]
        public Transform muzzle;
        public Transform model;
        public Material tracerMaterial;

        /// <summary>Вызывается, когда пуля попала во врага.</summary>
        public event Action Hit;

        float nextFireTime;
        LineRenderer tracer;
        float tracerTimer;
        Vector3 modelRestPosition;
        float recoil;

        void Awake()
        {
            if (playerCamera == null) playerCamera = GetComponentInParent<Camera>();
            if (model != null) modelRestPosition = model.localPosition;

            var tracerObject = new GameObject("Tracer");
            tracerObject.transform.SetParent(transform, false);
            tracer = tracerObject.AddComponent<LineRenderer>();
            tracer.positionCount = 2;
            tracer.startWidth = 0.04f;
            tracer.endWidth = 0.01f;
            tracer.useWorldSpace = true;
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (tracerMaterial != null) tracer.material = tracerMaterial;
            tracer.enabled = false;
        }

        void Update()
        {
            if (tracerTimer > 0f)
            {
                tracerTimer -= Time.deltaTime;
                if (tracerTimer <= 0f) tracer.enabled = false;
            }

            // Отдача: оружие дёргается назад и плавно возвращается
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 6f);
            if (model != null) model.localPosition = modelRestPosition + new Vector3(0f, recoil * 0.05f, -recoil * 0.12f);

            if (Time.timeScale == 0f || !PlayerController.IsCursorLocked) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            bool trigger = automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
            if (trigger && Time.time >= nextFireTime) Fire();
        }

        void Fire()
        {
            nextFireTime = Time.time + 1f / fireRate;
            recoil = 1f;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 end = ray.origin + ray.direction * range;

            if (Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null && health.GetComponent<PlayerController>() == null)
                {
                    health.TakeDamage(damage);
                    Hit?.Invoke();
                }
            }

            Vector3 start = muzzle != null ? muzzle.position : ray.origin;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerTimer = 0.05f;
        }
    }
}
