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
                branch.Type = BoundCalcBranchType.HVDC;
            } else if ( ctrl.Type == BoundCalcCtrlType.QB) {
                branch.Type = BoundCalcBranchType.QB;
            }
        }

        public static void SetType(this BoundCalcBranch b) {
            if ( string.IsNullOrEmpty(b.LinkType) ) {
                return;
            }
            if ( b.Type!=BoundCalcBranchType.Other) {
                return;
            }
            if ( string.Compare(b.LinkType,"SSSC") == 0 ) {
                b.Type = BoundCalcBranchType.SSSC;
            } else if ( string.Compare(b.LinkType,"Series Capacitor",true) == 0 ) {
                b.Type = BoundCalcBranchType.SeriesCapacitor;
            } else if ( string.Compare(b.LinkType,"Series Reactor",true) == 0 ) {
                b.Type = BoundCalcBranchType.SeriesReactor;
            } else if ( string.Compare(b.LinkType,"Transformer",true) == 0 ) {
                b.Type = BoundCalcBranchType.Transformer;
            } else if ( string.Compare(b.LinkType,"cable",true) == 0 ) {
                b.Type = BoundCalcBranchType.Cable;
            } else if ( b.LinkType.ToLower().Contains("composite") ) {
                b.Type = BoundCalcBranchType.Composite;
            } else if ( b.LinkType.ToLower().Contains("ohl") ) {
                b.Type = BoundCalcBranchType.OHL;
            } else if ( b.LinkType.ToLower().Contains("transformer") ) {
                b.Type = BoundCalcBranchType.Transformer;
            } else if ( b.LinkType.ToLower().Contains("hvdc") ) {
                b.Type = BoundCalcBranchType.HVDC;
            } else if ( string.Compare(b.LinkType,"construct",true)==0 ) {
                b.Type = BoundCalcBranchType.Other;
            } else if ( string.Compare(b.LinkType,"Zero Length",true)==0 ) {
                b.Type = BoundCalcBranchType.Other;
            } else {
                Logger.Instance.LogInfoEvent($"Unexpected link type found [{b.LinkType}]");
            }
        }
    }
}