using System;
using UnityEngine;
using OneMoreKnight.Combat;
using OneMoreKnight.Flow;
using OneMoreKnight.Run.Scoring;
using OneMoreKnight.Waves;

namespace OneMoreKnight.Run
{
    /// <summary>
    /// One playthrough, from start to the Hero's death (CONTEXT.md: Run). Owns the Score
    /// and the current Wave number, and ends the Run when the Hero's Health hits zero —
    /// after a short beat, the GameOver scene takes over with the final readout.
    ///
    /// Permadeath: playing again loads a fresh Game scene, so nothing carries over.
    /// Meta-progression (Relics) is a post-MVP concern and has no hook here yet.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        [SerializeField] private Health heroHealth;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private BossDirector bossDirector;
        [SerializeField] [Min(0f)] private float gameOverDelay = 1.2f;

        public int Score { get; private set; }
        public int Wave { get; private set; }
        public bool IsOver { get; private set; }

        /// <summary>The active wave modifier's display label, "" when none (#57).</summary>
        public string WaveModifierLabel { get; private set; } = "";

        private float gameOverAt;

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

        public void ReportWave(int waveNumber, string modifierLabel = "")
        {
            Wave = waveNumber;
            WaveModifierLabel = modifierLabel ?? "";
            Changed?.Invoke();
        }

        private void OnHeroDied(Health _)
        {
            if (IsOver) return;
            IsOver = true;
            gameOverAt = Time.time + gameOverDelay;
            LastRun.Score = Score;
            LastRun.Wave = Wave;
            waveSpawner.StopSpawning();
            ScoreSubmitter.Create()
                .Submit(Score, Wave, bossDirector != null ? bossDirector.BossesDefeated : 0);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (IsOver && Time.time >= gameOverAt) SceneFlow.LoadGameOver();
        }
    }
}
