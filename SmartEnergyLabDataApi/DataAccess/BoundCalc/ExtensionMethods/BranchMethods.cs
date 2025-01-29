using HaloSoft.EventLogger;
using NHibernate.Mapping.Attributes;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    public static class BranchMethods {
        public static string GetKey(this BoundCalcBranch b) {
            return b.LineName;
        }
        public static void SetCode(this BoundCalcBranch b, string key) {
            var cpnts = key.Split(':');
            b.Code = cpnts[1];
        }

        public static void SetCtrl(this BoundCalcBranch branch, BoundCalcCtrl ctrl) {
            branch.Ctrl = ctrl;
            //
            if ( ctrl.Type == BoundCalcCtrlType.HVDC) {
                branch.Type = BranchType.HVDC;
            } else if ( ctrl.Type == BoundCalcCtrlType.QB) {
                branch.Type = BranchType.QB;
            }
        }

        public static void SetType(this BoundCalcBranch b) {
            if ( string.IsNullOrEmpty(b.LinkType) ) {
                return;
            }
            if ( b.Type!=BranchType.Other) {
                return;
            }
            if ( string.Compare(b.LinkType,"SSSC") == 0 ) {
                b.Type = BranchType.SSSC;
            } else if ( string.Compare(b.LinkType,"Series Capacitor",true) == 0 ) {
                b.Type = BranchType.SeriesCapacitor;
            } else if ( string.Compare(b.LinkType,"Series Reactor",true) == 0 ) {
                b.Type = BranchType.SeriesReactor;
            } else if ( string.Compare(b.LinkType,"Transformer",true) == 0 ) {
                b.Type = BranchType.Transformer;
            } else if ( string.Compare(b.LinkType,"cable",true) == 0 ) {
                b.Type = BranchType.Cable;
            } else if ( b.LinkType.ToLower().Contains("composite") ) {
                b.Type = BranchType.Composite;
            } else if ( b.LinkType.ToLower().Contains("ohl") ) {
                b.Type = BranchType.OHL;
            } else if ( b.LinkType.ToLower().Contains("transformer") ) {
                b.Type = BranchType.Transformer;
            } else if ( b.LinkType.ToLower().Contains("hvdc") ) {
                b.Type = BranchType.HVDC;
            } else if ( string.Compare(b.LinkType,"construct",true)==0 ) {
                b.Type = BranchType.Other;
            } else if ( string.Compare(b.LinkType,"Zero Length",true)==0 ) {
                b.Type = BranchType.Other;
            } else {
                Logger.Instance.LogInfoEvent($"Unexpected link type found [{b.LinkType}]");
            }
        }
    }
}