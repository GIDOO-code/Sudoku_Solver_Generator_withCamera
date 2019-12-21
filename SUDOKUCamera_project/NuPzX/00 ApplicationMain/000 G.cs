using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using OpenCvSharp;

namespace GIDOOCV{
    public static class G{
        static public char[] _sep={' ',',','\t'};
        static public char[] _sepC={','};
        static public string _backupDir="backupDir";

        static public Dictionary<string,Color> ColorDic=new Dictionary<string,Color>();  
        static public Scalar[] colorLst={ Scalar.Red,  Scalar.Blue, Scalar.Green,  Scalar.Yellow, Scalar.Purple, Scalar.RoyalBlue };

        static public double pixelsPerDip;
        static public int    cellSize = 36;
        static public int    cellSizeP;
        static public int    lineWidth = 1;
//        static public GFont            gsFont = new GFont( "Times New Romaon", 22 );    

        static G(){ 
            ColorDic=new Dictionary<string,Color>();
            ColorDic["Board"]        = Color.FromArgb(255,220,220,220);
            ColorDic["BoardLine"]    = Colors.Navy;

            ColorDic["CellForeNo"]   = Colors.Navy;
            ColorDic["CellBkgdPNo"]  = Color.FromArgb(255,160,160,160);
            ColorDic["CellBkgdMNo"]  = Color.FromArgb(255,190,190,200);
            ColorDic["CellBkgdZNo"]  = Colors.White;
            ColorDic["CellBkgdZNo2"] = Color.FromArgb(255,150,150,250);

            ColorDic["CellBkgdFix"]  = Colors.LightGreen;
            ColorDic["CellFixed"]    = Colors.Red;
        }
    }
}
