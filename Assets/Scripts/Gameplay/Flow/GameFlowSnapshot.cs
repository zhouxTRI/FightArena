namespace FightArena.Gameplay.Flow
{
    public readonly struct GameFlowSnapshot
    {
        public GameFlowState State { get; }
        public RunMode Mode { get; }
        public int KillCount { get; }
        public int KillTarget { get; }
        public float ElapsedSeconds { get; }

        public GameFlowSnapshot(
            GameFlowState state,
            RunMode mode,
            int killCount,
            int killTarget,
            float elapsedSeconds)
        {
            State = state;
            Mode = mode;
            KillCount = killCount;
            KillTarget = killTarget;
            ElapsedSeconds = elapsedSeconds;
        }
    }
}
