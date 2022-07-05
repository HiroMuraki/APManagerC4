namespace APManagerC4
{
    /// <summary>
    /// 用于等待指定时间间隔
    /// </summary>
    class IntervalWaiter
    {
        public TimeSpan Interval { get; init; }

        /// <summary>
        /// 进入等待
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Wait()
        {
            try
            {
                await Task.Delay(Interval, _cts.Token);
                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }
        /// <summary>
        /// 重置等待
        /// </summary>
        /// <returns></returns>
        public void Reset()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
        }

        private CancellationTokenSource _cts = new();
    }
}
