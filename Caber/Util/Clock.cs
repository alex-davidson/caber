namespace Caber.Util
{
    public static class Clock
    {
        public static IClock Default => new RealClock();
    }
}
