using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static System.Console;

using GIDOO_space;

namespace GNPZ_sdk {
    public partial class SuperLinkMan{     
        
        public USuperLink CTD_EvalSuperLinkChain( UCell P0, int no0, bool DevelopB=false ){
			try{	
				int dbX=0, rc0=P0.rc; 
                _dbCC=0;   
                        if(DevelopB){ WriteLine($"\r\r10 *Origin rc0:{rc0} no0:{no0}"); }
				 
                USuperLink GNL_Result=new USuperLink(rc0,no0);
				var Qtrue =GNL_Result.Qtrue;
				var Qfalse=GNL_Result.Qfalse;
                Qtrue =GNL_Result.Qtrue;
  				Qfalse=GNL_Result.Qfalse;
				var rcQue=new Queue<GroupedLink>();

                //===== First GLK Process =====
                foreach( GroupedLink R in IEGet_SuperLinkFirst(P0,no0).Where(p=>p.type==W)){
                    rcQue.Enqueue(R);     //Enqueue first GLK 
                }

				//====================== Radiation Search ==============================================================
				while(rcQue.Count>0){
                    {   //----- Distinct -----
                        var Q2=rcQue.ToList().Distinct();
                        rcQue.Clear();
                        foreach( var X in Q2) rcQue.Enqueue(X);
                            //if(DevelopB){ WriteLine(); foreach(var P in rcQue) WriteLine($"--rcQue---{P.GrLKToString()}"); WriteLine(); }
                    }                        
   
					GroupedLink R = rcQue.Dequeue();                                        //dequeue next element(pre-GLink)
                            if(DevelopB){ WriteLine($"\r20�� {dbX++}---Queue:"+R.GrLKToString()); __Debug_ChainPrint(R); }

					foreach( GroupedLink RR in IEGet_SuperLink(R).Where(p=>p.AvailableF) ){                                               
                        if(_IsLooped(RR)) continue;
						if(R.type!=RR.type && R.UGCellsA.Equals(RR.UGCellsB) )  continue;   //Skip back_links

                        if( !(R.UsedCs&RR.UGCellsB.B81).IsZero() )  continue;
                        RR.preGrpedLink=R;                                                  //set preLink
                        RR.UsedCs = R.UsedCs | RR.UGCellsB.B81;
                        rcQue.Enqueue(RR);                                                  //enqueue next-GLink to RadiationSearchQueue
                            if(DevelopB){ __Debug_ChainPrint(RR); WriteLine($"��   {dbX++}-{++chkX}--RR:"+RR.GrLKToString()); }
                                           					
                        int no2=RR.no2; //no2:next digit                                      
						if(RR.type==S){   //========== Check connection conditions. case StrongLink(F->T) ==========
					      //if(!(RR.UGCellsB.B81&Qtrue[RR.no2]).IsZero() )  continue;   //xxx if the next element is already setted true, go to the next process
							if(RR.UGCellsB.Count==1){                                       //if the link's next element is a single cell
								UCell P=RR.UGCellsB[0];                                     //RRnxtC:next cell
                                
                                if(P.rc==rc0){                                              //Check Loop completion
                                    GNL_Result.resultGLK=RR;  
                                    SolCode = Contradiction_CheckSolution(GNL_Result);  //There are cells/#n that can be excluded.
                                    if(SolCode>0)  goto  LoopCompletion;                    //Established Sudoku solution 
                                    goto ErrorTrial;                                        //Failed                                                                                         
                                }                                   
                                
                                if(Qtrue[no2].IsHit(P.rc)){ RR.AvailableF=false; continue; }//AvailableF=false
								Qtrue[no2].BPSet(P.rc);                                     // set P.rc#no2 is true
                                        if(DevelopB) WriteLine($"        30S ->P:{P}  +#{no2} is true.");                          
                                
								foreach( int nox in P.FreeB.IEGet_BtoNo().Where(q=>(q!=no2)) ){ // P.rc#no2:True => #nox(!=no2):False                               
									if(Qfalse[nox].IsHit(P.rc))  continue;
                                    Qfalse[nox].BPSet(P.rc);                                //set P.rc#no2 to false 
                                        if(DevelopB) WriteLine($"        31S ->P:{P}  -#{nox} is false.");
								}
							}
						}

						else if(RR.type==W){   //========== Check connection conditions. case WeakLink(T->F) ==========
							if(!(RR.UGCellsB.B81&Qfalse[RR.no2]).IsZero() )  continue;      //if the next element is setted false, go to the next process
                          
							foreach( UCell P in RR.UGCellsB ){                              //foreach next GLink 
                                if(Qfalse[no2].IsHit(P.rc))  continue;
								Qfalse[no2].BPSet(P.rc);                                    //set P.rc#no2 is true
                                        if(DevelopB) WriteLine($"       -40W *RR:{P}  -#{no2} is false.");
							}                           
                            rcQue.Enqueue(RR);                                              //enqueue next-GLink to RadiationSearchQueue

							if( RR.UGCellsB.Count==1 ){                                     //if the link's next element is a single cell                  
								UCell P=RR.UGCellsB[0];   
								if(P.FreeBC==2){                                            //If the next is a binary cell
									int nox=P.FreeB.DifSet(1<<no2).BitToNum();
                                    //if(!_Check_LogicalError_GNL(no2,P.rc,Qtrue) )  goto ErrorTrial; //No check required!                                                                                         
                                    //if(Qtrue[nox].IsHit(P.rc)){ RR.AvailableF=false; continue; }//AvailableF=false. No check required!
									Qtrue[nox].BPSet(P.rc);		                  	        //set P.rc#no2 is true	

									GroupedLink RRR = new GroupedLink(P,no2,nox,S);         //GLK_ViVal:GLink in cell(P#no2 StrongLink)
                                    RRR.preGrpedLink=RR;                                    //set preLink                                    
                       	                if(DevelopB) WriteLine($"       +41W *RR:{P}  -#{nox} is true. :{RRR.GrLKToString()}"); 

                                    if(P.rc==rc0){
                                        GNL_Result.resultGLK=RR; 
                                        SolCode = Contradiction_CheckSolution(GNL_Result);
                                        if(SolCode>0)  goto  LoopCompletion;               //Solution found 
                                        goto ErrorTrial;  
                                    }                                                                        
 
                                }
							}
						}					
					}
				}
				return null;

              ErrorTrial:
                return null;

              LoopCompletion:
                GNL_Result.contDiscontF = 999999999;
//////
                if(DevelopB){
			        L2SprLkB81[no0].BPSet(rc0);                                            // Set the searched flag
                    if(DevelopB) developDisp( rc0, no0, GNL_Result, DevelopB );
                    L2SprLK[rc0,no0]=GNL_Result;
                }
//////
                return GNL_Result;

			}
			catch( Exception ex ){
				WriteLine($"{ex.Message}+\r{ex.StackTrace}");
			}
			return null;
        }
        
        private int Contradiction_CheckSolution( USuperLink GNL_Result, int contDiscontF=99999999){ 
            bool SolFound=false;

            GroupedLink GLKnxt=GNL_Result.resultGLK;
            if(GLKnxt==null)  return -1;                        //Not established
            List<GroupedLink> SolLst=Convert_ChainToList_GNL(GNL_Result);
            if(SolLst==null || SolLst.Count<2)  return -1;     //Not established
            GroupedLink GLKorg=SolLst[0];
            bool SolType = Check_SuperLinkSequence(GLKnxt,GLKorg); //true:Continuous  false:DisContinuous
            contDiscontF = SolType? 1: 2;

            if(SolType){        //==================== continuous ====================
                Bit81 UsedCs=SolLst.Aggregate(new Bit81(),(Q,P)=>Q|P.UGCellsB.B81);

                foreach( var LK in SolLst.Where(P=>(P.type==W))){
                    int   noB=1<<LK.no;        
                    Bit81 SolBP=new Bit81();      
                    LK.UGCellsA.ForEach(P=>{ if((P.FreeB&noB)>0) SolBP.BPSet(P.rc); });
                    LK.UGCellsB.ForEach(P=>{ if((P.FreeB&noB)>0) SolBP.BPSet(P.rc); });
                    if( SolBP.BitCount()<=1 ) continue;
                    foreach( var P in pBDL.Where(p=>(p.FreeB&noB)>0) ){
                        if( UsedCs.IsHit(P.rc) ) continue;
                        if( !(SolBP-ConnectedCells[P.rc]).IsZero() )  continue;
                        if( (P.FreeB&noB)==0 )  continue;
                        P.CancelB |= noB;�@�@//exclusion digit
                        SolFound=true;
                    }
                }

                var LKpre=SolLst[0];               
                foreach( var LK in SolLst.Skip(1) ){  
                    if( LKpre.type==S && LK.type==S && LK.UGCellsA.Count==1 ){
                        var P=pBDL[LK.UGCellsA[0].rc];  //(for MultiAns code)
                        int noB2=P.FreeB-((1<<LKpre.no2)|(1<<LK.no));                       
                        if( noB2>0 ){ P.CancelB |= noB2; SolFound=true; }
                    }
                    LKpre=LK;
                }
                if(SolFound) SolCode=1;
            }
            else{           //==================== discontinuous ====================
                int dcTyp= GLKorg.type*10+GLKnxt.type;
                UCell P=pBDL[GLKorg.UGCellsA[0].rc];   //(for MultiAns code)
                switch(dcTyp){
                    case 11: 
                        P.FixedNo=GLKorg.no+1; //Cell number determination
                        P.CancelB=P.FreeB.DifSet(1<<(GLKorg.no));
                        SolCode=1; SolFound=true; //(1:Fixed�j
                        break;
                    case 12: P.CancelB=1<<GLKnxt.no; SolCode=2; SolFound=true; break;   //(SolCode=2 : Exclude from candidates�j
                    case 21: P.CancelB=1<<GLKorg.no; SolCode=2; SolFound=true; break;
                    case 22: 
                        if( GLKorg.no==GLKnxt.no ){ P.CancelB=1<<GLKorg.no; SolFound=true; SolCode=2; }
                        break;
                }
            }

            if(SolFound) return SolCode;
            return -1;                    //Not established
        }

    }
}