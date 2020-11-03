using System;
using System.Threading;

namespace Aws.Crt
{
    public class CrtResult<T>
    {
        public delegate void OnCompletion(T result);
        public delegate void OnException(Exception exception);

        private enum ResultState
        {
            INCOMPLETE,
            COMPLETE
        }

        private T Result;
        private Exception Exception;
        private ManualResetEvent CompletionSignal;
        private ResultState State;

        private OnCompletion OnCompletionCallback;
        private OnException OnExceptionCallback;

        public CrtResult()
        {
            Result = default(T);
            Exception = null;
            CompletionSignal = new ManualResetEvent(false);
            State = ResultState.INCOMPLETE;
            OnCompletionCallback = null;
            OnExceptionCallback = null;
        }

        public void Complete(T result)
        {
            bool invokeCallback = false;
            lock(this)
            {
                if (State == ResultState.INCOMPLETE)
                {
                    State = ResultState.COMPLETE;
                    Result = result;
                    invokeCallback = true;
                }
                else
                {
                    throw new CrtException("Result already completed");
                }
            }

            if (invokeCallback)
            {
                OnCompletionCallback(result);
                CompletionSignal.Set();
            }
        }

        public void CompleteExceptionally(Exception exception)
        {
            bool invokeCallback = false;
            lock (this)
            {
                if (State == ResultState.INCOMPLETE)
                {
                    State = ResultState.COMPLETE;
                    Exception = exception;
                    invokeCallback = true;
                }
                else
                {
                    throw new CrtException("Result already completed");
                }
            }

            if (invokeCallback)
            {
                OnExceptionCallback(exception);
                CompletionSignal.Set();
            }
        }

        public T Get()
        {
            CompletionSignal.WaitOne();

            // may not be necessary, but let's start off safe
            lock (this)
            {
                if (Exception != null)
                {
                    throw Exception;
                }
                else
                {
                    return Result;
                }
            }
        }

        public OnCompletion CompletionCallback { 
            set 
            {
                bool invokeCallback = false;
                T result = default(T);

                lock(this)
                {
                    if (OnCompletionCallback != null)
                    {
                        throw new CrtException("Cannot set result completion callback twice");
                    }

                    OnCompletionCallback = value;
                    if (State == ResultState.COMPLETE && Exception == null)
                    {
                        invokeCallback = true;
                        result = Result;
                    }
                }

                if (invokeCallback)
                {
                    OnCompletionCallback.Invoke(result);
                }
            } 
        }

        public OnException ExceptionCallback {
            set
            {
                bool invokeCallback = false;
                Exception exception = null;

                lock (this)
                {
                    if (OnExceptionCallback != null)
                    {
                        throw new CrtException("Cannot set result exception callback twice");
                    }

                    OnExceptionCallback = value;
                    if (State == ResultState.COMPLETE && Exception != null)
                    {
                        invokeCallback = true;
                        exception = Exception;
                    }
                }

                if (invokeCallback)
                {
                    OnExceptionCallback.Invoke(exception);
                }
            }
        }

    }
}