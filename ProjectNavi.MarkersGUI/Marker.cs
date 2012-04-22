using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.MarkersGUI
{
    public class MarkerLine
    {
        private int[][] lineCode = new int[4][] {   
                                                new int[5] { 1, 0, 0, 0, 0 }, 
                                                new int[5] { 1, 0, 1, 1, 1 }, 
                                                new int[5] { 0, 1, 0, 0, 1 }, 
                                                new int[5] { 0, 1, 1, 1, 0 } 
                                                };
        private int[] lineBits;
        private int value;


        public int Value { get { return value; } }
        public int[] LineBits { get { return lineBits; } }

        public void EncodeLine(int value)
        {
            if (value >= lineCode.Length)
                return;
            this.value = value;
            lineBits = lineCode[value];
        }

    }
    public class Marker
    {
        private MarkerLine[] lines = new MarkerLine[5] { new MarkerLine(), new MarkerLine(), new MarkerLine(), new MarkerLine(), new MarkerLine() };

        public MarkerLine[] Lines { get { return lines; } }

        public void EncodeValue(int value)
        {
            // 0&0=0 0&1=0 1&0=0 1&1=1
            int res = value; // 0111001001 &00000000011 = 0000000001 

            for (int index = lines.Length - 1; index >= 0; index--)
            {
                //800- 0111001
                lines[index].EncodeLine(res & 3);
                res = res >> 2;
            }
        }

    }
}
