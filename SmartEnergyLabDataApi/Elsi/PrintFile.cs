public class PrintFile {
    private static FileStream _fs;
    private static StreamWriter _ts;
    public static void Init() {
        var fn = "C:\\Users\\rolda\\Projects\\SmartEnergyLab\\Smart-Energy-Lab-Data-Api\\ElsiDebugCSharp.txt";
        if ( _fs!=null) {
            _fs.Close();
        }
        _fs = File.Create(fn);
        _ts = new StreamWriter(_fs);
        _ts.AutoFlush = true;        
        PrintVars("C#");
    }

    public static void PrintVars(params object[] vars) {
        #if !DEBUG
            throw new Exception("Attempt to use PrintVars in release mode");
        #endif
        if ( _ts==null ) {
            return;
        }
        int i=0;
        foreach( var v in vars) {
            if ( v is double) {
                _ts.Write($"{v:n8} ");
            } else if ( v is int) {
                _ts.Write($"{v} ");
            } else {
                _ts.Write($"{v}");
                if ( i%2 == 0 && vars.Length>1 ) {
                    _ts.Write("=");
                } else {
                    _ts.Write(" ");
                }
            }
            i++;
        }
        _ts.WriteLine();        
    }

    public static void NewLine() {
        if ( _ts==null ) {
            return;
        }
        _ts.WriteLine();
    }
}