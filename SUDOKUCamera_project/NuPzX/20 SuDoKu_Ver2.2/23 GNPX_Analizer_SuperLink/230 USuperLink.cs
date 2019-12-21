using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static System.Console;

using GIDOO_space;

namespace GNPZ_sdk {
    public class USuperLink{
        public int rc0;
        public int no0;
        public Bit81[]      Qtrue;
        public Bit81[]      Qfalse;
        public object[,]    chainDesLK;
        public object[,]    chainDesLKT; //�� 
        public object[,]    chainDesLKF; //��
		public bool			SolFound;
		public string       stMsg;
        public GroupedLink  resultGLK;
        public int          contDiscontF;

        public USuperLink( int rc0, int no0 ){
            this.rc0=rc0; this.no0=no0;
            Qtrue=new Bit81[9];
            Qfalse=new Bit81[9];
            for(int k=0; k<9; k++ ){ Qtrue[k]=new Bit81();  Qfalse[k]=new Bit81(); }
            chainDesLK=new object[81,9];
            chainDesLKT=new object[81,9]; //�� 
            chainDesLKF=new object[81,9]; //��
            resultGLK=null;
        }
    }
}