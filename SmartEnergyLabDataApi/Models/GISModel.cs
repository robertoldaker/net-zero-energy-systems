using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;

namespace SmartEnergyLabDataApi.Models 
{
    public class GISModel : DbModel 
    {
        public GISModel(ControllerBase c) : base(c) {

        }

        public IList<GISBoundary> GetBoundaries(int gisDataId) {
            return _da.GIS.GetBoundaries(gisDataId);
        }
    }
}