namespace SmartEnergyLabDataApi.Loadflow
{
    public enum StageResultEnum {Pass, Fail, Warn}
    public class StageResults {

        private List<StageResult> _stageResults;

        public StageResults() {
            _stageResults = new List<StageResult>();
        }
        public StageResult NewStage(string name) {            
            return new StageResult(name);
        }

        public void StageResult(StageResult sr, StageResultEnum result, string comment) {
            sr.Finish(result, comment);
            _stageResults.Add(sr);
        }
        public List<StageResult> Results {
            get {
                return _stageResults;
            }
        }
    }

    public class StageResult {
        public StageResult(string name) {
            Name = name;
        }

        public void Finish(StageResultEnum result, string comment) {
            Result = result;
            Comment = comment;
        }

        public string Name { get; private set;}
        public StageResultEnum Result {get; private set;}
        public string Comment {get; private set;}
    }

}