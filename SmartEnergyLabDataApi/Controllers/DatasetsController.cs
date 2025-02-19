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

    /// <summary>
    /// Get list of dataset ids that are derived from the given dataset id. The list includes the provided dataset id as the first element.
    /// </summary>
    [HttpGet]
    [Route("DerivedIds")]
    public int[] DerivedIds(int datasetId) {
        using ( var da = new DataAccess() ) {
            return da.Datasets.GetDerivedDatasetIds(datasetId);
        }
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
                } else if ( dataset.Type == DatasetType.BoundCalc) {
                    return da.BoundCalc.GetResultCount(datasetId);
                } else {
                    throw new Exception($"Unexpected dataset type found [{dataset.Type}]");
                }
            } else {
                throw new Exception($"Could not find dataset for id=[{datasetId}]");
            }
        }
    }

    /// <summary>
    /// Edit an item such as a Node, Branch, Ctrl, Zone etc.
    /// </summary>
    /// <param name="editItem">data for item to be edited</param>
    [HttpPost]
    [Route("EditItem")]
    public IActionResult EditItem([FromBody] EditItem editItem) {
        using ( var m = new EditItemModel(this,editItem)) {
            if ( m.Save() ) {
                return Ok(m.DatasetData);
            } else {
                return this.ModelErrors(m.Errors);
            }
        }
    }

    /// <summary>
    /// Delete an item such as a Node, Branch, Ctrl, Zone etc.
    /// </summary>
    /// <param name="editItem">data for item to be deleted</param>
    [HttpPost]
    [Route("DeleteItem")]
    public object DeleteItem([FromBody] EditItem editItem) {
        using ( var m = new EditItemModel(this,editItem)) {
            var msg = m.Delete();
            return new { msg=msg };
        }
    }

    /// <summary>
    /// Undeletes a previous deleted item
    /// </summary>
    /// <param name="editItem">data for item to be undeleted</param>
    [HttpPost]
    [Route("UnDeleteItem")]
    public object UnDeleteItem([FromBody] EditItem editItem) {
        using ( var m = new EditItemModel(this,editItem)) {
            var msg = m.UnDelete();
            return new { msg=msg, datasets = m.DatasetData };
        }
    }

}
