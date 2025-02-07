namespace SmartEnergyLabDataApi.BoundCalc
{

    public static class LPhdr {
        public const double lpEpsilon = 0.000001;
        public const int lpOptimum = 0;                 // Optimum found
        public const int lpUnbounded = 1;               // Unresolvable cost reducing constraint (see return2 for id)
        public const int lpInfeasible = 2;              // Unresolvable negative basis variable  (see return 2 for id)
        public const int lpZeroPivot = 3;               // Unable to Build or Update Basis matrix
        public const int lpIters = 4;                   // Exceeded maximum iterations
        public const int lpUnknown = 5;                 // Unknown error
        public const int InvIters = 8;                  // Number of iters before rebuild

        public const int CTLTE = 0;                    // Less than or equal
        public const int CTGTE = 1;                    // Greater than or equal
        public const int CTEQ = 2;                     // Equal

        public static LPModel NewLPModel() {
            return new LPModel();
        }

        public static SparseMatrix NewSparseMatrix() {
            return new SparseMatrix();
        }

        public static SolveLin NewSolveLin() {
            return new SolveLin();
        }

        public static LP NewLP() {
            return new LP();
        }

        public static MO NewMO() {
            return new MO();
        }

        // Merge sort - stable (i.e. leaves sorted lists unchanged)
        // Sort first m items, optionally starting n items in, of c() by altering ord()
        // Non recursive, uses aux memory, stable
        public static void MergeSortFlt( double[] c, int[] ord, int m, int n=0, bool zeroIndex=true) {
            int w, b, upb, sz, i;
            int[] tmp;
            int il, im, Id;
            int first, last;

            //??
            b = zeroIndex ? 0 : 1;
            upb = ord.Length-1;

            if ( m>upb + 1 -b ) {
                m = upb+1-b;
            }

            if ( n>=m ) {
                return;
            }

            sz = m-n;
            first = b+n;
            last = m-1+b;
            tmp = new int[last+1];
            w=1;
            while( w < sz ) {
                for(i=first;i<=last;i+=2*w) {
                    int left, middle, right;
                    left = i;
                    middle = i+w;
                    right = i + 2*w;

                    if ( right > last+1) {
                        right = last+1;
                    }
                    if ( middle<=right) {
                        il = left;
                        im = middle;
                        Id = left;
                        while( il < middle || im < right) {
                            if ( il < middle && im < right ) {
                                if ( c[ord[il]] <= c[ord[im]]) {
                                    tmp[Id] = ord[il];
                                    il = il + 1;
                                } else {
                                    tmp[Id] = ord[im];
                                    im = im + 1;
                                }
                            } else if ( il < middle ) {
                                tmp[Id] = ord[il];
                                il = il + 1;
                            } else {
                                tmp[Id] = ord[im];
                                im = im + 1;
                            }
                            Id = Id + 1;
                        }
                    }
                }
                for(i=first;i<=last;i++) {
                    ord[i] = tmp[i];
                }
                w = w*2;
            }
        }

        // Merge sort - stable (i.e. leaves sorted lists unchanged)
        // Sort first m items, optionally starting n items in, of c() by altering ord()
        // Non recursive, uses aux memory, stable
        public static void MergeSortInt( int[] c, int[] ord, int m, int n=0, bool zeroIndex=true) {
            int w, b, upb, sz, i;
            int[] tmp;
            int il, im, Id;
            int first, last;

            //??
            b = zeroIndex ? 0 : 1;
            upb = ord.Length-1;

            if ( m>upb + 1 -b ) {
                m = upb+1-b;
            }

            if ( n>=m ) {
                return;
            }

            sz = m-n;
            first = b+n;
            last = m-1+b;
            tmp = new int[last+1];
            w=1;
            while( w < sz ) {
                for(i=first;i<=last;i+=2*w) {
                    int left, middle, right;
                    left = i;
                    middle = i+w;
                    right = i + 2*w;

                    if ( right > last+1) {
                        right = last+1;
                    }
                    if ( middle<=right) {
                        il = left;
                        im = middle;
                        Id = left;
                        while( il < middle || im < right) {
                            if ( il < middle && im < right ) {
                                if ( c[ord[il]] <= c[ord[im]]) {
                                    tmp[Id] = ord[il];
                                    il = il + 1;
                                } else {
                                    tmp[Id] = ord[im];
                                    im = im + 1;
                                }
                            } else if ( il < middle ) {
                                tmp[Id] = ord[il];
                                il = il + 1;
                            } else {
                                tmp[Id] = ord[im];
                                im = im + 1;
                            }
                            Id = Id + 1;
                        }
                    }
                }
                for(i=first;i<=last;i++) {
                    ord[i] = tmp[i];
                }
                w = w*2;
            }
        }

        public static void TestLP() {
            var a = new LP();
            Console.WriteLine(a.Test1());
        }
    }

    public class ZeroPivotException : Exception {
        public ZeroPivotException(string msg) : base(msg) {
            
        }
    }
}