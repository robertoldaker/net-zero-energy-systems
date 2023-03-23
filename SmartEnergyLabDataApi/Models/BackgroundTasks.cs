using Microsoft.AspNetCore.SignalR;
using static SmartEnergyLabDataApi.Models.TaskRunner;

namespace SmartEnergyLabDataApi.Models
{
    public interface IBackgroundTasks
    {
        public ClassificationToolBackgroundTask ClassificationTool { get; }
    }
    public class BackgroundTasks : IBackgroundTasks
    {
        public enum BackgroundTasksEnum { ClassificationTool }

        public BackgroundTasks(IHubContext<NotificationHub> hubContext)
        {
            ClassificationTool = new ClassificationToolBackgroundTask(hubContext);
        }

        public ClassificationToolBackgroundTask ClassificationTool { get; }

        public BackgroundTaskBase GetTask( BackgroundTasksEnum task) {
            if ( task == BackgroundTasksEnum.ClassificationTool ) {
                return ClassificationTool;
            } else {
                throw new Exception($"Unexpected task found [{task}]");
            }
        }

    }

    public abstract class BackgroundTaskBase {
        public abstract void Cancel();
        public abstract bool IsRunning { get; }

    }

    public class ClassificationToolBackgroundTask : BackgroundTaskBase
    {
        private TaskRunner _ctTask;
        private int _gaId;
        private ClassificationToolInput? _input = null;
        private IHubContext<NotificationHub> _hubContext;

        public ClassificationToolBackgroundTask(IHubContext<NotificationHub> hubContext){
            _hubContext = hubContext;
            _ctTask = new TaskRunner(_hubContext, (taskRunner) =>
            {
                if (_input == null)
                {
                    throw new Exception("Null input found running background task.");
                }
                stateUpdate(new TaskState(TaskState.RunningState.Running, "Classification tool started", 0));
                try {
                    using (var m = new ClassificationTool())
                    {
                        m.RunAll(_gaId, _input, (TaskRunner?)taskRunner);
                    }
                    stateUpdate(new TaskState(TaskState.RunningState.Finished, "Classification tool finished", 100));
                } catch( Exception e) {
                    stateUpdate(new TaskState(TaskState.RunningState.Finished, $"Classification tool aborted [{e.Message}]", 0));
                }
            });
            _ctTask.StateUpdateEvent+=stateUpdate;

        }
        private void stateUpdate(TaskState state) {
            _hubContext.Clients.All.SendAsync("BackgroundTaskUpdate_ClassificationTool", state);
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
    }

}
