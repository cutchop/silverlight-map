using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Maps.MapControl;
using System.IO;
using System.Json;
using System.Collections.Generic;

namespace ExsunSilverlightMap
{

    public class Common
    {
        /// <summary>
        /// 根据两点经纬度算距离（默认采用WGS84）
        /// </summary>
        /// <param name="lng1">第一点经度</param>
        /// <param name="lat1">第一点纬度</param>
        /// <param name="lng2">第二点经度</param>
        /// <param name="lat2">第二点纬度</param>
        /// <returns>米</returns>
        public static double DistanceOfTwoPoints(double lng1, double lat1, double lng2, double lat2)
        {
            return DistanceOfTwoPoints(lng1, lat1, lng2, lat2, GaussSphere.WGS84);
        }

        /// <summary>
        /// 根据两点经纬度算距离
        /// </summary>
        /// <param name="lng1">第一点经度</param>
        /// <param name="lat1">第一点纬度</param>
        /// <param name="lng2">第二点经度</param>
        /// <param name="lat2">第二点纬度</param>
        /// <param name="gs">高斯球</param>
        /// <returns>米</returns>
        public static double DistanceOfTwoPoints(double lng1, double lat1, double lng2, double lat2, GaussSphere gs)
        {
            double radLat1 = ToRadian(lat1);
            double radLat2 = ToRadian(lat2);
            double a = radLat1 - radLat2;
            double b = ToRadian(lng1) - ToRadian(lng2);
            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) +
             Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2)));
            s = s * (gs == GaussSphere.WGS84 ? 6378137.0 : (gs == GaussSphere.Xian80 ? 6378140.0 : 6378245.0));
            s = Math.Round(s * 10000) / 10000;
            return s;
        }

        /// <summary>
        /// 根据lc中的点计算总长度
        /// </summary>
        /// <param name="lc">坐标点集合</param>
        /// <returns>公里数</returns>
        public static double DistanceOfTwoPoints(LocationCollection lc)
        {
            double s = 0;
            for (int i = 1; i < lc.Count; i++)
            {
                s += DistanceOfTwoPoints(lc[i - 1].Longitude, lc[i - 1].Latitude, lc[i].Longitude, lc[i].Latitude);
            }
            return Math.Round(s / 1000, 2);
        }

        /// <summary>
        /// 根据lc中的点计算总长度
        /// </summary>
        /// <param name="lc">坐标点集合</param>
        /// <returns></returns>
        public static string DistanceOfTwoPointsStr(LocationCollection lc)
        {
            double s = 0;
            for (int i = 1; i < lc.Count; i++)
            {
                s += DistanceOfTwoPoints(lc[i - 1].Longitude, lc[i - 1].Latitude, lc[i].Longitude, lc[i].Latitude);
            }
            string ret = "";
            if (s >= 1000)
            {
                ret = Math.Round(s / 1000, 2) + "公里";
            }
            else
            {
                ret = Math.Round(s, 2) + "米";
            }
            return ret;
        }
        
        /// <summary>
        /// 计算多边形面积
        /// </summary>
        /// <param name="lc">多边形顶点集合</param>
        /// <returns></returns>
        public static string CalcArea(LocationCollection lc)
        {
            #region [ 否定算法,范围过小时出现负值 ]
            //int Count = lc.Count;
            //if (Count < 3)
            //{
            //    return "0平方公里";
            //}
            //if ((lc[0].Latitude != lc[Count - 1].Latitude) || (lc[0].Longitude != lc[Count - 1].Longitude))
            //{
            //    lc.Add(new Location(lc[0].Latitude, lc[0].Longitude));
            //}
            //double LowX = 0.0;
            //double LowY = 0.0;
            //double MiddleX = 0.0;
            //double MiddleY = 0.0;
            //double HighX = 0.0;
            //double HighY = 0.0;

            //double AM = 0.0;
            //double BM = 0.0;
            //double CM = 0.0;

            //double AL = 0.0;
            //double BL = 0.0;
            //double CL = 0.0;

            //double AH = 0.0;
            //double BH = 0.0;
            //double CH = 0.0;

            //double CoefficientL = 0.0;
            //double CoefficientH = 0.0;

            //double ALtangent = 0.0;
            //double BLtangent = 0.0;
            //double CLtangent = 0.0;

            //double AHtangent = 0.0;
            //double BHtangent = 0.0;
            //double CHtangent = 0.0;

            //double ANormalLine = 0.0;
            //double BNormalLine = 0.0;
            //double CNormalLine = 0.0;

            //double OrientationValue = 0.0;

            //double AngleCos = 0.0;

            //double Sum1 = 0.0;
            //double Sum2 = 0.0;
            //double Count2 = 0;
            //double Count1 = 0;


            //double Sum = 0.0;
            //double Radius = 6378137.0;

            //for (int i = 0; i < Count; i++)
            //{
            //    if (i == 0)
            //    {
            //        LowX = lc[Count - 1].Latitude * Math.PI / 180;
            //        LowY = lc[Count - 1].Longitude * Math.PI / 180;
            //        MiddleX = lc[0].Latitude * Math.PI / 180;
            //        MiddleY = lc[0].Longitude * Math.PI / 180;
            //        HighX = lc[1].Latitude * Math.PI / 180;
            //        HighY = lc[1].Longitude * Math.PI / 180;
            //    }
            //    else if (i == Count - 1)
            //    {
            //        LowX = lc[Count - 2].Latitude * Math.PI / 180;
            //        LowY = lc[Count - 2].Longitude * Math.PI / 180;
            //        MiddleX = lc[Count - 1].Latitude * Math.PI / 180;
            //        MiddleY = lc[Count - 1].Longitude * Math.PI / 180;
            //        HighX = lc[0].Latitude * Math.PI / 180;
            //        HighY = lc[0].Longitude * Math.PI / 180;
            //    }
            //    else
            //    {
            //        LowX = lc[i - 1].Latitude * Math.PI / 180;
            //        LowY = lc[i - 1].Longitude * Math.PI / 180;
            //        MiddleX = lc[i].Latitude * Math.PI / 180;
            //        MiddleY = lc[i].Longitude * Math.PI / 180;
            //        HighX = lc[i + 1].Latitude * Math.PI / 180;
            //        HighY = lc[i + 1].Longitude * Math.PI / 180;
            //    }

            //    AM = Math.Cos(MiddleY) * Math.Cos(MiddleX);
            //    BM = Math.Cos(MiddleY) * Math.Sin(MiddleX);
            //    CM = Math.Sin(MiddleY);
            //    AL = Math.Cos(LowY) * Math.Cos(LowX);
            //    BL = Math.Cos(LowY) * Math.Sin(LowX);
            //    CL = Math.Sin(LowY);
            //    AH = Math.Cos(HighY) * Math.Cos(HighX);
            //    BH = Math.Cos(HighY) * Math.Sin(HighX);
            //    CH = Math.Sin(HighY);


            //    CoefficientL = (AM * AM + BM * BM + CM * CM) / (AM * AL + BM * BL + CM * CL);
            //    CoefficientH = (AM * AM + BM * BM + CM * CM) / (AM * AH + BM * BH + CM * CH);

            //    ALtangent = CoefficientL * AL - AM;
            //    BLtangent = CoefficientL * BL - BM;
            //    CLtangent = CoefficientL * CL - CM;
            //    AHtangent = CoefficientH * AH - AM;
            //    BHtangent = CoefficientH * BH - BM;
            //    CHtangent = CoefficientH * CH - CM;


            //    AngleCos = (AHtangent * ALtangent + BHtangent * BLtangent + CHtangent * CLtangent) / (Math.Sqrt(AHtangent * AHtangent + BHtangent * BHtangent + CHtangent * CHtangent) * Math.Sqrt(ALtangent * ALtangent + BLtangent * BLtangent + CLtangent * CLtangent));

            //    AngleCos = Math.Acos(AngleCos);

            //    ANormalLine = BHtangent * CLtangent - CHtangent * BLtangent;
            //    BNormalLine = 0 - (AHtangent * CLtangent - CHtangent * ALtangent);
            //    CNormalLine = AHtangent * BLtangent - BHtangent * ALtangent;

            //    if (AM != 0)
            //    {
            //        OrientationValue = ANormalLine / AM;
            //    }
            //    else if (BM != 0)
            //    {
            //        OrientationValue = BNormalLine / BM;
            //    }
            //    else
            //    {
            //        OrientationValue = CNormalLine / CM;
            //    }

            //    if (OrientationValue > 0)
            //    {
            //        Sum1 += AngleCos;
            //        Count1++;
            //    }
            //    else
            //    {
            //        Sum2 += AngleCos;
            //        Count2++;
            //    }

            //}

            //if (Sum1 > Sum2)
            //{
            //    Sum = Sum1 + (2 * Math.PI * Count2 - Sum2);
            //}
            //else
            //{
            //    Sum = (2 * Math.PI * Count1 - Sum1) + Sum2;
            //}
            //double area = (Sum - (Count - 2) * Math.PI) * Radius * Radius;
            //string ret = "";
            //if (area > 10000)
            //{
            //    ret = Math.Round(area / 500000, 2) + "平方公里";
            //}
            //else
            //{
            //    ret = Math.Round(area * 2, 2) + "平方米";
            //}
            //return ret;
            #endregion

            List<Point> lp = new List<Point>();
            foreach (Location l in lc)
            {
                lp.Add(new Point(l.Latitude, l.Longitude));
            }
            double area = GetAreaOfPolyGon(lp) * 10000;
            if (area < 0.01)
            {
                return Math.Round(area * 1000000, 2) + "平方米";
            }
            else
            {
                return Math.Round(area, 2) + "平方公里";
            }
        }

        private static double GetAreaOfPolyGon(List<Point> points)
        {
            double area = 0;
            if (points.Count < 3)
            {
                return 0;
            }
            Point p1 = points[0];
            for (int i = 1; i < points.Count - 1; i++)
            {
                Point p2 = points[i];
                Point p3 = points[i + 1];
                //构造向量
                Point vecP1P2 = new Point(p2.X - p1.X, p2.Y - p1.Y);
                Point vecP2P3 = new Point(p3.X - p2.X, p3.Y - p2.Y);
                double vecMult = vecP1P2.X * vecP2P3.Y - vecP1P2.Y * vecP2P3.X;//用于判断顺时针还是逆时针
                int sign = 0;
                if (vecMult > 0)
                {
                    sign = 1;
                }
                else if (vecMult < 0)
                {
                    sign = -1;
                }
                double triArea = GetAreaOfTriangle(p1, p2, p3) * sign;
                area += triArea;
            }
            return Math.Abs(area);
        }

        private static double GetAreaOfTriangle(Point p1, Point p2, Point p3)
        {
            double area = 0;
            double p1p2 = GetLineLength(p1, p2);
            double p2p3 = GetLineLength(p2, p3);
            double p3p1 = GetLineLength(p3, p1);
            double s = (p1p2 + p2p3 + p3p1) / 2;
            area = s * (s - p1p2) * (s - p2p3) * (s - p3p1);
            area = Math.Sqrt(area);
            return area;
        }

        private static double GetLineLength(Point p1, Point p2)
        {
            double length;
            length = Convert.ToDouble((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
            length = Math.Sqrt(length);
            return length;
        }

        /// <summary>
        /// 获取JsonArray对象
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>JsonArray</returns>
        public static JsonArray ConvertToJson(string str)
        {
            try
            {
                Stream responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(str));
                return (JsonArray)JsonArray.Load(responseStream);
            }
            catch
            {
                return null;
            }
        }

        public static double ToRadian(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public static double ToDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }

        public static LocationCollection CreateCircle(Location center, double radius)
        {
            var earthRadius = 6371;
            var lat = ToRadian(center.Latitude); //radians
            var lng = ToRadian(center.Longitude); //radians
            var d = radius / earthRadius; // d = angular distance covered on earth's surface
            var locations = new LocationCollection();

            for (var x = 0; x <= 360; x++)
            {
                var brng = ToRadian(x);
                var latRadians = Math.Asin(Math.Sin(lat) * Math.Cos(d) + Math.Cos(lat) * Math.Sin(d) * Math.Cos(brng));
                var lngRadians = lng + Math.Atan2(Math.Sin(brng) * Math.Sin(d) * Math.Cos(lat), Math.Cos(d) - Math.Sin(lat) * Math.Sin(latRadians));

                locations.Add(new Location(ToDegrees(latRadians), ToDegrees(lngRadians)));
            }

            return locations;
        }
    }

    /// <summary>
    /// 高斯投影中所选用的参考椭球
    /// </summary>
    public enum GaussSphere
    {
        Beijing54,
        Xian80,
        WGS84
    }
    public enum DistanceMeasure
    {
        Miles,
        Kilometers
    }

    public enum MapDrawType
    {
        NONE = 0,
        POLYGON = 1,
        POLYLINE = 2,
        RECTANGLE = 3,
        MEASURELENGTH = 4,
        MEASUREAREA = 5,
        FRAMEZOOMIN = 6,
        POINT = 7,
        CIRCLE = 8
    }
}
