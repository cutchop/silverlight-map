using System;
using Microsoft.Maps.MapControl;
using Microsoft.Maps.MapControl.Core;

namespace ExsunSilverlightMap
{
    public class ChinaMode : MercatorMode
    {
        /// <summary>
        /// 纬度范围
        /// </summary>
        public Range<double> LatitudeRange = new Range<double>(-50.0, 50.0);
        /// <summary>
        /// 经度范围
        /// </summary>
        public Range<double> LongitudeRange = new Range<double>(70.0, 140.0);
        /// <summary>
        /// 缩放范围
        /// </summary>
        public Range<double> MapZoomRange = new Range<double>(4.0, 16.0);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="center">地图中心地理坐标</param>
        /// <param name="zoomLevel">地图缩放级别</param>
        /// <param name="heading">定向视图标题</param>
        /// <param name="pitch"></param>
        /// <returns>是否有值更改</returns>
        public override bool ConstrainView(Location center, ref double zoomLevel, ref double heading, ref double pitch)
        {
            return base.ConstrainView(center, ref zoomLevel, ref heading, ref pitch);
        }

        /// <summary>
        /// 确定地图深度缩放范围
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        protected override Range<double> GetZoomRange(Location center)
        {
            return this.MapZoomRange;
        }

        /// <summary>
        /// 重写鼠标中键滚轮操作行为
        /// </summary>
        /// <param name="e"></param>
        public override void OnMouseWheel(MapMouseWheelEventArgs e)
        {
            if ((e.WheelDelta > 0.0) && (this.ZoomLevel >= this.MapZoomRange.To))
            {
                e.Handled = true;
            }
            else
            {
                base.OnMouseWheel(e);
            }
        }

        /// <summary>
        /// 重写鼠标拖放地图行为
        /// </summary>
        /// <param name="e"></param>
        public override void OnMouseDragBox(MapMouseDragEventArgs e)
        {
            if (((this.TargetBoundingRectangle.East <= this.LongitudeRange.To)
                && (this.TargetBoundingRectangle.West >= this.LongitudeRange.From)))
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
    }
}
