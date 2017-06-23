using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LogicUnit.Interface;
using LogicUnit.Data;

namespace Goline2
{
    /// <summary>
    /// Interaction logic for Board.xaml
    /// </summary>
    public partial class Board : UserControl, IBoard
    {
        public double ChessLength
        {
            get { return (double)GetValue(ChessLengthProperty); }
            set { SetValue(ChessLengthProperty, value); }
        }
        public static readonly DependencyProperty ChessLengthProperty =
            DependencyProperty.Register("ChessLength", typeof(double), typeof(Board), new PropertyMetadata(48d));
        private static void onChessLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var board = d as Board;
            board?.updateForChessLength((double)e.NewValue);
        }

        public event LatticClickEventHandler LatticClick;
        public double RadiuLength { get; private set; } = 24;
        public double LatticeLength { get; private set; }
        private Thickness chessMargin = new Thickness(-24);
        private Size partPathSize;
        private SolidColorBrush[] brushes = new SolidColorBrush[] { Brushes.SkyBlue, Brushes.SeaGreen, Brushes.Salmon};

        private void updateForChessLength(double chessLength)
        {
            RadiuLength = chessLength / 2;
            chessMargin = new Thickness(-chessLength / 2);
        }

        public Board()
        {
            InitializeComponent();
            this.Loaded += onBoardLoaded;
        }

        private void onBoardLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= onBoardLoaded;
            this.Unloaded += onBoardUnloaded;
            partPath.SizeChanged += onPartPathSizeChanged;
            LatticeLength = partPath.RenderSize.Height / 6;
        }

        private void onBoardUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= onBoardUnloaded;
            partPath.SizeChanged -= onPartPathSizeChanged;
        }

        private void onPartPathSizeChanged(object sender, SizeChangedEventArgs e)
        {
            partPathSize = e.NewSize;
            LatticeLength = e.NewSize.Height / 6;
        }

        public void DrawChess(int x, int y, int color)
        {
            this.Dispatcher.Invoke(() =>
            {
                var ellipse = new Ellipse()
                {
                    Width = ChessLength,
                    Height = ChessLength,
                    Margin = chessMargin,
                    Fill = brushes[color]
                };
                Grid.SetColumn(ellipse, x);
                Grid.SetRow(ellipse, y);
                chessRoot.Items.Add(ellipse);
            });
        }

        public void RemoveChess(int x, int y)
        {
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Point clickPoint = e.GetPosition(partPath);
            IntPoint p;
            if (tryGetColumnRow(clickPoint, LatticeLength, RadiuLength, out p))
            {
                LatticClick?.Invoke(p.X, p.Y);
            }
        }

        private bool tryGetColumnRow(Point clickPoint, double latticeLength, double radiuLength, out IntPoint p)
        {
            p = default(IntPoint);
            //Calculate colmn and row
            int column = Convert.ToInt32(clickPoint.X / latticeLength);
            int row = Convert.ToInt32(clickPoint.Y / latticeLength);
            //check if click point is in circle with radius equal to radiuLength
            if (Math.Abs(column * latticeLength - clickPoint.X) > radiuLength 
                || Math.Abs(row * latticeLength - clickPoint.Y) > radiuLength) return false;
            p.X = column;
            p.Y = row;
            return true;
        }

    }
}
