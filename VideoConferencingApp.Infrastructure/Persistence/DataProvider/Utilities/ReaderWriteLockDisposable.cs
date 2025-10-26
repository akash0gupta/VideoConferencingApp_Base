namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider.Utilities
{
    /// <summary>
    /// A helper class for managing a ReaderWriterLockSlim with a 'using' block.
    /// </summary>
    public class ReaderWriteLockDisposable : IDisposable
    {
        private readonly ReaderWriterLockSlim _locker;

        public ReaderWriteLockDisposable(ReaderWriterLockSlim locker)
        {
            _locker = locker;
            _locker.EnterWriteLock();
        }

        public void Dispose()
        {
            _locker.ExitWriteLock();
        }
    }
}
