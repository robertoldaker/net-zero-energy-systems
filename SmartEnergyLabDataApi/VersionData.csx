
namespace SmartEnergyLabDataApi;

public class VersionData {

    public string Version {
        get {
            return "1.0";
        }
    }

    public string CommitId {
        get {
            return "$COMMIT_ID$";
        }
    }

    public string CommitDate {
        get {
            return "$COMMIT_DATE$";
        }
    }
}