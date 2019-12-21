using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static System.Console;
using static System.Math;

using GIDOO_space;

namespace GNPZ_sdk {
    public class UGLinkMan{
        public List<UCell>    pBDL;             
        public Bit81[]        pHouseCells;
        public Bit81[]        p_connectedCells;
        private List<Bit81>   HBL=new List<Bit81>();
        private List<UGLink>  UGLLst=null;
        private List<int>     usedLKIgnrLst=null;

        private Bit81[]       Colored=new Bit81[9];
        private Bit81[]       Processed=new Bit81[9];
        public  Bit81[]       BPnoBLst = new Bit81[9];

        static public bool printSW=false;

        //..7.2..4546.5....9.95.....7.....8.3.9...6...1.7.2...987......8384...1.5253..8.9..  
        //83..76..2....85.....1...7...8...3....67...13....7...4...2...3.....24....9..63..25 

        public UGLinkMan( AnalyzerBaseV2 AnB ){
            this.pBDL = AnB.pBDL;
            this.pHouseCells       = AnalyzerBaseV2.HouseCells;
            this.p_connectedCells   = AnalyzerBaseV2.ConnectedCells;
            UGLink.p_connectedCells = AnalyzerBaseV2.ConnectedCells;
            for(int no=0; no<9; no++ ){
                Colored[no]=new Bit81(); Processed[no]=new Bit81();
            }
            UGLink.pUGLM=this;
        }

        public int PrepareUGLinkMan( bool printB=false ){
            Bit81Chk.p_connectedCells   = AnalyzerBaseV2.ConnectedCells;
            UGLLst=new List<UGLink>();
            usedLKIgnrLst=new List<int>();
            UGLink.SrNum0=0;
            for(int no=0; no<9; no++){          // row/column/block elements
                Bit81 BPnoB=new Bit81(pBDL,(1<<no));
                BPnoBLst[no] = BPnoB;
                for(int tfx=0; tfx<27; tfx++){ 
                    Bit81 Q = pHouseCells[tfx]&BPnoB;
                    if(Q.IsZero()) continue;
                    Q.no=no;
                    Q.ID=(tfx<<4)|no;
                    if(UGLLst.All(P=>(P.rcBit81.no!=no || P.rcBit81!=Q))){ UGLLst.Add(new UGLink(Q)); } // Q is unique?
                    else usedLKIgnrLst.Add(Q.ID);
                }
            }
            foreach( var UC in pBDL.Where(p=>p.No==0) )  UGLLst.Add(new UGLink(UC));    // cell elements
            UGLink.pBPnoBLst=BPnoBLst;

            if(printB)  UGLLst.ForEach(P=>WriteLine(P.ToString("prepare")));
            return UGLLst.Count;
        }

        public int __checkCC=0;
        public IEnumerable<UBasCov> IEGet_BaseSet( int sz, int rnk ){ 
            if(UGLLst==null)  yield break;

            __checkCC=0;

            List<UGLink>  basUGLs=new List<UGLink>();
            Bit981        HB981=new Bit981();           //BaseSet bitPattern
            Bit324        usedLK=new Bit324();          //usedLink(by serial number)
            List<int>     usedLKLst=new List<int>();

            Bit81Chk      coverChk=new Bit81Chk(sz,rnk,basUGLs,usedLKLst,usedLKIgnrLst);

            var cmbBas=new Combination(UGLLst.Count,sz);
            long rcbn36=0;

            int jkC=0;
            int nxt=int.MaxValue;   //(skip function)
            while(cmbBas.Successor(nxt)){
                                    GeneralLogicGen.ChkBas1++;   //*****

                jkC++;
              //*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==* sz=1
                if(sz==1){
                    UGLink UGL=UGLLst[cmbBas.Index[0]];
                    if( UGL.UC is UCell ) goto LNextSet; // if size=1, cell-type is invalid

                    // UGL.rcBit81 is Bit81
                    int blkB=(int)UGL.Get_rcbnFrame(2);
                    if(blkB.BitCount()>1)  goto LNextSet;

                    int no=UGL.rcBit81.no, b=blkB.BitToNum();
                    var P = UGLLst.Find(U => U.Equal_no_block(no,b) );
                    if(P==null) goto LNextSet;
                    if((P.rcBit81-UGL.rcBit81).Count==0)  goto LNextSet;

                    //there are numbers only within one block
                    HB981.Clear();    HB981.BPSet(UGL.rcBit81.no,UGL.rcBit81,tfbSet:false);
                    basUGLs.Clear();  basUGLs.Add(UGL);
                                GeneralLogicGen.ChkBas2++;   //*****
                    goto LBSFound;   //possibility of solution
                }

              //*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==*==* sz>=2
                HB981.Clear();
                basUGLs.Clear();
                usedLKLst.Clear();
                coverChk.Clear();
                rcbn36=0;

                //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2

                for(int k=0; k<sz; k++ ){
                    nxt=k;
                    UGLink UGL=UGLLst[cmbBas.Index[k]];
                    rcbn36 |= UGL.rcbnFrame2 ;

                    if( !Check_rcbnCondition(sz,rnk,k,rcbn36) ) goto LNextSet;  //# Extremely efficient

                    if(UGL.rcBit81 is Bit81){    // ............ rcb ............
                        if(k>0 && HB981.IsHit(UGL.rcBit81.no,UGL.rcBit81))  goto LNextSet; //included in BaseSet
                        HB981.BPSet(UGL.rcBit81.no,UGL.rcBit81,tfbSet:true);               //register to BaseSet
                        usedLKLst.Add(UGL.rcBit81.ID);    //tfx<<4 | no
                        int no = UGL.rcBit81.no;
                        coverChk.noB |= 1<<no;  //qq
                    }
                    else{                        // ........... Cell ............
                        UCell UC=UGL.UC;
                        int   rc=UC.rc;  
                        foreach( var n in UC.FreeB.IEGet_BtoNo(9) ){
                            if(k>0 && HB981.IsHit(n,rc))   goto LNextSet;
                            HB981.BPSet(n,rc,tfbSet:true);
                        }
                        int IDrc = rc<<17 | 1<<16;
                        usedLKLst.Add(IDrc);    //tfx<<4 | no
                    }
                    basUGLs.Add(UGL); //qq                   
                }
                                            GeneralLogicGen.ChkBas2++;   //*****

                bool niceB=coverChk.Check_BaseSetCondition(HB981);//########## check_rcB9 ##########
                if( !niceB )    goto LNextSet;

                //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2     
                if(sz>=2){
                    int noBP=HB981.nzBit;
                    int noCC=noBP.BitCount();
                    if(noCC>=2){
                        List<int> IX = noBP.IEGet_BtoNo().ToList();

                        if(sz>=2){
                            for(int k=0; k<noCC; k++){
                                Bit81 A=new Bit81(), B=new Bit81();
                                int no=IX[k];
                                foreach(var P in basUGLs){
                                    if(P.rcBit81 is Bit81){
                                        if(P.rcBit81.no==no) A |= P.rcBit81;
                                        else                 B |= P.rcBit81;
                                    }
                                    else{
                                        int rc=P.UC.rc;
                                        foreach(var n in P.UC.FreeB.IEGet_BtoNo()){
                                            if(n==no) A.BPSet(rc);
                                            else      B.BPSet(rc);
                                        }
                                    }
                                }

                                int nOL=(B&A).Count;
                                if(rnk==0 && nOL<2)  goto LNextSet;
                                if(rnk>0  && nOL<1)  goto LNextSet;                           
                            //    WriteLine("---------- no:{0} sz:{1} rnk:{2} nOL:{3} (A-B):{4} (B-A):{5}", no, sz,rnk, nOL, (A-B).Count, (B-A).Count );
                            }       
                            //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2
                        }
                    }
                }

                //---------------------------------------------------------------------------------------  
              LBSFound:
                    if(SDK_Ctrl.UGPMan.stageNo==12 && sz>=3){// && rnk==1 ){
                        WriteLine("\r sz:{0} rnk:{1} jkc:{2}", sz, rnk, jkC );
                        Check_rcbnCondition(sz,rnk,sz,rcbn36,printB:true);
                        basUGLs.ForEach(P=>WriteLine(P.ToString("BaseSet")));
                    }

                usedLK.Clear();
                basUGLs.ForEach(P=> usedLK.BPSet(P.SrNum) ); //IDrc: rc<<17 | 1<<16
                UBasCov UBC=new UBasCov( basUGLs, HB981, sz, usedLK );
                yield return UBC;

              LNextSet:
                continue;
            }
            yield break;
        }

        private bool Check_rcbnCondition3(int sz,int rnk, long rcbn36, int[] rcbn3){//########## Extremely efficient ##########
            int rC = ((int)(rcbn36&0x1FF)).BitCount();
            int cC = ((int)(rcbn36>>9)&0x1FF).BitCount();
            int bC = ((int)(rcbn36>>18)&0x1FF).BitCount();
            int nC = ((int)(rcbn36>>27)&0x1FF).BitCount();
            if(sz==2){
                if(nC==1){

                }
            }
            return true;
        }
        private bool Check_rcbnCondition(int sz,int rnk, int kx, long rcbn36, bool printB=false ){ //########## Extremely efficient ##########
            // kx is a cycle no 
            //Extremely efficient method by consideration
            int rC = ((int)(rcbn36&0x1FF)).BitCount();
            int cC = ((int)(rcbn36>>9)&0x1FF).BitCount();
            int bC = ((int)(rcbn36>>18)&0x1FF).BitCount();
            int nC = ((int)(rcbn36>>27)&0x1FF).BitCount();
            List<int> _S = new List<int>();
            _S.Add(rC); _S.Add(cC); _S.Add(bC);   //aa  _S.Add(nC);
            _S.Sort();
                                                            ////    if(rC==0) rC=511;
                                                            ////    if(cC==0) cC=511;
                                                            ////    if(bC==0) bC=511;
            int rcbC = Min(rC,cC);
            rcbC = Min(rcbC,bC);
            
            if(sz>=2){
                if(printB){
                    Write( "  rC:{0} cC:{1} b:{2} nC:{3}  ->", rC, cC, bC, nC );
                    _S.ForEach(p=>Write(" {0}",p));
                    WriteLine();
                }

                if(nC==1)  return (_S[1]<=sz+rnk);
                else if(nC==2) return (_S[0]<=sz+rnk);
            }
            return (Min(rcbC,bC)+(nC-1) <= sz+rnk);
        }

        //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2            
        public IEnumerable<UBasCov> IEGet_CoverSet( UBasCov UBC, int rnk ){ 
            if(UGLLst==null)  yield break;

            List<UGLink> basUGLs=UBC.basUGLs;
            Bit981 HB981=UBC.HB981;
            Bit324 usedLK=UBC.usedLK;
            int    nzBit=HB981.nzBit;
            int    nzBitCC=nzBit.BitCount();
            int    sz=UBC.sz;

            List<UGLink> UGLCovLst=new List<UGLink>();  // UGLCovLst:candidate link

          #region Create UGLCovLst
            Bit81 Bcmp = HB981.CompressToHitCells();
            foreach( var P in UGLLst.Where(q=>!usedLK.IsHit(q.SrNum)) ){
                if(P.rcBit81 is Bit81){ //Row, column, block link
                    if( (nzBit&(1<<P.rcBit81.no))==0 ) continue;
                    int B = (HB981._BQ[P.rcBit81.no] & P.rcBit81).BitCount();
                    if(B==0)  continue;

                    // if rank:0, the CoverSet has two or more common items
                    if( rnk==0 && (HB981._BQ[P.rcBit81.no]&P.rcBit81).Count<2 ) continue;                  
                    UGLCovLst.Add(P);
                }
                else{   //Cell
                    if(nzBitCC<=1) continue;
                    if( (nzBit&P.UC.FreeB)==0 )  continue;
                    int B=nzBit&P.UC.FreeB;
                    if(B==0)  continue;
                    int rc=P.UC.rc;
                    
                    int kcc=0, kccLim=(rnk==0)? 2:1;
                    foreach( var no in B.IEGet_BtoNo() ){
                        if( !HB981._BQ[no].IsHit(rc) )  continue;
                        if(++kcc>=kccLim){ UGLCovLst.Add(P); break; }
                    }
                } 
            }
          #endregion
            if(UGLCovLst.Count<sz+rnk)  yield break;
            Bit981 HC981=new Bit981();
            Bit981 HLapB=new Bit981();
            Bit981 Can981=new Bit981();
            Combination cmbCvr=new Combination(UGLCovLst.Count,sz+rnk);
            int nxt=int.MaxValue;
            while( cmbCvr.Successor(nxt) ){
                                    ++GeneralLogicGen.ChkCov1;
            
                HC981.Clear();
                Array.ForEach( cmbCvr.Index, m=> HC981 |= UGLCovLst[m].rcbn2 );

                if( !(HB981-HC981).IsZero() ) goto LNextSet;    //BaseSet is covered?
                Bit981 CsubB = HC981-HB981;
                if( CsubB.IsZero() ) goto LNextSet;             //excludable candidates is exist?

                List<UGLink>  covUGLs=new List<UGLink>();
                Array.ForEach( cmbCvr.Index, m=> covUGLs.Add(UGLCovLst[m]) );

                if(rnk==0){ Can981=CsubB; }
                else{   //if(rnk>0){
                    bool SolFound=false;
                    foreach( int n in CsubB.nzBit.IEGet_BtoNo() ){
                        foreach( int rc in CsubB._BQ[n].IEGetRC() ){
                            int kc = covUGLs.Count(Q=>Q.IsHit(n,rc));
                            if(kc==rnk+1){
                                Can981.BPSet(n,rc);
                                SolFound=true;
                            }
                        }
                    }
                    if(!SolFound) continue;     
                }
                                    ++GeneralLogicGen.ChkCov2;   //*****

                UBC.addCoverSet( covUGLs, HC981, Can981, rnk );
                yield return UBC;

              LNextSet:
                continue;
            }
            yield break;
        }

        public class Bit81Chk{
            static public Bit81[]    p_connectedCells;

            private List<int> usedLKLst;
            private List<int> usedLKIgnrLst;

            private int  sz;
            private int  rnk;
            public  int  noB;
            private List<UGLink> basUGLs;

            public Bit81Chk( int sz, int rnk, List<UGLink> basUGLs,
                             List<int> usedLKLst, List<int> usedLKIgnrLst ): base(){
                this.sz=sz; this.rnk=rnk; this.basUGLs=basUGLs;
                this.usedLKLst=usedLKLst; this.usedLKIgnrLst=usedLKIgnrLst;
            }

            public void Clear(){ noB=0; }// /*rcB9=0;*/ base.Clear(); }

        //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2

            private int jkC=0;
            public bool Check_BaseSetCondition( Bit981 HB981 ){//for sz>=2
                bool niceB=false, E=false;
                jkC++;

              #region There is a link between the link and the other link group
                for ( int k=0; k<sz; k++ ){
                    UGLink UGL=basUGLs[k];
                    Bit981 Bcum=new Bit981();
                    for(int m=0; m<sz; m++ ){
                        if(m==k)  continue;
                        Bcum |= basUGLs[m].rcbn2;
                    }
                    if( (Bcum&UGL.rcbn_conn2).Count<=0 ) goto Lreturn; //false
                }
              #endregion
                                        GeneralLogicGen.ChkBas3++;   //*****                
              #region Every element of the number(#no) has a link
                //���ꂼ��̃����N�́A���̑��̃����N�Q�ƘA������B
                //�����N���Ȃ��Z����rnk���ȉ��łȂ���΂Ȃ�Ȃ��B
                            //if(E) for(int n=0; n<9; n++ ) WriteLine(HB981.tfx27Lst[n].ToBitString(27));
                Bit981 Q9 = HB981.Copy();                    
                usedLKLst.ForEach(p=> HB981.tfxReset(p&0xF,p>>4) );
                usedLKIgnrLst.ForEach(p=> HB981.tfxReset(p&0xF,p>>4) );
                            //if(E) for(int n=0; n<9; n++ ) WriteLine(HB981.tfx27Lst[n].ToBitString(27));
                int QPP=0;
                foreach( var no in noB.IEGet_BtoNo() ){
                    Bit81 Q = Q9._BQ[no];//new Bit81(HB981._BQ[no]);
                            //if(E) WriteLine( "HB981.tfxLst[no]: "+HB981.tfx27Lst[no].ToBitString(27) );
                    foreach( var tfx in HB981.tfx27Lst[no].IEGet_tfb() ){
                        int bp=HB981.GetBitPattern_tfnx(no,tfx);
                        if(bp<=0) continue;
                        int nc=bp.BitCount(); 
                        if(nc>=2){
                                    if(E) WriteLine("Q: "+Q.ToString() );
                            foreach( var nx in bp.IEGet_BtoNo() ){
                                int rc=tfx.Get_tfx_rc(nx);
                                Q.BPReset(rc);
                            }
                            //if(E) WriteLine("Q: "+Q.ToString() );
                        }
                           
                    }
                    QPP += Q.Count;  
                }
                if(QPP<=rnk){ niceB=true; goto Lreturn; }
              #endregion
                                        GeneralLogicGen.ChkBas3++;   //*****
                
              #region There is a possibility link between the patterns of numbers n1,n2
                //�����Ԃ̃����N���Ȃ��Z���́Arnk*2�ȉ��łȂ���΂Ȃ�Ȃ��B
                if( HB981.nzBit.BitCount()>=2 ){
                    bool  firstB=true;
                    Bit81 Q=null;
                    foreach( var n in HB981.nzBit.IEGet_BtoNo() ){
                        if(firstB){ Q=HB981._BQ[n]; firstB=false; }
                        else Q |= HB981._BQ[n];
                    }
                    foreach( var rc in Q.IEGetRC() ){
                        if( HB981.GetBitPattern_rcN(rc).BitCount()>=2 ){ //�Z��rc�̐����ԃ����N������Ƃ��́AQ9���Z�b�g
                            foreach( var no in noB.IEGet_BtoNo() ) Q9._BQ[no].BPReset(rc);
                        }
                    }
                    int Q9cc = Q9.BitCount();
                    if(Q9cc<=rnk){ niceB=true; goto Lreturn; } //���ڃ����N���Ȃ��Z����rnk�ȉ��̂Ƃ��́A���̃e�X�g�ɐi��
                    if(Q9cc<=rnk*2){
                                            GeneralLogicGen.ChkBas1B++;   //*****
                        foreach( var n1 in Q9.nzBit.IEGet_BtoNo() ){
                            foreach( var rc in Q9._BQ[n1].IEGetRC() ){
                                foreach( var n2 in Q9.nzBit.IEGet_BtoNo().Where(nx=>nx!=n1) ){
                                    if( (Q9._BQ[n2] & p_connectedCells[rc]).BitCount()==0 ) continue;
                                    //�����N���Ȃ��Z���Ԃ��q�������N������i�h�\������h�Ŏ��̃e�X�g�ɐi��
                                            GeneralLogicGen.ChkBas1A++;   //*****
                                    niceB=true; goto Lreturn;
                                }
                            }
                        }

                    }
                }
              #endregion

                                GeneralLogicGen.ChkBas4++;   //*****
              Lreturn:
                return niceB;
            }
        }
    }

    public class UBasCov{
        public Bit324       usedLK;
        public List<UGLink> basUGLs; //
        public List<UGLink> covUGLs; //
        public Bit981 HB981;
        public Bit981 HC981;
        public Bit981 Can981;
        public int    rcCan;
        public int    noCan;
        public int    sz;
        public int    rnk;

        public UBasCov( List<UGLink> basUGLs, Bit981 HB981, int sz, Bit324 usedLK ){
            this.basUGLs=basUGLs; this.HB981=HB981; this.sz=sz; this.usedLK=usedLK;
        }
 
        public void addCoverSet( List<UGLink> covUGLs, Bit981 HC981, Bit981 Can981, int rnk ){
            this.covUGLs=covUGLs; this.HC981=HC981; this.Can981=Can981; this.rnk=rnk;
        }
        public override string ToString(){
            string st="";
            foreach( var UGL in basUGLs){
                if(UGL.rcBit81 is Bit81){   // RCB
                    int no=UGL.rcBit81.no;
                    st += string.Format("Bit81: no:{0}  {1}\r", no, UGL.rcBit81 );
                }
                else{   // Cell
                    UCell UC=UGL.UC;
                    st += string.Format("UCell: {0}\r", UC );
                }
            }
            return st;
        }
    }
        
    //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2
    public class UGLink{
        static public UGLinkMan pUGLM;
        static public Bit81[]   p_connectedCells;
        static public Bit81[]   pBPnoBLst;
        static public int       SrNum0;
        public List<UCell>      pBDL{ get{ return pUGLM.pBDL; } } 
        public int      sz;
        public int      SrNum;

        public Bit981   rcbn2;        // for _connectivity check
        public Bit981   rcbn_conn2;   //   elated cells

        //-----------------------------------------
        public UCell    UC=null;
        //-----------------------------------------
        public Bit81    rcBit81=null;
        public long     rcbnFrame2;   //aggregate to frames
        public long     rcbnID{
            get{ 
                if(rcBit81 is Bit81){ return (1<<(rcBit81.ID>>4)); }
                else{ return (rcbnFrame2&0x7FFFFFF); }
            }
        }

        public int tfx{
            get{ return( (rcBit81 is Bit81)? (rcBit81.ID>>4): -1); }
            set{ if(rcBit81 is Bit81) rcBit81.ID=(rcBit81.ID&0xF)|(value<<4); }
        }

        public UGLink( Bit81 rcBit81 ){
            this.SrNum=SrNum0++; 
            this.rcBit81=rcBit81; this.sz=rcBit81.BitCount();
            rcbn2=new Bit981(rcBit81);

            rcbn_conn2=new Bit981();   
            var _conn=new Bit81();
            foreach(var rc in rcBit81.IEGet_rc()){
                _conn |= p_connectedCells[rc];
                for(int n=0; n<9; n++ ) rcbn_conn2.BPSet(n,rc);
            }
            int no=rcBit81.no;
            rcbn_conn2._BQ[no] = _conn-rcBit81;
            rcbnFrame2 = (long)rcBit81.Get_RowColumnBlock() | ((long)1<<(no+27));   //��
        }
        public UGLink( UCell UC ){
            this.SrNum=SrNum0++; this.UC=UC;
            rcbn2=new Bit981();
            rcbn_conn2=new Bit981();
            foreach(var n in UC.FreeB.IEGet_BtoNo() )  rcbn2.BPSet(n,UC.rc);
            for(int n=0; n<9; n++) rcbn_conn2._BQ[n] |= p_connectedCells[UC.rc];

            int _rcbFrame = (1<<UC.r | 1<<(UC.c+9) | 1<<(UC.b+18));
            rcbnFrame2 = (long)_rcbFrame | ((long)UC.FreeB)<<27;                   //��
        }
        public bool IsHit( int no, int rc ){
            if( rcBit81 is Bit81 ){ if( this.rcBit81.no==no && rcBit81.IsHit(rc) ) return true; }
            else{ if( UC.rc==rc && (UC.FreeB&(1<<no))>0 ) return true; }
            return false;
        }

        public bool Equal_no_block( int noX, int blk ){
            if(rcBit81 is Bit81){
                if(rcBit81.no!=noX) return false;
                if((tfx-18)!=blk)   return false;
                return true;
            }
            return false;
        }

        public int Get_rcbnFrame(int nx){
            switch(nx){
                case 0: return (int)(rcbnFrame2&0x1FF);
                case 1: return (int)(rcbnFrame2>>9)&0x1FF;
                case 2: return (int)(rcbnFrame2>>18)&0x1FF;
                case 3: return (int)(rcbnFrame2>>27)&0x1FF;
            }
            return -1;
        }

        public bool Check_connected( Bit981 HB981 ){
            if(rcBit81 is Bit81){
                int no=rcBit81.no;
                return  HB981._BQ[no].IsHit(rcbn_conn2._BQ[no]);
            }
            else{
                int rc=UC.rc;
                foreach( var n in UC.FreeB.IEGet_BtoNo() ){
                    if( HB981._BQ[n].IsHit(rc) )  return true;
                }
                return false;
            }
        }      

        public string ToString( string ttl="" ){
            string st = ttl+" UGLink SrNum:"+SrNum+" tfx:"+ tfx.tfxToString()+"("+tfx+")";
            if(UC!=null) st+="UCell "+ UC.ToString();
            else         st+="ULink no:"+ (rcBit81.no) + " Bit81 "+rcBit81.ToString();
            return st;
        }
        public string ToString2(){
            if(rcBit81 is Bit81) return (tfx.tfxToString()+("#"+(rcBit81.no+1))+" ");
            else             return (UC.rc.ToRCString()+" ");
        }
    }
}