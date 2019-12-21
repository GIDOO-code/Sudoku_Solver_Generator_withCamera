using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using GIDOO_space;

namespace GNPZ_sdk{
    public partial class ALSTechGen: AnalyzerBaseV2{

        //Algorithm using ALS and RCC((Restricted Common Candidate).
        // http://csdenpe.web.fc2.com/page50.html
        public bool ALS_XZ( ){
			Prepare();
            if( ALSMan.ALSLst==null || ALSMan.ALSLst.Count<=2 ) return false;
        
		    for(int sz=4; sz<=14; sz++ ){ if( _ALSXZsub(sz) ) return true; }
            return false;
        }

        private bool _ALSXZsub( int sz ){
            if( ALSMan.ALSLst.Count<2 ) return false;

            var cmb = new Combination(ALSMan.ALSLst.Count,2);
            int nxt=99;
            while( cmb.Successor(nxt) ){                        //Select two ALSs
                UALS UA = ALSMan.ALSLst[cmb.Index[0]];
                nxt=0; if( !UA.singly || UA.Size==1 || UA.Size>(sz-2) ) continue;

                UALS UB = ALSMan.ALSLst[cmb.Index[1]];
                nxt=1; if( !UB.singly || UB.Size==1 || (UA.Size+UB.Size)!=sz ) continue;

                int RCC = ALSMan.Get_AlsAlsRcc(UA,UB);          //Common numbers, House contact, Without overlap
                if(RCC==0) continue;               

                if(RCC.BitCount()==1){        //===== Singly Linked =====
                    int EnoB = (UA.FreeB&UB.FreeB).DifSet(RCC); //Exclude candidate digit
                    if( EnoB>0 && _ALSXZ_SinglyLinked(UA,UB,RCC,EnoB) ){
                        SolCode = 2;
                        ALSXZ_SolResult(RCC,UA,UB );

                        if(__SimpleAnalizerB__)  return true;
                        if(!pAnMan.SnapSaveGP(true))  return true;
                    }
                }
                else if(RCC.BitCount()==2){   //===== Doubly Linked =====
                    if(_ALSXZ_DoublyLinked(UA,UB,RCC)){
                        SolCode=2;
                        ALSXZ_SolResult(RCC,UA,UB);

                        if(__SimpleAnalizerB__)  return true;
                        if(!pAnMan.SnapSaveGP(true))  return true;
                    }
                }
            }
            return false;
        }
        private void ALSXZ_SolResult( int RCC, UALS UA, UALS UB ){
            string st = "ALS-XZ "+((RCC.BitCount()==1)? "(Singly Linked)": "(Doubly Linked)");
            Result = st;
            
            if(SolInfoB){            
                foreach( var P in UA.UCellLst ) P.SetNoBBgColor(P.FreeB,AttCr2,SolBkCr);
                foreach( var P in UB.UCellLst ) P.SetNoBBgColor(P.FreeB,AttCr2,SolBkCr2);

                st += "\r ALS1: "+UA.ToStringRCN();
                st += "\r ALS2: "+UB.ToStringRCN();
                st += "\r  RCC: #"+RCC.ToBitStringN(9);
                ResultLong = st;
            }
        }       
  
        private bool _ALSXZ_SinglyLinked( UALS UA, UALS UB, int RCC, int EnoB ){    // *=*=* SinglyLinked subroutine *=*=*
            bool solF=false;
            foreach( var no in EnoB.IEGet_BtoNo() ){
                int EnoBx=1<<no;
           
                Bit81 UEz=new Bit81();  //Covered cells
                foreach( var P in UA.UCellLst.Where(p=>(p.FreeB&EnoBx)>0)) UEz.BPSet(P.rc);
                foreach( var P in UB.UCellLst.Where(p=>(p.FreeB&EnoBx)>0)) UEz.BPSet(P.rc);
     
                Bit81 Elm = (new Bit81(pBDL,EnoBx)) - (UA.B81|UB.B81); //Scan Cells

                foreach( var rc in Elm.IEGet_rc() ){
                    if( (UEz-ConnectedCells[rc]).IsZero() ){ pBDL[rc].CancelB|=EnoBx; solF=true; }
                }
            }
            return solF;
        }
        private bool _ALSXZ_DoublyLinked( UALS UA, UALS UB, int RCC ){              // *=*=* DoublyLinked subroutine *=*=*
            //----- RCC -----
            Bit81 UEz=new Bit81(); //Covered cells
            bool solF=false;
            foreach( int no in RCC.IEGet_BtoNo() ){
                int noB=1<<no;
                UEz.Clear();
                foreach( var P in UA.UCellLst.Where(p=>(p.FreeB&noB)>0) ) UEz.BPSet(P.rc);
                foreach( var P in UB.UCellLst.Where(p=>(p.FreeB&noB)>0) ) UEz.BPSet(P.rc);

                Bit81 Elm=(new Bit81(pBDL,noB))-(UA.B81|UB.B81);    //Scan Cells
                foreach( var rc in Elm.IEGet_rc() ){
                    if( (UEz-ConnectedCells[rc]).IsZero() ){ pBDL[rc].CancelB|=noB; solF=true; }
                }
            }

            //----- ALS element digit other than RCC -----
            int nRCC = UA.FreeB.DifSet(RCC);
            foreach( int no in nRCC.IEGet_BtoNo() ){
                int noB=1<<no;
                UEz.Clear();
                foreach( var P in UA.UCellLst.Where(p=>(p.FreeB&noB)>0) ) UEz.BPSet(P.rc);
                Bit81 Elm =(new Bit81(pBDL,noB))-(UA.B81|UB.B81);   //Scan Cells
                foreach( var rc in Elm.IEGet_rc() ){
                    if((UEz-ConnectedCells[rc]).IsZero()){ pBDL[rc].CancelB|=noB; solF=true; }
                }
            }
            nRCC = UB.FreeB.DifSet(RCC);
            foreach( int no in nRCC.IEGet_BtoNo() ){
                int noB=1<<no;
                UEz.Clear();
                foreach( var P in UB.UCellLst.Where(p=>(p.FreeB&noB)>0) ) UEz.BPSet(P.rc);
                Bit81 Elm=(new Bit81(pBDL,noB))-(UA.B81|UB.B81);    //Scan Cells
                foreach( var rc in Elm.IEGet_rc() ){
                    if((UEz-ConnectedCells[rc]).IsZero()){ pBDL[rc].CancelB|=noB; solF=true; }
                }
            }
            return solF;
        }
    }
}
 