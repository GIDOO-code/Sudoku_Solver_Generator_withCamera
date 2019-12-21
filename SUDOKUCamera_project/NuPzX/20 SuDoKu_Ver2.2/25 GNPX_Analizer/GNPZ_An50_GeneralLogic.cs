using System;
using System.Linq;
using System.Collections.Generic;
using static System.Console;

using GIDOO_space;

namespace GNPZ_sdk{
    public partial class GeneralLogicGen: AnalyzerBaseV2{
        static private int GLtrialCC=0;
        static public int ChkBas1=0, ChkBas2=0, ChkBas3=0,  ChkBas4=0;
        static public int ChkCov1=0, ChkCov2=0;
        static public int ChkBas1A=0, ChkBas1B=0;
        private int GStageMemo;
        private UGLinkMan UGLMan;
        private int GLMaxSize;
        private int GLMaxRank;

        public GeneralLogicGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }
        //1..4....8.9.1...5.....63.....13.5.79..3...8..76.2.94.....75.....1...6.4.8....4..2

        public bool GeneralLogicExnm( ){
            if(pAnMan.GStage!=GStageMemo){
				GStageMemo=pAnMan.GStage;
                GLMaxSize = GNPXApp000.GMthdOption["GenLogMaxSize"].ToInt();
                GLMaxRank = GNPXApp000.GMthdOption["GenLogMaxRank"].ToInt();
                UGLMan=new UGLinkMan(this);
                if(SDK_Ctrl.UGPMan==null)  SDK_Ctrl.UGPMan=new UPuzzleMan(1);
                UGLMan.PrepareUGLinkMan( printB:false );    //SDK_Ctrl.UGPMan.stageNo==12 ); //false); 
			}
                 
            WriteLine( "--- GeneralLogic --- trial:{0}", ++GLtrialCC);
            for(int sz=1; sz<=GLMaxSize; sz++ ){
                for(int rnk=0; rnk<=GLMaxRank; rnk++ ){ 
                    if(rnk>=sz) continue;
                    ChkBas1=0; ChkBas2=0; ChkBas3=0; ChkBas4=0; ChkCov1=0; ChkCov2=0;
                    ChkBas1A=0; ChkBas1B=0;
                    if(GeneralLogicExB(sz,rnk)){
                        WriteLine($" {sz} {rnk} ++ Bas:{ChkBas2}({ChkBas3},{ChkBas4})/{ChkBas1}  Cov:{ChkCov2}/{ChkCov1}  interNum({ChkBas1A}/{ChkBas1B})" );
                        return true;
                    }
                    WriteLine($" {sz} {rnk}    Bas:{ChkBas2}({ChkBas3},{ChkBas4})/{ChkBas1}  Cov:{ChkCov2}/{ChkCov1}  interNum({ChkBas1A}/{ChkBas1B})");
                }
            }
            return false;
        }
        
        private bool GeneralLogicExB( int sz, int rnk ){
            if(sz>GLMaxSize || rnk>GLMaxRank)  return false;

            foreach( var UBC in UGLMan.IEGet_BaseSet(sz,rnk) ){         //### BaseSet generation
                if( pAnMan.CheckTimeOut() ) return false;

                var bas=UBC.HB981;
                foreach( var UBCc in UGLMan.IEGet_CoverSet(UBC,rnk) ){  //### CoverSet generation

                    for(int no=0; no<9; no++ ){
                        if( !(UBCc.HC981._BQ[no]-UBCc.HB981._BQ[no]).IsZero() )  goto SolFound;
                    }
                    continue;

                  SolFound:
                    foreach( int n in UBCc.Can981.nzBit.IEGet_BtoNo() ){
                        foreach( int rc in UBCc.Can981._BQ[n].IEGetRC() ){
                            pBDL[rc].CancelB |= (1<<n);
                        }
                    }

                    if(SolInfoB)  _generalLogicResult(UBCc);
                    if(__SimpleAnalizerB__)  return true;
                    if(!pAnMan.SnapSaveGP(false)) return true;                 
                }
                
               // if(sz>=3) WriteLine("----------------------------Check\r"+UBC);
            }
            return false;
        }

        private void _generalLogicResult( UBasCov UBCc ){
            try{
                Bit81 Q=new Bit81();
                foreach( var P in UBCc.covUGLs )  Q |= P.rcbn2.CompressToHitCells();
                foreach( var UC in Q.IEGetRC().Select(rc=>pBDL[rc]))   UC.SetCellBgColor(SolBkCr2);
           
                for(int rc=0; rc<81; rc++ ){
                    int noB=UBCc.HB981.IsHit(rc);
                    if(noB>0) pBDL[rc].SetNoBBgColor(noB,AttCr,SolBkCr);
                }

                string msg = "\r     BaseSet: ";
                string msgB="";
                foreach( var P in UBCc.basUGLs ){
                    if(P.rcBit81 is Bit81) msgB += P.tfx.tfxToString()+$"#{(P.rcBit81.no+1)} ";
                    else msgB += P.UC.rc.ToRCString()+" ";
                }
                msg += ToString_SameHouseComp1(msgB);
               
                msg += "\r    CoverSet: ";
                string msgC="";
                foreach( var P in UBCc.covUGLs ){
                    if(P.rcBit81 is Bit81) msgC += P.tfx.tfxToString()+$"#{(P.rcBit81.no+1)} ";
                    else msgC +=P.UC.rc.ToRCString()+" ";
                }
                msg += ToString_SameHouseComp1(msgC);

                string st=$"GeneralLogic N:{UBCc.sz} rank:{UBCc.rnk}";
                Result = st;
                msg += $"\r\r ChkBas:{ChkBas4}/{ChkBas1}  ChkCov:{ChkCov2}/{ChkCov1}";
                ResultLong = st+msg;  
            }
            catch( Exception ex ){
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }
        }

        private string ToString_SameHouseComp1( string st ){
            char[] sep=new Char[]{ ' ', ',', '\t' };
            List<string> T=st.Trim().Split(sep).ToList();
            if(T.Count<=1) return st;
            List<_ClassNSS> URCBCell=new List<_ClassNSS>();
            T.ForEach(P=> URCBCell.Add(new _ClassNSS(P)));
            return ToString_SameHouseComp2(URCBCell);
        }
        private string ToString_SameHouseComp2( List<_ClassNSS> URCBCell){
            string st;
            bool cmpF=true;
            do{
                cmpF=false;
                foreach( var P in URCBCell ){
                    List<_ClassNSS> Qlst=URCBCell.FindAll(Q=>(Q.stRCB==P.stRCB && Q.stNum[0]==P.stNum[0]));
                    if(Qlst.Count>=2){
                        st=Qlst[0].stNum[0].ToString();
                        Qlst.ForEach(Q=>st+=Q.stNum.Substring(1,Q.stNum.Length-1));
                        _ClassNSS R=URCBCell.Find(Q=>(Q.stRCB==P.stRCB));
                        R.stNum = st;
                        foreach(var T in Qlst.Skip(1)) URCBCell.Remove(T);
                        cmpF=true;
                        break;
                    }

                    Qlst=URCBCell.FindAll(Q=>(Q.stNum==P.stNum && Q.stRCB[0]==P.stRCB[0]));
                    if(Qlst.Count>=2){
                        st=Qlst[0].stRCB[0].ToString();
                        Qlst.ForEach(Q=>st+=Q.stRCB.Substring(1,Q.stRCB.Length-1));
                        _ClassNSS R=URCBCell.Find(Q=>(Q.stNum==P.stNum));
                        R.stRCB = st;
                        foreach(var T in Qlst.Skip(1)) URCBCell.Remove(T);
                        cmpF=true;
                        break;
                    }
                }

            }while(cmpF);
            st="";
            URCBCell.ForEach(P=> st+=(P.stRCB+P.stNum+" ") );
            return st;
        }
        private class _ClassNSS{
            public int sz;
            public string stRCB;
            public string stNum="  ";
            public _ClassNSS( int sz, string stRCB, string stNum ){
                this.sz=sz; this.stRCB=stRCB; this.stNum=stNum;
            }
            public _ClassNSS( string st ){
                try{
                    sz=1; 
                    if(st.Length>=2) stRCB=st.Substring(0,2);
                    if(st.Length>=4) stNum=st.Substring(2,2);
                }
                catch(Exception){ }
            }
        }
    }  
}
/*
 --- GeneralLogic ---
 1 0++ ChkBas:93/93 ChkCov:22/91
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/114
 2 0++ ChkBas:525/12003 ChkCov:62/2753
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/106
 2 0++ ChkBas:823/19699 ChkCov:151/3745
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/98
 2 0   ChkBas:1001/22438 ChkCov:154/3443
 2 1++ ChkBas:150/3205 ChkCov:1/49185
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/96
 2 0   ChkBas:999/22438 ChkCov:154/3288
 2 1   ChkBas:999/22438 ChkCov:0/307800
 3 0++ ChkBas:2786/61340 ChkCov:201/32460
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/92
 2 0++ ChkBas:754/19205 ChkCov:149/2769
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/86
 2 0   ChkBas:997/22912 ChkCov:156/2652
 2 1++ ChkBas:286/6243 ChkCov:1/89215
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/82
 2 0   ChkBas:1006/23070 ChkCov:156/2386
 2 1   ChkBas:1006/23070 ChkCov:0/259596
 3 0++ ChkBas:13679/243322 ChkCov:1049/98539
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/78
 2 0   ChkBas:1017/23228 ChkCov:156/2261
 2 1   ChkBas:1017/23228 ChkCov:0/256172
 3 0++ ChkBas:1454/33372 ChkCov:102/11552
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:34/78
 2 0   ChkBas:1003/23386 ChkCov:156/2108
 2 1   ChkBas:1003/23386 ChkCov:0/237574
 3 0   ChkBas:16990/280563 ChkCov:1116/97797
 3 1++ ChkBas:590/14150 ChkCov:1/1509911
--- GeneralLogic ---
 1 0++ ChkBas:18/18 ChkCov:3/13
--- GeneralLogic ---
 1 0   ChkBas:120/160 ChkCov:36/76
 2 0   ChkBas:994/23702 ChkCov:172/1973
 2 1   ChkBas:994/23702 ChkCov:0/223552
 3 0   ChkBas:16014/276481 ChkCov:1192/85045
 3 1++ ChkBas:1253/29585 ChkCov:1/3294416
--- GeneralLogic ---
 1 0   ChkBas:117/156 ChkCov:36/66
 2 0   ChkBas:948/22794 ChkCov:164/1529
 2 1   ChkBas:948/22794 ChkCov:0/189390
 3 0   ChkBas:14737/259340 ChkCov:1294/60163
 3 1   ChkBas:14737/259340 ChkCov:0/37206592
 4 0++ ChkBas:46397/736632 ChkCov:1809/754867
--- GeneralLogic ---
 1 0++ ChkBas:51/51 ChkCov:13/26
--- GeneralLogic ---
 1 0   ChkBas:117/156 ChkCov:38/60
 2 0++ ChkBas:14/314 ChkCov:3/43
--- GeneralLogic ---
 1 0   ChkBas:117/156 ChkCov:38/56
 2 0++ ChkBas:607/18725 ChkCov:180/1013
 */