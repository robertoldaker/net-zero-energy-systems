using HaloSoft.EventLogger;
using Microsoft.AspNetCore.SignalR;

namespace SmartEnergyLabDataApi.Models
{
    public class TaskRunner
    {
        private Task _task;
        private Action<object?> _action;
        private CancellationTokenSource _tokenSource;

        public delegate void StateUpdateHandler(TaskState.RunningState state, string message, int progress=-1);
        public event StateUpdateHandler StateUpdateEvent;
        public delegate void ProgressUpdateHandler(int progress);
        public event ProgressUpdateHandler ProgressUpdateEvent;
        public delegate void MessageUpdateHandler(string message, bool addToLog=true);
        public event MessageUpdateHandler MessageUpdateEvent;


        public TaskRunner(Action<object?> action)
        {
            _action = action;
        }

        public void Run()
        {
            if ( IsRunning ) {
                throw new Exception("Attempt to start task that is currently running");
            }
            _tokenSource = new CancellationTokenSource();
            _task = new Task(_action, this, _tokenSource.Token);
            _task.Start();
        }

        public void Cancel()
        {
            _tokenSource.Cancel();
        }

        public CancellationToken CancellationToken { 
            get {
                return _tokenSource.Token;
            }
        }

        public bool IsRunning
        {
            get
            {
                return _task!=null && _task.Status == TaskStatus.Running;
            }
        }

        public void CheckCancelled() {
            _tokenSource.Token.ThrowIfCancellationRequested();
        }

        public void Update(TaskState.RunningState state, string message, int progress=-1) {
            StateUpdateEvent?.Invoke(state, message,progress);
        }
        
        public void Update(int progress) {
            ProgressUpdateEvent?.Invoke(progress);
        }
        public void Update(string message,bool addToLog=true) {
            MessageUpdateEvent?.Invoke(message,addToLog);
        }

        public class TaskState
        {
            public enum RunningState { Running, Finished }

            public RunningState State { get; private set;}
            public string Message { get; set; }
            public int Progress { get; set; }
            public int TaskId {get; set;}
            public TaskState(int id, RunningState state, string message, int progress=-1) {
                TaskId = id;
                State = state;
                Message = message;
                Progress = progress;
            }
        }

    }
}

