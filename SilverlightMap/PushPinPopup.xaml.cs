using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ExsunSilverlightMap
{
    public partial class PushPinPopup : Grid
    {
        public delegate void HisQueryEventHandler(object sender, OnHisQueryEventArgs e);
        public event HisQueryEventHandler hisQuery;
        public virtual void OnHisQuery(OnHisQueryEventArgs e)
        {
            if (hisQuery != null)
            {
                hisQuery(this, e);
            }
        }

        private string target = "";
        public string Target
        {
            get { return target; }
            set { target = value; }
        }

        public string DisplayText
        {
            get 
            {
                if (string.IsNullOrEmpty(txtTitle.Text))
                {
                    return txtContent.Text;
                }
                return txtTitle.Text + "\n" + txtContent.Text;
            }
            set
            {
                if (value.IndexOf("\n") > 0)
                {
                    this.txtTitle.Text = value.Substring(0, value.IndexOf("\n"));
                    this.txtContent.Text = value.Substring(value.IndexOf("\n") + 1);
                }
                else
                {
                    this.txtContent.Text = value;
                }
            }
        }

        public bool IsOpen
        {
            get { return (this.Visibility == System.Windows.Visibility.Visible); }
        }

        public PushPinPopup()
        {
            InitializeComponent();
        }

        public void Open()
        {
            Storyboard1.Begin();
        }

        public void Close()
        {
            Storyboard2.Begin();
            info.Visibility = System.Windows.Visibility.Visible;
            history.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void hlbClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void sbclose_Completed(object sender, EventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void hbToHistory_Click(object sender, RoutedEventArgs e)
        {
            Storyboard story = new Storyboard();
            DoubleAnimationUsingKeyFrames dukf = new DoubleAnimationUsingKeyFrames();
            DiscreteDoubleKeyFrame dd = new DiscreteDoubleKeyFrame();
            dd.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0));
            dd.Value = 0;
            EasingDoubleKeyFrame ed = new EasingDoubleKeyFrame();
            ed.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            ed.Value = 90;
            DiscreteDoubleKeyFrame dd2 = new DiscreteDoubleKeyFrame();
            dd2.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            dd2.Value = -90;
            EasingDoubleKeyFrame ed2 = new EasingDoubleKeyFrame();
            ed2.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 500));
            ed2.Value = 0;
            dukf.KeyFrames.Add(dd);
            dukf.KeyFrames.Add(ed);
            dukf.KeyFrames.Add(dd2);
            dukf.KeyFrames.Add(ed2);
            PlaneProjection pp = (PlaneProjection)popup.Projection;
            Storyboard.SetTarget(dukf, pp);
            Storyboard.SetTargetProperty(dukf, new PropertyPath(PlaneProjection.RotationYProperty));
            story.Children.Add(dukf);

            ObjectAnimationUsingKeyFrames oaukf = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dokf = new DiscreteObjectKeyFrame();
            dokf.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            dokf.Value = Visibility.Collapsed;
            oaukf.KeyFrames.Add(dokf);
            Storyboard.SetTarget(oaukf, info);
            Storyboard.SetTargetProperty(oaukf, new PropertyPath(Grid.VisibilityProperty));
            story.Children.Add(oaukf);

            ObjectAnimationUsingKeyFrames oaukf2 = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dokf2 = new DiscreteObjectKeyFrame();
            dokf2.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            dokf2.Value = Visibility.Visible;
            oaukf2.KeyFrames.Add(dokf2);
            Storyboard.SetTarget(oaukf2, history);
            Storyboard.SetTargetProperty(oaukf2, new PropertyPath(Grid.VisibilityProperty));
            story.Children.Add(oaukf2);

            story.Begin();

            dpStart.SelectedDate = DateTime.Now;
            dpEnd.SelectedDate = DateTime.Now;
            cmbStartTime.SelectedIndex = 0;
            cmbEndTime.SelectedIndex = DateTime.Now.Hour * 2 + 1;
        }

        private void hbToCurrent_Click(object sender, RoutedEventArgs e)
        {
            Storyboard story = new Storyboard();
            DoubleAnimationUsingKeyFrames dukf = new DoubleAnimationUsingKeyFrames();
            DiscreteDoubleKeyFrame dd = new DiscreteDoubleKeyFrame();
            dd.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0));
            dd.Value = 0;
            EasingDoubleKeyFrame ed = new EasingDoubleKeyFrame();
            ed.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            ed.Value = 90;
            DiscreteDoubleKeyFrame dd2 = new DiscreteDoubleKeyFrame();
            dd2.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            dd2.Value = -90;
            EasingDoubleKeyFrame ed2 = new EasingDoubleKeyFrame();
            ed2.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 500));
            ed2.Value = 0;
            dukf.KeyFrames.Add(dd);
            dukf.KeyFrames.Add(ed);
            dukf.KeyFrames.Add(dd2);
            dukf.KeyFrames.Add(ed2);
            PlaneProjection pp = (PlaneProjection)popup.Projection;
            Storyboard.SetTarget(dukf, pp);
            Storyboard.SetTargetProperty(dukf, new PropertyPath(PlaneProjection.RotationYProperty));
            story.Children.Add(dukf);

            ObjectAnimationUsingKeyFrames oaukf = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dokf = new DiscreteObjectKeyFrame();
            dokf.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            dokf.Value = Visibility.Visible;
            oaukf.KeyFrames.Add(dokf);
            Storyboard.SetTarget(oaukf, info);
            Storyboard.SetTargetProperty(oaukf, new PropertyPath(Grid.VisibilityProperty));
            story.Children.Add(oaukf);

            ObjectAnimationUsingKeyFrames oaukf2 = new ObjectAnimationUsingKeyFrames();
            DiscreteObjectKeyFrame dokf2 = new DiscreteObjectKeyFrame();
            dokf2.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, 250));
            dokf2.Value = Visibility.Collapsed;
            oaukf2.KeyFrames.Add(dokf2);
            Storyboard.SetTarget(oaukf2, history);
            Storyboard.SetTargetProperty(oaukf2, new PropertyPath(Grid.VisibilityProperty));
            story.Children.Add(oaukf2);

            story.Begin(); 
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DateTime start = DateTime.Parse(((DateTime)dpStart.SelectedDate).ToString("yyyy-MM-dd") + " " + (cmbStartTime.SelectedItem as ComboBoxItem).Content);
            DateTime end = DateTime.Parse(((DateTime)dpEnd.SelectedDate).ToString("yyyy-MM-dd") + " " + (cmbEndTime.SelectedItem as ComboBoxItem).Content);
            OnHisQueryEventArgs arg = new OnHisQueryEventArgs(target, start, end);
            OnHisQuery(arg);
        }
    }

    public class OnHisQueryEventArgs : EventArgs
    {
        private string phone;
        private DateTime starttime;
        private DateTime endtime;
        public string Phone
        {
            get { return phone; }
            set { phone = value; }
        }
        public DateTime StartTime
        {
            get { return starttime; }
            set { starttime = value; }
        }
        public DateTime EndTime
        {
            get { return endtime; }
            set { endtime = value; }
        }
        public OnHisQueryEventArgs(string p, DateTime s, DateTime e)
        {
            phone = p;
            starttime = s;
            endtime = e;
        }
    }
}
