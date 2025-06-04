using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using ExcelDataReader;
using HaloSoft.DataAccess;
using HaloSoft.EventLogger;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ObjectPool;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Mapping.Attributes;
using SmartEnergyLabDataApi.BoundCalc;
using SmartEnergyLabDataApi.Loadflow;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    public class BoundCalcDS : DataSet {
        public BoundCalcDS(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get {
                return (DataAccess)_dataAccess;
            }
        }

        public IList<T> GetRawData<T>(System.Linq.Expressions.Expression<Func<T, bool>> whereFcn) where T : class
        {
            //
            var q = Session.QueryOver<T>().Where(whereFcn);

            return q.List();
        }

        #region Nodes
        public void Add(Node obj)
        {
            Session.Save(obj);
        }
        public void Delete(Node obj)
        {
            Session.Delete(obj);
        }

        public Node GetNode(string code)
        {
            return Session.QueryOver<Node>().Where(m => m.Code == code).Take(1).SingleOrDefault();
        }

        public Node GetNode(int id)
        {
            return Session.Get<Node>(id);
        }

        public IList<Node> GetNodes(Dataset dataset)
        {
            return Session.QueryOver<Node>().
            Where(m => m.Dataset == dataset).
            Fetch(SelectMode.Fetch, m => m.Zone).
            OrderBy(m => m.Id).Asc.
            List();
        }

        public void SetNodeVoltagesAndLocations(int datasetId)
        {
            var nodes = Session.QueryOver<Node>().Where(m => m.Dataset.Id == datasetId).List();
            foreach (var node in nodes) {
                node.SetVoltage();
                node.SetLocation(this.DataAccess);
            }
        }

        public bool NodeExists(int datasetId, string code, out Dataset? dataset)
        {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var node = Session.QueryOver<Node>().
                Where(m => m.Code.IsInsensitiveLike(code)).
                Where(m => m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Fetch(SelectMode.Fetch, m => m.Dataset).
                Take(1).
                SingleOrDefault();
            if (node != null) {
                dataset = node.Dataset;
            } else {
                dataset = null;
            }
            return node != null;
        }

        public DatasetData<Node> GetNodeDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<Node, bool>> expression, bool updateRefs)
        {
            // get Node itself
            var nodeQuery = Session.QueryOver<Node>();
            if (expression != null) {
                nodeQuery = nodeQuery.Where(expression);
            }
            var nodeDi = new DatasetData<Node>(DataAccess, datasetId, m => m.Id.ToString(), nodeQuery);

            if (updateRefs) {
                this.updateRefs(datasetId, nodeDi.Data);
                //??this.updateRefs(datasetId, nodeDi.DeletedData);
            }

             return nodeDi;
        }

        private void updateRefs(int datasetId, IList<Node> nodes)
        {
            // update node.Location
            var locIds = nodes.Where(m => m.Location != null).Select(m => m.Location.Id).ToArray();
            var locDi = DataAccess.NationalGrid.GetLocationDatasetData(datasetId, m => m.Id.IsIn(locIds));
            foreach (var node in nodes) {
                if (node.Location != null) {
                    node.Location = locDi.GetItem(node.Location.Id);
                }
            }
            // update node.Generators
            var nodeIds = nodes.Select(m => m.Id).ToArray();
            var nodeGenDi = DataAccess.BoundCalc.GetNodeGeneratorDatasetData(datasetId, m => m.Node.Id.IsIn(nodeIds));
            var genIds = nodeGenDi.Data.Where(m => m.Generator != null).Select(m => m.Generator.Id).ToArray();
            var genDi = GetGeneratorDatasetData(datasetId, m => m.Id.IsIn(genIds));
            // update
            foreach (var node in nodes) {
                node.UpdateGenerators(nodeGenDi);
            }
        }

        public int GetNodeCountForLocation(int locationId, bool isSourceEdit)
        {
            int count;
            if (isSourceEdit) {
                var q = Session.QueryOver<Node>().Where(m => m.Location != null && m.Location.Id == locationId);
                count = q.RowCount();
            } else {
                var nodeIds = Session.QueryOver<Node>().Where(m => m.Location != null && m.Location.Id == locationId).Select(m => m.Id).List<int>();
                // exclude user deleted branches
                var nodeIdKeys = nodeIds.Select(m => m.ToString()).ToArray<string>();
                var deleteCount = Session.QueryOver<UserEdit>().Where(m => m.TableName == "Node" && m.IsRowDelete && m.Key.IsIn(nodeIdKeys)).RowCount();
                count = nodeIds.Count - deleteCount;
            }
            return count;
        }



        #endregion

        #region Zone
        public void Add(Zone obj)
        {
            Session.Save(obj);
        }
        public void Delete(Zone obj)
        {
            Session.Delete(obj);
        }
        public Zone GetZone(string code)
        {
            return Session.QueryOver<Zone>().Where(m => m.Code == code).Take(1).SingleOrDefault();
        }
        public Zone GetZone(int id)
        {
            return Session.Get<Zone>(id);
        }
        public IList<Zone> GetZones(Dataset dataset)
        {
            return Session.QueryOver<Zone>().
            Where(m => m.Dataset == dataset).
            OrderBy(m => m.Id).Asc.
            List();
        }

        public bool ZoneExists(int datasetId, string code, out Dataset? dataset)
        {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var zone = Session.QueryOver<Zone>().
                Where(m => m.Code.IsInsensitiveLike(code)).
                Where(m => m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Fetch(SelectMode.Fetch, m => m.Dataset).
                Take(1).
                SingleOrDefault();
            if (zone != null) {
                dataset = zone.Dataset;
            } else {
                dataset = null;
            }
            return zone != null;
        }

        public DatasetData<Zone> GetZoneDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<Zone, bool>> expression)
        {
            var zoneQuery = Session.QueryOver<Zone>().Where(expression);
            var zoneDi = new DatasetData<Zone>(DataAccess, datasetId, m => m.Id.ToString(), zoneQuery);
            return zoneDi;
        }

        #endregion

        #region Boundary
        public void Add(Boundary obj)
        {
            Session.Save(obj);
        }
        public void Delete(Boundary obj)
        {
            Session.Delete(obj);
        }
        public Boundary GetBoundary(string code)
        {
            return Session.QueryOver<Boundary>().Where(m => m.Code == code).Take(1).SingleOrDefault();
        }

        public Boundary GetBoundary(int id)
        {
            return Session.Get<Boundary>(id);
        }

        public IList<Boundary> GetBoundaries(Dataset dataset)
        {
            return Session.QueryOver<Boundary>().
                Where(m => m.Dataset == dataset).
                OrderBy(m => m.Id).Asc.
                List();
        }
        public DatasetData<Boundary> GetBoundaryDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<Boundary, bool>> expression)
        {
            var boundQuery = Session.QueryOver<Boundary>().Where(expression);
            var boundDi = new DatasetData<Boundary>(DataAccess, datasetId, m => m.Id.ToString(), boundQuery);
            // add zones they belong to
            var boundDict = GetBoundaryZoneDict(boundDi.Data);
            foreach (var b in boundDi.Data) {
                if (boundDict.ContainsKey(b)) {
                    b.Zones = boundDict[b];
                } else {
                    b.Zones = new List<Zone>();
                }
            }
            return boundDi;
        }

        #endregion

        #region BoundaryZone
        public void Add(BoundaryZone obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundaryZone obj)
        {
            Session.Delete(obj);
        }

        public IList<BoundaryZone> GetBoundaryZones(Dataset dataset)
        {
            return Session.QueryOver<BoundaryZone>().
                Where(m => m.Dataset == dataset).
                OrderBy(m => m.Id).Asc.
                List();
        }

        public IList<BoundaryZone> GetBoundaryZones(int boundaryId)
        {
            return Session.QueryOver<BoundaryZone>().
            Where(m => m.Boundary.Id == boundaryId).
            Fetch(SelectMode.Fetch, m => m.Boundary).
            Fetch(SelectMode.Fetch, m => m.Zone).
            OrderBy(m => m.Id).Asc.List();
        }

        public Dictionary<Boundary, List<Zone>> GetBoundaryZoneDict(IList<Boundary> boundaries)
        {
            var boundaryIds = boundaries.Select(m => m.Id).ToArray();
            var bzs = Session.QueryOver<BoundaryZone>().
                Fetch(SelectMode.Fetch, m => m.Boundary).
                Fetch(SelectMode.Fetch, m => m.Zone).
                Where(m => m.Boundary.Id.IsIn(boundaryIds)).
                OrderBy(m => m.Id).Asc.
                List();

            var dict = bzs.
                GroupBy(x => x.Boundary, x => x.Zone).
                ToDictionary(x => x.Key, x => x.ToList());
            return dict;
        }

        #endregion

        #region Branch
        public void Add(Branch obj)
        {
            Session.Save(obj);
        }
        public void Delete(Branch obj)
        {
            Session.Delete(obj);
        }

        public Branch GetBranch(int datasetId, string code)
        {
            var datasetIds = this.DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            return Session.QueryOver<Branch>().
                    Where(m => m.Code == code).
                    And(m => m.Dataset.Id.IsIn(datasetIds)).
                    Take(1).SingleOrDefault();
        }
        public Branch GetBranch(int id)
        {
            return Session.QueryOver<Branch>().
                    Where(m => m.Id == id).
                    Fetch(SelectMode.Fetch, m => m.Node1).
                    Fetch(SelectMode.Fetch, m => m.Node2).
                    Take(1).SingleOrDefault();
        }

        public IList<Branch> GetBranches(Dataset dataset)
        {
            return Session.QueryOver<Branch>().
                Where(m => m.Dataset == dataset).
                Fetch(SelectMode.Fetch, m => m.Node1).
                Fetch(SelectMode.Fetch, m => m.Node2).
                OrderBy(m => m.Id).Asc.
                List();
        }

        public IList<Ctrl> GetCtrlsForBranch(Branch b, Dataset dataset)
        {
            var datasetIds = DataAccess.Datasets.GetInheritedDatasetIds(dataset.Id);
            var ctrls = Session.QueryOver<Ctrl>().
                Where(m => m.Branch.Id == b.Id).
                Where(m => m.Dataset.Id.IsIn(datasetIds)).
                List();
            return ctrls;
        }

        public bool BranchExists(int datasetId, string code, int node1Id, int node2Id, out Dataset? dataset)
        {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var branch = Session.QueryOver<Branch>().
                Where(m => m.Code == code).
                Where(m => m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Where(m => m.Node1.Id == node1Id).
                Where(m => m.Node2.Id == node2Id).
                Fetch(SelectMode.Fetch, m => m.Dataset).
                Take(1).
                SingleOrDefault();
            if (branch != null) {
                dataset = branch.Dataset;
            } else {
                dataset = null;
            }
            return branch != null;
        }
        public (DatasetData<Branch> branchDi, DatasetData<Ctrl>? ctrlDi) GetBranchDatasetData(int datasetId,
            System.Linq.Expressions.Expression<Func<Branch, bool>> expression,
            bool updateRefs)
        {
            var q = Session.QueryOver<Branch>().Where(expression);
            var branchDi = new DatasetData<Branch>(DataAccess, datasetId, m => m.Id.ToString(), q);
            //
            DatasetData<Ctrl> ctrlDi = null;
            // Ensure branches reference up-to-date nodes
            if (updateRefs) {
                var ctrlIds = branchDi.Data.Where(m => m.Ctrl != null).Select(m => m.Ctrl.Id).ToArray();
                var delCtrlIds = branchDi.DeletedData.Where(m => m.Ctrl != null).Select(m => m.Ctrl.Id).ToArray();
                ctrlDi = GetCtrlDatasetData(datasetId, m => m.Id.IsIn(ctrlIds) || m.Id.IsIn(delCtrlIds));
                this.updateRefs(datasetId, branchDi.Data, ctrlDi);
                this.updateRefs(datasetId, branchDi.DeletedData, ctrlDi);
            }

            //
            return (branchDi, ctrlDi);
        }

        private void updateRefs(int datasetId, IList<Branch> branches, DatasetData<Ctrl> ctrlDi)
        {
            var node1Ids = branches.Select(m => m.Node1.Id).ToList<int>();
            var node2Ids = branches.Select(m => m.Node2.Id).ToList<int>();
            var nodeDi = GetNodeDatasetData(datasetId, m => m.Id.IsIn(node1Ids) || m.Id.IsIn(node2Ids),true);
            foreach (var b in branches) {
                // nodes
                b.Node1 = nodeDi.GetItemOrDeletedItem(b.Node1.Id);
                b.Node2 = nodeDi.GetItemOrDeletedItem(b.Node2.Id);
                // ctrls
                if (b.Ctrl != null) {
                    var ctrl = ctrlDi.GetItem(b.Ctrl.Id);
                    if (ctrl != null) {
                        b.Ctrl = ctrl;
                        ctrl.Branch = b;
                    }
                }
            }
        }

        public int GetBranchCountForNode(int nodeId, bool isSourceEdit)
        {
            int count;
            if (isSourceEdit) {
                var q = Session.QueryOver<Branch>().Where(m => m.Node1.Id == nodeId || m.Node2.Id == nodeId);
                count = q.RowCount();
            } else {
                var branchIds = Session.QueryOver<Branch>().Where(m => m.Node1.Id == nodeId || m.Node2.Id == nodeId).Select(m => m.Id).List<int>();
                // exclude user deleted branches
                var branchIdKeys = branchIds.Select(m => m.ToString()).ToArray<string>();
                var deleteCount = Session.QueryOver<UserEdit>().Where(m => m.TableName == "Branch" && m.IsRowDelete && m.Key.IsIn(branchIdKeys)).RowCount();
                count = branchIds.Count - deleteCount;
            }
            return count;
        }

        #endregion

        #region Ctrl
        public void Add(Ctrl obj)
        {
            Session.Save(obj);
        }
        public void Delete(Ctrl obj)
        {
            Session.Delete(obj);
        }
        public Ctrl GetCtrl(int id)
        {
            return Session.QueryOver<Ctrl>().
                Where(m => m.Id == id).
                Take(1).
                SingleOrDefault();
        }

        public IList<Ctrl> GetCtrls(Dataset dataset)
        {
            return Session.QueryOver<Ctrl>().
                Where(m => m.Dataset == dataset).
                OrderBy(m => m.Id).
                Asc.List();
        }
        #endregion

        public string LoadFromXlsm(IFormFile formFile)
        {
            using (var da = new DataAccess()) {
                var loader = new BoundCalcXlsmLoader();
                return loader.Load(formFile);
            }
        }

        public FileStreamResult SaveBranchesAsCsv(string? region = null)
        {
            var q = Session.QueryOver<Branch>();
            if (region != null) {
                q = q.Where(m => m.Region == region);
            }
            var branches = q.List();

            //
            MemoryStream mms;
            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false))) {
                    //
                    sw.WriteLine($"\"Region\",\"Node1\",\"Node2\",\"Code\",\"R\",\"X\",\"OHL\",\"Cap\",\"LinkType\"");
                    foreach (var b in branches) {
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

        public FileStreamResult SaveNodesAsCsv(string? region = null)
        {

            Node node = null;

            var sq = QueryOver.Of<Branch>().Where(m => m.Node1.Id == node.Id || m.Node2.Id == node.Id);
            if (region != null) {
                sq = sq.Where(m => m.Region == region);
            }
            sq = sq.Select(m => m.Id);

            var q = Session.QueryOver<Node>(() => node).WithSubquery.WhereExists(sq);
            var nodes = q.List();

            //
            MemoryStream mms;
            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false))) {
                    //
                    sw.WriteLine($"\"Code\",\"Demand\",\"Zone\"");
                    foreach (var n in nodes) {
                        sw.WriteLine($"\"{n.Code}\",\"{n.Demand}\",\"{n.Zone.Code}\"");
                    }
                    sw.Flush();
                    mms = new MemoryStream(ms.ToArray());
                }
            }
            var fsr = new FileStreamResult(mms, "application/CSV");
            fsr.FileDownloadName = $"Nodes ({region}).csv";
            return fsr;
        }
        public FileStreamResult SaveBoundaryZonesAsCsv()
        {

            var q = Session.QueryOver<BoundaryZone>();
            var bzs = q.List();

            //
            MemoryStream mms;
            using (var ms = new MemoryStream()) {
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false))) {
                    //
                    sw.WriteLine($"\"Boundary\",\"Zone\"");
                    foreach (var bz in bzs) {
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

        public IList<Branch> GetVisibleBranches(int datasetId)
        {
            var datasetIds = this.DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            Node node1 = null, node2 = null;
            GridSubstationLocation location1 = null, location2 = null;
            var branches = Session.QueryOver<Branch>().
                Left.JoinAlias(m => m.Node1, () => node1).
                Left.JoinAlias(m => m.Node2, () => node2).
                Left.JoinAlias(() => node1.Location, () => location1).
                Left.JoinAlias(() => node2.Location, () => location2).
                Where(m => m.Dataset.Id.IsIn(datasetIds)).
                Where(m => m.LinkType == null || !m.LinkType.IsInsensitiveLike("Transformer")).
                Where(m => location1.Id != location2.Id).
                List();
            return branches;
        }

        public DatasetData<Ctrl> GetCtrlDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<Ctrl, bool>> expression)
        {
            var ctrlQuery = Session.QueryOver<Ctrl>().Where(expression);

            var ctrlDi = new DatasetData<Ctrl>(DataAccess, datasetId, m => m.Id.ToString(), ctrlQuery);
            return ctrlDi;
        }

        #region LoadflowResults
        public void Add(BoundCalcResult lfr)
        {
            Session.Save(lfr);
        }

        public void Delete(BoundCalcResult lfr)
        {
            Session.Delete(lfr);
        }

        public BoundCalcResult GetBoundCalcResult(int datasetId)
        {
            return Session.QueryOver<BoundCalcResult>().Where(m => m.Dataset.Id == datasetId).Take(1).SingleOrDefault();
        }
        public int GetResultCount(int datasetId)
        {

            var dsIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            return Session.QueryOver<BoundCalcResult>().
                Where(m => m.Dataset.Id.IsIn(dsIds)).
                RowCount();
        }

        public void DeleteResults(int datasetId)
        {
            var dsIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var results = Session.QueryOver<BoundCalcResult>().
                Where(m => m.Dataset.Id.IsIn(dsIds)).
                List();
            foreach (var r in results) {
                Delete(r);
            }
        }

        #endregion

        #region BoundCalcAdjustments
        public void Add(BoundCalcAdjustment obj)
        {
            Session.Save(obj);
        }
        public void Delete(BoundCalcAdjustment obj)
        {
            Session.Delete(obj);
        }
        public BoundCalcAdjustment GetBoundCalcAdjustment(int id)
        {
            return Session.Get<BoundCalcAdjustment>(id);
        }
        public IList<BoundCalcAdjustment> GetBoundCalcAdjustments(int sourceYear, int targetYear)
        {
            return Session.QueryOver<BoundCalcAdjustment>().
                Where(m => m.SourceYear == sourceYear).
                And(m => m.TargetYear == targetYear).
                List();
        }
        #endregion

        #region Generators
        public void Add(Generator obj)
        {
            Session.Save(obj);
        }
        public void Delete(Generator obj)
        {
            Session.Delete(obj);
        }
        public Generator GetGenerator(int id)
        {
            return Session.Get<Generator>(id);
        }
        public IList<Generator> GetGenerators(Dataset dataset)
        {
            return Session.QueryOver<Generator>().
            Where(m => m.Dataset == dataset).
            OrderBy(m => m.Id).Asc.
            List();
        }

        public bool GeneratorExists(int itemId, int datasetId, string name, out Dataset? dataset)
        {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var gen = Session.QueryOver<Generator>().
                Where(m => m.Name.IsInsensitiveLike(name)).
                Where(m => m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Where(m => m.Id != itemId).
                Fetch(SelectMode.Fetch, m => m.Dataset).
                Take(1).
                SingleOrDefault();
            if (gen != null) {
                dataset = gen.Dataset;
            } else {
                dataset = null;
            }
            return gen != null;
        }
        public DatasetData<Generator> GetGeneratorDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<Generator, bool>> expression=null)
        {
            var genQuery = Session.QueryOver<Generator>();
            if (expression != null) {
                genQuery = genQuery.Where(expression);
            }
            var genDi = new DatasetData<Generator>(DataAccess, datasetId, m => m.Id.ToString(), genQuery);
            return genDi;
        }

        #endregion

        #region TransportModel
        public void Add(TransportModel obj)
        {
            Session.Save(obj);
        }
        public void Delete(TransportModel obj)
        {
            Session.Delete(obj);
        }
        public TransportModel GetTransportModel(int id)
        {
            return Session.Get<TransportModel>(id);
        }
        public IList<TransportModel> GetTransportModels(Dataset dataset)
        {
            return Session.QueryOver<TransportModel>().
            Where(m => m.Dataset == dataset).
            OrderBy(m => m.Id).Asc.
            List();
        }
        public bool TransportModelExists(int datasetId, string name, out Dataset? dataset)
        {
            // need to look at all datasets belonging to the user
            var derivedIds = DataAccess.Datasets.GetDerivedDatasetIds(datasetId);
            var inheritedIds = DataAccess.Datasets.GetInheritedDatasetIds(datasetId);
            var zone = Session.QueryOver<TransportModel>().
                Where(m => m.Name.IsInsensitiveLike(name)).
                Where(m => m.Dataset.Id.IsIn(derivedIds) || m.Dataset.Id.IsIn(inheritedIds)).
                Fetch(SelectMode.Fetch, m => m.Dataset).
                Take(1).
                SingleOrDefault();
            if (zone != null) {
                dataset = zone.Dataset;
            } else {
                dataset = null;
            }
            return zone != null;
        }
        public DatasetData<TransportModel> GetTransportModelDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<TransportModel, bool>> expression, bool updateRefs)
        {
            var query = Session.QueryOver<TransportModel>();
            if (expression != null) {
                query = query.Where(expression);
            }
            var di = new DatasetData<TransportModel>(DataAccess, datasetId, m => m.Id.ToString(), query);
            if (updateRefs) {
                this.updateRefs(datasetId, di.Data);
                //?? not convinced this is required and seems to cause infinte loop if called??
                //??updateRefs(datasetId, di.DeletedData);
            }
            return di;
        }
        private void updateRefs(int datasetId, IList<TransportModel> tms)
        {
            // update location references
            var tmIds = tms.Select(m => m.Id).ToArray();
            var tmeDi = GetTransportModelEntryDatasetData(datasetId, m => m.TransportModel.Id.IsIn(tmIds));
            foreach (var tm in tms) {
                tm.Entries = tmeDi.Data.Where(m => m.TransportModel.Id == tm.Id).ToList();
            }
        }

        #endregion
        #region TransportModelEntry
        public void Add(TransportModelEntry obj)
        {
            Session.Save(obj);
        }
        public void Delete(TransportModelEntry obj)
        {
            Session.Delete(obj);
        }
        public TransportModelEntry GetTransportModelEntry(int id)
        {
            return Session.Get<TransportModelEntry>(id);
        }
        public DatasetData<TransportModelEntry> GetTransportModelEntryDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<TransportModelEntry, bool>> expression = null)
        {
            var query = Session.QueryOver<TransportModelEntry>();
            if (expression != null) {
                query = query.Where(expression);
            }
            var di = new DatasetData<TransportModelEntry>(DataAccess, datasetId, m => m.Id.ToString(), query);
            return di;
        }

        #endregion

        #region NodeGenerator
        public void Add(NodeGenerator ng)
        {
            Session.Save(ng);
        }
        public void Delete(NodeGenerator ng)
        {
            Session.Delete(ng);
        }
        public IList<NodeGenerator> GetNodeGenerators(Dataset dataset)
        {
            return Session.QueryOver<NodeGenerator>().
            Where(m => m.Dataset == dataset).
            OrderBy(m => m.Id).Asc.
            List();
        }
        public IList<NodeGenerator> GetNodeGenerators(int nodeId)
        {
            return Session.QueryOver<NodeGenerator>().
            Where(m => m.Node.Id == nodeId).
            List();
        }
        public DatasetData<NodeGenerator> GetNodeGeneratorDatasetData(int datasetId, System.Linq.Expressions.Expression<Func<NodeGenerator, bool>> expression=null)
        {
            // get Node itself
            var ngQuery = Session.QueryOver<NodeGenerator>();
            if (expression != null) {
                ngQuery = ngQuery.Where(expression);
            }
            var ngDi = new DatasetData<NodeGenerator>(DataAccess, datasetId, m => m.Id.ToString(), ngQuery);

            return ngDi;
        }

        #endregion
    }
}
