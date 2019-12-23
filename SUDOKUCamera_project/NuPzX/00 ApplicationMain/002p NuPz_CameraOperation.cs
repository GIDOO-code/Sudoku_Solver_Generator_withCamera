using System;
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
        static public  string   DigRegMode;
        private int             camID=-1;
        private string          stringCapType;
        private bool            cameraCancel;
        private bool            FlipModeX;
        private bool            FlipModeY;

        public  Mat             Frame00=new Mat();
        public  UPuzzle         pGP_DgtRecog=null;
        static  public int[]    SDK81;
        private WriteableBitmap w1 = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);   //PixelFormats.Gray8
        private string          fName="_LMparameter.txt";
        private List<string>    captureTypeList=new List<string>{"3.1MP 4:3 2048x1530", "1.9MP 4:3 1600x1200", "0.3MP 4:3 640x480" };

        public sdkFrameRecgV3Man SDKRecgMan=null;

    #region Camera opperation        
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
        private void _ThreadActivateSub(){ 
            var selP=rdbVideoCameraLst.Find(p=>(bool)p.IsChecked);
            camID = int.Parse((string)selP.Content);
            stringCapType = (string)captureType.SelectedValue;
          //FlipModeX = (bool)chbXaxis.IsChecked;
          //FlipModeY = (bool)chbYaxis.IsChecked;

            ThreadPool.QueueUserWorkItem(this.cameraCapture);
            cameraCancel=false;
        }

        private void cameraCapture( object state ){
            try{
                if(camID<0)  return;
                                WriteLine($"camID:{camID}");

                var sp = stringCapType.Split(' ');
                var eLst = sp[2].Split('x');
                int camW = int.Parse(eLst[0]);  // 2048,1600,640
                int camH = int.Parse(eLst[1]);  // 1530,1200,480
                WriteLine($"camID]{camID} size:({camW},{camH})");

                var camera=new VideoCapture(camID){ FrameWidth=camW, FrameHeight=camH, /*Fps=60*/ };
                if(camera==null)  return;

                AnalyzerLap.Start();
                if(SDKRecgMan==null) SDKRecgMan = new sdkFrameRecgV3Man(this,fName:fName);
                
                using(var img = new Mat())
                using(camera){
                    while(true){
                        if(cameraCancel)  return;
                        camera.Read(img); 
                        if(img.Empty()){ Thread.Sleep(100); goto nextTry;}

                        this.Dispatcher.Invoke(() => {
                                FlipModeX = (bool)chbXaxis.IsChecked;
                                FlipModeY = (bool)chbYaxis.IsChecked;
                        });

                        Frame00 = img;
                        if(FlipModeX) Frame00 = Frame00.Flip(FlipMode.X);
                        if(FlipModeY) Frame00 = Frame00.Flip(FlipMode.Y);

                        this.Dispatcher.Invoke(() => { this.webCamera.Source=Frame00.ToWriteableBitmap(); } );
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
        }
    #endregion Camera opperation

    #region Recognition progress display    
        private void btnRecog_Click(object sender, RoutedEventArgs e){
            if((string)btnSudokuDetection.Content=="Detection"){
                SDK81 = null;
                btnSudokuDetection.Content="Stop";
                DigRegMode = "DigRecogTry";
                pGP_DgtRecog = new UPuzzle();
                bdrCamera.BorderBrush=Brushes.Orange;
                cameraMessageBox.Content="Reading";

                if(SDKRecgMan==null) SDKRecgMan = new sdkFrameRecgV3Man(this,fName:fName);   //MLtype,MidLSize described in parameter file

                tokSrc = new CancellationTokenSource();　//procedures for suspension 
                taskSDK = new Task( ()=> SDKRecgMan.DigitRecogMlt(tokSrc.Token), tokSrc.Token );
                taskSDK.ContinueWith( t=> DigitsRecogComplated() ); //procedures for completion
                taskSDK.Start();

                AnalyzerLap.Reset();
                AnalyzerLap.Start();
            }
            else{
                btnSudokuDetection.Content="Detection";
              //tokSrc.Cancel();
                try{ taskSDK.Wait(); }
                catch(AggregateException){ DigRegMode="DigRecogCancel"; }
            }

        }
        public void DigitsRecogReport( object sender, SDKEventArgs e ){ 
            try{ 
                if(e.SDK81==null) return;
                SDK81=e.SDK81;
                if(pGP_DgtRecog!=null) pGP_DgtRecog.SetNo_fromIntArray(SDK81);
                else WriteLine();

                GNP00.GNPX_Eng.pGP=pGP_DgtRecog;
                displayTimer.Start();
                //WriteLine( $"***** DigitsRecogReport *****");
            }
            catch(Exception e2){ WriteLine( e2.Message+"\r"+e2.StackTrace); }
        }

        private void btnSavePuzzleDetect_Click(object sender, RoutedEventArgs e) {
            __SavePuzzle();
        }
        private void DigitsRecogComplated( ){
            try{
                this.Dispatcher.Invoke((Action)(() => btnSudokuDetection.Content="Detection" ));
                DigRegMode="DigRecogCmplate"; 
                AnalyzerLap.Stop();

                //***** (auto)save *****
                bool AutoSave=false;
                this.Dispatcher.Invoke(() => AutoSave=(bool)chbAutoSave.IsChecked );
                if(AutoSave)  __SavePuzzle();

                GNP00.GSmode = "RecogComplated";
                        //WriteLine( $"***** DigitsRecogComplated *****");
            }
            catch(Exception e ){ WriteLine( e.Message+"\r"+e.StackTrace); }
        }

        private void __SavePuzzle(){
            this.Dispatcher.Invoke((Action)(() =>{
                UPuzzle GPML=GNP00.SDK_ToUPuzzle(SDK81,saveF:true);
                btnProbPre.IsEnabled = (GPML.ID>=1);
                btnProbNxt.IsEnabled = (GPML.ID<GNP00.SDKProbLst.Count-1); 
                GNP00.CurrentPrbNo=999999999;
            } ));
        }
    #endregion
    }
}