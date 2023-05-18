using System.Text.Json.Serialization;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models
{
    public class ConditionalDataLoader {
        //
        public string Load(CKANDataLoader.CKANDataset spd, Func<string> loadData) {
            string message = "";
            //
            using( var da = new DataAccess() ) {
                if ( spd!=null ) {
                    var dsi = GetDataSourceInfo(da, spd);
                    if ( spd.NeedsImport(dsi.LastModified) ) {
                        try {
                            // get caller to load necessary data
                            message=loadData.Invoke();
                            //
                            dsi.LastImported = DateTime.UtcNow;
                            dsi.State = ImportState.OK;
                            dsi.Message = "";
                            dsi.LastModified = spd.last_modified.ToUniversalTime();
                        } catch( Exception e) {
                            // This needs sorting out properly as it looks like the driver is allowing only UTC datetimes to be specified
                            // but when loaded from the db they do not have the Kind flag set to UTC.
                            // Hence we need to set all of them explicitly here
                            if ( dsi.LastImported!=null ) {
                                DateTime.SpecifyKind((DateTime) dsi.LastImported,DateTimeKind.Utc);
                            }
                            if ( dsi.LastModified!=null) {
                                DateTime.SpecifyKind((DateTime) dsi.LastModified,DateTimeKind.Utc);
                            }
                            dsi.State = ImportState.Error;
                            dsi.Message = e.Message;
                            message=e.Message+"\n";
                        }
                    } else {
                        message=$"Ignoring import for [{spd.name}] since it has not been modified since last import\n";
                    }
                }
                //
                //??da.CommitChanges();
            }
            //
            return message;
        }

        private DataSourceInfo GetDataSourceInfo( DataAccess da,  CKANDataLoader.CKANDataset spd) {
            var dataSourceInfo = da.Admin.GetDataSourceInfo(spd.id);
            if ( dataSourceInfo==null) {
                dataSourceInfo = new DataSourceInfo();
                da.Admin.Add(dataSourceInfo);
            }
            dataSourceInfo.Name = spd.name;
            dataSourceInfo.Url = spd.url;
            dataSourceInfo.Reference = spd.id;
            //
            return dataSourceInfo;
        }

    }
}