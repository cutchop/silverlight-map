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

namespace ExsunSilverlightMap
{
    public class MapMark
    {
        private Microsoft.Maps.MapControl.Pushpin _pushpin;
        /// <summary>
        /// 地图上显示的图标
        /// </summary>
        public Microsoft.Maps.MapControl.Pushpin Pushpin
        {
            get { return _pushpin; }
            set { _pushpin = value; }
        }

        private Microsoft.Maps.MapControl.MapPolyline _locus;
        /// <summary>
        /// 轨迹线
        /// </summary>
        public Microsoft.Maps.MapControl.MapPolyline Locus
        {
            get { return _locus; }
            set { _locus = value; }
        }

        private Border _tag;
        /// <summary>
        /// 名称/标记
        /// </summary>
        public Border Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }
    }
}
