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
using SmartEnergyLabDataApi.Loadflow;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data
{
    public class Loadflow : DataSet
    {
        public Loadflow(DataAccess da) : base(da)
        {

        }

        public DataAccess DataAccess
        {
            get
            {
                return (DataAccess) _dataAccess;
            }
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

        public Node GetNode(string code) {
            return Session.QueryOver<Node>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }
        public IList<Node> GetNodes() {
            return Session.QueryOver<Node>().
            Fetch(SelectMode.Fetch, m=>m.Zone).
            OrderBy(m=>m.Id).Asc.
            List();
        }

        public IList<Node> GetNodesWithLocations() {
            var nodes = Session.QueryOver<Node>().Fetch(SelectMode.Fetch, m=>m.Zone).Where(m=>m.Location!=null).List();
            return nodes;
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
        public Zone GetZone(string code) {
            return Session.QueryOver<Zone>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }        
        public IList<Zone> GetZones() {
            return Session.QueryOver<Zone>().OrderBy(m=>m.Id).Asc.List();
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
        public Boundary GetBoundary(string code) {
            return Session.QueryOver<Boundary>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }
        public IList<Boundary> GetBoundaries() {
            return Session.QueryOver<Boundary>().OrderBy(m=>m.Id).Asc.List();
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

        public IList<BoundaryZone> GetBoundaryZones() {
            return Session.QueryOver<BoundaryZone>().OrderBy(m=>m.Id).Asc.List();
        }

        public IList<BoundaryZone> GetBoundaryZones(int boundaryId) {
            return Session.QueryOver<BoundaryZone>().
            Where(m=>m.Boundary.Id == boundaryId).
            OrderBy(m=>m.Id).Asc.List();
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
        public Branch GetBranch(string code) {
            return Session.QueryOver<Branch>().Where( m=>m.Code == code).Take(1).SingleOrDefault();
        }
        public IList<Branch> GetBranches() {
            return Session.QueryOver<Branch>().
                Fetch(SelectMode.Fetch,m=>m.Node1).
                Fetch(SelectMode.Fetch,m=>m.Node2).
                OrderBy(m=>m.Id).Asc.
                List();
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
        public IList<Ctrl> GetCtrls() {
            return Session.QueryOver<Ctrl>().OrderBy(m=>m.Id).Asc.List();
        }
        #endregion

        public string LoadFromXlsm(IFormFile formFile) {
            var msg = "";
            using ( var da = new DataAccess() ) {
                var loader = new LoadflowXlsmLoader(da);
                msg+=loader.LoadNodes(formFile) + "\n";
                msg+=loader.LoadBranches(formFile) + "\n";
                msg+=loader.LoadCtrls(formFile) + "\n";
                msg+=loader.LoadBoundaries(formFile) + "\n";
                da.CommitChanges();
            }
            return msg;
        }
         
        public FileStreamResult SaveBranchesAsCsv(string? region=null) {
            var q = Session.QueryOver<Branch>();
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

            Node node=null;

            var sq = QueryOver.Of<Branch>().Where( m=>m.Node1.Id==node.Id || m.Node2.Id==node.Id);
            if ( region!=null) {
                sq = sq.Where(m=>m.Region == region);
            }
            sq = sq.Select(m=>m.Id);

            var q = Session.QueryOver<Node>(()=>node).WithSubquery.WhereExists(sq);
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

            var q = Session.QueryOver<BoundaryZone>();
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

        public IList<Branch> GetVisibleBranches() {
            Node node1=null, node2=null;
            GridSubstationLocation location1=null, location2=null;
            var branches = Session.QueryOver<Branch>().
                Left.JoinAlias(m=>m.Node1,()=>node1).
                Left.JoinAlias(m=>m.Node2,()=>node2).
                Left.JoinAlias(()=>node1.Location,()=>location1).
                Left.JoinAlias(()=>node2.Location,()=>location2).
                Where( m=>!m.LinkType.IsInsensitiveLike("Transformer")).
                Where( m=>location1.Id!=location2.Id).List();
            return branches;
        }
    }
}