using System;
using Microsoft.Maps.MapControl;

namespace ExsunSilverlightMap
{
    /// <summary>
    /// 依迅地图
    /// </summary>
    public class ExsunTileSource : TileSource
    {
        public ExsunTileSource()
            : base("http://221.235.53.39:8008/r{quadkey}.png")
        { }
    }
    /// <summary>
    /// google卫星图
    /// </summary>
    public class GoogleAerialTileSource : TileSource
    {
        public GoogleAerialTileSource()
            : base("http://mt{0}.google.cn/vt/lyrs=s@84&gl=cn&x={1}&y={2}&z={3}")
        { }

        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            return new Uri(string.Format(this.UriFormat, x % 4, x, y, zoomLevel));
        }
    }
    /// <summary>
    /// google卫星图地理信息
    /// </summary>
    public class GoogleAerialTileSource2 : TileSource
    {
        public GoogleAerialTileSource2()
            : base("http://mt{0}.google.cn/vt/imgtp=png32&lyrs=h@146000000&hl=zh-CN&gl=cn&x={1}&y={2}&z={3}&s=Galileo")
        { }

        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            return new Uri(string.Format(this.UriFormat, x % 4, x, y, zoomLevel));
        }
    }
    /// <summary>
    /// google地图
    /// </summary>
    public class GoogleRoadTileSource : TileSource
    {
        public GoogleRoadTileSource()
            : base("http://mt{0}.google.cn/vt/lyrs=m@146&hl=zh-CN&gl=cn&x={1}&y={2}&z={3}")
        { }

        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            return new Uri(string.Format(this.UriFormat, x % 4, x, y, zoomLevel));
        }
    }
    /// <summary>
    /// mapabc地图
    /// </summary>
    public class MapabcTileSource : TileSource
    {
        public MapabcTileSource()
            : base("http://221.235.53.39:8008/mapimg.aspx?t=mapabc&x={0}&y={1}&z={2}")
        { }

        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            return new Uri(string.Format(this.UriFormat, x, y, zoomLevel));
        }
    }
}
