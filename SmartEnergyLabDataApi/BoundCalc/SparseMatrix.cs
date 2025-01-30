namespace SmartEnergyLabDataApi.BoundCalc
{
    public class SparseMatrix {
        private double[] val;   // storage for element values
        public int[] ind;      // storage for element indices
        public int[] lst;      // storage for row pointers
        private int maxr;       // number of rows-1
        private int maxs;       // store upb
        private int maxc;       // number of columns-1
        private int last;       // last used store
        private double myepr;   // elements per row storage estimate

        public SparseMatrix(int Rupb, int cupb, double epr = 2.5) {
            Init(Rupb, cupb, epr);
        }

        public void Init(int Rupb, int cupb, double epr = 2.5) {
            // Initialise an empty matrix
            // rupb row upper bound, epr elements per row assumption for storage sizing
            myepr = epr;
            maxr = Rupb;
            maxc = cupb;
            maxs = (int)((maxr + 1) * epr + 0.5);
            lst = new int[maxr+2];
            val = new double[maxs+1];
            ind = new int[maxs+1];
        }

        public SparseMatrix() {
            
        }

        public int Rupb {
            get {
                return maxr;
            }
        }

        public int Cupb {
            get {
                return maxc;
            }
        }
        public int size {
            get {
                return lst[maxr+1] - lst[0];
            }
        }

        public int GetRsize(int r) {
            return lst[r+1] - lst[r];
        }

        // Become a copy of an existing matrix or part of (no spare storage)
        public void Copy( SparseMatrix sm, int rs=0, int re=-1) {
            int r,i=0;
            int k,sz=0;
            if ( re<0 ) {
                re = sm.Rupb;
            }

            for(r=rs; r<=re;r++) {
                sz+=sm.GetRsize(r);
            }
            maxs = sz -1;
            maxr = re-rs;
            maxc = sm.Cupb;
            myepr = ((double) (maxs + 1)) / ((double) (maxr + 1));
            lst = new int[maxr+2];
            val = new double[maxs+1];
            ind = new int[maxs+1];
            //
            for(r=rs; r<=re;r++) {
                lst[r - rs] = i;
                k = sm.FirstKey(r);
                while ( k!=-1) {
                    sm.Contents(k, ref ind[i], ref val[i]);
                    i++;
                    k = sm.NextKey(k, r);
                }
            }
            //
            lst[maxr+1] = i;
        }

        // Become a copy of an existing matrix with rows selected by map (no spare storage)
        public void CopyMap(SparseMatrix sm, int[] map, int rmax = -1) {
            int r, i=0, rm;
            int k, sz=0;
            if ( rmax < 0) {
                rmax = map.Length-1;
            }
            for (r=0;r<=rmax;r++) {
                sz+=sm.GetRsize(map[r]);
            }
            maxs = sz -1;
            maxr = rmax;
            maxc = sm.Cupb;
            myepr = ((double) (maxs + 1)) / ((double)(maxr + 1));
            //
            lst = new int[maxr+2];
            val = new double[maxs+1];
            ind = new int[maxs+1];
            for(r=0; r<=rmax;r++) {
                lst[r] = i;
                rm = map[r];

                k = sm.FirstKey(rm);
                while ( k!=-1) {
                    sm.Contents(k, ref ind[i], ref val[i]);
                    i++;
                    k = sm.NextKey(k, rm);
                }
            } 
            lst[maxr+1] = i;
        }

        // Become a copy of an existing matrix row
        public void CopyRow(SparseMatrix sm, int smr ) 
        {
            int i=0;
            int k;
            int indi=0;
            double vali=0;
            maxs = sm.GetRsize(smr) -1;
            maxr = 0;
            maxc = sm.Cupb;
            myepr = maxs+1;
            //
            lst = new int[maxr+2];
            val = new double[maxs+1];
            ind = new int[maxs+1];
            //
            lst[0] = i;
            k = sm.FirstKey(smr);
            while ( k!=-1) {

                sm.Contents(k, ref indi, ref vali);
                ind[i] = indi;
                val[i] = vali;

                i++;
                k = sm.NextKey(k, smr);
            }
            lst[1] = i;
        }

        public void Transpose( SparseMatrix sm) {
            int r, c=0;
            int k;
            double v=0;

            maxc = sm.size -1;
            maxr = sm.Cupb;
            maxc = sm.Rupb;
            myepr = ((double) (maxs + 1)) / ((double) (maxr + 1));

            lst = new int[maxr+2];
            val = new double[maxs+1];
            ind = new int[maxs+1];

            for( r=0;r<=sm.Rupb;r++) {
                k = sm.FirstKey(r);
                while ( k!=-1) {
                    sm.Contents(k, ref c, ref v);
                    Insert(c,r,v);
                    k = sm.NextKey(k, r);
                }
            }
        }

        // Linear search for element on row r with column index c
        // If found returns true with key of element
        // If not found returns false with key of next element
        public bool LFindKey( int r, int c, ref int key) {
            int i, ii;
            bool result;
            for ( i=lst[r];i<=lst[r+1]-1;i++) {
                ii = ind[i];
                if ( ii==c ) {
                    result = true;
                    key = i;
                    return result;
                } else if ( ii > c ) {
                    result = false;
                    key = i;
                    return result;
                }
            }
            result = false;
            key = i;
            return result;
        }

        public bool FFindKey(int r, int c, ref int key)  {
            int i, ii;
            int fi, li;
            int fii, lii;
            bool result;

            fi = lst[r];
            li = lst[r+1] -1;

            if ( fi > li) { //  List empty
                result = false;
                key = fi;
                return result;
            }

            fii = ind[fi];

            if ( fii == c) {     // First in list match
                result = true;
                key = fi;
                return result;
            } else if (fii > c) { // First in list too high
                result = false;
                key = fi;
                return result;
            } else if ( fi == li) { // First also last in list and too low
                result = false;
                key = li + 1;
                return result;
            }

            lii = ind[li];

            if ( lii == c ) {   //  Last in list match
                result =true;
                key = li;
                return result;
            } else if ( lii< c )  { // Last in list too low
                result = false;
                key = li +1;
                return result;
            }
            while( li - fi > 1 ) {
                i = (li + fi) /2; // Split list in middle
                ii = ind[i];

                if ( ii == c) {
                    result = true;
                    key = i;
                    return result;
                } else if ( ii < c) {
                    fi = i;
                    fii = ii;
                } else {
                    li = i;
                    lii = i;
                }
            }
            result = false;
            key = li;
            return result;
        }

        // Get first key of row r
        // Returns -1 if no keys
        // Warning: key may become invalid if new elements inserted into this object
        public int FirstKey( int r) {
            int result;
            if ( lst[r] == lst[r+1]) {
                result = -1;
            } else {
                result = lst[r];
            }
            return result;
        }

        // Get next key after k on row r
        // Warning: key may become invalid if new elements inserted into this object
        public int NextKey( int k, int r) {
            k++;
            int result;
            if ( k == lst[r+1]) {
                result = -1;
            } else {
                result = k;
            }
            return result;
        }

        // Get contents at key k
        // Warning: key may become invalid if new elements inserted into this object
        public void Contents( int k, ref int c, ref double v) {
            c = ind[k];
            v = val[k];
        }

        // Lookup element on row r column c
        public double Lookup( int r, int c) {
            int k=0;
            double result;
            if ( FFindKey(r, c, ref k) ) {
                result = val[k];
            } else {
                result = 0;
            }
            return result;
        }

        // Shuffle storage from i up n cells for use on row r, resize store if necessary
        //
        private void Shuffle( int i, int n, int r) {
            int nmax;
            int j;
            // i points to destination cell
            // lst(maxr + 1) points to next free store
            nmax = lst[maxr+1] -1 + n;
            if ( nmax > maxs) {
                // extend store
                if ( nmax > maxs + maxs /2 ) {
                    maxs = nmax;
                } else {
                    maxs = maxs + maxs /2;
                }
                Array.Resize(ref ind, maxs+1);
                Array.Resize(ref val, maxs+1);
            }

            if ( n>=0 ) {
                for( j=nmax; j>=(i+n);j--) {
                    ind[j] = ind[j-n];
                    val[j] = val[j-n];
                }
            } else {
                for ( j=i-n; j<=lst[maxr+1]-1;j++) {
                    ind[j+n] = ind[j];
                    val[j+n] = val[j];
                }
            }
            for( j=r+1;j<=maxr+1;j++) {
                lst[j] = lst[j] + n;
            }
        }

        // Best called in ascending order of c then r
        public void Insert( int r, int c, double v) {
            int i=0;
            if ( FFindKey(r, c, ref i) ) {
                val[i] = v;
                return;
            }
            // i points to destination cell
            Shuffle(i, 1, r);

            ind[i] = c;
            val[i] = v;
        }

        // Removes element (and sets to zero)
        public void Zero(int r, int c) {
            int i=0;
            if (!FFindKey(r, c, ref i)) {
                return;
            }
            Shuffle(i,-1, r);
        }

        // Removes all elements in row
        public void ZeroRow(int r) {
            int k, n;
            k = FirstKey(r);
            n = GetRsize(r);

            if ( (k>=0) && (n>0) ) {
                Shuffle( k, -n, r);
            }
        }

        public int dopurge( double eps = 0.00000001) {
            int r, c, i;
            int s, e, t=0;
            for( r=maxr; r>=0; r--) {
                s = -1;
                e = 0;
                for (i = lst[r+1]-1; i>=lst[r]; i--) {
                    if ( Math.Abs(val[i]) < eps) {
                        s = i;
                        e-=1;
                        t+=1;
                    } else {
                        if ( s >= 0) {
                            Shuffle(s,e,r);
                            s = -1;
                            e = 0;
                        }
                    }
                }
                if ( s>=0 ) {
                    Shuffle(s, e, r);
                }
            }
            return t;
        }

        public void Addin( int r, int c, double v) {
            int i=0;
            if ( FFindKey( r, c, ref i)) {
                val[i] = val[i] + v; // Add to existing cell
                return;
            }
            // i points to destination cell
            Shuffle( i, 1, r);
            ind[i] = c;
            val[i] = v; // Populate new cell
        }

        public void ReplaceRow( int ra, SparseMatrix sm, int smr) {
            int k, bi=0, aa, la, LB;
            double v=0;
            la = lst[ra+1] - lst[ra];
            LB = sm.GetRsize(smr);

            if ( LB > la) {
                aa = lst[ra+1];             // point at cell beyond end of row
                Shuffle(aa, LB - la, ra);   // make extra room
            } else if ( LB < la ) {
                aa = lst[ra] + LB - 1;
                Shuffle(aa, LB - la, ra);
            }

            aa = lst[ra];
            k = sm.FirstKey(smr);

            while ( k!=-1) {
                sm.Contents(k, ref bi, ref v);
                val[aa] = v;
                ind[aa] = bi;
                aa++;
                k = sm.NextKey(k, smr);
            }
        }

        // Replace a column with a row from another sparse matrix (use to update part of a transpose)
        public void ReplaceCol(int c, SparseMatrix sm, int smr)  {
            int r, i=0, k;
            double v=0;

            k=sm.FirstKey(smr);
            
            for( r=0;r<=maxr;r++) {
                if ( k!= -1 ) {
                    sm.Contents(k,ref i,ref v);
                    if ( r==i ) {
                        Insert(r,c,v);
                        k = sm.NextKey(k,smr);
                    } else {
                        Zero(r,c);
                    }
                } else {
                    Zero(r,c);
                }
            }
        }

        // Copy sparse row r to vector
        public void RowToVec(int r, ref double[] vec) {
            int k, c;
            double v;
            vec = new double[maxc+1];
            for (k=lst[r]; k<=lst[r+1]-1;k++ ) {
                vec[ind[k]] = val[k];
            }
        }

        public void SetCell(int r, int c, double v) {
            Insert(r,c,v);
        }

        public double GetCell(int r, int c) {
            return Lookup(r,c);
        }

        public double RowDotVec( int r, double[] vec) {
            int i;
            double res=0;

            for( i=lst[r]; i<=lst[r+1] -1 ; i++) {
                res+=val[i]*vec[ind[i]];
            }

            return res;
        }

        // R = A.V  R can be same as V
        public void MultVec( double[] vec, ref double[] rvec) {
            int r;
            double[] tvec;
            tvec = new double[maxr+1];
            for ( r=0; r<=maxr; r++) {
                tvec[r] = RowDotVec(r, vec);
            }

            rvec = tvec;
        }

        // Add scaled row b to row a:   rowa = rowa + sf.rowb
        // Uses Find to locate row a position
        // 
        // Public Sub AddRowI(ra As Long, rb As Long, sf As Double)
        //    Dim b As Long, bb As Long
        // 
        //    For b = 0 To lst(rb + 1) - 1 - lst(rb)  // modifications of rowa may change lst(rb)
        //        bb = lst(rb) + b
        //        Addin ra, ind(bb), sf * val(bb)
        //    Next b
        // End Sub

        // Add scaled row b to row a:   rowa = rowa + sf.rowb
        // Avoids Find to locate rowa position
        // cb permits elements in rowb to be skipped where ind<cb (i.e. upper diagonal only)
        public void AddRow( int ra, int rb, double sf, int cb = 0) {
            int i, aa, ai;
            int b=0, bb, bi;
            double v;

            aa = lst[ra];
            // note: mods of rowa may change lst[rb]

            if ( cb > 0) {
                bb = lst[rb];
                while( true)  {
                    if ( bb >= lst[rb+1]) {
                        return;
                    }
                    if ( ind[bb]>=cb ) {
                        break;
                    }
                    bb++;
                }
                b = bb - lst[rb];
            }

            while( true) {
                if ( b > lst[rb+1] - 1 - lst[rb]) {
                    return;
                }

                if ( aa> lst[ra+1] - 1) {
                    break;
                }
                bb = lst[rb] + b;
                ai = ind[aa];
                bi = ind[bb];

                if ( ai < bi ) {
                    aa++;
                } else if ( ai == bi) {
                    val[aa] = val[aa] + sf *val[bb];
                    aa++;
                    b++;
                } else {
                    v = sf*val[bb];
                    Shuffle(aa, 1, ra);
                    val[aa] = v;
                    ind[aa] = bi;
                    aa++;
                    b++;
                }
            }
            Shuffle( aa, lst[rb+1]-lst[rb] -b, ra); // make space for remainder of rowb

            bb = lst[rb] + b;
        
            for ( i=0; i<=lst[rb+1]-1-bb;i++) {
                val[aa+i] = sf*val[bb+i];
                ind[aa+i] = ind[bb+i];
            }
        }
        //  Add scaled row of another sparse matrix: rowa = rowa + sf.sm.row_smr
        //  Uses Find to locate rowa position
        //  Warning: errors likely if sm is the same as myself
        // 
        // Public Sub AddMatRowI(ra As Long, sm As SparseMatrix, smr As Long, sf As Double)
        //     Dim k As Long, c As Long, v As Double
        // 
        //     k = sm.FirstKey(smr)
        //     While k <> -1
        //         sm.Contents k, c, v
        //         Addin ra, c, sf * v
        //         k = sm.NextKey(k, smr)
        //     Wend
        // End Sub
        // 
        //  Add scaled row of another sparse matrix: rowa = rowa + sf.smrowr
        //  Avoids Find to locate rowa position
        //  Warning: errors likely if sm is the same as myself
        public void AddMatRow( int ra, SparseMatrix sm, int smr, double sf) {
            int k, bi=0, n=0, kc;
            int aa, ai;
            double v=0;

            aa = lst[ra];
            k = sm.FirstKey(smr);

            while(true) {
                if ( k==-1) {
                    return;
                }
                if ( aa > lst[ra+1] -1 ) {
                    break;
                }

                sm.Contents(k,ref bi,ref v);
                ai = ind[aa];

                if ( ai< bi) {
                    aa++;
                } else if ( ai == bi ) {
                    val[aa] = val[aa] + sf*v;
                    aa++;
                    k = sm.NextKey(k,smr);
                } else {
                    Shuffle(aa, 1, ra);
                    val[aa] = sf*v;
                    ind[aa] = bi;
                    aa++;
                    k = sm.NextKey(k, smr);
                }
            }
            // count elements remaining in smrow
            kc = k;
            do {
                n++;
                kc = sm.NextKey(kc, smr);
            } while( kc!=-1);

            Shuffle(aa, n, ra); // Make space

            do {
                sm.Contents(k,ref bi, ref v);
                val[aa] = sf*v;
                ind[aa] = bi;
                aa++;
                k = sm.NextKey(k,smr);
            } while( k!=-1);

        }

        //
        // Calculate dot product of row and another sparse row
        //
        public double RowDotRow(int r, SparseMatrix sm, int smr) {
            int i, e, k, c=0;
            double res=0, v=0;

            i=lst[r];
            e = lst[r+1] - 1;
            k=sm.FirstKey(smr);

            while(true) {
                if ( i>e) {
                    break;
                }
                if( k==-1) {
                    break;
                }
                sm.Contents(k,ref c,ref v);

                if ( ind[i] == c) {
                    res=res+val[i]*v;
                    i++;
                    k = sm.NextKey(k,smr);                    
                } else if ( ind[i] < c) {
                    i++;
                } else {
                    k = sm.NextKey(k,smr);
                }
            }

            return res;
        }
        //
        // Calculate dot product of row and a column of another sparsematrix
        //
        public double RowDotCol(int r, SparseMatrix sm, int smc) {
            int i;
            double res=0;
            for(i=lst[r];i<=lst[r+1]-1;i++) {
                res+=val[i] * sm.GetCell(ind[i],smc);
            }
            return res;
        }

        public double Rmaxel(int r) {
            int i;
            double res=0;
            for( i=lst[r];i<=lst[r+1]-1;i++) {
                if ( Math.Abs(val[i]) > Math.Abs(res)) {
                    res = val[i];
                }
            }
            return res;
        }

        public static bool Test() {
            var sm = new SparseMatrix(9,9);
            sm.SetCell(0,0,101);
            sm.SetCell(0,5,105);
            sm.SetCell(0,9,110);
            sm.SetCell(9,9, 199);
            sm.Zero(0,0);
            sm.AddRow(5, 0, 2);
            var result = (sm.size == 5) && ( sm.GetCell(5,5) == 210);
            return result;            
        }

        public void PrintState() {
            int i;
            PrintFile.PrintVars("maxr", maxr, "maxs", maxs, "maxc", maxc, "last", last, "myepr", myepr);
            if ( maxr>0 ) {
                for ( i=0;i<=maxr+1;i++) {
                    PrintFile.PrintVars("i", i, "lst", lst[i]);
                }
            }
            if ( maxs>0 ) {
                for (i=0;i<=maxs;i++) {
                    PrintFile.PrintVars("i", i, "val", val[i], "ind", ind[i]);
                }
            }
        }
    }
}