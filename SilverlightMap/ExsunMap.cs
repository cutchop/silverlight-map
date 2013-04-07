using System;
using Microsoft.Maps.MapControl;
using Microsoft.Maps.MapControl.Navigation;

namespace ExsunSilverlightMap
{
    public class ExsunMap : Map
    {
        public ExsunMap()
            : base()
        {
            base.LoadingError += (sender, e) =>
            {
                //移除错误提示(Invalid Credentials.Sign up for a developer account at:http://www.microsoft.com/maps/developers)
                base.RootLayer.Children.RemoveAt(5);
            };

            base.MapForeground.TemplateApplied += delegate(object sender, EventArgs args)
            {
                base.MapForeground.NavigationBar.TemplateApplied += delegate(object obj, EventArgs e)
                {
                    NavigationBar navBar = base.MapForeground.NavigationBar;
                    //navBar.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(Convert.ToByte(255), Convert.ToByte(36), Convert.ToByte(58), Convert.ToByte(118)));
                    navBar.HorizontalPanel.Children.Clear();
                    //navBar.VerticalPanel.Children.Clear();
                    
                    CommandToggleButton btnRoad = new CommandToggleButton(new CustomCommand("GoogleRoad"), "地图", "点击切换到地图");
                    btnRoad.IsChecked = false;
                    navBar.HorizontalPanel.Children.Add(btnRoad);

                    CommandToggleButton btnAerial = new CommandToggleButton(new CustomCommand("GoogleAerial"), "卫星", "点击切换到卫星图");
                    btnAerial.IsChecked = false;
                    navBar.HorizontalPanel.Children.Add(btnAerial);

                    //navBar.HorizontalPanel.Children.Add(new CommandSeparator());

                    //CommandToggleButton btnMapabc = new CommandToggleButton(new CustomCommand("Mapabc"), "Mapabc地图", "点击切换Mapabc地图");
                    //btnMapabc.IsChecked = false;
                    //navBar.HorizontalPanel.Children.Add(btnMapabc);
                };
            };
        }
    }
}
