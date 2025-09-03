using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HaloSoft.EventLogger;
using LumenWorks.Framework.IO.Csv;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models;

public class ElexonGspDemandProfileLoader {

    private int _startYear = 2020;
    private HttpClient _httpClient;
    private object _httpClientLock = new object();
    private enum CsvColumn {
        GSPGroup,
        SettlementDate,
        SettlementRunType,
        GSPId,
        SettlementPeriod,
        EstimateIndicator,
        MeterVolume,
        ImportExportIndicator,
    }
    private Dictionary<CsvColumn, int> _headerDict = new Dictionary<CsvColumn, int>();


    public void Load()
    {
        // get current date range of entries
        var dateRange = getDateRange();
        //
        var currentYear = DateTime.Now.Year;
        //
        for(int year=_startYear; year<=currentYear; year++) {
            var url = getUrl(year);
            if (dateRange.earliest == null || (year <= ((DateTime)dateRange.earliest).Year) ||
                  dateRange.latest == null || (year >= ((DateTime)dateRange.latest).Year)) {
                var zipFile = Path.Combine(AppFolders.Instance.Temp, $"GP9_{year}.zip");
                if (File.Exists(zipFile)) {
                    processZipFile(zipFile, dateRange);
                } else {
                    processUrl(url, dateRange);
                }
            }
        }
        addLocations();
    }

    private string getUrl(int year)
    {
        return $"https://www.elexon.co.uk/open-data/GP9_{year}.zip";
    }

    private int getYear(string url)
    {
        var regEx = new Regex(@"GP9_(\d{4}).zip$");
        var match = regEx.Match(url);
        if (match.Success) {
            var yearStr = match.Groups[1].Value;
            var year = int.Parse(yearStr);
            return year;
        } else {
            throw new Exception($"Could not get year from url [{url}]");
        }
    }

    private void processUrl(string url, (DateTime? earliest, DateTime? latest) dateRange) {
        var outFolder = Path.Combine(AppFolders.Instance.Temp, "GSP_Data");
        var client = getHttpClient();
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, url)) {
            var response = client.SendAsync(message).Result;
            //
            if (response.IsSuccessStatusCode) {
                var stream = response.Content.ReadAsStream();
                var cd = response.Content.Headers.ContentDisposition;
                if (stream != null) {
                    // Clear out folder before extracting .zip
                    deleteCsvFiles(outFolder);
                    // Extract files from zip
                    ZipFile.ExtractToDirectory(stream, outFolder, true);
                    // process files
                    processFiles(outFolder, dateRange);
                } else {
                    throw new Exception($"NUll stream downloading data from [{url}]");
                }

            } else {
                throw new Exception($"Unexpected response [{response.StatusCode}] [{response.ReasonPhrase}]");
            }
        }
    }

    private void processZipFile(string zipFile, (DateTime? earliest, DateTime? latest) dateRange)
    {
        var outFolder = Path.Combine(AppFolders.Instance.Temp, "GSP_Data");
        // Clear out folder before extracting .zip
        deleteCsvFiles(outFolder);
        // Extract files from zip
        ZipFile.ExtractToDirectory(zipFile, outFolder, true);
        // process files
        processFiles(outFolder, dateRange);
    }

    private void deleteCsvFiles(string folder)
    {
        if (Directory.Exists(folder)) {
            var gspFiles = Directory.EnumerateFiles(folder, "*.csv");
            foreach (var file in gspFiles) {
                //
                File.Delete(file);
            }
        }
    }

    private void addLocations()
    {
        using (var da = new DataAccess()) {
            var dict = da.NationalGrid.GetGridSubstationLocationDict();
            var profiles = da.Elexon.GetGspDemandProfilesWithNoLocation();
            foreach (var p in profiles) {
                if (dict.ContainsKey(p.GspCode)) {
                    p.Location = dict[p.GspCode];
                }
            }
            da.CommitChanges();
        }
    }

    private (DateTime? earliest, DateTime? latest) getDateRange()
    {
        DateTime? earliest, latest;
        using (var da = new DataAccess()) {
            latest = da.Elexon.GetLatestDate();
            earliest = da.Elexon.GetEarliestDate();
        }
        return (earliest, latest);
    }

    private void processFiles(string folder, (DateTime? earliest, DateTime? latest) dateRange)
    {
        var gspFiles = Directory.EnumerateFiles(folder);
        gspFiles = gspFiles.OrderBy(m => getDateTimeForCsv(m)).ToList();

        foreach (var file in gspFiles) {
            if (Path.GetExtension(file) == ".csv") {
                try {
                    var fileDateTime = getDateTimeForCsv(file);
                    // since the settlement dates in the file lag the write times of the file
                    // by about 7 days add some wiggle room
                    var startTime = fileDateTime - new TimeSpan(10, 0, 0, 0);
                    var endTime = fileDateTime + new TimeSpan(31, 0, 0, 0);
                    if (dateRange.latest == null || dateRange.latest < endTime ||
                        dateRange.earliest == null || dateRange.earliest > startTime) {
                        processFile(file, dateRange);
                    } else {
                        var fn = Path.GetFileName(file);
                        Logger.Instance.LogInfoEvent($"Skipping GSP Period Data file [{fn}]");
                    }
                } finally {
                    // clear up files
                    if (File.Exists(file)) {
                        File.Delete(file);
                    }
                }
            }
        }
    }

    private DateTime getDateTimeForCsv(string file)
    {
        var regEx = new Regex(@"(\d{4})(\d{2})\.csv$");
        var match = regEx.Match(file);
        if (match.Success) {
            var yearStr = match.Groups[1].Value;
            var year = int.Parse(yearStr);
            var monthStr = match.Groups[2].Value;
            var month = int.Parse(monthStr);
            return new DateTime(year,month,1);
        } else {
            throw new Exception($"Cannot find month from csv file [{file}");
        }
    }

    private void processFile(string file, (DateTime? earliest, DateTime? latest) dateRange)
    {
        _headerDict.Clear();
        var fn = Path.GetFileName(file);
        Logger.Instance.LogInfoEvent($"Processing GSP Period Data file [{fn}]");
        int profileCount = 0;
        // Read from downloaded file
        using (var tr = new StreamReader(file)) {
            using (CsvReader reader = new CsvReader(tr, true)) {
                setHeaderDict(reader);
                reader.ReadNextRecord();
                while (processCsvSegment(reader, dateRange, ref profileCount)) {
                    Logger.Instance.LogInfoEvent($"Saved [{profileCount}] profiles");
                }
            }
        }
        //
        Logger.Instance.LogInfoEvent($"Profiles added=[{profileCount}]");
    }

    private void setHeaderDict(CsvReader reader)
    {
        var headers = reader.GetFieldHeaders().ToList();
        _headerDict.Add(CsvColumn.GSPGroup, headers.FindIndex(m => m == "GSP Group Id"));
        _headerDict.Add(CsvColumn.SettlementDate, headers.FindIndex(m => m == "Settlement Date"));
        _headerDict.Add(CsvColumn.SettlementRunType, headers.FindIndex(m => m == "Settlement Run Type"));
        _headerDict.Add(CsvColumn.GSPId, headers.FindIndex(m => m == "GSP Id"));
        _headerDict.Add(CsvColumn.SettlementPeriod, headers.FindIndex(m => m == "Settlement Period"));
        _headerDict.Add(CsvColumn.EstimateIndicator, headers.FindIndex(m => m == "Estimate Indicator"));
        _headerDict.Add(CsvColumn.MeterVolume, headers.FindIndex(m => m == "Meter Volume"));
        _headerDict.Add(CsvColumn.ImportExportIndicator, headers.FindIndex(m => m == "Import/Export Indicator"));
    }

    private int getIndex(CsvColumn col)
    {
        return _headerDict[col];
    }

    private void processCsvFile(string fileName)
    {

    }

    private bool processCsvSegment(CsvReader reader, (DateTime? earliest, DateTime? latest) dateRange, ref int profileCount)
    {
        int nProfiles = 0;
        int profilesToProcess = 500;
        bool moreAvailable = false;
        using (var da = new DataAccess()) {
            bool moreData = true;
            while (moreData) {

                /*int percent = (rowCount * 100) / totalRows;
                if (percent != prevPercent) {
                    _taskRunner.Update(percent);
                    prevPercent = percent;
                }
                if (rowCount % 100 == 0) {
                    _taskRunner.CheckCancelled();
                }
                */
                (bool processedFile, moreData) = processProfile(da, reader, dateRange);
                if (processedFile) {
                    profileCount++;
                    nProfiles++;
                    if (nProfiles >= profilesToProcess) {
                        moreAvailable = true;
                        break;
                    }
                }
            }
            da.CommitChanges();
        }
        return moreAvailable;
    }

    private (bool processedFile, bool moreData) processProfile(DataAccess da, CsvReader reader, (DateTime? earliest, DateTime? latest) dateRange)
    {
        bool moreData = false;
        // only process ones with settlement run time = II
        var runType = reader[getIndex(CsvColumn.SettlementRunType)];
        if (runType != "II") {
            moreData = reader.ReadNextRecord();
            return (false,moreData);
        }
        // Settlement date
        var sdStr = reader[getIndex(CsvColumn.SettlementDate)];
        var year = int.Parse(sdStr.Substring(0, 4));
        var month = int.Parse(sdStr.Substring(4, 2));
        var date = int.Parse(sdStr.Substring(6, 2));
        var dt = new DateTime(year, month, date);
        // if date outside range then exit
        if ( (dateRange.latest!=null && dt <= dateRange.latest) &&
             (dateRange.earliest!=null && dt >= dateRange.earliest) ) {
            moreData = reader.ReadNextRecord();
            return (false, moreData);
        }
        // look for
        var sp = int.Parse(reader[getIndex(CsvColumn.SettlementPeriod)]);
        if (sp == 1) {
            var data = new GspDemandProfileData();
            // Gsp group Id
            data.GspGroupId = reader[getIndex(CsvColumn.GSPGroup)];
            data.Date = dt;
            // Gsp id
            data.GspId = reader[getIndex(CsvColumn.GSPId)];
            // Code which is the first 4 letters
            data.GspCode = data.GspId.Substring(0, 4);
            // Demand
            data.Demand = new double[48];
            data.IsEstimate = new bool[48];
            (double demand, bool isEstimate) = getDemand(reader);
            data.Demand[sp - 1] = demand;
            data.IsEstimate[sp - 1] = isEstimate;
            int spCount = 2;
            // read next rows until we reach another one with profile = 1 and store in demand array
            while (true) {
                moreData = reader.ReadNextRecord();
                if (!moreData) {
                    break;
                }
                sp = int.Parse(reader[getIndex(CsvColumn.SettlementPeriod)]);
                // This is the next profile so abort
                if (sp == 1) {
                    // this means there was < 48 profiles
                    if (spCount != 49) {
                        Logger.Instance.LogWarningEvent($"Unexpected value for sp, found [{sp}], expected [{spCount}]");
                    }
                    break;
                }
                (demand, isEstimate) = getDemand(reader);
                if (sp < 0 || sp > data.Demand.Length) {
                    Logger.Instance.LogWarningEvent($"Unexpected value for sp, found [{sp}]");
                } else {
                    data.Demand[sp - 1] = demand;
                    data.IsEstimate[sp - 1] = isEstimate;
                }
                spCount++;
            }
            // add to database
            da.Elexon.Add(data);
        } else {
            throw new Exception($"Expected settlement period of 1 but found [{sp}]");
        }

        return (true,moreData);
    }

    private (double demand, bool isEstimate) getDemand(CsvReader reader)
    {
        // Estimate
        var eStr = reader[getIndex(CsvColumn.EstimateIndicator)];
        bool isEstimate = eStr == "T" ? true : false;
        // Import/export
        var ieStr = reader[getIndex(CsvColumn.ImportExportIndicator)];
        double fac = ieStr == "I" ? 1 : -1;
        // Demand
        var mvStr = reader[getIndex(CsvColumn.MeterVolume)];
        // data is total MWh over the 0.5h period so double for MW
        var demand = (double.Parse(mvStr) * 2)*fac;
        return (demand, isEstimate);
    }

    private void extractZip(string folder, string fn, string outFolder)
    {
        fn = fn.Trim('"');
        string zipPath = Path.Combine(folder, fn);

        outFolder = Path.Combine(folder, outFolder);

        ZipFile.ExtractToDirectory(zipPath, outFolder, true);

    }

    private HttpClient getHttpClient()
    {
        if (_httpClient == null) {
            lock (_httpClientLock) {
                _httpClient = new HttpClient();
               //?? _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            }
        }
        //
        return _httpClient;
    }
    private HttpRequestMessage getRequestMessage(HttpMethod httpMethod, string method, object data = null)
    {
        HttpRequestMessage message = new HttpRequestMessage(httpMethod, method);
        message.Headers.Add("Accept", "*/*");
        message.Headers.Add("User-Agent", "PostmanRuntime/7.37.3");
        if (data != null) {
            string reqStr;
            if (data is string) {
                reqStr = (string)data;
            } else {
                reqStr = JsonSerializer.Serialize(data);
            }
            message.Content = new StringContent(reqStr, Encoding.UTF8, "application/json");
        }
        return message;
    }

}
