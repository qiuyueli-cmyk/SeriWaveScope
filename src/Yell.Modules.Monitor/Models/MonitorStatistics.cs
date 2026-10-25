namespace Yell.Modules.Monitor.Models
{
    /// <summary>
    /// 实时采样统计：总数、当前值、最小值、最大值。
    /// </summary>
    public sealed class MonitorStatistics
    {
        private bool _hasValue;

        public long Count { get; private set; }

        public double Current { get; private set; }

        public double Min { get; private set; }

        public double Max { get; private set; }

        public void Add(double value)
        {
            if (!_hasValue)
            {
                Min = value;
                Max = value;
                _hasValue = true;
            }
            else
            {
                Min = Math.Min(Min, value);
                Max = Math.Max(Max, value);
            }

            Count++;
            Current = value;
        }

        public void Reset()
        {
            Count = 0;
            Current = 0;
            Min = 0;
            Max = 0;
            _hasValue = false;
        }
    }
}
