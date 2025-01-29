using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using NHibernate.Criterion;
using SmartEnergyLabDataApi.BoundCalc;
using SmartEnergyLabDataApi.Loadflow;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    public class BoundCalcDS : DataSet
    {
        public BoundCalcDS(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
        }

        public IList<T> GetRawData<T>(System.Linq.Expressions.Expression<Func<T,bool>> whereFcn) where T: class{
            //
            var q = Session.QueryOver<T>().Where(whereFcn);

            return q.List();
        }

        #region Nodes
        public void Add(BoundCalcNode obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundCalcNode obj)
        {
            Session.Delete(obj);
        }

        public BoundCalcNode GetNode(string code) {
            return Session.QueryOver<BoundCalcNode>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }

        public BoundCalcNode GetNode(int id) {
            return Session.Get<BoundCalcNode>(id);
        }

        public IList<BoundCalcNode> GetNodes(Dataset dataset) {
            return Session.QueryOver<BoundCalcNode>().
            Where( m=>m.Dataset==dataset).
            Fetch(SelectMode.Fetch, m=>m.Zone).
            OrderBy(m=>m.Id).Asc.
            List();
        }

        public void SetNodeVoltagesAndLocations(int datasetId) {
            var nodes = Session.QueryOver<BoundCalcNode>().Where( m=>m.Dataset.Id == datasetId).List();
            foreach( var node in nodes) {
                node.SetVoltage();
                node.SetLocation(this.DataAccess);
            }
        } 

        public bool NodeExists(int datasetId, string code, out Dataset? dataset) {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var node = Session.QueryOver<BoundCalcNode>().
                Where( m=>m.Code.IsInsensitiveLike(code)).
                Where( m=>m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Fetch(SelectMode.Fetch,m=>m.Dataset).
                Take(1).
                SingleOrDefault();
            if ( node!=null) {
                dataset = node.Dataset;
            } else {
                dataset = null;
            }
            return node!=null;
        }

        public DatasetData<BoundCalcNode> GetNodeDatasetData(int datasetId,System.Linq.Expressions.Expression<Func<BoundCalcNode, bool>> expression,
             out DatasetData<GridSubstationLocation> locDi) {
            var nodeQuery = Session.QueryOver<BoundCalcNode>().Where(expression);
            var nodeDi = new DatasetData<BoundCalcNode>(DataAccess,datasetId,m=>m.Id.ToString(), nodeQuery);
            var locIds = nodeDi.Data.Where(m=>m.Location!=null).Select(m=>m.Location.Id).ToArray();
            locDi = DataAccess.NationalGrid.GetLocationDatasetData(datasetId,m=>m.Id.IsIn(locIds));
            foreach( var node in nodeDi.Data) {
                if ( node.Location!=null ) {
                    node.Location = locDi.GetItem(node.Location.Id);
                }
            }
            return nodeDi;        
        }

        public int GetNodeCountForLocation(int locationId, bool isSourceEdit) {
            int count;
            if ( isSourceEdit ) {
                var q = Session.QueryOver<BoundCalcNode>().Where( m=>m.Location!=null && m.Location.Id == locationId);
                count =  q.RowCount(); 
            } else {
                var nodeIds = Session.QueryOver<BoundCalcNode>().Where( m=>m.Location!=null && m.Location.Id == locationId).Select(m=>m.Id).List<int>();
                // exclude user deleted branches
                var nodeIdKeys = nodeIds.Select(m=>m.ToString()).ToArray<string>();
                var deleteCount = Session.QueryOver<UserEdit>().Where( m=>m.TableName=="Node" && m.IsRowDelete && m.Key.IsIn(nodeIdKeys)).RowCount();
                count = nodeIds.Count - deleteCount;
            }
            return count;
        }   



        #endregion

        #region Zone
        public void Add(BoundCalcZone obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundCalcZone obj)
        {
            Session.Delete(obj);
        }
        public BoundCalcZone GetZone(string code) {
            return Session.QueryOver<BoundCalcZone>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }        
        public BoundCalcZone GetZone(int id) {
            return Session.Get<BoundCalcZone>(id);
        }        
        public IList<BoundCalcZone> GetZones(Dataset dataset) {
            return Session.QueryOver<BoundCalcZone>().
            Where( m=>m.Dataset == dataset).
            OrderBy(m=>m.Id).Asc.
            List();
        }

        public bool ZoneExists(int datasetId, string code, out Dataset? dataset) {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var zone = Session.QueryOver<BoundCalcZone>().
                Where( m=>m.Code.IsInsensitiveLike(code)).
                Where( m=>m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Fetch(SelectMode.Fetch,m=>m.Dataset).
                Take(1).
                SingleOrDefault();
            if ( zone!=null) {
                dataset = zone.Dataset;
            } else {
                dataset = null;
            }
            return zone!=null;
        }

        public DatasetData<BoundCalcZone> GetZoneDatasetData(int datasetId,System.Linq.Expressions.Expression<Func<BoundCalcZone, bool>> expression) {
            var zoneQuery = Session.QueryOver<BoundCalcZone>().Where(expression);
            var zoneDi = new DatasetData<BoundCalcZone>(DataAccess,datasetId,m=>m.Id.ToString(), zoneQuery);
            return zoneDi;        
        }

        #endregion

        #region Boundary
        public void Add(BoundCalcBoundary obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundCalcBoundary obj)
        {
            Session.Delete(obj);
        }
        public BoundCalcBoundary GetBoundary(string code) {
            return Session.QueryOver<BoundCalcBoundary>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }

        public BoundCalcBoundary GetBoundary(int id) {
            return Session.Get<BoundCalcBoundary>(id);
        }

        public IList<BoundCalcBoundary> GetBoundaries(Dataset dataset) {
            return Session.QueryOver<BoundCalcBoundary>().
                Where( m=>m.Dataset == dataset).
                OrderBy(m=>m.Id).Asc.
                List();
        }
        public DatasetData<BoundCalcBoundary> GetBoundaryDatasetData(int datasetId,System.Linq.Expressions.Expression<Func<BoundCalcBoundary, bool>> expression) {
            var boundQuery = Session.QueryOver<BoundCalcBoundary>().Where(expression);
            var boundDi = new DatasetData<BoundCalcBoundary>(DataAccess,datasetId,m=>m.Id.ToString(), boundQuery);
            // add zones they belong to
            var boundDict = GetBoundaryZoneDict(boundDi.Data);
            foreach( var b in boundDi.Data) {
                if ( boundDict.ContainsKey(b) ) {
                    b.Zones = boundDict[b];
                } else {
                    b.Zones = new List<BoundCalcZone>();
                }
            }
            return boundDi;        
        }

        #endregion

        #region BoundaryZone
        public void Add(BoundCalcBoundaryZone obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundCalcBoundaryZone obj)
        {
            Session.Delete(obj);
        }

        public IList<BoundCalcBoundaryZone> GetBoundaryZones(Dataset dataset) {
            return Session.QueryOver<BoundCalcBoundaryZone>().
                Where( m=>m.Dataset == dataset).
                OrderBy(m=>m.Id).Asc.
                List();
        }

        public IList<BoundCalcBoundaryZone> GetBoundaryZones(int boundaryId) {
            return Session.QueryOver<BoundCalcBoundaryZone>().
            Where(m=>m.Boundary.Id == boundaryId).
            Fetch(SelectMode.Fetch,m=>m.Boundary).
            Fetch(SelectMode.Fetch,m=>m.Zone).
            OrderBy(m=>m.Id).Asc.List();
        }

        public Dictionary<BoundCalcBoundary,List<BoundCalcZone>> GetBoundaryZoneDict(IList<BoundCalcBoundary> boundaries) {
            var boundaryIds = boundaries.Select(m=>m.Id).ToArray();
            var bzs =  Session.QueryOver<BoundCalcBoundaryZone>().
                Fetch(SelectMode.Fetch,m=>m.Boundary).
                Fetch(SelectMode.Fetch,m=>m.Zone).
                Where(m=>m.Boundary.Id.IsIn(boundaryIds)).
                OrderBy(m=>m.Id).Asc.
                List();

            var dict = bzs.
                GroupBy(x => x.Boundary,x=>x.Zone).
                ToDictionary(x => x.Key, x => x.ToList());
            return dict;
        }

        #endregion
        
        #region Branch
        public void Add(BoundCalcBranch obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundCalcBranch obj)
        {
            Session.Delete(obj);
        }
        public BoundCalcBranch GetBranch(string code) {
            return Session.QueryOver<BoundCalcBranch>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }
        public BoundCalcBranch GetBranch(int id) {
            return Session.QueryOver<BoundCalcBranch>().
                    Where( m=>m.Id == id).
                    Fetch(SelectMode.Fetch,m=>m.Node1).
                    Fetch(SelectMode.Fetch,m=>m.Node2).
                    Take(1).SingleOrDefault();
        }

        public IList<BoundCalcBranch> GetBranches(Dataset dataset) {
            return Session.QueryOver<BoundCalcBranch>().
                Where( m=>m.Dataset == dataset).
                Fetch(SelectMode.Fetch,m=>m.Node1).
                Fetch(SelectMode.Fetch,m=>m.Node2).
                OrderBy(m=>m.Id).Asc.
                List();
        }

        public IList<BoundCalcCtrl> GetCtrlsForBranch(BoundCalcBranch b, Dataset dataset) {
            var datasetIds = DataAccess.Datasets.GetInheritedDatasetIds(dataset.Id);
            var ctrls = Session.QueryOver<BoundCalcCtrl>().
                Where( m=>m.Branch.Id == b.Id).
                Where( m=>m.Dataset.Id.IsIn(datasetIds)).
                List();
            return ctrls;
        }

        public bool BranchExists(int datasetId, string code, int node1Id, int node2Id, out Dataset? dataset) {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var branch = Session.QueryOver<BoundCalcBranch>().
                Where( m=>m.Code == code ).
                Where( m=>m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Where(m=>m.Node1.Id == node1Id).
                Where(m=>m.Node2.Id == node2Id).
                Fetch(SelectMode.Fetch,m=>m.Dataset).
                Take(1).
                SingleOrDefault();
            if ( branch!=null) {
                dataset = branch.Dataset;
            } else {
                dataset = null;
            }
            return branch!=null;
        }
        public DatasetData<BoundCalcBranch> GetBranchDatasetData(int datasetId,
            System.Linq.Expressions.Expression<Func<BoundCalcBranch, bool>> expression,
            out DatasetData<BoundCalcCtrl> ctrlDi,
            out DatasetData<BoundCalcNode> nodeDi,
            out DatasetData<GridSubstationLocation> locDi) {            
            var q = Session.QueryOver<BoundCalcBranch>().Where(expression);            
            var branchDi = new DatasetData<BoundCalcBranch>(DataAccess,datasetId,m=>m.Id.ToString(), q);
            //
            var ctrlIds = branchDi.Data.Where( m=>m.Ctrl!=null).Select(m=>m.Ctrl.Id).ToArray();
            ctrlDi = GetCtrlDatasetData(datasetId,m=>m.Id.IsIn(ctrlIds));
            //
            var node1Ids = branchDi.Data.Select(m=>m.Node1.Id).ToList<int>();
            var node2Ids = branchDi.Data.Select(m=>m.Node2.Id).ToList<int>();
            nodeDi = GetNodeDatasetData(datasetId, m=>m.Id.IsIn(node1Ids) || m.Id.IsIn(node2Ids), out locDi );
            foreach( var b in branchDi.Data) {
                b.Node1 = nodeDi.GetItem(b.Node1.Id);
                b.Node2 = nodeDi.GetItem(b.Node2.Id);
            }
            return branchDi;
        }

        public int GetBranchCountForNode(int nodeId, bool isSourceEdit) {
            int count;
            if ( isSourceEdit ) {
                var q = Session.QueryOver<BoundCalcBranch>().Where( m=>m.Node1.Id == nodeId || m.Node2.Id == nodeId);
                count =  q.RowCount(); 
            } else {
                var branchIds = Session.QueryOver<BoundCalcBranch>().Where( m=>m.Node1.Id == nodeId || m.Node2.Id == nodeId).Select(m=>m.Id).List<int>();
                // exclude user deleted branches
                var branchIdKeys = branchIds.Select(m=>m.ToString()).ToArray<string>();
                var deleteCount = Session.QueryOver<UserEdit>().Where( m=>m.TableName=="Branch" && m.IsRowDelete && m.Key.IsIn(branchIdKeys)).RowCount();
                count = branchIds.Count - deleteCount;
            }
            return count;
        }   

        #endregion

        #region Ctrl
        public void Add(BoundCalcCtrl obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundCalcCtrl obj)
        {
            Session.Delete(obj);
        }
        public BoundCalcCtrl GetCtrl(int id) {
            return Session.QueryOver<BoundCalcCtrl>().
                Where( m=>m.Id == id).
                Take(1).
                SingleOrDefault();
        }

        public IList<BoundCalcCtrl> GetCtrls(Dataset dataset) {
            return Session.QueryOver<BoundCalcCtrl>().
                Where( m=>m.Dataset == dataset).
                OrderBy(m=>m.Id).
                Asc.List();
        }
        #endregion

        public string LoadFromXlsm(IFormFile formFile) {
            using ( var da = new DataAccess() ) {
                var loader = new BoundCalcXlsmLoader();
                return loader.Load(formFile);
            }
        }
         
        public FileStreamResult SaveBranchesAsCsv(string? region=null) {
            var q = Session.QueryOver<BoundCalcBranch>();
            if ( region!=null) {
                q = q.Where(m=>m.Region == region);
            }
            var branches = q.List();

            //
            MemoryStream mms;
            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false))) {
                    //
                    sw.WriteLine($"\"Region\",\"Node1\",\"Node2\",\"Code\",\"R\",\"X\",\"OHL\",\"Cap\",\"LinkType\"");
                    foreach( var b in branches) {
                        sw.WriteLine($"\"{b.Region}\",\"{b.Node1.Code}\",\"{b.Node2.Code}\",\"{b.Code}\",\"{b.R}\",\"{b.X}\",\"{b.OHL}\",\"{b.Cap}\",\"{b.LinkType}\"");
                    }
                    sw.Flush();
                    mms = new MemoryStream(ms.ToArray());
                }
            }
            var fsr = new FileStreamResult(mms, "application/CSV");
            fsr.FileDownloadName = $"Branches ({region}).csv";
            return fsr;
        }

        public FileStreamResult SaveNodesAsCsv(string? region=null) {

            BoundCalcNode node=null;

            var sq = QueryOver.Of<BoundCalcBranch>().Where( m=>m.Node1.Id==node.Id || m.Node2.Id==node.Id);
            if ( region!=null) {
                sq = sq.Where(m=>m.Region == region);
            }
            sq = sq.Select(m=>m.Id);

            var q = Session.QueryOver<BoundCalcNode>(()=>node).WithSubquery.WhereExists(sq);
            var nodes = q.List();

            //
            MemoryStream mms;
            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false))) {
                    //
                    sw.WriteLine($"\"Code\",\"Demand\",\"Generation\",\"Zone\"");
                    foreach( var n in nodes) {
                        sw.WriteLine($"\"{n.Code}\",\"{n.Demand}\",\"{n.Generation}\",\"{n.Zone.Code}\"");
                    }
                    sw.Flush();
                    mms = new MemoryStream(ms.ToArray());
                }
            }
            var fsr = new FileStreamResult(mms, "application/CSV");
            fsr.FileDownloadName = $"Nodes ({region}).csv";
            return fsr;
        }
        public FileStreamResult SaveBoundaryZonesAsCsv() {

            var q = Session.QueryOver<BoundCalcBoundaryZone>();
            var bzs = q.List();

            //
            MemoryStream mms;
            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false))) {
                    //
                    sw.WriteLine($"\"Boundary\",\"Zone\"");
                    foreach( var bz in bzs) {
                        sw.WriteLine($"\"{bz.Boundary.Code}\",\"{bz.Zone.Code}\"");
                    }
                    sw.Flush();
                    mms = new MemoryStream(ms.ToArray());
                }
            }
            var fsr = new FileStreamResult(mms, "application/CSV");
            fsr.FileDownloadName = $"BoundaryZones.csv";
            return fsr;
        }

        public IList<BoundCalcBranch> GetVisibleBranches(int datasetId) {
            var datasetIds = this.DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            BoundCalcNode node1=null, node2=null;
            GridSubstationLocation location1=null, location2=null;
            var branches = Session.QueryOver<BoundCalcBranch>().
                Left.JoinAlias(m=>m.Node1,()=>node1).
                Left.JoinAlias(m=>m.Node2,()=>node2).
                Left.JoinAlias(()=>node1.Location,()=>location1).
                Left.JoinAlias(()=>node2.Location,()=>location2).
                Where(m=>m.Dataset.Id.IsIn(datasetIds)).
                Where( m=>m.LinkType==null || !m.LinkType.IsInsensitiveLike("Transformer")).
                Where( m=>location1.Id!=location2.Id).
                List();
            return branches;
        }

        public DatasetData<BoundCalcCtrl> GetCtrlDatasetData(int datasetId,System.Linq.Expressions.Expression<Func<BoundCalcCtrl, bool>> expression) {
            var ctrlQuery = Session.QueryOver<BoundCalcCtrl>().Where(expression);
            ctrlQuery = ctrlQuery.Fetch(SelectMode.Fetch,m=>m.Branch);
            ctrlQuery = ctrlQuery.Fetch(SelectMode.Fetch,m=>m.Branch.Node1.Location);
            ctrlQuery = ctrlQuery.Fetch(SelectMode.Fetch,m=>m.Branch.Node1.Location.GISData);
            ctrlQuery = ctrlQuery.Fetch(SelectMode.Fetch,m=>m.Branch.Node2.Location);
            ctrlQuery = ctrlQuery.Fetch(SelectMode.Fetch,m=>m.Branch.Node2.Location.GISData);
            var ctrlDi = new DatasetData<BoundCalcCtrl>(DataAccess,datasetId,m=>m.Id.ToString(), ctrlQuery);
            return ctrlDi;        
        }

        #region LoadflowResults
        public void Add( BoundCalcResult lfr) {
            Session.Save(lfr);
        }
        
        public void Delete( BoundCalcResult lfr) {
            Session.Delete(lfr);
        }

        public BoundCalcResult GetBoundCalcResult(int datasetId) {
            return Session.QueryOver<BoundCalcResult>().Where( m=>m.Dataset.Id == datasetId).Take(1).SingleOrDefault();
        }
        public int GetResultCount(int datasetId) {

            var dsIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            return Session.QueryOver<BoundCalcResult>().
                Where(m=>m.Dataset.Id.IsIn(dsIds)).
                RowCount();            
        }

        public void DeleteResults(int datasetId) {
            var dsIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var results = Session.QueryOver<BoundCalcResult>().
                Where(m=>m.Dataset.Id.IsIn(dsIds)).
                List();
            foreach( var r in results ) {
                Delete(r);
            }
        }

        #endregion
    }
}