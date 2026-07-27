using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using OneMoreKnight.Combat;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// One playthrough, from start to the Hero's death (CONTEXT.md: Run). Owns the Score
    /// and the current Wave number, and ends the Run when the Hero's Health hits zero.
    ///
    /// Permadeath: restarting reloads the scene, so nothing carries over. Meta-progression
    /// (Relics) is a post-MVP concern and has no hook here yet.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        [SerializeField] private Health heroHealth;
        [SerializeField] private WaveSpawner waveSpawner;

        public int Score { get; private set; }
        public int Wave { get; private set; }
        public bool IsOver { get; private set; }

        public event Action Changed;

        private void Awake() => heroHealth.Died += OnHeroDied;

        private void OnDestroy()
        {
            if (heroHealth != null) heroHealth.Died -= OnHeroDied;
        }

        public void AddScore(int amount)
        {
            if (IsOver || amount <= 0) return;
            Score += amount;
            Changed?.Invoke();
        }

        public void ReportWave(int waveNumber)
        {
            Wave = waveNumber;
            Changed?.Invoke();
        }

        private void OnHeroDied(Health _)
        {
            if (IsOver) return;
            IsOver = true;
            waveSpawner.StopSpawning();
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!IsOver) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Restart();
        }

        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
