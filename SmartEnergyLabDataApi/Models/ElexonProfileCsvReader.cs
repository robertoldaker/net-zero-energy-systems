using ExcelDataReader;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models;

public class ElexonProfileCsvReader {

    public void Load(IFormFile file)
    {
        using (var stream = file.OpenReadStream()) {
            using (var reader = ExcelReaderFactory.CreateCsvReader(stream)) {
                // Choose one of either 1 or 2:
                int sheetCount = 1;
                // 1. Use the reader methods
                do {
                    // Count of customers
                    if (sheetCount == 1) {
                        var dssIds = readRows(reader);
                        //
                        runClassificationTool(dssIds);
                    }
                    sheetCount++;
                } while (reader.NextResult());

            }
        }
    }

    private void runClassificationTool(List<int> dssIds) {
        var ct = new ClassificationTool();
        ct.Run(dssIds[0]);
    }

    private List<int> readRows(IExcelDataReader reader)
    {
        Logger.Instance.LogInfoEvent("Started reading Elexon profile data");
        var dssIds = new List<int>();
        using ( var da = new DataAccess() ) {
            // Consume the header
            reader.Read();
            // Read data by row
            int numRead = 0;
            int numParamsAdded = 0;
            int numNotFound=0;
            int numClsAdded = 0;
            var subsDict = new Dictionary<string,bool>();
            while (reader.Read()) {
                var substationNumber = reader.GetString(3);
                var substationName = reader.GetString(4);
                if ( substationNumber==null || subsDict.ContainsKey(substationNumber)) {
                    continue;
                } else {                
                    var substationMount = getSubstationMount(reader, 6);
                    var rating = getDouble(reader, 7);
                    var lvFeederCount = getInteger(reader,8);
                    var totalCustomers = getInteger(reader,10);
                    int index= 11;
                    var profile = new int[8];
                    for( int i=0;i<8;i++) {
                        profile[i] = getInteger(reader,index+i);
                    }
                    subsDict.Add(substationNumber,true);
                    //
                    var dss = da.Substations.GetDistributionSubstation(ImportSource.NationalGridDistributionOpenData,substationNumber);                
                    if ( dss!=null) {
                        var distParams = dss.SubstationParams;
                        // add distribution data if not exist
                        if ( distParams==null ) {
                            numParamsAdded++;
                            distParams = new SubstationParams(dss) {
                                Mount = substationMount,
                                NumberOfFeeders = lvFeederCount,
                                Rating = rating
                            };
                        }
                        var classifications = da.SubstationClassifications.GetSubstationClassifications(dss);
                        for( int i=0; i<profile.Length;i++) {
                            var num = i+1;
                            var cls = classifications.Where(m=>m.Num==num).FirstOrDefault();
                            if ( cls==null ) {
                                cls = new SubstationClassification(num,dss);
                                da.SubstationClassifications.Add(cls);                                
                                numClsAdded++;
                            }
                            cls.NumberOfCustomers = profile[i];
                        }
                        //
                        dssIds.Add(dss.Id);
                    } else {
                        numNotFound++;
                        Logger.Instance.LogInfoEvent($"Could not find dss [{substationNumber}] [{substationName}]");
                    }
                    numRead++;
                }            
            }
            //
            da.CommitChanges();
            Logger.Instance.LogInfoEvent($"Finished reading Elexon profile data numRead=[{numRead}], numNotFound=[{numNotFound}], numDataAdded=[{numParamsAdded}], numClsAdded=[{numClsAdded}]");
            return dssIds;
        }
        
    }

    private int getInteger(IExcelDataReader reader, int index) {
        var str = reader.GetString(index);
        if ( !string.IsNullOrEmpty(str) ) {
            return int.Parse(str);
        } else {
            return 0;
        }
    }

    private double getDouble(IExcelDataReader reader, int index) {
        var str = reader.GetString(index);
        if ( !string.IsNullOrEmpty(str) ) {
            return double.Parse(str);
        } else {
            return 0;
        }
    }

    private SubstationMount getSubstationMount(IExcelDataReader reader, int index) {
        var str = reader.GetString(index);
        if ( str.StartsWith("Grd")) {
            return SubstationMount.Ground;
        } else {
            return SubstationMount.Pole;
        }
    }

    private DistributionSubstationType getTransformerType(IExcelDataReader reader, int index) {
        var str = reader.GetString(index);
        if ( str.StartsWith("Grd")) {
            return DistributionSubstationType.Ground;
        } else {
            return DistributionSubstationType.Pole;
        }
    }
}