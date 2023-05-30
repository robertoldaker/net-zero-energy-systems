using HaloSoft.EventLogger;
using Microsoft.AspNetCore.SignalR;
using static SmartEnergyLabDataApi.Models.TaskRunner;

namespace SmartEnergyLabDataApi.Models
{
    public interface IBackgroundTasks
    {
        public T GetTask<T>(int taskId) where T:BackgroundTaskBase;
        public int Register(BackgroundTaskBase backgroundTask);
        public void StateUpdate(TaskState state);
    }

    public class BackgroundTasks : IBackgroundTasks
    {
        private Dictionary<int,BackgroundTaskBase> _tasksDict;
        private int _taskId;
        IHubContext<NotificationHub> _hubContext;

        public BackgroundTasks(IHubContext<NotificationHub> hubContext)
        {
            _tasksDict = new Dictionary<int, BackgroundTaskBase>();
            _taskId = 0;
            _hubContext = hubContext;
        }

        public int Register(BackgroundTaskBase task) {
            lock(_tasksDict) {
                _taskId++;
                _tasksDict[_taskId] = task;
                return _taskId;
            }
        }

        public T GetTask<T>( int taskId ) where T : BackgroundTaskBase {
            if ( _tasksDict.TryGetValue(taskId, out BackgroundTaskBase task)) {
                return (T) task;
            } else {
                throw new Exception($"Task with id [{taskId}] not found. Has it been registered with a call to Register?");
            }
        }

        public void StateUpdate(TaskState state) {
            _hubContext.Clients.All.SendAsync("BackgroundTaskUpdate", state);
        }

    }

    public abstract class BackgroundTaskBase {
        private TaskState _lastTaskState;
        protected IBackgroundTasks _tasks;
        protected int _id;
        protected BackgroundTaskBase(IBackgroundTasks tasks) {
            _tasks = tasks;
            _id = tasks.Register(this);
        }
        public abstract void Cancel();
        public abstract bool IsRunning { get; }

        protected void stateUpdate(TaskState.RunningState state, string message, int progress=-1) {
            var taskState = new TaskState(_id,state,message,progress);
            _tasks.StateUpdate(taskState);
            Logger.Instance.LogInfoEvent(message);
            _lastTaskState = taskState;
        }

        protected void percentUpdate(int progress) {
            if ( _lastTaskState!=null) {
                _lastTaskState.Progress = progress;
                _tasks.StateUpdate(_lastTaskState);
            }
        }

        protected void messageUpdate(string message) {
            if ( _lastTaskState!=null) {
                _lastTaskState.Message = message;
                _tasks.StateUpdate(_lastTaskState);
                Logger.Instance.LogInfoEvent(message);
            }
        }
    }

    public class ClassificationToolBackgroundTask : BackgroundTaskBase
    {
        private TaskRunner _ctTask;
        private int _gaId;
        private ClassificationToolInput? _input = null;

        public ClassificationToolBackgroundTask(IBackgroundTasks tasks) : base(tasks) {
            _ctTask = new TaskRunner((taskRunner) =>
            {
                if (_input == null)
                {
                    throw new Exception("Null input found running background task.");
                }
                stateUpdate(TaskState.RunningState.Running, "Classification tool started", 0);
                try {
                    using (var m = new ClassificationTool())
                    {
                        m.RunAll(_gaId, _input, (TaskRunner?)taskRunner);
                    }
                    stateUpdate(TaskState.RunningState.Finished, "Classification tool finished", 100);
                } catch( Exception e) {
                    stateUpdate(TaskState.RunningState.Finished, $"Classification tool aborted [{e.Message}]", 0);
                }
            });
            _ctTask.StateUpdateEvent+=stateUpdate;
            _ctTask.ProgressUpdateEvent+=percentUpdate;
            _ctTask.MessageUpdateEvent+=messageUpdate;
        }



        public override bool IsRunning =>  _ctTask.IsRunning;

        public override void Cancel()
        {
            _ctTask.Cancel();
        }

        public void Run(int gaId, ClassificationToolInput input)
        {
            if (_ctTask.IsRunning)
            {
                throw new Exception("Classification tool is currently in progress, please try again later");
            }
            _gaId = gaId;
            _input = input;
            _ctTask.Run();
        }
        public static void Register(IBackgroundTasks tasks) {
            var instance = new ClassificationToolBackgroundTask(tasks);
            Id = instance._id;
        }

        public static int Id;
    }

    public class DatabaseBackupBackgroundTask : BackgroundTaskBase
    {
        private TaskRunner _ctTask;

        protected DatabaseBackupBackgroundTask(IBackgroundTasks tasks) : base(tasks) {
            _ctTask = new TaskRunner( (taskRunner) =>
            {
                stateUpdate(TaskState.RunningState.Running, "Database backup started", 0);
                try {
                    var m = new DatabaseBackup((TaskRunner?)taskRunner);
                    m.Run();
                    stateUpdate(TaskState.RunningState.Finished, "Database backup finished", 100);
                } catch( Exception e) {
                    stateUpdate(TaskState.RunningState.Finished, $"Database task aborted aborted [{e.Message}]", 0);
                }
            });
            _ctTask.StateUpdateEvent+=stateUpdate;
            _ctTask.ProgressUpdateEvent+=percentUpdate;
            _ctTask.MessageUpdateEvent+=messageUpdate;
        }
        public override bool IsRunning =>  _ctTask.IsRunning;

        public override void Cancel()
        {
            _ctTask.Cancel();
        }

        public void Run()
        {
            if (_ctTask.IsRunning)
            {
                throw new Exception("Database backup is currently in progress, please try again later");
            }
            _ctTask.Run();
        }

        public static void Register(IBackgroundTasks tasks) {
            var instance = new DatabaseBackupBackgroundTask(tasks);
            Id = instance._id;
        }

        public static int Id;
    }

    public class LoadNetworkDataBackgroundTask : BackgroundTaskBase
    {
        private TaskRunner _ctTask;
        private const string NAME="Network data load";

        protected LoadNetworkDataBackgroundTask(IBackgroundTasks tasks) : base(tasks) {
            _ctTask = new TaskRunner( (taskRunner) =>
            {
                stateUpdate(TaskState.RunningState.Running, $"{NAME} started", 0);
                try {
                    //??stateUpdate(TaskState.RunningState.Running,"Started loading Distribution Data");
                    //??var dataLoader = new DistributionDataLoader((TaskRunner?)taskRunner);
                    //??dataLoader.Load();
                    //??stateUpdate(TaskState.RunningState.Running,"Started loading Geo Spatial data");
                    //??var spatialLoader = new GeoSpatialDataLoader((TaskRunner?)taskRunner);
                    //??spatialLoader.Load();
                    stateUpdate(TaskState.RunningState.Running,"Started loading UK Power Network data");
                    var ukPowerNetworksLoader = new UKPowerNetworkLoader((TaskRunner?)taskRunner);
                    ukPowerNetworksLoader.Load();
                    stateUpdate(TaskState.RunningState.Finished, $"{NAME} finished", 100);
                } catch( Exception e) {
                    stateUpdate(TaskState.RunningState.Finished, $"{NAME} aborted aborted [{e.Message}]", 0);
                }
            });
            _ctTask.StateUpdateEvent+=stateUpdate;
            _ctTask.ProgressUpdateEvent+=percentUpdate;
            _ctTask.MessageUpdateEvent+=messageUpdate;
        }
        public override bool IsRunning =>  _ctTask.IsRunning;

        public override void Cancel()
        {
            _ctTask.Cancel();
        }

        public void Run()
        {
            if (_ctTask.IsRunning)
            {
                throw new Exception("Database backup is currently in progress, please try again later");
            }
            _ctTask.Run();
        }

        public static void Register(IBackgroundTasks tasks) {
            var instance = new LoadNetworkDataBackgroundTask(tasks);
            Id = instance._id;
        }

        public static int Id;
    }
}
