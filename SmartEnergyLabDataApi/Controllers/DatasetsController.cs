using Microsoft.AspNetCore.Mvc;
using SmartEnergyLabDataApi.Data;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Controllers;

[Route("Datasets")]
[ApiController]
public class DatasetsController : ControllerBase
{
    public DatasetsController()
    {
    }

    /// <summary>
    /// Get list of datasets for the given type and logged on user
    /// </summary>
    [HttpGet]
    [Route("Datasets")]
    public IList<Dataset> Datasets(DatasetType type) {
        using ( var da = new DataAccess() ) {
            return da.Datasets.GetDatasets(type,this.GetUserId());
        }
    }

    /// <summary>
    /// Creates a new Dataset for the given type and logged on user
    /// </summary>
    [HttpPost]
    [Route("New")]
    public IActionResult New([FromBody] NewDataset dv) {
        using( var m = new NewDatasetModel(this, dv)) {
            if ( !m.Save() ) {
                return this.ModelErrors(m.Errors);
            }
            // return the id of the new object created
            return Ok(m.Id.ToString());
        }
    }

    /// <summary>
    /// Saves changes to an existing Dataset for the logged on user
    /// </summary>
    [HttpPost]
    [Route("Save")]
    public IActionResult Save([FromBody] Dataset dv) {
        using( var m = new EditDatasetModel(this, dv)) {
            if ( !m.Save() ) {
                return this.ModelErrors(m.Errors);
            }
            return Ok();
        }
    }

    /// <summary>
    /// Deletes an existing Dataset for the logged on user
    /// </summary>
    [HttpPost]
    [Route("Delete")]
    public IActionResult Delete([FromBody] int id) {
        using( var m = new EditDatasetModel(this, id)) {
            m.Delete();
            return Ok();
        }
    }

    [HttpPost]
    [Route("SaveUserEdit")]
    public IActionResult SaveUserEdit([FromBody] UserEdit userEdit) {
        using( var da = new DataAccess() ) {
            da.Datasets.SaveUserEdit(userEdit);
            da.CommitChanges();
        }
        return Ok();
    }

    [HttpPost]
    [Route("DeleteUserEdit")]
    public IActionResult DeleteUserEdit([FromBody] int id) {
        using( var da = new DataAccess() ) {
            da.Datasets.DeleteUserEdit(id);
            da.CommitChanges();
        }
        return Ok();
    }

    /// <summary>
    /// Get result count for given dataset
    /// </summary>
    [HttpGet]
    [Route("ResultCount")]
    public int ResultCount(int datasetId) {
        using ( var da = new DataAccess() ) {
            var dataset = da.Datasets.GetDataset(datasetId);
            if ( dataset!=null ) {
                if ( dataset.Type == DatasetType.Elsi) {
                    return da.Elsi.GetResultCount(datasetId);
                } else if ( dataset.Type == DatasetType.Loadflow) {
                    return da.Loadflow.GetResultCount(datasetId);
                } else {
                    throw new Exception($"Unexpected dataset type found [{dataset.Type}]");
                }
            } else {
                throw new Exception($"Coulnd not find dataset for id=[{datasetId}]");
            }
        }
    }
}
