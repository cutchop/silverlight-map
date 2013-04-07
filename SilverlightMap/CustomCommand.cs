using System;
using Microsoft.Maps.MapControl;
using Microsoft.Maps.MapControl.Navigation;
using System.Windows;
using Microsoft.Maps.MapControl.Core;

namespace ExsunSilverlightMap
{
    public class CustomCommand : NavigationBarCommandBase
    {
        private string _mapMode;
        public string MapMode
        {
            get { return _mapMode; }
            set { _mapMode = value; }
        }
        public CustomCommand(string mapmode)
        {
            _mapMode = mapmode; 
        }

        public override void Execute(Microsoft.Maps.MapControl.Core.MapBase map)
        {
            if (_mapMode.ToLower() == "exsun")
            {
                (map.Children[0] as MapTileLayer).TileSources.Clear();
                (map.Children[0] as MapTileLayer).TileSources.Add(new ExsunTileSource());
                if (map.Mode is ExsunMode)
                {
                    map.Mode = new MercatorMode();
                }
            }
            else if (_mapMode.ToLower() == "googleaerial")
            {
                (map.Children[0] as MapTileLayer).TileSources.Clear();
                (map.Children[0] as MapTileLayer).TileSources.Add(new GoogleAerialTileSource());
                (map.Children[0] as MapTileLayer).TileSources.Add(new GoogleAerialTileSource2());
                if (map.Mode is ExsunMode)
                {
                    map.Mode = new MercatorMode();
                }
            }
            else if (_mapMode.ToLower() == "googleroad")
            {
                (map.Children[0] as MapTileLayer).TileSources.Clear();
                (map.Children[0] as MapTileLayer).TileSources.Add(new GoogleRoadTileSource());
                if (map.Mode is ExsunMode)
                {
                    map.Mode = new MercatorMode();
                }
            }
            else if (_mapMode.ToLower() == "mapabc")
            {
                (map.Children[0] as MapTileLayer).TileSources.Clear();
                (map.Children[0] as MapTileLayer).TileSources.Add(new MapabcTileSource());
                if (!(map.Mode is ExsunMode))
                {
                    map.Mode = new ExsunMode();
                }
            }
            //double longitude = 0d;
            //double latitude = 0d;

            ////根据指定地点的经度和纬度进行定位
            //if (CityName.Equals("武汉"))
            //{
            //    longitude = double.Parse("106.489384971208");
            //    latitude = double.Parse("29.5076372217973");
            //    map.ZoomLevel = 8;
            //}

            //map.Center = new Location(latitude, longitude);

            //NavigationBarCommandStatus status = this.GetStatus(map);
            //if (status == NavigationBarCommandStatus.Checked)
            //{
            //    map.ScaleVisibility = Visibility.Collapsed;
            //}
            //else if (status == NavigationBarCommandStatus.Normal)
            //{
            //    map.ScaleVisibility = Visibility.Visible;
            //}
        }

        public override NavigationBarCommandStatus GetStatus(Microsoft.Maps.MapControl.Core.MapBase map)
        {
            return base.GetStatus(map);
        }
    }
}
