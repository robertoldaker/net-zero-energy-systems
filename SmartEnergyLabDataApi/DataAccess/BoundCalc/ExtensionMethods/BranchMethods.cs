using HaloSoft.EventLogger;
using NHibernate.Mapping.Attributes;
using Npgsql.Replication;
using SmartEnergyLabDataApi.Models;

namespace SmartEnergyLabDataApi.Data.BoundCalc
{
    public static class BranchMethods {
        public static string GetKey(this Branch b)
        {
            return b.LineName;
        }
        public static void SetCode(this Branch b, string key)
        {
            var cpnts = key.Split(':');
            b.Code = cpnts[1];
        }

        public static void SetCtrl(this Branch branch, Ctrl ctrl)
        {
            branch.Ctrl = ctrl;
            //
            if (ctrl.Type == BoundCalcCtrlType.HVDC) {
                branch.Type = BoundCalcBranchType.HVDC;
                branch.X = 0; // needs to be 0 to work correctly in BoundCalc model
            } else if (ctrl.Type == BoundCalcCtrlType.QB) {
                branch.Type = BoundCalcBranchType.QB;
            }
        }

        public static void SetType(this Branch b)
        {
            if (string.IsNullOrEmpty(b.LinkType)) {
                return;
            }
            if (b.Type != BoundCalcBranchType.Other) {
                return;
            }
            if (string.Compare(b.LinkType, "SSSC") == 0) {
                b.Type = BoundCalcBranchType.SSSC;
            } else if (string.Compare(b.LinkType, "Series Capacitor", true) == 0) {
                b.Type = BoundCalcBranchType.SeriesCapacitor;
            } else if (string.Compare(b.LinkType, "Series Reactor", true) == 0) {
                b.Type = BoundCalcBranchType.SeriesReactor;
            } else if (string.Compare(b.LinkType, "Transformer", true) == 0) {
                b.Type = BoundCalcBranchType.Transformer;
            } else if (string.Compare(b.LinkType, "cable", true) == 0) {
                b.Type = BoundCalcBranchType.Cable;
            } else if (b.LinkType.ToLower().Contains("composite")) {
                b.Type = BoundCalcBranchType.Composite;
            } else if (b.LinkType.ToLower().Contains("ohl")) {
                b.Type = BoundCalcBranchType.OHL;
            } else if (b.LinkType.ToLower().Contains("transformer")) {
                b.Type = BoundCalcBranchType.Transformer;
            } else if (b.LinkType.ToLower().Contains("hvdc")) {
                b.Type = BoundCalcBranchType.HVDC;
            } else if (string.Compare(b.LinkType, "construct", true) == 0) {
                b.Type = BoundCalcBranchType.Other;
            } else if (string.Compare(b.LinkType, "Zero Length", true) == 0) {
                b.Type = BoundCalcBranchType.Other;
            } else {
                Logger.Instance.LogInfoEvent($"Unexpected link type found [{b.LinkType}]");
            }
        }

        public static void UpdateLengths(this Branch b)
        {
            if (b.Node1.Location != null && b.Node2.Location != null) {
                var gis1 = b.Node1.Location.GISData;
                var gis2 = b.Node2.Location.GISData;
                var dist = GISUtilities.Distance(gis1.Latitude, gis1.Longitude, gis2.Latitude, gis2.Longitude);
                if (b.Type == BoundCalcBranchType.OHL) {
                    b.OHL = dist;
                    b.CableLength = 0;
                } else if (b.Type == BoundCalcBranchType.Cable) {
                    b.OHL = 0;
                    b.CableLength = dist;
                } else if (b.Type == BoundCalcBranchType.Composite) {
                    if (b.OHL !=0 && b.CableLength != 0) {
                        var rat = b.OHL / b.CableLength;
                        b.CableLength = dist * rat/2;
                        b.OHL = dist * (1 - rat/2);
                    } else if (b.OHL !=0 ) {
                        b.OHL = dist;
                    } else if (b.CableLength != 0) {
                        b.CableLength = dist;
                    } else {
                        b.OHL = dist / 2;
                        b.CableLength =  dist / 2;
                    }
                }

            }
        }
    }
}
