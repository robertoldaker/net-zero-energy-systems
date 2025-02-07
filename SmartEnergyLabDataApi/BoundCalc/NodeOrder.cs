namespace SmartEnergyLabDataApi.BoundCalc
{
    public class NodeOrder {

        private const int BPL = 31;  // bits per long
        public int nz;               // Original non-zeros (for upper diagonal only)
        public int fz;               // Final nz count
        public int nn;               // Upper bound of connected nodes
        private int[] nord;          // Order position (0..n) of node index i (i=0..n)
        private int[] npos;          // Position of ith node
        private int[,] bitmap;        // Bit per nz element

        public NodeOrder(Nodes nodes, Branches branches) {

            nn = nodes.Count - 1;   // assume all nodes ac connected
            var nw = nn / BPL;
            bitmap = new int[nn+1,nw+1];
            var rowbits = new int[nn+1];
            nord = new int[nn+1];
            npos = new int[nn+1];

            // Mark non-zero elements in bitmap
            int n1, n2;
            foreach( var branch in branches.Objs ) {
                if ( branch.Obj.X != 0) {
                    n1 = branch.Node1.Index - 1;
                    n2 = branch.Node2.Index - 1;

                    bitmap[n1, MWord(n1)] = bitmap[n1, MWord(n1)] | Mask(n1);
                    bitmap[n1, MWord(n2)] = bitmap[n1, MWord(n2)] | Mask(n2);
                    bitmap[n2, MWord(n2)] = bitmap[n2, MWord(n2)] | Mask(n2);
                    bitmap[n2, MWord(n1)] = bitmap[n2, MWord(n1)] | Mask(n1);
                }
            }
            // Count non-zero elements
            nz = 0;
            int i,j;
            for( i=0; i<=nn;i++) {
                nord[i] = i;
                for ( j=0; j<=nw; j++) {
                    rowbits[i] = rowbits[i] + CountBits(bitmap[i,j]);
                }
                nz = nz + rowbits[i];
            }

            // move unconnected nodes to end
            i=0;
            int t;
            while (i<=nn) {
                if ( rowbits[nord[i]]==0 ) {
                    if ( i!=nn) {
                        t = nord[i];
                        nord[i] = nord[nn];
                        nord[nn] = t;
                    }
                    nn--;
                } else {
                    i++;
                }            
            }

            // 
            nz = (nz - nn - 1) / 2 + nn + 1;  // Adjust for upper diagonal only

            // Each column except last (where there is no choice)
            int minc, mini, w, m;
            for( i=0; i<nn;i++) {
                // find a pivot with fewest non-zeros
                minc = rowbits[nord[i]];
                mini = i;
                for ( j=i+1; j<=nn;j++) {
                    if ( rowbits[nord[j]] < minc) {
                        minc = rowbits[nord[j]];
                        mini = j;
                    }
                }
                if ( mini!=i) {
                    t = nord[i];
                    nord[i] = nord[mini];
                    nord[mini] = t;
                }
                // bits to be eliminated, the column corresponding to the pivot row
                w = MWord(nord[i]);
                m = Mask(nord[i]);
                // simulate gaussian elimination of all non-zeros in column of pivot
                for( j=i+1; j<=nn; j++) {
                    if ( (bitmap[nord[j],w] & m) !=0 ) {
                        rowbits[nord[j]] = SimSubRow(nord[i], nord[j]) - 1;
                        bitmap[nord[j], w] = bitmap[nord[j], w] ^ m;
                    }
                }
            }

            // Total final non zeros and mapping from node row to order number
            fz = 0;
            for( i=0; i<nodes.Count;i++) {
                npos[nord[i]] = i;
                fz+=rowbits[i];
            }
        }

        private int Mask(int n) {
            var nn = n % BPL;
            return 1 << nn;
        }

        private int MWord(int n) {
            return n / BPL;
        }

        private int CountBits(int v) {            
            int r=0;
            for( int i=0; i<BPL;i++) {
                if ( (v & Mask(i)) !=0) {
                    r++;
                }
            }
            return r;
        }

        // Simulate guassian elimination subtract

        private int SimSubRow( int p, int t) {
            int nz=0, i;
            for ( i=0; i<bitmap.GetLength(1); i++) {
                bitmap[t,i] = bitmap[t,i] | bitmap[p,i];
                nz+= CountBits(bitmap[t,i]);
            }
            return nz;
        }

        public int NodeId( int p) {
            return nord[p] + 1;
        }

        public int NodePos(int nid) {
            return npos[nid - 1];
        }
    }
}