using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MultigridProjector.Utilities
{
    public class RwLock
    {
        private int _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Reader Read()
        {
            return new Reader(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer Write()
        {
            return new Writer(this);
        }

        public class Reader: IDisposable
        {
            private readonly RwLock _rwLock;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Reader(RwLock rwLock)
            {
                _rwLock = rwLock;
                _rwLock.AcquireForReading();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _rwLock.ReleaseAfterReading();
            }
        }

        public class Writer: IDisposable
        {
            private readonly RwLock _rwLock;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Writer(RwLock rwLock)
            {
                _rwLock = rwLock;
                _rwLock.AcquireForWriting();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _rwLock.ReleaseAfterWriting();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AcquireForWriting()
        {
            var spinWait = new SpinWait();
            while (Interlocked.CompareExchange(ref _value, -1, 0) != 0)
            {
                spinWait.SpinOnce();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseAfterWriting()
        {
            _value = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AcquireForReading()
        {
            var spinWait = new SpinWait();
            var previous = _value;
            while (previous < 0 || previous != Interlocked.CompareExchange(ref _value, previous + 1, previous))
            {
                spinWait.SpinOnce();
                previous = _value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseAfterReading()
        {
            Interlocked.Decrement(ref _value);
        }
    }
}