using System.Diagnostics;

namespace SmartEnergyLabDataApi.BoundCalc;

public class ProgressManager {

    private int _totalCycles;
    private int _cycleCount;
    private Stopwatch _sw;
    private double _lastUpdate;
    public ProgressManager() {
        _sw = new Stopwatch();
    }

    public delegate void ProgressHandler(string msg, int percent);
    public event ProgressHandler ProgressUpdate;

    public void Start(string msg, int totalCycles) {

        _totalCycles = totalCycles;
        _cycleCount = 0;
        _lastUpdate = 0;
        _sw.Start();        
        ProgressUpdate?.Invoke(msg,0);
    }

    public void Update(string msg) {
        _cycleCount++;
        var elaspedSecs = _sw.Elapsed.TotalSeconds;
        if ( ( elaspedSecs - _lastUpdate)>0.25 ) {
            var percent = (_cycleCount*100)/_totalCycles;
            ProgressUpdate?.Invoke(msg,percent);
            _lastUpdate = elaspedSecs;
        }
    }

    public void Finish(string msg=null) {
        _sw.Stop();
        if ( msg==null ) {
            msg = $"Finished in {_sw.Elapsed.TotalSeconds:0.00}s";
        }
        ProgressUpdate?.Invoke(msg,100);
    }


}