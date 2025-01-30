using SmartEnergyLabDataApi.Common;

namespace SmartEnergyLabDataApi.BoundCalc
{
    public class MO
    {
        private Collection<object[]> segs;
        private int[] ord;
        private double[] siz;
        private double[] cst;
        private bool sorted;
        public MO()
        {
            segs = new Collection<object[]>();
        }

        public void Add(string name, double cost, double size)
        {
            int idno;
            idno = segs.Count;
            segs.Add(new object[] { name, idno, cost, size }, name);
            sorted = false;
        }

        public bool SegExists(string name)
        {
            return segs.ContainsKey(name);
        }

        public void Remove(string name)
        {
            segs.Remove(name);
            sorted = false;
        }

        public void Update(string name, double cost, double size)
        {
            var seg = segs.Item(name);
            segs.Remove(name);
            seg[2] = cost;
            seg[3] = size;
            segs.Add(seg, name);
            sorted = false;
        }

        public void Details(string name, ref double cost, ref double size)
        {
            var seg = segs.Item(name);
            cost = (double)seg[2];
            size = (double)seg[3];
        }

        public int Count()
        {
            return segs.Count;
        }

        // Copy this merit order into another
        public void Copy(MO tomo)
        {
            string nm;
            double cst, sz;
            foreach (var seg in segs.Items)
            {
                nm = (string)seg[0];
                cst = (double)seg[2];
                sz = (double)seg[3];
                tomo.Add(nm, cst, sz);
            }
        }

        public void Order()
        {
            int c, i;
            c = segs.Count - 1;
            object[] seg;

            if (c >= 0)
            {
                ord = new int[c + 1];
                cst = new double[c + 1];
                siz = new double[c + 1];

                for (i = 0; i <= c; i++)
                {
                    seg = segs.Item(i + 1);
                    cst[i] = (double)seg[2];
                    siz[i] = (double)seg[3];
                    ord[i] = i;
                }

                LPhdr.MergeSortFlt(cst, ord, c + 1);
            }
            sorted = true;
        }

        // Schedule to meet demand
        //
        // returns order position or -1 if no segments
        // returns slack which will be negative if volume not achievable
        public int Vschedule(double volume, ref double Slack)
        {
            int i, j, c;
            double b, s = 0;
            if (!sorted)
            {
                Order();
            }
            c = segs.Count - 1;
            b = 0;
            for (i = 0; i <= c; i++)
            {
                j = ord[i];
                s = siz[j];
                if (b + s >= volume)
                {
                    break;
                }
                b = b + s;
            }

            if (i > c)
            {
                i = c;
            }
            Slack = s - (volume - b); // might be negative
            return i;
        }

        // Return the size of all segments below pos
        public double Base(int pos)
        {
            double b = 0;
            int i, j;
            if (!sorted)
            {
                Order();
            }
            for (i = 0; i < pos; i++)
            {
                j = ord[i];
                b = b + siz[j];
            }
            return b;
        }

        // Return the cost of all segments below pos
        // Returns 0 if pos unset
        public double BaseCost(int pos)
        {
            int i, j;
            double c = 0;
            if (!sorted)
            {
                Order();
            }
            for (i = 0; i < pos; i++)
            {
                j = ord[i];
                c = c + siz[j] * cst[j];
            }
            return c;
        }

        // Return segment details
        public void PosDetails(int pos, ref string name, ref double cost, ref double size)
        {
            int j,c;
            object[] seg;
            if (!sorted) {
                Order();
            }
            c = segs.Count-1;
            if ( pos>=0 && pos <=c ) {
                j=ord[pos];
                seg = segs.Item(j+1);
                name=(string) seg[0];
                cost=(double) seg[2];
                size = (double) seg[3];
            } else {
                name = "";
                cost = 0;
                size = 0;
            }
        }

        // Find segment of cost equal or greater than specified price
        // If no segment costs are greater than price then return highest
        public int Pschedule(double price) 
        {
            int i,j,c,p;
            if ( !sorted) {
                Order();
            }

            c = segs.Count;
            p = c-1;
            for(i=c-1;i>=0;i--) {
                j = ord[i];
                if ( cst[j] >= price) {
                    p = i;
                } else {
                    break;
                }
            }

            return p;
        }

        public bool IsFirst(int pos) {
            return pos==0;
        }

        public bool IsLast(int pos) {
            return (pos == segs.Count-1);
        }

        public int Up(int pos) {
            int result;
            if (!IsLast(pos)) {
                result = pos +1;
            } else {
                result = pos;
            }
            return result;
        }

        public int Down(int pos) {
            int result;
            if (!IsFirst(pos)) {
                result = pos-1;
            } else {
                result = pos;
            }
            return result;
        }

        public double CostDown(int pos) {
            if ( !sorted) {
                Order();
            }
            return cst[Down(pos)];
        }

        public double CostUp(int pos) {
            if ( !sorted) {
                Order();
            }
            return cst[Up(pos)];
        }

        public int Dispatch(string name, int pos) {
            object[] seg;
            int sop;
            if ( !sorted) {
                Order();
            }
            seg = segs.Item(name);
            sop = ord[(int)seg[1]];
            int result;
            if ( sop<pos) {
                result = 1;
            } else if ( sop==pos) {
                result = 0;
            } else {
                result = -1;
            }
            return result;
        }

        public bool Test() {
            int pos;
            double sl=0, cst=0, sz=0;
            string nm="";
            this.Add("a",10,100);
            this.Add("b",20,200);
            this.Add("c",30,300);
            this.Update("b",25,250);
            pos = this.Vschedule(200,ref sl);
            this.PosDetails(pos,ref nm,ref cst,ref sz);

            return (nm=="b") && (sl==150);
        }
    }
}