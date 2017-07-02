using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Collections.Generic;
using LogicUnit.Interface;
using LogicUnit.Data;

namespace GoLine2
{
    /// <summary>
    /// Interaction logic for ChessBg.xaml
    /// </summary>
    public partial class Chessboard : UserControl, IBoard
    {
        public Brush BasicLineBrush
        {
            get { return (Brush)GetValue(BasicLineBrushProperty); }
            set { SetValue(BasicLineBrushProperty, value); }
        }
        public static readonly DependencyProperty BasicLineBrushProperty =
            DependencyProperty.Register("BasicLineBrush", typeof(Brush), typeof(Chessboard), new PropertyMetadata(Brushes.SkyBlue, OnBasicLineBrushChanged));

        public bool AutoNoticeNewest
        {
            get { return (bool)GetValue(AutoNoticeNewestProperty); }
            set { SetValue(AutoNoticeNewestProperty, value); }
        }

        public static readonly DependencyProperty AutoNoticeNewestProperty =
            DependencyProperty.Register("AutoNoticeNewest", typeof(bool), typeof(Chessboard), new PropertyMetadata(false));

        public double HighLightWaitTime
        {
            get { return (double)GetValue(HighLightWaitTimeProperty); }
            set { SetValue(HighLightWaitTimeProperty, value); }
        }

        public static readonly DependencyProperty HighLightWaitTimeProperty =
            DependencyProperty.Register("HighLightWaitTime", typeof(double), typeof(Chessboard), new PropertyMetadata(5d));

        public bool RefuseNewChess
        {
            get { return (bool)GetValue(RefuseNewChessProperty); }
            set { SetValue(RefuseNewChessProperty, value); }
        }

        public static readonly DependencyProperty RefuseNewChessProperty =
            DependencyProperty.Register("RefuseNewChess", typeof(bool), typeof(Chessboard), new PropertyMetadata(false));

        private static void OnBasicLineBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var board = d as Chessboard;
            if (board != null)
            {
                board.updateLineBrush();
            }
        }

        public Brush HostBrush { get; private set; } = Brushes.SeaGreen;
        public Brush JoinBrush { get; private set; } = Brushes.Salmon;
        public double RadiuLength { get; private set; }
        public double LatticeLength { get; private set; }
        public bool CalculateLattice { get; set; } = true;
        private double chessLength;
        public double ChessLength
        {
            get { return chessLength; }
            set {
                if (chessLength != value)
                {
                    chessLength = value;
                    RadiuLength = value / 2;
                    chessMargin = new Thickness(-value / 2);
                }
            }
        }
        //Private
        private Stack<int> columns;
        private Stack<int> rows;
        private Storyboard scaleStoryboard;
        private Storyboard removingStoryboard;
        private Storyboard delayInStoryboard;
        private Storyboard spangleStoryboard;
        private Ellipse[,] ellipses;
        private Thickness chessMargin;
        private Ellipse newestTips;
        private Ellipse stepTips;
        private Size newerSize;


        public Chessboard()
        {
            InitializeComponent();
            subscribeHandler();
            this.Loaded += onLoaded;
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= onLoaded;
            //Initilize basic data for ellipse
            ChessLength = 48d;
            ellipses = new Ellipse[7, 7];
            scaleStoryboard = FindResource("ScaleStoryboard") as Storyboard;
            removingStoryboard = FindResource("RemovingStoryboard") as Storyboard;
            delayInStoryboard = FindResource("DelayInStoryboard") as Storyboard;
            spangleStoryboard = FindResource("SpangleStoryboard") as Storyboard;
            newestTips = new Ellipse
            {
                Width = ChessLength,
                Height = ChessLength,
                Margin = chessMargin,
                Stroke = Brushes.White,
                StrokeThickness = 4d,
                Visibility = Visibility.Collapsed
            };
            stepTips = new Ellipse
            {
                Width = ChessLength,
                Height = ChessLength,
                Margin = chessMargin,
                Fill = Brushes.AliceBlue,
                Visibility = Visibility.Collapsed
            };
            Storyboard.SetTarget(delayInStoryboard, newestTips);
            Storyboard.SetTarget(spangleStoryboard, stepTips);
            Panel.SetZIndex(newestTips, 20);
            ChessLayer.Items.Add(newestTips);
            ChessLayer.Items.Add(stepTips);
            columns = new Stack<int>();
            rows = new Stack<int>();
        }

        private void subscribeHandler()
        {
            SizeChangedEventHandler sizeHandler = null;
            RoutedEventHandler unloadHandler = null;
            sizeHandler = (sender, e) =>
            {
                newerSize = e.NewSize;
                LatticeLength = e.NewSize.Height / 6;
            };
            unloadHandler = (sender, e) =>
            {
                Path.Unloaded -= unloadHandler;
                Path.SizeChanged -= sizeHandler;
            };
            Path.Unloaded += unloadHandler;
            Path.SizeChanged += sizeHandler;
        }

        private void updateLineBrush()
        {
            if (BasicLineBrush != null)
                Path.Stroke = BasicLineBrush;
        }
        

        #region Chess Add/Remove/Update
        
        private void drawChess(Brush brush, int col, int row)
        {
            spangleStoryboard.SkipToFill();
            columns.Push(col);
            rows.Push(row);
            var ellipse = new Ellipse()
            {
                Width = ChessLength,
                Height = ChessLength,
                Margin = chessMargin,
                Fill = brush
            };
            Grid.SetColumn(ellipse, col);
            Grid.SetRow(ellipse, row);
            Grid.SetColumn(newestTips, col);
            Grid.SetRow(newestTips, row);
            newestTips.Visibility = Visibility.Visible;
            newestTips.Opacity = 0;
            delayInStoryboard.Begin();

            ChessLayer.Items.Add(ellipse);
            ellipses[col, row] = ellipse;
        }

        private void removeChess(int col, int row)
        {
            spangleStoryboard.SkipToFill();
            if (ellipses[col, row] != null)
            {
                columns.Pop();
                rows.Pop();
                if(columns.Count < 1)
                {
                    newestTips.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Grid.SetColumn(newestTips, columns.Peek());
                    Grid.SetRow(newestTips, rows.Peek());
                }
                EventHandler completeHandler = null;
                completeHandler = (sender, e) =>
                {
                    removingStoryboard.Completed -= completeHandler;
                    ChessLayer.Items.Remove(ellipses[col, row]);
                    ellipses[col, row] = null;
                };
                Storyboard.SetTarget(removingStoryboard, ellipses[col, row]);
                removingStoryboard.Completed += completeHandler;
                removingStoryboard.Begin();
            }
        }

        private void clearChess()
        {
            ellipses = new Ellipse[7, 7];
            ChessLayer.Items.Clear();
            newestTips.Visibility = Visibility.Collapsed;
            stepTips.Visibility = Visibility.Collapsed;
            ChessLayer.Items.Add(newestTips);
            ChessLayer.Items.Add(stepTips);
        }
        
        public void ShowStepTips(int col, int row)
        {
            stepTips.Visibility = Visibility.Visible;
            Grid.SetColumn(stepTips, col);
            Grid.SetRow(stepTips, row);
            spangleStoryboard.Begin();
        }

        public void UpdateChessFill(Brush oldBrush, Brush newBrush)
        {
            if (HostBrush == oldBrush)
                HostBrush = newBrush;
            else if (JoinBrush == oldBrush)
                JoinBrush = newBrush;
            if (ellipses == null) return;
            foreach (var ellipse in ellipses)
            {
                if (ellipse == null) continue;
                if (ellipse.Fill == oldBrush) ellipse.Fill = newBrush;
            }
        }
        
        #endregion


        #region Implement IBoard

        public void ClearChess()
        {
            this.Dispatcher.Invoke(() =>
            {
                clearChess();
            });
        }

        public void DrawChess(int x, int y, bool host)
        {
            if (RefuseNewChess) return;
            var brush = host ? HostBrush : JoinBrush;
            drawChess(brush, x, y);
        }

        public void RemoveChess(int x, int y)
        {
            removeChess(x, y);
        }

        public event LatticClickEventHandler LatticClick;

        #endregion


        #region MouseDown EventHandler

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (!CalculateLattice) return;
            //Get click position
            Point clickPoint = e.GetPosition(Path);
            IntPoint point;
            if (tryGetColumnRow(clickPoint, LatticeLength, RadiuLength, out point) )
            {
                LatticClick?.Invoke(point.X, point.Y);
            }
        }

        private bool tryGetColumnRow(Point clickPoint, double latticeLength, double radiuLength, out IntPoint point)
        {
            point = new IntPoint(-1,-1);
            //Calculate colmn and row
            int column = Convert.ToInt32(clickPoint.X / latticeLength), row = Convert.ToInt32(clickPoint.Y / latticeLength);
            //check if click point is in circle with radius equal to radiuLength
            if (Math.Abs(column * latticeLength - clickPoint.X) > radiuLength || Math.Abs(row * latticeLength - clickPoint.Y) > radiuLength) return false;
            point = new IntPoint(column, row);
            return true;
        }
        
        #endregion

    }

}
