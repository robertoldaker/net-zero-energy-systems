using System.Diagnostics;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using static SmartEnergyLabDataApi.Models.ClassificationToolOutput;
using static SmartEnergyLabDataApi.Models.ClassificationToolOutput.LoadProfileData;
using static SmartEnergyLabDataApi.Models.TaskRunner;

namespace SmartEnergyLabDataApi.Models
{
    public enum SubstationMountEnum { Ground, Pole }
    public class ClassificationTool : IDisposable
    {
        private static string _spreadsheetId = "1KAgiLCZK5oZ1BeKKI-G3qaZnHMHTXTAJk1oy-s_i6_Q";
        private static string[] _scopes = { SheetsService.Scope.Spreadsheets };
        private ServiceAccountCredential _credentials;
        private SheetsService _service;

        public ClassificationTool()
        {
            string serviceAccountEmail = "smart-energy-lab@smart-energy-lab.iam.gserviceaccount.com";
            string jsonCredentials = loadCredentials();
            _credentials = (ServiceAccountCredential)
            GoogleCredential.FromJson(jsonCredentials).UnderlyingCredential;

            var initializer = new ServiceAccountCredential.Initializer(_credentials.Id) {
                User = serviceAccountEmail,
                Key = _credentials.Key,
                Scopes = _scopes
            };
            _credentials = new ServiceAccountCredential(initializer);

            //
            // Create Google Sheets API service.
            _service = new SheetsService(new BaseClientService.Initializer() {
                HttpClientInitializer = _credentials,
                ApplicationName = "Smart-energy-lab",
            });

        }

        private string loadCredentials()
        {
            //
            // this file needs to appear in the home directory and also will not be in the source code repository
            // since the repository is now open source
            //
            string fileName = "smart-energy-lab (service account).json";
            string file = Path.Combine(AppEnvironment.Instance.RootFolder, fileName);
            if ( File.Exists(file)) {
                string json = File.ReadAllText(file);
                return json;
            } else {
                throw new Exception($"Cannot find service account credentials [{fileName}]");
            }
        }

        public void Dispose()
        {
            _service.Dispose();
        }


        public  void RunAll(int gaId, ClassificationToolInput input, TaskRunner? taskRunner=null) {
            using ( var da = new DataAccess() ) {
                var dsss = da.Substations.GetDistributionSubstationsByGAId(gaId);
                var loadProfiles = da.SubstationLoadProfiles.GetAllGeographicalAreaLoadProfiles(gaId,LoadProfileSource.Tool, 2016);
                int i=0;
                foreach( var dss in dsss) {
                    if ( taskRunner!=null) {
                        taskRunner.CancellationToken.ThrowIfCancellationRequested();
                    }
                    i++;
                    // Use Eleoxprofile from distribution substation
                    var classifications = da.SubstationClassifications.GetSubstationClassifications(dss);
                    input.ElexonProfile = classifications.GetElexonProfile();
                    // Run tool
                    var output = Run(input);
                    // Update any tool generated load profiles we currently have or create new ones
                    updateLoadProfiles(da,loadProfiles,dss,Day.All,output);
                    updateLoadProfiles(da,loadProfiles,dss,Day.Saturday,output);
                    updateLoadProfiles(da,loadProfiles,dss,Day.Sunday,output);
                    updateLoadProfiles(da,loadProfiles,dss,Day.Weekday,output);
                    //
                    var message = $"Created load profile data for [{dss.Name}]";
                    if ( taskRunner!=null ) {
                        int percentCompleted = (i*100)/dsss.Count;
                        taskRunner.Update(TaskState.RunningState.Running, message, percentCompleted);
                    }
                    Logger.Instance.LogInfoEvent(message);
                }
                //
                da.CommitChanges();
            }
        }

        public  void Run(int id) {
            using ( var da = new DataAccess() ) {
                var dss = da.Substations.GetDistributionSubstation(id);
                var loadProfiles = da.SubstationLoadProfiles.GetDistributionSubstationLoadProfiles(id,LoadProfileSource.Tool, 2016);
                var input = dss.GetClassificatonToolInput(da);
                // Run tool
                var output = Run(input);
                // Update any tool generated load profiles we currently have or create new ones
                updateLoadProfiles(da,loadProfiles,dss,Day.All,output);
                updateLoadProfiles(da,loadProfiles,dss,Day.Saturday,output);
                updateLoadProfiles(da,loadProfiles,dss,Day.Sunday,output);
                updateLoadProfiles(da,loadProfiles,dss,Day.Weekday,output);
                //
                var message = $"Created data for [{dss.Name}]";
                Logger.Instance.LogInfoEvent(message);
                //
                da.CommitChanges();
            }
        }

        private void updateLoadProfiles(DataAccess da, IList<SubstationLoadProfile> cons, DistributionSubstation dss, Day day, ClassificationToolOutput output) {
            // See if it exists
            var con = cons.Where( m=>m.Day == day && m.DistributionSubstation.Id == dss.Id).FirstOrDefault();
            if ( con == null ) {
                con = new SubstationLoadProfile(dss);
                con.Day = day;
                con.Source = LoadProfileSource.Tool;
                //
                da.SubstationLoadProfiles.Add(con);
            }

            DayLoadProfile lpd = null;
            if ( con.Day == Day.All) {
                lpd = output.LoadProfile.All;
            } else if ( con.Day == Day.Saturday) {
                lpd = output.LoadProfile.Saturday;
            } else if ( con.Day == Day.Sunday) {
                lpd = output.LoadProfile.Sunday;
            } else if ( con.Day == Day.Weekday ) {
                lpd = output.LoadProfile.Weekday;
            } else {
                throw new Exception($"Unexpected day [{con.Day}]");
            }
            //
            var data = new double[lpd.Load.Length];
            for( int i=0; i<data.Length;i++ ) {
                data[i] = lpd.Load[i] * lpd.Peak;
            }
            //
            con.Data = data;
            con.IntervalMins = 24*60 / data.Length;
            // Actually a guess for the mo!!
            con.Season = Season.Winter;
        }

        /// <summary>
        /// Runs the classification tool and returns the output
        /// </summary>
        /// <param name="input">Input to the tool</param>
        /// <returns>Output from the tool</returns>
        public ClassificationToolOutput Run(ClassificationToolInput input)
        {

            var valueRanges = getValueRanges(input);
            var body = new BatchUpdateValuesRequest();
            body.Data = valueRanges;
            body.ValueInputOption = "USER_ENTERED";

            var requestRunner = new RequestRunner();

            // Run request to save the parameters
            requestRunner.Run(()=> {
                var request1 = _service.Spreadsheets.Values.BatchUpdate(body,_spreadsheetId);
                request1.Execute();
            });

            // Run request to extract data
            BatchGetValuesResponse response=null;
            requestRunner.Run(()=>{
                var request2 = _service.Spreadsheets.Values.BatchGet(_spreadsheetId);
                request2.Ranges = new string[] {
                    "ClassRulesXL1.csv!E33:E42",  // Cluster probabilities
                    "ClassRulesXL1.csv!F43",      // Cluster number
                    "ClassRulesXL1.csv!C55:G198", // Load data (All, Sat, Sun, Weekday)
                    "ClassRulesXL1.csv!D46:D49",  // Peak loads
                };
                response = request2.Execute();
            });

            // Now get the results
            var output = new ClassificationToolOutput();
            output.ClusterProbabilities = getDoubleArray(response.ValueRanges[0], 0, 10);
            output.ClusterNumber = (int) getDoubleArray(response.ValueRanges[1],0, 1)[0];

            // Loads
            output.LoadProfile.All.Load = getDoubleArray(response.ValueRanges[2], 0, 144);
            output.LoadProfile.Weekday.Load = getDoubleArray(response.ValueRanges[2], 1, 144);
            output.LoadProfile.Saturday.Load = getDoubleArray(response.ValueRanges[2], 2, 144);
            output.LoadProfile.Sunday.Load = getDoubleArray(response.ValueRanges[2], 3, 144);
            // Time of day
            output.LoadProfile.TimeOfDay = getStringArray(response.ValueRanges[2], 4, 144);
            // Peaks
            var peaks = getDoubleArray(response.ValueRanges[3], 0, 4);
            output.LoadProfile.All.Peak = peaks[0];
            output.LoadProfile.Weekday.Peak = peaks[1];
            output.LoadProfile.Saturday.Peak = peaks[2];
            output.LoadProfile.Sunday.Peak = peaks[3];
            //
            return output;
        }

        private bool retryRequest(Google.GoogleApiException e) {
            if ( e.Message.Contains("Quota exceeded") ) {
                return true;
            } else {
                return false;
            }
        }

        private double[] getDoubleArray( ValueRange valueRange, int column, int numberOfValues) {
            IList<IList<Object>> values = valueRange.Values;
            if (values != null && values.Count == numberOfValues) {
                var data = values.Select(m=> double.Parse((string) m[column])).ToArray();
                return data;
            }
            else {
                throw new Exception("Could not get data from spreadsheet. Unexpected values returned");
            }
        }

        private string[] getStringArray( ValueRange valueRange, int column, int numberOfValues) {
            IList<IList<Object>> values = valueRange.Values;
            if (values != null && values.Count == numberOfValues) {
                var data = values.Select(m=> (string) m[column]).ToArray();
                return data;
            }
            else {
                throw new Exception("Could not get data from spreadsheet. Unexpected values returned");
            }
        }

        private List<ValueRange> getValueRanges(ClassificationToolInput input) {
            var valueRanges = new List<ValueRange>();

            // Elexon profile
            var elexonValues = new List<IList<object>>();
            foreach( int val in input.ElexonProfile ) {
                elexonValues.Add(new List<object>() { val.ToString() });
            }
            var elexonVr = new ValueRange();
            elexonVr.Range = "Input Sheet!C6:C13";
            elexonVr.Values = (IList<IList<object>>) elexonValues;
            //
            valueRanges.Add(elexonVr);

            // Other
            var otherValues = new List<IList<object>>();
            if ( input.SubstationMount==SubstationMountEnum.Ground ) {
                otherValues.Add( new List<object>() { "Ground Mounted" });
            } else {
                otherValues.Add( new List<object>() { "Pole Mounted" });
            }
            // Transformer rating
            otherValues.Add(new List<object>() { input.TransformerRating.ToString() });
            // Percentage industrial customers
            otherValues.Add(new List<object>() { $"{input.PercentIndustrialCustomers.ToString()}%" });
            // Number of feeders
            otherValues.Add(new List<object>() { input.NumberOfFeeders.ToString() });
            // Percentage half-hourly load
            otherValues.Add(new List<object>() { $"{input.PercentageHalfHourlyLoad.ToString()}%" });
            //  Total length
            otherValues.Add(new List<object>() { input.TotalLength.ToString() });
            // Percentage overhead
            otherValues.Add(new List<object>() { $"{input.PercentageOverhead.ToString()}%" });
            var otherVr = new ValueRange();
            otherVr.Range = "Input Sheet!C15:C21";
            otherVr.Values = otherValues;
            //
            valueRanges.Add( otherVr);

            return valueRanges;
        }

        private class RequestRunner {
            private int _nWaits = 0;

            public void Run(Action a) {
                bool cont=true;
                while (cont) {
                    try {
                        a.Invoke();
                        cont = false;
                        _nWaits = 0;
                    } catch (Google.GoogleApiException e) {
                        if ( e.Message.Contains("Quota exceeded")) {
                            var waitTime = getWaitTime();
                            if (waitTime==0 ) {
                                throw new Exception("Quota wait time exceeded");
                            }
                            Logger.Instance.LogInfoEvent($"Quota exceeded waiting for {waitTime}s before retrying request");
                            Thread.Sleep(1000*waitTime);
                        } else {
                            // Not quota exceeded so just throw it
                            throw;
                        }
                    }
                }
            }
            private int getWaitTime() {
                _nWaits++;
                if ( _nWaits==10 ) {
                    return 0;
                } else {
                    return 1<<_nWaits;
                }
            }
        }
    }


    /// <summary>
    /// Class to define inputs into classification tool
    /// </summary>
    public class ClassificationToolInput {

        /// <summary>
        /// Elexon profile number of customers
        /// </summary>
        /// <value></value>
        public int[] ElexonProfile {get; set;}

        /// <summary>
        /// How the substation is mounted
        /// </summary>
        /// <value></value>
        public SubstationMountEnum SubstationMount {get; set;}

        /// <summary>
        /// Rating for the transformer in KVA
        /// </summary>
        /// <value></value>
        public double TransformerRating {get; set;}

        /// <summary>
        /// Percentage of industrial customers
        /// </summary>
        /// <value></value>
        public double PercentIndustrialCustomers {get; set;}

        /// <summary>
        /// Number of low-voltage feeders
        /// </summary>
        /// <value></value>
        public int NumberOfFeeders {get; set;}

        /// <summary>
        /// Percentage half-hourly load
        /// </summary>
        /// <value></value>
        public double PercentageHalfHourlyLoad {get; set;}

        /// <summary>
        /// Total length
        /// </summary>
        /// <value></value>
        public double TotalLength {get; set;}

        /// <summary>
        /// Percentage overhead (primary substation)
        /// </summary>
        /// <value></value>
        public double PercentageOverhead {get; set;}
    }

    /// <summary>
    /// Output of classification tool
    /// </summary>
    public class ClassificationToolOutput {

        public ClassificationToolOutput()
        {
            LoadProfile = new LoadProfileData();
        }

        /// <summary>
        /// Classification number
        /// </summary>
        /// <value></value>
        public int ClusterNumber {get; set;}

        /// <summary>
        /// Cluster probabilities
        /// </summary>
        /// <value></value>
        public double[] ClusterProbabilities {get; set;}

        /// <summary>
        /// Estimated load
        /// </summary>
        public LoadProfileData LoadProfile { get; set; }

        public class LoadProfileData
        {
            public LoadProfileData()
            {
                All = new DayLoadProfile();
                Saturday = new DayLoadProfile();
                Sunday = new DayLoadProfile();
                Weekday = new DayLoadProfile();
            }
            /// <summary>
            /// String representing time of day in format HH:mm
            /// </summary>
            public string[] TimeOfDay { get; set; }
            /// <summary>
            /// Weighted average of Sat, Sun adn weekday profiles
            /// </summary>
            public DayLoadProfile All { get; set; }
            /// <summary>
            /// Load profile for Saturday
            /// </summary>
            public DayLoadProfile Saturday { get; set; }
            /// <summary>
            /// Load profile for Sunday
            /// </summary>
            public DayLoadProfile Sunday { get; set; }
            /// <summary>
            /// Load profile for a weekday
            /// </summary>
            public DayLoadProfile Weekday { get; set; }
            public class DayLoadProfile
            {
                /// <summary>
                /// Load values as a percentage of peak
                /// </summary>
                public double[] Load { get; set; }
                /// <summary>
                /// Peak load in kwh
                /// </summary>
                public double Peak { get; set; }
            }
        }
    }
}
