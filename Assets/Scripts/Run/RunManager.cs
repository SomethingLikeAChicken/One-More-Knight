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
        [SerializeField] private Scoring.RunStats runStats;
        [SerializeField] [Min(0f)] private float gameOverDelay = 1.2f;

        /// <summary>Raw earned points — the Boss pacing clock (#123). Multipliers
        /// never touch this; a Score lever must never also be a pacing change
        /// (#117 §2.4).</summary>
        public int Score { get; private set; }

        /// <summary>The ranked figure (CONTEXT.md: Leaderboard): raw points times the
        /// multiplier active when they were earned. What the HUD shows, the Game Over
        /// readout displays, and the submitter sends (#123).</summary>
        public int LeaderboardScore { get; private set; }

        /// <summary>Multiplier applied to LeaderboardScore gains. Wave-modifier
        /// scoped (#57 Gilded): the WaveSpawner sets it per Wave and resets it in
        /// StopSpawning, so Bosses pay unmultiplied.</summary>
        public float WaveScoreMultiplier { get; set; } = 1f;

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
            LeaderboardScore += Mathf.RoundToInt(amount * WaveScoreMultiplier);
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
            LastRun.Score = LeaderboardScore;
            LastRun.Wave = Wave;
            waveSpawner.StopSpawning();
            ScoreSubmitter.Create().Submit(LeaderboardScore,
                runStats != null ? runStats.ToMetaJson()
                    : $"{{\"wave\":{Wave},\"bosses\":{(bossDirector != null ? bossDirector.BossesDefeated : 0)}}}");
            Changed?.Invoke();
        }

        private void Update()
        {
            if (IsOver && Time.time >= gameOverAt) SceneFlow.LoadGameOver();
        }
    }
}
