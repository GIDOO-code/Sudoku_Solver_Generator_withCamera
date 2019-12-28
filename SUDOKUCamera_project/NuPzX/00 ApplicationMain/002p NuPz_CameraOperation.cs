using System;
using System.Linq;
using System.Collections.Generic;
using static System.Console;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Threading;
using System.Threading.Tasks;

using OpenCvSharp;
using OpenCvSharp.Extensions;

using GIDOOCV;

namespace GNPZ_sdk{
    public partial class NuPz_Win{
        static public event SDKSolutionEventHandler Send_webCameraInfo;
        static  public  string  DigRegMode; 
        static  public int[]    SDK81;
        static  public  Mat     Frame00;
        private int             threadCC=0;

        private int             camID=-1;
        private string          stringCapType;
        private bool            cameraCancel;
        private bool            FlipModeX;
        private bool            FlipModeY;

        public  UPuzzle         pGP_DgtRecog=null;

        private WriteableBitmap w1 = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);   //PixelFormats.Gray8
        private string          fName="_LMparameter.txt";
        private List<string>    captureTypeList=new List<string>{"3.1MP 4:3 2048x1530", "1.9MP 4:3 1600x1200", "0.3MP 4:3 640x480" };

        public sdkFrameRecgV3Man SDKRecgMan=null;
      
        private void chbAutoSave_CheckedUnchecked(object sender, RoutedEventArgs e){
            if(chbAutoSave==null)  return;

            if((bool)chbAutoSave.IsChecked){
                btnSavePuzzleDetect.IsHitTestVisible=false;
                btnSavePuzzleDetect.Opacity=0.4;
            }
            else {
                btnSavePuzzleDetect.IsHitTestVisible=true;
                btnSavePuzzleDetect.Opacity=1.0;
            }
            btnSavePuzzleDetect.IsHitTestVisible = !(bool)chbAutoSave.IsChecked;
        }

        private void tabSudokuCreate_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if(tabSudokuCreate==null)  return;
            TabItem tb=tabSudokuCreate.SelectedItem as TabItem;
            if(tb==null)  return;
            if((string)tb.Name=="WebCam") _ThreadActivateSub();
            else                          cameraCancel=true;
        }

        private void btnCameraActivate_Click(object sender, RoutedEventArgs e){
            _ThreadActivateSub();
        }


     //============================= Thread #0 --> on anather Thread #1 ============================= 
        private void _ThreadActivateSub(){ 
            if(threadCC>1){
                this.Dispatcher.Invoke((Action)(() => cameraMessageBox.Content=$"Camera thread is running. ({threadCC})" ));
                return;
            }
            camID = -1;
            Thread.Sleep(400);
            var selP=rdbVideoCameraLst.Find(p=>(bool)p.IsChecked);
            camID = int.Parse((string)selP.Content);
            stringCapType = (string)captureType.SelectedValue;
            FlipModeX = (bool)chbXaxis.IsChecked;
            FlipModeY = (bool)chbYaxis.IsChecked;

            ThreadPool.QueueUserWorkItem(this.cameraCapture); /* Thread #1 */
            cameraCancel=false;
            AnalyzerLap.Reset();
            AnalyzerLap.Stop();
        }
        private void cameraCapture( object state ){
            threadCC++;
            try{
                if(camID<0)  goto threadEnd;
                                WriteLine($"camID:{camID}");

                var sp = stringCapType.Split(' ');
                var eLst = sp[2].Split('x');
                int camW = int.Parse(eLst[0]);  // 2048,1600,640
                int camH = int.Parse(eLst[1]);  // 1530,1200,480
                WriteLine($"camID]{camID} size:({camW},{camH})");

                var camera=new VideoCapture(camID){ FrameWidth=camW, FrameHeight=camH, /*Fps=60*/ };
                if(camera==null)  goto threadEnd;

                AnalyzerLap.Reset();
                if(SDKRecgMan==null) SDKRecgMan = new sdkFrameRecgV3Man(this,fName:fName);
                
                using(camera){
                    while(true){
                        if(cameraCancel)  goto threadEnd;

                        this.Dispatcher.Invoke(() => {
                            FlipModeX = (bool)chbXaxis.IsChecked;
                            FlipModeY = (bool)chbYaxis.IsChecked;
                        });

                        var img = new Mat();
                        camera.Read(img); 
                        if(img.Empty()){ Thread.Sleep(100); goto nextTry;}
                                    
                        if(FlipModeX) img = img.Flip(FlipMode.X);
                        if(FlipModeY) img = img.Flip(FlipMode.Y);
                        Frame00 = img;

                        this.Dispatcher.Invoke(() => { this.webCamera.Source=img.ToWriteableBitmap(); } );
                        this.Dispatcher.Invoke((Action)(() => lblMLRecogLap.Content = AnalyzerLapElaped ));
                        
                                //WriteLine( $"***** cameraTimer_Tick DigRegMode:{DigRegMode} *****");
                        if(DigRegMode!="DigRecogTry"){
                            this.Dispatcher.Invoke((Action)(() => bdrCamera.BorderBrush=Brushes.Blue ));
                        }
                nextTry:
                        Thread.Sleep(20);
                        continue;
                    }
                }
            }
            catch( Exception e ){
                WriteLine( e.Message+"\r"+e.StackTrace);
                this.Dispatcher.Invoke((Action)(() => cameraMessageBox.Content="Recognition failed" ));

            }
          threadEnd:
            threadCC--;
        }

     //============================= Thread #0 --> on anather Thread #2 =============================
        private void btnRecog_Click(object sender, RoutedEventArgs e){
            if((string)btnSudokuDetect.Content=="Detect"){
                SDK81 = null;
                btnSudokuDetect.Content="Stop";
                GNP00.GSmode = "DigRecog"; 
                DigRegMode = "DigRecogTry";
                pGP_DgtRecog = new UPuzzle();
                bdrCamera.BorderBrush=Brushes.Orange;
                cameraMessageBox.Content="Reading";

                if(SDKRecgMan==null) SDKRecgMan = new sdkFrameRecgV3Man(this,fName:fName);   //MLtype,MidLSize described in parameter file

                tokSrc = new CancellationTokenSource();　//procedures for suspension 
                taskSDK = new Task( ()=> SDKRecgMan.DigitRecogMlt(tokSrc.Token), tokSrc.Token );    /* Thread #2 */
                taskSDK.ContinueWith( t=> DigitsRecogComplated() ); //procedures for completion
                taskSDK.Start();

                AnalyzerLap.Reset();
                AnalyzerLap.Start();
            }
            else{
                btnSudokuDetect.Content="Detect";
              //tokSrc.Cancel();
                try{ taskSDK.Wait(); }
                catch(AggregateException){ DigRegMode="DigRecogCancel"; }
            }
        }

        public void DigitsRecogReport( object sender, SDKEventArgs e ){ //on Thread #2
            try{ 
                if(e.SDK81==null) return;
                DigitsRecog_ReportPuzzle( this, new SDKSolutionEventArgs(e.SDK81,succeedB:false) );
            }
            catch(Exception e2){ WriteLine( e2.Message+"\r"+e2.StackTrace); }
        }
        private void DigitsRecogComplated( ){    //on Thread #2
            try{
                DigitsRecog_ReportPuzzle( this, new SDKSolutionEventArgs(SDK81,succeedB:true) );
            }
            catch(Exception e ){ WriteLine( e.Message+"\r"+e.StackTrace); }
        } 

        private void DigitsRecog_ReportPuzzle(object sender, SDKSolutionEventArgs ex ){
            SDK81=ex.SDK81;
            if(pGP_DgtRecog!=null) pGP_DgtRecog.SetNo_fromIntArray(SDK81);
            else WriteLine();
            
            GNP00.GNPX_Eng.pGP=pGP_DgtRecog;
            displayTimer.Start();

            if(ex.succeedB){
                this.Dispatcher.Invoke((Action)(() => {
                    btnSudokuDetect.Content="Detect";
                    bool AutoSave=(bool)chbAutoSave.IsChecked;
                    UPuzzle GPML=GNP00.SDK_Puzzle_SetIndexAndSave(ex.SDK81,saveF:AutoSave);
                    if(AutoSave){
                        btnProbPre.IsEnabled = (GPML.ID>=1);
                        btnProbNxt.IsEnabled = (GPML.ID<GNP00.SDKProbLst.Count-1);
                        GNP00.CurrentPrbNo = 999999999;
                    }
                } ));
                
                DigRegMode="DigRecogCmplate"; 
                AnalyzerLap.Stop();
            }
            else{
                DigRegMode="DigRecogTry";
            }
                    //WriteLine( $"***** {DigRegMode} *****");
        }
     //-----------------------------------------------------------------------------

        private void btnSavePuzzleDetect_Click(object sender, RoutedEventArgs e) {
            UPuzzle GPML=GNP00.SDK_Puzzle_SetIndexAndSave(SDK81,saveF:true);
            btnProbPre.IsEnabled = (GPML.ID>=1);
            btnProbNxt.IsEnabled = (GPML.ID<GNP00.SDKProbLst.Count-1); 
            GNP00.CurrentPrbNo=999999999;
            __DispMode="DeletePuzzle";
            displayTimer.Start();
        }    

        private void btnDeletePuzzle_Click(object sender, RoutedEventArgs e){
            var PDel = GNP00.GetCurrentProble();
            if(PDel!=null) GNP00.SDK_RemovePuzzle(PDel);
            if(GNP00.SDKProbLst==null || GNP00.SDKProbLst.Count<=0){ 
                SDK81 = Enumerable.Repeat(0,81).ToArray();
                DigitsRecog_ReportPuzzle( this, new SDKSolutionEventArgs(SDK81,succeedB:false) );
            }
            __DispMode="DeletePuzzle";
            displayTimer.Start();
        }
     }
}