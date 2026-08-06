using System;

namespace Web3Fps.GameFoundation.Prototype
{
    public enum LocalPrototypePhase
    {
        Waiting,
        Running,
        Finished
    }

    public enum LocalPrototypeOutcome
    {
        Undecided,
        PlayerWin,
        BotWin,
        Draw
    }

    /// <summary>Pure C# rules for the generated offline deathmatch sample.</summary>
    public sealed class LocalDeathmatchRules
    {
        public int TargetKills { get; }
        public float DurationSeconds { get; }
        public int PlayerKills { get; private set; }
        public int BotKills { get; private set; }
        public float RemainingSeconds { get; private set; }
        public LocalPrototypePhase Phase { get; private set; }
        public LocalPrototypeOutcome Outcome { get; private set; }

        public LocalDeathmatchRules(int targetKills, float durationSeconds)
        {
            if (targetKills < 1) throw new ArgumentOutOfRangeException(nameof(targetKills));
            if (durationSeconds <= 0f || float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            TargetKills = targetKills;
            DurationSeconds = durationSeconds;
            Reset();
        }

        public void Start()
        {
            if (Phase != LocalPrototypePhase.Waiting)
                throw new InvalidOperationException("A local prototype match can only start from Waiting");
            Phase = LocalPrototypePhase.Running;
        }

        public bool RecordPlayerKill()
        {
            if (Phase != LocalPrototypePhase.Running) return false;
            PlayerKills++;
            if (PlayerKills >= TargetKills) Finish(LocalPrototypeOutcome.PlayerWin);
            return true;
        }

        public bool RecordBotKill()
        {
            if (Phase != LocalPrototypePhase.Running) return false;
            BotKills++;
            if (BotKills >= TargetKills) Finish(LocalPrototypeOutcome.BotWin);
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (Phase != LocalPrototypePhase.Running || deltaSeconds <= 0f ||
                float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return;
            RemainingSeconds = Math.Max(0f, RemainingSeconds - deltaSeconds);
            if (RemainingSeconds > 0f) return;
            Finish(PlayerKills == BotKills
                ? LocalPrototypeOutcome.Draw
                : PlayerKills > BotKills ? LocalPrototypeOutcome.PlayerWin : LocalPrototypeOutcome.BotWin);
        }

        public void Reset()
        {
            PlayerKills = 0;
            BotKills = 0;
            RemainingSeconds = DurationSeconds;
            Outcome = LocalPrototypeOutcome.Undecided;
            Phase = LocalPrototypePhase.Waiting;
        }

        private void Finish(LocalPrototypeOutcome outcome)
        {
            Outcome = outcome;
            Phase = LocalPrototypePhase.Finished;
        }
    }
}
