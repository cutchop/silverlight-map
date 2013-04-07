using System;
using Microsoft.Maps.MapControl;
using Microsoft.Maps.MapControl.Core;

namespace ExsunSilverlightMap
{
    public class ExsunMode : MercatorMode
    {
        /// <summary>
        /// 缩放范围
        /// </summary>
        public Range<double> MapZoomRange = new Range<double>(1.0, 18.0);

        /// <summary>
        /// 确定地图深度缩放范围
        /// </summary>
        /// <param name="center"></param>
        /// <returns></returns>
        protected override Range<double> GetZoomRange(Location center)
        {
            return this.MapZoomRange;
        }
    }
}
