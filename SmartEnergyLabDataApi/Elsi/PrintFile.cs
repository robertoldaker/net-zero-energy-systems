using System.Diagnostics;

public class PrintFile {
    private static FileStream _fs;
    private static StreamWriter _ts;
    public static void Init(string fn) {
        if ( _fs!=null) {
            _fs.Close();
        }
        _fs = File.Create(fn);
        _ts = new StreamWriter(_fs);
        _ts.AutoFlush = true;        
        PrintVars("C#");
    }

    public static void Close() {
        if ( _fs!=null) {
            _fs.Close();
        }
    }

    public static void PrintVars(params object[] vars) {
        #if !DEBUG
            throw new Exception("Attempt to use PrintVars in release mode");
        #endif
        if ( _ts==null ) {
            return;
        }
        int i=0;
        var msg = "";
        foreach( var v in vars) {
            if ( v is double) {
                msg+=$"{v:n8} ";
            } else if ( v is int) {
                msg+=$"{v} ";
            } else {
                msg+=$"{v}";
                if ( i%2 == 0 && vars.Length>1 ) {
                    msg+="=";
                } else {
                    msg+=" ";
                }
            }
            i++;
        }
        _ts.WriteLine(msg);
        Debug.Print(msg);
    }

    public static void NewLine() {
        if ( _ts==null ) {
            return;
        }
        _ts.WriteLine();
    }
}