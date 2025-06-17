using System.IO.Compression;
using System.Text;
using System.Text.Json;
using HaloSoft.EventLogger;
using LumenWorks.Framework.IO.Csv;
using Org.BouncyCastle.Crypto.Signers;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models;

public class ElexonGspDemandProfileLoader {

    private string _url = "https://www.elexon.co.uk/open-data/GP9_2025.zip";
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
        var latestDate = getLatestDate();
        var outFolder = Path.Combine(AppFolders.Instance.Temp, "GSP_Data");
        // Download json
        var client = getHttpClient();
        using (HttpRequestMessage message = getRequestMessage(HttpMethod.Get, _url)) {
            var response = client.SendAsync(message).Result;
            //
            if (response.IsSuccessStatusCode) {
                var stream = response.Content.ReadAsStream();
                var cd = response.Content.Headers.ContentDisposition;
                if (stream != null) {
                    ZipFile.ExtractToDirectory(stream, outFolder, true);
                    //
                    processFiles(outFolder, latestDate);
                } else {
                    throw new Exception($"NUll stream downloading data from [{_url}]");
                }

            } else {
                throw new Exception($"Unexpected response [{response.StatusCode}] [{response.ReasonPhrase}]");
            }
        }
        addLocations();
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

    private DateTime? getLatestDate()
    {
        using (var da = new DataAccess()) {
            return da.Elexon.GetLatestDate();
        }
    }

    private void processFiles(string folder, DateTime? latestDate)
    {
        var gspFiles = Directory.EnumerateFiles(folder);
        gspFiles = gspFiles.OrderBy(m => File.GetLastWriteTime(m)).ToList();

        foreach (var file in gspFiles) {
            if (Path.GetExtension(file) == ".csv") {
                // since the settlementdates in the file lag the write times of the file
                // by about 7 days add some wiggle room
                if (latestDate < File.GetLastWriteTime(file) - new TimeSpan(10, 0, 0)) {
                    processFile(file, latestDate);
                }
            }
        }
    }

    private void processFile(string file, DateTime? latestDate)
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
                while (processCsvSegment(reader, latestDate, ref profileCount)) {
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

    private bool processCsvSegment(CsvReader reader, DateTime? latestDate, ref int profileCount)
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
                (bool processedFile, moreData) = processProfile(da, reader, latestDate);
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

    private (bool processedFile, bool moreData) processProfile(DataAccess da, CsvReader reader, DateTime? latestDate)
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
        // date has to be after our latest date to add to db so ignore if before or the same
        if (latestDate!=null && dt <= latestDate) {
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
                data.Demand[sp - 1] = demand;
                data.IsEstimate[sp - 1] = isEstimate;
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
