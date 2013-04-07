/****************
 * Description: Silverlight地图控件                            
 * Creator:     伍凯 2011-03-03
 * Update:      2011-03-09 增加在地图上绘图的功能
 * Update:      2011-03-15 增加播放历史轨迹的功能
 ****************/
using System;
using System.IO;
using System.Text;
using System.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Browser;
using Microsoft.Maps.MapControl;
using Microsoft.Maps.MapControl.Core;
using Microsoft.Maps.MapControl.Navigation;
using Microsoft.Maps.MapControl.Overlays;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Collections.Generic;

namespace ExsunSilverlightMap
{
    public partial class MainPage : UserControl
    {
        #region [ 私有字段 ]
        private MapDrawType _drawtype = MapDrawType.NONE; //绘图类型
        private MapLayer _layer;        //绘图层
        private MapLayer _layerT;       //临时绘图层
        private MapLayer _layerM;       //遮罩层
        private LocationCollection _locations;//绘图点集合
        private LocationCollection _locationsT;//临时绘图点集合
        private LocationCollection _locationsH;//历史轨迹
        private string _drawEndCallback;   //绘图完毕之后调用的js方法名
        private string _hisplayCallback;   //绘图完毕之后调用的js方法名
        private PushPinPopup _ppp;       //标记弹出框
        private MapPolyline _hisMPL;    //历史轨迹 折线
        private MapPolyline _hisMPL2;    //历史轨迹 折线
        private Pushpin _hisPP;         //历史轨迹 标记
        private int _hisPlaySpeed = 5;      //历史轨迹回放速度
        private bool _paused;               //暂停播放
        private JsonArray _json;
        private Dictionary<string, MapMark> _dicVehicles;
        private Dictionary<string, MapMark> _dicInterestPoint;
        private Dictionary<string, MapMark> _dicInterestLine;
        private Dictionary<string, UIElement> _dicOther;
        #endregion

        #region [ 构造函数 ]
        public MainPage()
        {
            InitializeComponent();
            _dicVehicles = new Dictionary<string, MapMark>();
            _dicInterestPoint = new Dictionary<string, MapMark>();
            _dicInterestLine = new Dictionary<string, MapMark>();
            _dicOther = new Dictionary<string, UIElement>();
            this._locations = new LocationCollection();
            this._locationsT = new LocationCollection();
            this._locationsH = new LocationCollection();
            this._ppp = new PushPinPopup();
            this._ppp.Name = "popup";
            this._ppp.hisQuery += new PushPinPopup.HisQueryEventHandler(_ppp_hisQuery);

            //地图初始化设置
            map.Mode = new MercatorMode();
            map.ZoomLevel = 4;
            map.CopyrightVisibility = Visibility.Collapsed;
            map.LogoVisibility = Visibility.Collapsed;
            MapTileLayer tileLayer = new MapTileLayer();
            ExsunTileSource ets = new ExsunTileSource();
            tileLayer.TileSources.Add(ets);
            map.Children.Add(tileLayer);
            this._layer = new MapLayer();
            this._layerM = new MapLayer();
            this._layerM.Background = new System.Windows.Media.SolidColorBrush(Colors.Black);
            this._layerM.Opacity = 0.3;
            this._layerM.Visibility = System.Windows.Visibility.Collapsed;
            this._layerT = new MapLayer();
            map.Children.Add(_layer); //绘图层
            map.Children.Add(_layerM);//遮罩层
            map.Children.Add(_layerT);//临时绘图层
            
            //地图事件初始化
            map.MouseMove += new System.Windows.Input.MouseEventHandler(map_MouseMove);
            map.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(map_MouseLeftButtonDown);
            map.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(map_MouseLeftButtonUp);
            map.MousePan += new EventHandler<MapMouseDragEventArgs>(map_MousePan);
            map.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(map_MouseRightButtonDown);

            //html端注册
            HtmlPage.RegisterScriptableObject("map", this);

            //鼠标滚轮事件(解决非IE浏览器滚轮不能改变缩放级别的问题)
            HtmlPage.Window.AttachEvent("DOMMouseScroll", OnMouseWheel);    //Firefox
            HtmlPage.Window.AttachEvent("onmousewheel", OnMouseWheel);      //Chrome
            //HtmlPage.Document.AttachEvent("onmousewheel", OnMouseWheel);   //IE

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

        }
        #endregion

        #region [ 私有事件 ]
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            HtmlPage.Window.Invoke("maploaded");
        }
        //全屏
        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Host.Content.IsFullScreen = !Application.Current.Host.Content.IsFullScreen;
            ToolTipService.SetToolTip(btnFullScreen, Application.Current.Host.Content.IsFullScreen ? "退出全屏" : "全屏");
            Brush b = btnFullScreen.Background;
            btnFullScreen.Background = btnFullScreen.Foreground;
            btnFullScreen.Foreground = b;
        }
        //鼠标滚轮缩放
        private void OnMouseWheel(object sender, HtmlEventArgs args)
        {
            double mouseDelta = 0;
            ScriptObject e = args.EventObject;
            if (e.GetProperty("wheelDelta") != null)
            {
                mouseDelta = ((double)e.GetProperty("wheelDelta")) / 120;
            }
            else if (e.GetProperty("detail") != null)
            {
                mouseDelta = ((double)e.GetProperty("detail")) / -3;
            }
            if (mouseDelta != 0)
            {
                map.ZoomLevel = Math.Round(map.ZoomLevel + mouseDelta, 0);
            }
        }

        private void map_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this._drawtype != MapDrawType.NONE)
            {
                Location l = map.ViewportPointToLocation(e.GetPosition(map));
                this._locations.Add(l);
                this._locationsT.Add(l);
                if (this._drawtype == MapDrawType.POINT)
                {
                    ExitDraw();
                    e.Handled = true;
                }
            }
        }

        private void map_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this._drawtype == MapDrawType.POLYGON || this._drawtype == MapDrawType.POLYLINE 
                || this._drawtype == MapDrawType.MEASURELENGTH || this._drawtype == MapDrawType.MEASUREAREA)
            {
                if (this._locationsT.Count > 0)
                {
                    Location l = map.ViewportPointToLocation(e.GetPosition(map));
                    if (this._locationsT.Count > this._locations.Count)
                    {
                        this._locationsT[this._locationsT.Count - 1] = l;
                    }
                    else
                    {
                        this._locationsT.Add(l);
                    }
                    if (this._layerT.Children.Count == 0)
                    {
                        switch (this._drawtype)
                        {
                            case MapDrawType.POLYGON:
                            case MapDrawType.MEASUREAREA:
                                MapPolygon polygon = new MapPolygon();
                                polygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                                polygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                                polygon.StrokeThickness = 2;
                                polygon.Opacity = 0.5;
                                polygon.Locations = this._locationsT;
                                this._layerT.Children.Add(polygon);
                                break;
                            case MapDrawType.POLYLINE:
                            case MapDrawType.MEASURELENGTH:
                                MapPolyline polyline = new MapPolyline();
                                polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                                polyline.StrokeThickness = 2;
                                polyline.Opacity = 0.5;
                                polyline.Locations = this._locationsT;
                                this._layerT.Children.Add(polyline);
                                break;
                        }
                    }
                    else
                    {
                        if (this._layerT.Children.Count > 1)
                        {
                            this._layerT.Children.RemoveAt(1);
                        }
                        if (this._drawtype == MapDrawType.MEASURELENGTH)
                        {
                            this._layerT.AddChild(GetTooltipObj(Common.DistanceOfTwoPointsStr(this._locationsT) + "(鼠标右键单击结束)"), l);
                        }
                        else
                        {
                            this._layerT.AddChild(GetTooltipObj("鼠标右键单击结束"), l);
                        }
                    }
                }
            }
        }

        private void map_MousePan(object sender, MapMouseDragEventArgs e)
        {
            if (this._drawtype == MapDrawType.RECTANGLE || this._drawtype == MapDrawType.FRAMEZOOMIN || this._drawtype == MapDrawType.CIRCLE)
            {
                if (this._locationsT.Count > 0)
                {
                    Location l = map.ViewportPointToLocation(e.ViewportPoint);
                    if (this._drawtype == MapDrawType.CIRCLE)
                    {
                        if (this._locationsT.Count > 1)
                        {
                            this._locationsT[1] = l;
                        }
                        else
                        {
                            this._locationsT.Add(l);
                        }
                        double radius = Common.DistanceOfTwoPoints(this._locationsT);
                        this._locations.Clear();
                        LocationCollection tlc = Common.CreateCircle(this._locationsT[0], radius);
                        foreach (Location tl in tlc)
                        {
                            this._locations.Add(tl);
                        }
                    }
                    else
                    {
                        if (this._locations.Count == 1)
                        {
                            this._locations.Add(new Location(this._locations[0].Latitude, l.Longitude));
                            this._locations.Add(l);
                            this._locations.Add(new Location(l.Latitude, this._locations[0].Longitude));
                        }
                        else
                        {
                            this._locations[1].Longitude = l.Longitude;
                            this._locations[2] = l;
                            this._locations[3].Latitude = l.Latitude;
                        }
                    }
                    if (this._layerT.Children.Count == 0)
                    {
                        MapPolygon rect = new MapPolygon();
                        rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                        rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                        rect.StrokeThickness = 2;
                        rect.Opacity = 0.5;
                        rect.Locations = this._locations;
                        this._layerT.Children.Add(rect);
                    }
                    e.Handled = true;
                }
            }
        }

        private void map_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this._drawtype == MapDrawType.RECTANGLE)
            {
                if (this._locations.Count > 1)
                {
                    LocationCollection lc = new LocationCollection();
                    for (int i = 0; i < this._locations.Count; i++)
                    {
                        lc.Add(this._locations[i]);
                    }
                    MapPolygon rect = new MapPolygon();
                    rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                    rect.StrokeThickness = 2;
                    rect.Opacity = 0.5;
                    rect.Locations = lc;
                    this._layer.Children.Add(rect);
                    _dicOther.Add("no_" + _dicOther.Count.ToString(), rect);
                }
                ExitDraw();
            }
            else if (this._drawtype == MapDrawType.FRAMEZOOMIN)
            {
                if (this._locations.Count > 1)
                {
                    //设置缩放级别
                    Point p1 = this.map.LocationToViewportPoint(_locations[0]);
                    Point p2 = this.map.LocationToViewportPoint(_locations[2]);
                    double framearea = Math.Abs(p1.X - p2.X) * Math.Abs(p1.Y - p2.Y);
                    double maparea = this.map.ActualWidth * this.map.ActualHeight;
                    this.map.ZoomLevel = this.map.ZoomLevel + (int)(maparea / framearea / 4);
                    //设置中心点
                    Location l = new Location();
                    if (this._locations[0].Latitude > this._locations[2].Latitude)
                    {
                        l.Latitude = this._locations[2].Latitude + (this._locations[0].Latitude - this._locations[2].Latitude) / 2;
                    }
                    else
                    {
                        l.Latitude = this._locations[0].Latitude + (this._locations[2].Latitude - this._locations[0].Latitude) / 2;
                    }
                    if (this._locations[0].Longitude > this._locations[2].Longitude)
                    {
                        l.Longitude = this._locations[2].Longitude + (this._locations[0].Longitude - this._locations[2].Longitude) / 2;
                    }
                    else
                    {
                        l.Longitude = this._locations[0].Longitude + (this._locations[2].Longitude - this._locations[0].Longitude) / 2;
                    }
                    this.map.Center = l;
                }
                ExitDraw();
            }
            else if (this._drawtype == MapDrawType.CIRCLE)
            {
                if (this._locations.Count > 1)
                {
                    LocationCollection lc = new LocationCollection();
                    for (int i = 0; i < this._locations.Count; i++)
                    {
                        lc.Add(this._locations[i]);
                    }
                    MapPolygon rect = new MapPolygon();
                    rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                    rect.StrokeThickness = 2;
                    rect.Opacity = 0.5;
                    rect.Locations = lc;
                    this._layer.Children.Add(rect);
                    _dicOther.Add("no_" + _dicOther.Count.ToString(), rect);
                }
                ExitDraw();
            }
        }

        private void map_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this._drawtype == MapDrawType.POLYGON || this._drawtype == MapDrawType.POLYLINE
                || this._drawtype == MapDrawType.MEASURELENGTH || this._drawtype == MapDrawType.MEASUREAREA)
            {
                if (this._locations.Count > 1)
                {
                    LocationCollection lc = new LocationCollection();
                    for (int i = 0; i < this._locations.Count; i++)
                    {
                        lc.Add(this._locations[i]);
                    }
                    switch (this._drawtype)
                    {
                        case MapDrawType.POLYGON:
                            if (this._locations.Count > 2)
                            {
                                MapPolygon polygon = new MapPolygon();
                                polygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                                polygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                                polygon.StrokeThickness = 2;
                                polygon.Opacity = 0.5;
                                polygon.Locations = lc;
                                this._layer.Children.Add(polygon);
                                _dicOther.Add("no_" + _dicOther.Count.ToString(), polygon);
                            }
                            break;
                        case MapDrawType.POLYLINE:
                            MapPolyline polyline = new MapPolyline();
                            polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                            polyline.StrokeThickness = 2;
                            polyline.Opacity = 0.5;
                            polyline.Locations = lc;
                            this._layer.Children.Add(polyline);
                            _dicOther.Add("no_" + _dicOther.Count.ToString(), polyline);
                            break;
                        case MapDrawType.MEASURELENGTH:
                            MapPolyline m = new MapPolyline();
                            m.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                            m.StrokeThickness = 2;
                            m.Opacity = 0.5;
                            m.Locations = lc;
                            this._layer.Children.Add(m);
                            Border b = GetTooltipObj("总距离:" + Common.DistanceOfTwoPointsStr(lc));
                            this._layer.AddChild(b, lc[lc.Count - 1]);
                            _dicOther.Add("no_" + _dicOther.Count.ToString(), m);
                            _dicOther.Add("no_" + _dicOther.Count.ToString(), b);
                            break;
                        case MapDrawType.MEASUREAREA:
                            if (this._locations.Count > 2)
                            {
                                MapPolygon polygon = new MapPolygon();
                                polygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                                polygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                                polygon.StrokeThickness = 2;
                                polygon.Opacity = 0.5;
                                polygon.Locations = lc;
                                this._layer.Children.Add(polygon);
                                Border b2 = GetTooltipObj("面积:" + Common.CalcArea(lc));
                                this._layer.AddChild(b2, lc[lc.Count - 1]);
                                _dicOther.Add("no_" + _dicOther.Count.ToString(), polygon);
                                _dicOther.Add("no_" + _dicOther.Count.ToString(), b2);
                            }
                            break;
                    }
                }
                e.Handled = true;
                ExitDraw();
            }
        }

        private void Pushpin_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty((sender as Pushpin).Tag.ToString()))
            {
                Location l = (sender as Pushpin).Location;
                Point p = map.LocationToViewportPoint(l);
                if (p.X < 120 || map.LocationToViewportPoint(l).Y < 260 || p.X > map.ActualWidth - 200)
                {
                    map.Center = (sender as Pushpin).Location;
                }
                if (this._layer.FindName("popup") != null)
                {
                    this._layer.Children.Remove(this._ppp);
                }
                this._layer.AddChild(this._ppp, (sender as Pushpin).Location);
                this._ppp.DisplayText = (sender as Pushpin).Tag.ToString();
                this._ppp.Target = (sender as Pushpin).Name;
                this._ppp.Open();
            }
        }

        private void _hisMPL_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            for (int i = 0; i < this._layer.Children.Count; i++)
            {
                if (this._layer.Children[i] is Pushpin)
                {
                    if ((this._layer.Children[i] as Pushpin).Width == 10)
                    {
                        (this._layer.Children[i] as Pushpin).Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }

        private void _hisMPL_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            for (int i = 0; i < this._layer.Children.Count; i++)
            {
                if (this._layer.Children[i] is Pushpin)
                {
                    if ((this._layer.Children[i] as Pushpin).Width == 10)
                    {
                        (this._layer.Children[i] as Pushpin).Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
            }
        }

        private void point_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this._ppp.Close();
        }

        private void point_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!string.IsNullOrEmpty((sender as Pushpin).Tag.ToString()))
            {
                Point p = e.GetPosition(map);
                if (this._layer.FindName("popup") != null)
                {
                    this._layer.Children.Remove(this._ppp);
                }
                p.Y += 20;
                this._layer.AddChild(this._ppp, map.ViewportPointToLocation(p));
                this._ppp.DisplayText = (sender as Pushpin).Tag.ToString();
                this._ppp.Open();
            }
        }

        private void _ppp_hisQuery(object sender, OnHisQueryEventArgs e)
        {
            HtmlPage.Window.Invoke("history", new object[] { e.Phone, e.StartTime.ToString(), e.EndTime.ToString() });
        }
        #endregion

        #region [ 私有方法 ]
        private Pushpin GetPushpinByName(string name)
        {
            Pushpin ret = null;
            foreach (UIElement ue in this._layer.Children)
            {
                if (ue is Pushpin)
                {
                    if ((ue as Pushpin).Name == name)
                    {
                        ret = (ue as Pushpin);
                        break;
                    }
                }
            }
            return ret;
        }
        private Border GetTooltipByName(string name)
        {
            Border ret = null;
            foreach (UIElement ue in this._layer.Children)
            {
                if (ue is Border)
                {
                    if ((ue as Border).Name == name)
                    {
                        ret = (ue as Border);
                        break;
                    }
                }
            }
            return ret;
        }

        private MapPolyline GetPolylineByName(string name)
        {
            MapPolyline ret = null;
            foreach (UIElement ue in this._layer.Children)
            {
                if (ue is MapPolyline)
                {
                    if ((ue as MapPolyline).Name == name)
                    {
                        ret = (ue as MapPolyline);
                        break;
                    }
                }
            }
            return ret;
        }

        private Border GetTooltipObj(string text)
        {
            Border b = new Border();            
            b.Background = new System.Windows.Media.SolidColorBrush(
                Color.FromArgb(System.Convert.ToByte("FF", 16), System.Convert.ToByte("FD", 16), 
                System.Convert.ToByte("F8", 16), System.Convert.ToByte("CE", 16)));
            b.BorderThickness = new Thickness(1);
            b.BorderBrush = new System.Windows.Media.SolidColorBrush(Colors.Black);
            b.Padding = new Thickness(6,2,2,2);
            TextBlock tb = new TextBlock();
            tb.Text = text;
            b.Child = tb;
            return b;
        }

        private void StartDraw(MapDrawType mdt)
        {
            StartDraw(mdt, "");
        }

        private void StartDraw(MapDrawType mdt, string method)
        {
            this._drawtype = mdt;
            this._layerM.Visibility = System.Windows.Visibility.Visible;
            this._drawEndCallback = method;
        }

        private string LocationsToJson(LocationCollection lc)
        {
            string jsonData = "{success:true,data:[";
            for (int i = 0; i < lc.Count; i++)
            {
                jsonData += "{'Latitude':" + lc[i].Latitude + ",'Longitude':" + lc[i].Longitude + "},";
            }
            if (lc.Count > 0)
            {
                jsonData = jsonData.Substring(0, jsonData.Length - 1);
            }
            jsonData += "]}";
            return jsonData;
        }
        //定时器画轨迹
        private void histimer_Completed(object sender, EventArgs e)
        {
            if (!_paused)
            {
                if (_locationsH.Count > 0)
                {
                    _hisMPL.Locations.Add(_locationsH[0]);
                    _hisMPL2.Locations.Add(_locationsH[0]);
                    map.Center = _locationsH[0];
                    _hisPP.Location = _locationsH[0];
                    _hisPP.Tag = _json[_hisMPL.Locations.Count - 1]["Desc"].ToString().Replace("\\n", "\n").Replace("\"", "").Replace("\\", "");
                    if (_hisPP.Content != null)
                    {
                        _hisPP.Content = _json[_hisMPL.Locations.Count - 1]["ImageUrl"].ToString().Replace("\"", "").Replace("\\", "");
                    }
                    //加点
                    if (_hisMPL.Locations.Count > 1)
                    {
                        Pushpin p = new Pushpin();
                        p.Visibility = System.Windows.Visibility.Collapsed;
                        p.Cursor = System.Windows.Input.Cursors.Hand;
                        p.Location = _hisMPL.Locations[_hisMPL.Locations.Count - 2];
                        p.Margin = new Thickness(0, 0, 0, -5);
                        p.Width = 10;
                        p.Height = 10;
                        switch (_json[_hisMPL.Locations.Count - 2]["StatusName"].ToString().Replace("\"", ""))
                        {
                            case "行驶":
                                p.Foreground = new SolidColorBrush(Colors.Green);
                                break;
                            case "停车":
                                p.Foreground = new SolidColorBrush(Colors.Orange);
                                break;
                            case "熄火":
                                p.Foreground = new SolidColorBrush(Colors.Red);
                                break;
                            case "离线":
                                p.Foreground = new SolidColorBrush(Colors.Black);
                                break;
                            default:
                                p.Foreground = new SolidColorBrush(Colors.Black);
                                break;
                        }
                        p.Template = Application.Current.Resources["PushPinTemplate2"] as ControlTemplate;
                        p.Tag = _json[_hisMPL.Locations.Count - 2]["Desc"].ToString().Replace("\\n", "\n").Replace("\"", "").Replace("\\", "");
                        p.MouseEnter += new System.Windows.Input.MouseEventHandler(point_MouseEnter);
                        p.MouseLeave += new System.Windows.Input.MouseEventHandler(point_MouseLeave);
                        this._layer.Children.Add(p);
                    }
                    _locationsH.RemoveAt(0);
                    //histimer.Duration = new Duration(TimeSpan.FromSeconds(1 / (double)_hisPlaySpeed));
                    hisprogressBar.Value = _hisMPL.Locations.Count;
                    histimer.Begin();
                }
            }
        }
        #endregion

        #region [ 公开给JS调用的方法 ]
        /// <summary>
        /// 设置地图中心点
        /// </summary>
        /// <param name="latitude">纬度</param>
        /// <param name="longitude">经度</param>
        [ScriptableMemberAttribute]
        public void SetCenter(double latitude, double longitude)
        {
            map.Center = new Location(latitude, longitude);
        }
        /// <summary>
        /// 设置地图缩放级别
        /// </summary>
        /// <param name="zoom">缩放级别</param>
        [ScriptableMemberAttribute]
        public void SetZoom(int zoom)
        {
            map.ZoomLevel = zoom;
        }
        [ScriptableMemberAttribute]
        public int GetZoom()
        {
            return (int)map.ZoomLevel;
        }
        [ScriptableMemberAttribute]
        public void ZoomIn(int zoom)
        {
            map.ZoomLevel++;
        }
        [ScriptableMemberAttribute]
        public void ZoomOut(int zoom)
        {
            map.ZoomLevel--;
        }
        [ScriptableMemberAttribute]
        public void AddPushpin(double latitude, double longitude, string name, string content, string tooltip, bool isopenPupop)
        {
            if (content.StartsWith("http"))
            {
                AddVehicle(latitude, longitude, name, name, content, tooltip);
            }
            else
            {
                Pushpin p = new Pushpin();
                p.Cursor = System.Windows.Input.Cursors.Hand;
                p.Location = new Location(latitude, longitude);
                p.Tag = tooltip;
                p.Content = content;
                p.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(Pushpin_MouseLeftButtonDown);
                this._layer.Children.Add(p);
                if (isopenPupop) Pushpin_MouseLeftButtonDown(p, null);

                _dicOther.Add(string.IsNullOrEmpty(p.Name) ? "no_" + _dicOther.Count.ToString() : p.Name, p);
            }
        }
        //获取所有车辆
        [ScriptableMemberAttribute]
        public string GetVehicles()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, MapMark> t in _dicVehicles)
            {
                sb.AppendFormat("{0},", t.Key);
            }
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }
        //添加车辆
        [ScriptableMemberAttribute]
        public void AddVehicle(double lat, double lon, string id, string name, string image, string tip)
        {
            if (_dicVehicles.ContainsKey(id))
            {
                _dicVehicles[id].Pushpin.Location = new Location(lat, lon);
                _dicVehicles[id].Pushpin.Tag = tip;
                _dicVehicles[id].Pushpin.Content = image;
                _dicVehicles[id].Locus.Locations.Add(new Location(lat, lon));
                _layer.Children.Remove(_dicVehicles[id].Tag);
                _layer.AddChild(_dicVehicles[id].Tag, new Location(lat, lon));
                if (this._ppp.IsOpen && this._ppp.Target == _dicVehicles[id].Pushpin.Name)
                {
                    Pushpin_MouseLeftButtonDown(_dicVehicles[id].Pushpin, null);
                }
            }
            else
            {
                Pushpin p = new Pushpin();
                p.Name = id;
                p.Cursor = System.Windows.Input.Cursors.Hand;
                p.Location = new Location(lat, lon);
                p.Tag = tip;
                p.Content = image;
                p.Margin = new Thickness(0, 0, 0, -15);
                p.Template = Application.Current.Resources["PushPinTemplate4"] as ControlTemplate;
                p.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(Pushpin_MouseLeftButtonDown);
                this._layer.Children.Add(p);

                MapPolyline l = new MapPolyline();
                l.Cursor = System.Windows.Input.Cursors.Hand;
                l.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                l.StrokeThickness = 5;
                l.Opacity = 0.5;
                l.Locations = new LocationCollection();
                l.Locations.Add(new Location(lat, lon));
                this._layer.Children.Add(l);

                Border b = GetTooltipObj(name);
                b.Margin = new Thickness(0, 8, 0, 0);
                this._layer.AddChild(b, new Location(lat, lon));

                MapMark v = new MapMark();
                v.Pushpin = p;
                v.Locus = l;
                v.Tag = b;
                _dicVehicles.Add(id, v);
            }
        }
        //删除车辆
        [ScriptableMemberAttribute]
        public void RemoveVehicle(string id)
        {
            if (_dicVehicles.ContainsKey(id))
            {
                _layer.Children.Remove(_dicVehicles[id].Pushpin);
                _layer.Children.Remove(_dicVehicles[id].Locus);
                _layer.Children.Remove(_dicVehicles[id].Tag);
                _dicVehicles.Remove(id);
            }
        }
        //删除全部车辆
        [ScriptableMemberAttribute]
        public void RemoveAllVehicle()
        {
            foreach (KeyValuePair<string, MapMark> t in _dicVehicles)
            {
                if (_layer.Children.Contains(t.Value.Pushpin))
                {
                    _layer.Children.Remove(t.Value.Pushpin);
                }
                if (_layer.Children.Contains(t.Value.Locus))
                {
                    _layer.Children.Remove(t.Value.Locus);
                }
                if (_layer.Children.Contains(t.Value.Tag))
                {
                    _layer.Children.Remove(t.Value.Tag);
                }
            }
            _dicVehicles.Clear();
        }
        //添加兴趣点
        [ScriptableMemberAttribute]
        public void AddInterestPoint(double latitude, double longitude, string id, string name, string image)
        {
            if (_dicInterestPoint.ContainsKey(id))
            {
                _dicInterestPoint[id].Pushpin.Location = new Location(latitude, longitude);
                _dicInterestPoint[id].Pushpin.Content = image;
                this._layer.Children.Remove(_dicInterestPoint[id].Tag);
                this._layer.AddChild(_dicInterestPoint[id].Tag, new Location(latitude, longitude));
            }
            else
            {
                Pushpin p = new Pushpin();
                p.Location = new Location(latitude, longitude);
                p.Content = image;
                p.Margin = new Thickness(0, 0, 0, -15);
                p.Template = Application.Current.Resources["PushPinTemplate"] as ControlTemplate;
                this._layer.Children.Add(p);

                Border b = GetTooltipObj(name);
                this._layer.AddChild(b, p.Location);

                MapMark mark = new MapMark();
                mark.Pushpin = p;
                mark.Tag = b;

                _dicInterestPoint.Add(id, mark);
            }
        }
        //删除兴趣点
        [ScriptableMemberAttribute]
        public void RemoveInterestPoint(string id)
        {
            if (_dicInterestPoint.ContainsKey(id))
            {
                _layer.Children.Remove(_dicInterestPoint[id].Pushpin);
                _layer.Children.Remove(_dicInterestPoint[id].Tag);
                _dicInterestPoint.Remove(id);
            }
        }
        //删除全部兴趣点
        [ScriptableMemberAttribute]
        public void RemoveAllInterestPoint()
        {
            foreach (KeyValuePair<string, MapMark> t in _dicInterestPoint)
            {
                if (_layer.Children.Contains(t.Value.Pushpin))
                {
                    _layer.Children.Remove(t.Value.Pushpin);
                }
                if (_layer.Children.Contains(t.Value.Tag))
                {
                    _layer.Children.Remove(t.Value.Tag);
                }
            }
            _dicInterestPoint.Clear();
        }
        //添加兴趣线
        [ScriptableMemberAttribute]
        public void AddInterestLine(string locationJSON, string id, string name)
        {
            if (_dicInterestLine.ContainsKey(id))
            {
                JsonArray json = Common.ConvertToJson(locationJSON);
                if (json != null)
                {
                    LocationCollection lc = new LocationCollection();
                    for (int i = 0; i < json.Count; i++)
                    {
                        lc.Add(new Location(json[i]["latitude"], json[i]["longitude"]));
                    }
                    _dicInterestLine[id].Locus.Locations = lc;
                    this._layer.Children.Remove(_dicInterestLine[id].Tag);
                    this._layer.AddChild(_dicInterestLine[id].Tag, lc[lc.Count - 1]);
                }
            }
            else
            {
                JsonArray json = Common.ConvertToJson(locationJSON);
                if (json != null)
                {
                    LocationCollection lc = new LocationCollection();
                    for (int i = 0; i < json.Count; i++)
                    {
                        lc.Add(new Location(json[i]["latitude"], json[i]["longitude"]));
                    }
                    MapPolyline polyline = new MapPolyline();
                    polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                    polyline.StrokeThickness = 2;
                    polyline.Opacity = 0.5;
                    polyline.Locations = lc;
                    this._layer.Children.Add(polyline);

                    Border b = GetTooltipObj(name);
                    this._layer.AddChild(b, lc[lc.Count - 1]);

                    MapMark mark = new MapMark();
                    mark.Locus = polyline;
                    mark.Tag = b;

                    _dicInterestLine.Add(id, mark);
                }
            }
        }
        //删除兴趣线
        [ScriptableMemberAttribute]
        public void RemoveInterestLine(string id)
        {
            if (_dicInterestLine.ContainsKey(id))
            {
                _layer.Children.Remove(_dicInterestLine[id].Locus);
                _layer.Children.Remove(_dicInterestLine[id].Tag);
                _dicInterestLine.Remove(id);
            }
        }
        //删除全部兴趣线
        [ScriptableMemberAttribute]
        public void RemoveAllInterestLine()
        {
            foreach (KeyValuePair<string, MapMark> t in _dicInterestLine)
            {
                if (_layer.Children.Contains(t.Value.Locus))
                {
                    _layer.Children.Remove(t.Value.Locus);
                }
                if (_layer.Children.Contains(t.Value.Tag))
                {
                    _layer.Children.Remove(t.Value.Tag);
                }
            }
            _dicInterestLine.Clear();
        }

        [ScriptableMemberAttribute]
        public string GetAllPushpin()
        {
            return GetVehicles();
        }
        /// <summary>
        /// 删除所有标记
        /// </summary>
        [ScriptableMemberAttribute]
        public void RemoveALLPushpin()
        {
            _layer.Children.Clear();
        }
        [ScriptableMemberAttribute]
        public void RemoveAllOther()
        {
            foreach (KeyValuePair<string, UIElement> t in _dicOther)
            {
                if (_layer.Children.Contains(t.Value))
                {
                    _layer.Children.Remove(t.Value);
                }
            }
            _dicOther.Clear();
        }
        /// <summary>
        /// 添加一个多边形
        /// </summary>
        /// <param name="locationJSON">多边形顶点集合</param>
        [ScriptableMemberAttribute]
        public void AddNewPolygon(string locationJSON, string name)
        {
            JsonArray json = Common.ConvertToJson(locationJSON);
            if (json != null)
            {
                LocationCollection lc = new LocationCollection();
                for (int i = 0; i < json.Count; i++)
                {
                    lc.Add(new Location(json[i]["latitude"], json[i]["longitude"]));
                }
                MapPolygon polygon = new MapPolygon();
                polygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                polygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                polygon.StrokeThickness = 2;
                polygon.Opacity = 0.5;
                polygon.Locations = lc;
                _layer.Children.Add(polygon);
                if (!string.IsNullOrEmpty(name))
                {
                    Border b = GetTooltipObj(name);
                    _layer.AddChild(b, lc[lc.Count - 1]);
                    _dicOther.Add("no_" + _dicOther.Count.ToString(), b);
                }
                _dicOther.Add("no_" + _dicOther.Count.ToString(), polygon);
            }
        }
        /// <summary>
        /// 添加一个圆形
        /// </summary>
        /// <param name="locationJSON">圆形中心点和圆周上某点</param>
        [ScriptableMemberAttribute]
        public void AddNewCircle(string locationJSON, string name)
        {
            JsonArray json = Common.ConvertToJson(locationJSON);
            if (json != null)
            {
                LocationCollection lc = new LocationCollection();
                for (int i = 0; i < json.Count; i++)
                {
                    lc.Add(new Location(json[i]["latitude"], json[i]["longitude"]));
                }
                double radius = Common.DistanceOfTwoPoints(lc);
                lc = Common.CreateCircle(lc[0], radius);
                MapPolygon polygon = new MapPolygon();
                polygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                polygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                polygon.StrokeThickness = 2;
                polygon.Opacity = 0.5;
                polygon.Locations = lc;
                _layer.Children.Add(polygon);
                if (!string.IsNullOrEmpty(name))
                {
                    Border b = GetTooltipObj(name);
                    _layer.AddChild(b, lc[lc.Count - 1]);
                    _dicOther.Add("no_" + _dicOther.Count.ToString(), b);
                }
                _dicOther.Add("no_" + _dicOther.Count.ToString(), polygon);
            }
        }
        /// <summary>
        /// 添加一条折线
        /// </summary>
        /// <param name="locationJSON">折线点集合</param>
        [ScriptableMemberAttribute]
        public void AddNewPolyline(string locationJSON, string name)
        {
            JsonArray json = Common.ConvertToJson(locationJSON);
            if (json != null)
            {
                LocationCollection lc = new LocationCollection();
                for (int i = 0; i < json.Count; i++)
                {
                    lc.Add(new Location(json[i]["latitude"], json[i]["longitude"]));
                }
                MapPolyline polyline = new MapPolyline();
                polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                polyline.StrokeThickness = 2;
                polyline.Opacity = 0.5;
                polyline.Locations = lc;
                _layer.Children.Add(polyline);
                if (!string.IsNullOrEmpty(name))
                {
                    Border b = GetTooltipObj(name);
                    _layer.AddChild(b, lc[lc.Count - 1]);
                    _dicOther.Add("no_" + _dicOther.Count.ToString(), b);
                }
                _dicOther.Add("no_" + _dicOther.Count.ToString(), polyline);
            }
        }
        /// <summary>
        /// 在地图上画点
        /// </summary>
        [ScriptableMemberAttribute]
        public void DrawPoint(string callbackmethod)
        {
            ExitDraw();
            StartDraw(MapDrawType.POINT, callbackmethod);
        }
        /// <summary>
        /// 在地图上画折线
        /// </summary>
        [ScriptableMemberAttribute]
        public void DrawPolyline(string callbackmethod)
        {
            ExitDraw();
            StartDraw(MapDrawType.POLYLINE, callbackmethod);
        }
        /// <summary>
        /// 在地图上画多边形
        /// </summary>
        [ScriptableMemberAttribute]
        public void DrawPolygon(string callbackmethod)
        {
            ExitDraw();
            StartDraw(MapDrawType.POLYGON, callbackmethod);
        }
        /// <summary>
        /// 在地图上画矩形
        /// </summary>
        [ScriptableMemberAttribute]
        public void DrawRectangle(string callbackmethod)
        {
            ExitDraw();
            StartDraw(MapDrawType.RECTANGLE, callbackmethod);
        }
        /// <summary>
        /// 在地图上画圆形
        /// </summary>
        [ScriptableMemberAttribute]
        public void DrawCircle(string callbackmethod)
        {
            ExitDraw();
            StartDraw(MapDrawType.CIRCLE, callbackmethod);
        }
        /// <summary>
        /// 拉框放大
        /// </summary>
        [ScriptableMemberAttribute]
        public void FrameZoomIn()
        {
            ExitDraw();
            StartDraw(MapDrawType.FRAMEZOOMIN);
        }
        /// <summary>
        /// 在地图上画折线测量距离
        /// </summary>
        [ScriptableMemberAttribute]
        public void MeasureLength()
        {
            ExitDraw();
            StartDraw(MapDrawType.MEASURELENGTH);
        }
        /// <summary>
        /// 在地图上画多边形测量距离
        /// </summary>
        [ScriptableMemberAttribute]
        public void MeasureArea()
        {
            ExitDraw();
            StartDraw(MapDrawType.MEASUREAREA);
        }
        /// <summary>
        /// 退出绘画状态
        /// </summary>
        [ScriptableMemberAttribute]
        public void ExitDraw()
        {
            if (this._locations != null)
            {
                if(!string.IsNullOrEmpty(_drawEndCallback))
                {
                    if (this._drawtype == MapDrawType.CIRCLE)
                    {
                        HtmlPage.Window.Invoke(_drawEndCallback, LocationsToJson(this._locationsT));
                    }
                    else
                    {
                        HtmlPage.Window.Invoke(_drawEndCallback, LocationsToJson(this._locations));
                    }
                }
                _drawEndCallback = "";
                this._locations.Clear();
            }
            if (this._locationsT != null)
            {
                this._locationsT.Clear();
            }
            if (this._layerT != null)
            {
                this._layerT.Children.Clear();
                this._layerM.Visibility = System.Windows.Visibility.Collapsed;
            }
            this._drawtype = MapDrawType.NONE;
        }

        #region [ 历史记录回放相关操作 ]
        /// <summary>
        /// 历史记录回放
        /// </summary>
        /// <param name="locationJSON">历史位置信息</param>
        /// <param name="speed">播放速度(秒/点)</param>
        [ScriptableMemberAttribute]
        public void PlayHistory(string locationJSON, string callbackmethod)
        {
            _hisplayCallback = callbackmethod;
            _json = Common.ConvertToJson(locationJSON);
            if (_json != null)
            {
                for (int i = 0; i < _json.Count; i++)
                {
                    try
                    {
                        _locationsH.Add(new Location(Convert.ToDouble(_json[i]["Lat"].ToString().Replace("\"", "")), Convert.ToDouble(_json[i]["Lon"].ToString().Replace("\"", ""))));
                    }
                    catch { }
                }
                if (!_layer.Children.Contains(_hisMPL))
                {
                    if (_hisMPL == null)
                    {
                        _hisMPL = new MapPolyline();
                        _hisMPL.Cursor = System.Windows.Input.Cursors.Hand;
                        _hisMPL.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                        _hisMPL.StrokeThickness = 4;
                        _hisMPL.Opacity = 0.5;
                        _hisMPL.Locations = new LocationCollection();
                        _hisMPL.MouseEnter += _hisMPL_MouseEnter;
                        _hisMPL.MouseLeave += _hisMPL_MouseLeave;

                        _hisMPL2 = new MapPolyline();
                        _hisMPL2.Cursor = System.Windows.Input.Cursors.Hand;
                        _hisMPL2.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                        _hisMPL2.StrokeThickness = 20;
                        _hisMPL2.Opacity = 0;
                        _hisMPL2.Locations = new LocationCollection();
                        _hisMPL2.MouseEnter += _hisMPL_MouseEnter;
                        _hisMPL2.MouseLeave += _hisMPL_MouseLeave;
                    }
                    _layer.Children.Add(_hisMPL2);
                    _layer.Children.Add(_hisMPL);
                    histimer.Duration = new Duration(TimeSpan.FromSeconds(1 / _hisPlaySpeed));
                    histimer.Begin();
                }
                if (_hisPP == null)
                {
                    _hisPP = new Pushpin();
                    _hisPP.Location = _locationsH[0];
                    if (_json[0]["ImageUrl"].ToString().Replace("\"", "").Replace("\\", "").StartsWith("http"))
                    {
                        _hisPP.Margin = new Thickness(0, 0, 0, -15);
                        _hisPP.Content = _json[0]["ImageUrl"].ToString();
                        _hisPP.Template = Application.Current.Resources["PushPinTemplate"] as ControlTemplate;
                    }
                    _hisPP.Tag = _json[0]["Desc"].ToString().Replace("\\n", "\n").Replace("\"", "").Replace("\\", "");
                    _hisPP.MouseLeftButtonDown += Pushpin_MouseLeftButtonDown;
                    _layer.Children.Add(_hisPP);
                }
                //隐藏其他车辆
                foreach (KeyValuePair<string, MapMark> t in _dicVehicles)
                {
                    t.Value.Pushpin.Visibility = System.Windows.Visibility.Collapsed;
                    t.Value.Locus.Visibility = System.Windows.Visibility.Collapsed;
                    t.Value.Tag.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
        [ScriptableMemberAttribute]
        public int PH_Speedup()
        {
            _hisPlaySpeed = _hisPlaySpeed * 2;
            return _hisPlaySpeed;
        }
        [ScriptableMemberAttribute]
        public int PH_Slowdown()
        {
            _hisPlaySpeed = _hisPlaySpeed / 2;
            return _hisPlaySpeed;
        }

        private void hisprogressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int n = (int)hisprogressBar.Value;
            hisinfo.Text = n.ToString();
            while (_hisMPL.Locations.Count > n)
            {
                _locationsH.Insert(0, _hisMPL.Locations[_hisMPL.Locations.Count - 1]);
                _hisMPL.Locations.RemoveAt(_hisMPL.Locations.Count - 1);
            }
            while (_hisMPL.Locations.Count < n)
            {
                _hisMPL.Locations.Add(_locationsH[0]);
                _locationsH.RemoveAt(0);
            }
            _hisPP.Location = _locationsH[0];
        }
        //播放&暂停
        private void hisplayorpause_Click(object sender, RoutedEventArgs e)
        {
            if (_paused)
            {
                _paused = false;
                histimer.Begin();
            }
            else
            {
                _paused = true;
            }
        }
        //退出
        private void hisexit_Click(object sender, RoutedEventArgs e)
        {
            _hisPlaySpeed = 1;
            if (_layer.Children.Contains(_hisMPL))
            {
                _layer.Children.Remove(_hisMPL);
            }
            if (_layer.Children.Contains(_hisMPL2))
            {
                _layer.Children.Remove(_hisMPL2);
            }
            if (_layer.Children.Contains(_hisPP))
            {
                _layer.Children.Remove(_hisPP);
            }
            //显示其他车辆
            foreach (KeyValuePair<string, MapMark> t in _dicVehicles)
            {
                t.Value.Pushpin.Visibility = System.Windows.Visibility.Visible;
                t.Value.Locus.Visibility = System.Windows.Visibility.Visible;
                t.Value.Tag.Visibility = System.Windows.Visibility.Visible;
            }
            _hisMPL = null;
            _hisMPL2 = null;
            _hisPP = null;
            _drawEndCallback = null;
            _locationsH.Clear();
        }
        #endregion

        #endregion
    }
}
