namespace APManagerC4
{
    /// <summary>
    /// 用于等待指定时间间隔
    /// </summary>
    class IntervalWaiter
    {
        /// <summary>
        /// 进入等待
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Wait(TimeSpan waitingTime)
        {
            try
            {
                await Task.Delay(waitingTime, _cts.Token);
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
