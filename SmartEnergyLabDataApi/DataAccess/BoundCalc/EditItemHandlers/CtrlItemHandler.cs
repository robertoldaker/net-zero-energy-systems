using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Driver;

namespace SmartEnergyLabDataApi.Data.BoundCalc;

public class CtrlItemHandler : BaseEditItemHandler
{
    public override string BeforeUndelete(EditItemModel m)
    {
        Ctrl c = (Ctrl) m.Item;

        // see if we already have a ctrl pointing at the branch referenced by this ctrl
        var q = m.Da.Session.QueryOver<Ctrl>();
        q = q.Fetch(SelectMode.Fetch,m=>m.Node1);
        q = q.Fetch(SelectMode.Fetch,m=>m.Node2);
        var di = new DatasetData<Ctrl>(m.Da,m.Dataset.Id,m=>m.Id.ToString(),q);
        //
        var ctrl = di.Data.Where( m=>m.Branch.Id == c.Branch.Id).FirstOrDefault();
        if ( ctrl!=null ) {
            return $"There is already a ctrl pointing at the same branch so this ctrl cannot be undeleted.\n\n(called <b>{ctrl.DisplayName}</b> in dataset <b>{ctrl.Dataset.Name}</b>)";
        } else {
            return "";
        }
    }

    public override string BeforeDelete(EditItemModel m, bool isSourceEdit) {
        return "";
    }


    public override void Check(EditItemModel m)
    {
        //
        var type = getType(m);
        int? nodeId1=null, nodeId2=null, zoneId1=null, zoneId2=null;
        double? gpc1, gpc2;
        if (type == BoundCalcCtrlType.DecInc || type == BoundCalcCtrlType.InterTrip) {
            // node 1 id
            nodeId1 = getNode1(m);
            if (nodeId1 == null) {
                m.AddError("nodeId1", "Node 1 must be set");
            }
        } else if (type == BoundCalcCtrlType.Transfer) {
            // zone 1 id
            zoneId1 = getZone1(m);
            if (zoneId1 == null) {
                m.AddError("zoneId1", "Zone 1 must be set");
            }
            gpc1 = getGpc1(m);
            if (gpc1 == null) {
                m.AddError("gpc1", "Needs to be set");
            }
        }
        if (type == BoundCalcCtrlType.DecInc) {
            // node 2 id
            nodeId2 = getNode2(m);
            if (nodeId2 == null) {
                m.AddError("nodeId2", "Node 2 must be set");
            }
            if (nodeId1 != null && nodeId1 == nodeId2) {
                m.AddError("nodeId1", "Nodes must be different");
                m.AddError("nodeId2", "Nodes must be different");
            }
        } else if (type == BoundCalcCtrlType.InterTrip || type == BoundCalcCtrlType.Transfer) {
            // zone 2 id
            zoneId2 = getZone2(m);
            if (zoneId2 == null) {
                m.AddError("zoneId2", "Zone 2 must be set");
            }
            gpc2 = getGpc2(m);
            if (gpc2 == null) {
                m.AddError("gpc2", "Needs to be set");
            }
            if (type == BoundCalcCtrlType.Transfer) {
                if (zoneId1 != null && zoneId1 == zoneId2) {
                    m.AddError("zoneId1", "Zones must be different");
                    m.AddError("zoneId2", "Zones must be different");
                }
            }
        }

        // Ctrls
        m.CheckDouble("minCtrl",null,0);
        m.CheckDouble("maxCtrl",0);
        // Cost
        m.CheckDouble("cost",0);
    }

    private int? getNode1(EditItemModel m)
    {
        var nodeId = m.CheckInt("nodeId1");
        if (nodeId == null && m.ItemId != 0) {
            nodeId = ((Ctrl)m.Item).N1?.Id;
        }
        return nodeId;
    }

    private int? getNode2(EditItemModel m)
    {
        var nodeId = m.CheckInt("nodeId2");
        if (nodeId == null && m.ItemId != 0) {
            nodeId = ((Ctrl)m.Item).N2?.Id;
        }
        return nodeId;
    }

    private int? getZone1(EditItemModel m)
    {
        var zoneId = m.CheckInt("zoneId1");
        if (zoneId == null && m.ItemId != 0) {
            zoneId = ((Ctrl)m.Item).Z1?.Id;
        }
        return zoneId;
    }

    private int? getZone2(EditItemModel m)
    {
        var zoneId = m.CheckInt("zoneId2");
        if (zoneId == null && m.ItemId != 0) {
            zoneId = ((Ctrl)m.Item).Z2?.Id;
        }
        return zoneId;
    }

    private double? getGpc1(EditItemModel m)
    {
        var gpc = m.CheckDouble("gpc1");
        if (gpc == null && m.ItemId != 0) {
            gpc = ((Ctrl)m.Item).GPC1;
        }
        return gpc;
    }

    private double? getGpc2(EditItemModel m)
    {
        var gpc = m.CheckDouble("gpc2");
        if (gpc == null && m.ItemId != 0) {
            gpc = ((Ctrl)m.Item).GPC2;
        }
        return gpc;
    }

    private BoundCalcCtrlType getType(EditItemModel m)
    {
        var t = m.CheckEnum<BoundCalcCtrlType>("type");
        if (t == null && m.ItemId != 0) {
            var ctrl = (Ctrl)m.Item;
            return ctrl.Type;
        } else if (t != null) {
            return (BoundCalcCtrlType)t;
        } else {
            throw new Exception("Could not get a valid value of type");
        }
    }

    public override IDatasetIId GetItem(EditItemModel model)
    {
        var id = model.ItemId;
        return id > 0 ? model.Da.BoundCalc.GetCtrl(id) : new Ctrl(model.Dataset, null);
    }

    public override void Save(EditItemModel m)
    {
        Ctrl c = (Ctrl) m.Item;
        // type
        var type = m.CheckInt("type");
        if ( type!=null) {
            c.Type = (BoundCalcCtrlType) type;
        }
        //
        if (c.Type == BoundCalcCtrlType.DecInc) {
            // node id 1
            var nodeId1 = m.CheckInt("nodeId1");
            if (nodeId1 != null) {
                var node = m.Da.BoundCalc.GetNode((int)nodeId1);
                c.N1 = node;
            }
            // node id 2
            var nodeId2 = m.CheckInt("nodeId2");
            if (nodeId2 != null) {
                var node = m.Da.BoundCalc.GetNode((int)nodeId2);
                c.N2 = node;
            }
            // clear these
            c.Z1 = null;
            c.Z2 = null;
        } else if (c.Type == BoundCalcCtrlType.InterTrip) {
            // node id 1
            var nodeId1 = m.CheckInt("nodeId1");
            if (nodeId1 != null) {
                var node = m.Da.BoundCalc.GetNode((int)nodeId1);
                c.N1 = node;
            }
            // zone id 2
            var zoneId2 = m.CheckInt("zoneId2");
            if (zoneId2 != null) {
                var zone = m.Da.BoundCalc.GetZone((int)zoneId2);
                c.Z2 = zone;
            }
            //
            var gpc2 = m.CheckDouble("gpc2");
            if (gpc2 != null) {
                c.GPC2 = (double)gpc2;
            }
            // clear these
            c.Z1 = null;
            c.N2 = null;
        } else if (c.Type == BoundCalcCtrlType.Transfer) {
            // zone id 1
            var zoneId1 = m.CheckInt("zoneId1");
            if (zoneId1 != null) {
                var zone = m.Da.BoundCalc.GetZone((int)zoneId1);
                c.Z1 = zone;
            }
            // GPC 1
            var gpc1 = m.CheckDouble("gpc1");
            if (gpc1 != null) {
                c.GPC1 = (double)gpc1;
            }
            // zone id 2
            var zoneId2 = m.CheckInt("zoneId2");
            if (zoneId2 != null) {
                var zone = m.Da.BoundCalc.GetZone((int)zoneId2);
                c.Z2 = zone;
            }
            // GPC 2
            var gpc2 = m.CheckDouble("gpc2");
            if (gpc2 != null) {
                c.GPC2 = (double)gpc2;
            }
            // clear these
            c.N1 = null;
            c.N2 = null;
        }
        // Min Ctrl
        var minCtrl = m.CheckDouble("minCtrl");
        if ( minCtrl!=null) {
            c.MinCtrl = (double) minCtrl;
        }
        // Max Ctrl
        var maxCtrl = m.CheckDouble("maxCtrl");
        if ( maxCtrl!=null) {
            c.MaxCtrl = (double) maxCtrl;
        }
        // Cost
        var cost = m.CheckDouble("cost");
        if ( cost!=null) {
            c.Cost = (double) cost;
        }
        //
        if ( c.Id==0) {
            m.Da.BoundCalc.Add(c);
        }
    }

    public override List<DatasetData<object>> GetDatasetData(EditItemModel m)
    {
        using( var da = new DataAccess() ) {
            var list = new List<DatasetData<object>>();
            var ctrl = (Ctrl) m.Item;
            var ctrlDi = da.BoundCalc.GetCtrlDatasetData(m.Dataset.Id,m=>m.Id == ctrl.Id);
            list.Add(ctrlDi.getBaseDatasetData());
            return list;
        }
    }
}
