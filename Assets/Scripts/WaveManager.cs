using System;
using System.Collections;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Запускает волны врагов. С каждой волной врагов больше, они крепче и быстрее.
    /// Волна 1: обычные. С волны 2: быстрые. С волны 3: толстяки.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public enum EnemyType { Normal, Fast, Tank }

        public Transform player;
        public Transform[] spawnPoints;

        [Header("Волны")]
        public float timeBetweenWaves = 5f;
        public int firstWaveEnemies = 4;
        public int extraEnemiesPerWave = 3;
        public float spawnInterval = 0.7f;

        [Header("Материалы врагов")]
        public Material normalMaterial;
        public Material fastMaterial;
        public Material tankMaterial;
        public Material eyeMaterial;
        public Material pupilMaterial;

        public int Wave { get; private set; }
        public int Alive { get; private set; }
        public int ToSpawn { get; private set; }
        public int Remaining => Alive + ToSpawn;
        public int Kills { get; private set; }
        public int Coins { get; private set; }
        public bool InBreak { get; private set; }
        public float BreakTimeLeft { get; private set; }

        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;

        void Start()
        {
            StartCoroutine(WaveLoop());
        }

        IEnumerator WaveLoop()
        {
            while (true)
            {
                InBreak = true;
                BreakTimeLeft = timeBetweenWaves;
                while (BreakTimeLeft > 0f)
                {
                    BreakTimeLeft -= Time.deltaTime;
                    yield return null;
                }
                InBreak = false;

                Wave++;
                ToSpawn = firstWaveEnemies + (Wave - 1) * extraEnemiesPerWave;
                WaveStarted?.Invoke(Wave);

                while (ToSpawn > 0)
                {
                    SpawnEnemy(PickType());
                    ToSpawn--;
                    yield return new WaitForSeconds(spawnInterval);
                }

                while (Alive > 0) yield return null;
                WaveCleared?.Invoke(Wave);
            }
        }

        EnemyType PickType()
        {
            float roll = UnityEngine.Random.value;
            if (Wave >= 3 && roll < 0.2f) return EnemyType.Tank;
            if (Wave >= 2 && roll < 0.5f) return EnemyType.Fast;
            return EnemyType.Normal;
        }

        void SpawnEnemy(EnemyType type)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return;
            Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 2f;
            Vector3 position = point.position + new Vector3(offset.x, 0.1f, offset.y);

            float waveBonus = 1f + 0.15f * (Wave - 1);
            float speedBonus = 1f + 0.03f * (Wave - 1);

            float size, speed, hp, damage;
            int coins;
            Material body;
            switch (type)
            {
                case EnemyType.Fast:
                    size = 0.75f; speed = 6f; hp = 30f; damage = 6f; coins = 2; body = fastMaterial; break;
                case EnemyType.Tank:
                    size = 1.6f; speed = 2.2f; hp = 200f; damage = 20f; coins = 5; body = tankMaterial; break;
                default:
                    size = 1f; speed = 3.5f; hp = 50f; damage = 10f; coins = 1; body = normalMaterial; break;
            }

            Enemy enemy = CreateEnemyBody(type.ToString(), position, size, body);
            enemy.coinReward = coins;
            enemy.Init(player, speed * speedBonus, hp * waveBonus, damage);

            Alive++;
            enemy.GetComponent<Health>().Died += _ =>
            {
                Alive--;
                Kills++;
                Coins += coins;
            };
        }

        /// <summary>Собирает мультяшного врага из простых фигур: тело и два глаза.</summary>
        Enemy CreateEnemyBody(string name, Vector3 position, float size, Material bodyMaterial)
        {
            var root = new GameObject("Enemy_" + name);
            root.transform.position = position;
            root.transform.localScale = Vector3.one * size;

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.5f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            if (bodyMaterial != null) body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            for (int side = -1; side <= 1; side += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                Destroy(eye.GetComponent<Collider>());
                eye.transform.SetParent(root.transform, false);
                eye.transform.localPosition = new Vector3(0.2f * side, 1.45f, 0.4f);
                eye.transform.localScale = Vector3.one * 0.3f;
                if (eyeMaterial != null) eye.GetComponent<Renderer>().sharedMaterial = eyeMaterial;

                var pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pupil.name = "Pupil";
                Destroy(pupil.GetComponent<Collider>());
                pupil.transform.SetParent(eye.transform, false);
                pupil.transform.localPosition = new Vector3(0f, 0f, 0.38f);
                pupil.transform.localScale = Vector3.one * 0.45f;
                if (pupilMaterial != null) pupil.GetComponent<Renderer>().sharedMaterial = pupilMaterial;
            }

            root.AddComponent<Health>();
            return root.AddComponent<Enemy>();
        }
    }
}
