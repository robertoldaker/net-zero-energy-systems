namespace SmartEnergyLabDataApi.BoundCalc
{
    public enum BoundCalcStageResultEnum {Pass, Fail, Warn}
    public class BoundCalcStageResults {

        private List<BoundCalcStageResult> _stageResults;

        public BoundCalcStageResults()
        {
            _stageResults = new List<BoundCalcStageResult>();
        }
        public BoundCalcStageResult NewStage(string name) {
            return new BoundCalcStageResult(name);
        }

        public void StageResult(BoundCalcStageResult sr, BoundCalcStageResultEnum result, string comment) {
            sr.Finish(result, comment);
            // protect against multiple threads since they may be used ...
            lock (_stageResults) {
                _stageResults.Add(sr);
            }
        }
        public List<BoundCalcStageResult> Results {
            get {
                return _stageResults;
            }
        }
    }

    public class BoundCalcStageResult {
        public BoundCalcStageResult(string name) {
            Name = name;
        }

        public void Finish(BoundCalcStageResultEnum result, string comment) {
            Result = result;
            Comment = comment;
        }

        public string Name { get; private set;}
        public BoundCalcStageResultEnum Result {get; private set;}
        public string Comment {get; private set;}
    }

}
