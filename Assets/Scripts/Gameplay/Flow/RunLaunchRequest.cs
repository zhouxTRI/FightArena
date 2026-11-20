namespace FightArena.Gameplay.Flow
{
    public static class RunLaunchRequest
    {
        public static bool HasPending { get; private set; }
        public static RunMode Mode { get; private set; }
        public static int StressTargetCount { get; private set; }

        public static void RequestNormalRun()
        {
            Mode = RunMode.Normal;
            StressTargetCount = 0;
            HasPending = true;
        }

        public static void RequestStressRun(int targetCount)
        {
            Mode = RunMode.Stress;
            StressTargetCount = targetCount;
            HasPending = true;
        }

        public static bool Consume(
            out RunMode mode,
            out int stressTargetCount)
        {
            mode = Mode;
            stressTargetCount = StressTargetCount;
            bool pending = HasPending;

            HasPending = false;
            StressTargetCount = 0;
            return pending;
        }

        public static void ClearPending()
        {
            HasPending = false;
            StressTargetCount = 0;
        }
    }
}
