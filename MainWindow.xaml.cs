using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Force_Directed_Graphs.UserControls;
using Force_Directed_Graphs.Dialogs;
using System.Diagnostics;

namespace Force_Directed_Graphs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Canvas, graph and timer that are used throughout the program
        private Canvas c;
        private Graph g;
        //Ghost shapes that follow the mouse when it is moving
        private LabelledEllipse ghostEllipse;
        private TemplateShape ghostTemplate;
        //Variable to determine if the user is placing a node
        private bool noding;
        //The timer used for force direction
        private DispatcherTimer Timer;
        //Default node size
        private int NodeSize;
        //Toggle whether snapping is enabled
        private bool snapping;
        //Shapes within the toolbox
        private LabelledEllipse tool1;
        private LabelledEllipse tool2;
        private TemplateShape ts;

        public MainWindow()
        {
            InitializeComponent();
            snapping = false;
            NodeSize = 60;
            noding = false;
            Timer = new DispatcherTimer();
            Timer.Tick += Timer_Tick;
            Timer.IsEnabled = false;
            //Determines the interval of the timer
            Timer.Interval = TimeSpan.FromMilliseconds(2);
            //Gives the canvas its properties
            c = new Canvas
            {
                Background = Brushes.White,
                ClipToBounds = true,
                IsEnabled = true,
                Focusable = true
            };
            ScaleTransform st = new ScaleTransform();
            c.RenderTransform = st;
            c.MouseWheel += C_MouseWheel;
            //Creates the border and its properties
            Border cBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                ClipToBounds = true,
                Width = c.Width,
                Height = c.Height,
                Margin = new Thickness(5, 60, 150, 5),
                CornerRadius = new CornerRadius(4),
                Child = c
            };
            //Creates the ellipse that is drawn under the mouse when a node is being placed
            ghostEllipse = new LabelledEllipse
            {
                Height = 60,
                Width = 60,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                TextVisibility = Visibility.Hidden,
                Opacity = 0
            };
            ghostEllipse.ContextMenuOpening += CloseContextMenu;
            ghostTemplate = new TemplateShape();
            ghostTemplate.Opacity = 0;
            G1.KeyDown += G1_KeyDown;
            c.MouseMove += c_MouseMove;
            c.MouseLeftButtonDown += c_LeftClick;
            c.MouseRightButtonDown += C_MouseRightButtonDown;
            //Adds objects to the grid
            G1.Children.Add(cBorder);
            //Creates a new instance of a graph class
            g = new Graph(c, NodeBox, EdgeBox, colourButton);
        }

        //Creates the toolbox on the right side of the window
        private void CreateToolbox()
        {
            //Creates the canvas for the toolbox
            Canvas Toolbox = new Canvas
            {
                Background = Brushes.White,
                ClipToBounds = true,
                IsEnabled = true,
                Focusable = true
            };
            Border toolBorder = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(2),
                ClipToBounds = true,
                Width = c.Width,
                Height = c.Height,
                Margin = new Thickness(G1.ActualWidth - 145, 60, 3, 5),
                CornerRadius = new CornerRadius(4),
            };
            //Drop shadow applied to the objects within the toolbox
            DropShadowEffect ds = new DropShadowEffect
            {
                BlurRadius = 6,
                ShadowDepth = 3,
                Color = Colors.Black
            };
            //First tool in the toolbox is a labelled ellipse with no inner circle
            tool1 = new LabelledEllipse
            {
                Height = 100,
                Width = 100,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                TextVisibility = Visibility.Hidden,
                IsEnabled = true,
                Focusable = true,
                Effect = ds
            };
            //Second tool in the toolbox is a labelled ellipse with an inner circle
            tool2 = new LabelledEllipse
            {
                Height = 100,
                Width = 100,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                InnerVisibility = Visibility.Visible,
                TextVisibility = Visibility.Hidden,
                IsEnabled = true,
                Focusable = true,
                Effect = ds
            };
            //Drop shadow used for the template shape
            DropShadowEffect tempDs = new DropShadowEffect
            {
                BlurRadius = 5,
                ShadowDepth = 2,
                Color = Colors.Black
            };
            //Final tool in the toolbox is a template which contains 3 nodes and 2 edges
            ts = new TemplateShape
            {
                Width = 130,
                Height = 130,
                Effect = tempDs,
                IsEnabled = true,
                Focusable = true
            };
            //Sets the position and events of the shapes and adds them to the canvas of the toolbox
            Canvas.SetLeft(ts, 5);
            Canvas.SetTop(ts, 300);
            Toolbox.Children.Add(ts);
            Toolbox.ContextMenuOpening += CloseContextMenu;
            ts.MouseDown += Ts_MouseDown;
            tool1.MouseDown += Tools_MouseDown;
            tool2.MouseDown += Tools_MouseDown;
            tool1.ContextMenuOpening += CloseContextMenu;
            tool2.ContextMenuOpening += CloseContextMenu;
            ts.ContextMenuOpening += CloseContextMenu;
            Canvas.SetLeft(tool1, 20);
            Canvas.SetTop(tool1, 30);
            Toolbox.Children.Add(tool1);
            Canvas.SetLeft(tool2, 20);
            Canvas.SetTop(tool2, 160);
            Toolbox.Children.Add(tool2);
            toolBorder.Child = Toolbox;
            G1.Children.Add(toolBorder);
        }


        //Disables all toolbox shapes
        private void ToolboxDeselect()
        {
            if (noding)
            {
                if (ghostTemplate.Opacity == 0.6)
                {
                    ghostTemplate.Opacity = 0;
                    ts.Clicked = false;
                    ts.Stroke = Brushes.Black;
                    noding = false;
                }
                else
                {
                    ghostEllipse.Opacity = 0;
                    tool1.Clicked = false;
                    tool1.Stroke = Brushes.Black;
                    tool2.Clicked = false;
                    tool2.Stroke = Brushes.Black;
                }
                noding = false;
            }
        }

        #region Events

        //When the main canvas is right clicked, all placing is disabled
        private void C_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToolboxDeselect();
        }

        //Disables placing a node when the escape key is pressed
        private void G1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ToolboxDeselect();
            }
        }

        //Used for zooming in and out of the canvas
        private void C_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScaleTransform st = (ScaleTransform)c.RenderTransform;
            //If Ctrl is held, the user can zoom in and out of the canvas using the scroll wheel
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                st.CenterX = e.GetPosition(c).X;
                st.CenterY = e.GetPosition(c).Y;
                if (e.Delta > 0)
                {
                    st.ScaleX *= 1.04;
                    st.ScaleY *= 1.04;
                }
                else if (st.ScaleY / 1.04 < 1)
                {
                    st.ScaleX = 1;
                    st.ScaleY = 1;
                }
                else
                {
                    st.ScaleX /= 1.04;
                    st.ScaleY /= 1.04;
                }
            }
            //If Ctrl is not held then the user can move up and down the canvas using the scroll wheel
            else
            {
                double change = 100 / st.ScaleY;
                if (e.Delta > 0)
                {
                    if (st.CenterY - change > 0)
                    {
                        st.CenterY -= change;
                    }
                    else
                    {
                        st.CenterY = 5;
                    }
                }
                else
                {
                    if (st.CenterY + change < c.ActualHeight)
                    {
                        st.CenterY += change;
                    }
                    else
                    {
                        st.CenterY = c.ActualHeight - 5;
                    }
                }
            }
        }

        //Ensures the user cannot open the context menu of a ghost ellipse
        private void CloseContextMenu(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }

        //Fired when the mouse is clicked on the template shape
        private void Ts_MouseDown(object sender, MouseButtonEventArgs e)
        {            
            TemplateShape ts = (TemplateShape)sender;
            if (ts.Clicked)
            {
                //Turns the shape back to black and disables the ghost template
                ToolboxDeselect();
                ts.Stroke = Brushes.Black;
                ghostTemplate.Opacity = 0;
                noding = false;
            }
            else
            {
                //Turns the shape blue and enables the ghost template
                ToolboxDeselect();
                ts.Clicked = true;
                ts.Stroke = Brushes.Blue;
                ghostTemplate.Opacity = 0.6;
                noding = true;
            }
            ghostEllipse.Opacity = 0;
        }

        //Fired when an ellipse in the toolbox is clicked
        private void Tools_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LabelledEllipse le = (LabelledEllipse)sender;
            if (le.Clicked)
            {
                //Turns the shape back to black and disables the ghost ellipse
                ToolboxDeselect();
                le.Stroke = Brushes.Black;
                ghostEllipse.Opacity = 0;
                noding = false;
            }
            else
            {
                //Turns the shape blue and enables the ghost ellipse
                ToolboxDeselect();
                le.Clicked = true;
                le.Stroke = Brushes.Blue;
                ghostEllipse.Opacity = 0.6;
                ghostEllipse.InnerVisibility = le.InnerVisibility;
                noding = true;
            }
        }

        //Used to make the ghost shape follow the mouse
        private void c_MouseMove(object sender, MouseEventArgs e)
        {
            if (noding)
            {
                if (snapping)
                {
                    //Snaps the ellipse to set intervals
                    int x = Convert.ToInt32(e.GetPosition(c).X) / 15;
                    int y = Convert.ToInt32(e.GetPosition(c).Y) / 10;
                    Canvas.SetLeft(ghostEllipse, (x * 15) - ghostEllipse.ActualWidth / 2);
                    Canvas.SetTop(ghostEllipse, (y * 10) - ghostEllipse.ActualHeight / 2);
                    Canvas.SetLeft(ghostTemplate, (x * 15) - ghostTemplate.ActualWidth / 2);
                    Canvas.SetTop(ghostTemplate, (y * 10) - ghostTemplate.ActualHeight / 2);
                }
                else
                {
                    double eWidth = ghostEllipse.ActualWidth;
                    double eHeight = ghostEllipse.ActualHeight;
                    double tWidth = ghostTemplate.ActualWidth;
                    double tHeight = ghostTemplate.ActualHeight;
                    //Gets mouse position relative to canvas
                    double x = e.GetPosition(c).X;
                    double y = e.GetPosition(c).Y;
                    //Used to ensure that the ghost cannot be drawn off the canvas
                    if (x + eWidth / 2 > c.ActualWidth)
                    {
                        x = c.ActualWidth - eWidth / 2;
                    }
                    else if (x - eWidth / 2 < 0)
                    {
                        x = eWidth / 2;
                    }
                    if (y + eHeight / 2 > c.ActualHeight)
                    {
                        y = c.ActualHeight - eHeight / 2;
                    }
                    else if (y - eHeight / 2 < 0)
                    {
                        y = eHeight / 2;
                    }
                    //Ghost ellipse's position is set to the mouse's   
                    Canvas.SetLeft(ghostEllipse, x - eWidth / 2);
                    Canvas.SetTop(ghostEllipse, y - eHeight / 2);
                    x = e.GetPosition(c).X;
                    y = e.GetPosition(c).Y;
                    //Used to ensure that the ghost cannot be drawn off the canvas
                    if (x + tWidth / 2 > c.ActualWidth)
                    {
                        x = c.ActualWidth - tWidth / 2;
                    }
                    else if (x - tWidth / 2 < 0)
                    {
                        x = tWidth / 2;
                    }
                    if (y + tHeight / 2 > c.ActualHeight)
                    {
                        y = c.ActualHeight - tHeight / 2;
                    }
                    else if (y - tHeight / 2 < 0)
                    {
                        y = tHeight / 2;
                    }
                    //Ghost ellipse's position is set to the mouse's   
                    Canvas.SetLeft(ghostTemplate, x - tWidth / 2);
                    Canvas.SetTop(ghostTemplate, y - tHeight / 2);
                }
            }
        }

        //When the XAML is loaded, it calls the DrawCanvas subroutine and then adds the ghost shape to the children of the canvas
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CreateToolbox();
            DrawCanvas();
            c.Children.Add(ghostEllipse);
            c.Children.Add(ghostTemplate);
        }

        //Event that occurs when the canvas is clicked on
        private void c_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (noding)
            {
                //Places a node where the user clicks
                if (ghostEllipse.Opacity == 0.6)
                {
                    //Determines if the node has an inner ellipse or not
                    if (ghostEllipse.InnerVisibility == Visibility.Visible)
                    {
                        g.NodePlace(Canvas.GetLeft(ghostEllipse) + ghostEllipse.ActualWidth / 2, Canvas.GetTop(ghostEllipse) + ghostEllipse.ActualHeight / 2, true, NodeSize);
                    }
                    else
                    {
                        g.NodePlace(Canvas.GetLeft(ghostEllipse) + ghostEllipse.ActualWidth / 2, Canvas.GetTop(ghostEllipse) + ghostEllipse.ActualHeight / 2, false, NodeSize);
                    }
                }
                //Places a template where the user clicks
                else
                {
                    Node node1 = g.NodePlace(Canvas.GetLeft(ghostTemplate) + ghostTemplate.ActualWidth - 20, Canvas.GetTop(ghostTemplate) + 20, false, 40);
                    Node node2 = g.NodePlace(Canvas.GetLeft(ghostTemplate) + ghostTemplate.ActualWidth - 20, Canvas.GetTop(ghostTemplate) + ghostTemplate.ActualHeight - 20, false, 40);
                    Node node3 = g.NodePlace(Canvas.GetLeft(ghostTemplate) + 20, Canvas.GetTop(ghostTemplate) + 78, false, 40);
                    g.CreateEdge(node3, node1);
                    g.CreateEdge(node3, node2);
                }
            }
        }

        //Event fired when the timer ticks
        private void Timer_Tick(object sender, EventArgs e)
        {
            //Calls the direct method which makes the nodes in the graph move
            //Timer is disabled when the nodes have reached an equilibrium
            if (g.Direct())
            {
                Timer.IsEnabled = false;
                Direction1.Content = "Direct";
                Direction.IsChecked = false;
            }
        }

        //Event fired when the user attempts to close the window
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Asks the user if they would like to save their diagram before they close the program
            MessageBoxResult mbr = MessageBox.Show("Would you like to save your diagram before closing?", "Close", MessageBoxButton.YesNoCancel);
            if (mbr == MessageBoxResult.Yes)
            {
                SaveFile();
            }
            else if (mbr == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        #endregion

        //Draws a grid on the canvas
        private void DrawCanvas()
        {
            bool doneDrawingBackground = false;
            int nextY = 0;
            int nextX = 0;
            //Draws the vertical lines of the grid to the canvas
            while (!doneDrawingBackground)
            {
                Line l1 = new Line
                {
                    X1 = 0,
                    X2 = c.ActualWidth,
                    Y1 = nextY,
                    Y2 = nextY,
                    Stroke = Brushes.Gray
                };
                nextY += Convert.ToInt32(c.ActualHeight / 10);
                c.Children.Add(l1);
                if (nextY >= c.ActualHeight)
                {
                    doneDrawingBackground = true;
                }
            }
            //Draws the horizontal lines of the grid to the canvas
            doneDrawingBackground = false;
            while (!doneDrawingBackground)
            {
                Line l2 = new Line
                {
                    X1 = nextX,
                    X2 = nextX,
                    Y1 = 0,
                    Y2 = c.ActualHeight,
                    Stroke = Brushes.Gray
                };
                nextX += Convert.ToInt32(c.ActualWidth / 15);
                c.Children.Add(l2);
                if (nextX >= c.ActualWidth)
                {
                    doneDrawingBackground = true;
                }
            }
        }

        //Called when the program is to be saved
        private void SaveFile()
        {
            SaveFileDialog sd = new SaveFileDialog();
            sd.Title = "Save";
            sd.Filter = "Force Directed Graph (FDG)|*.fdg";
            if (sd.ShowDialog() == true)
            {
                File.WriteAllText(sd.FileName, g.Save());
            }
        }

        //Removes the children from the canvas and creates a new graph
        private void NewGraph()
        {
            c.Children.Clear();
            DrawCanvas();
            c.Children.Add(ghostEllipse);
            c.Children.Add(ghostTemplate);
            g = new Graph(c, NodeBox, EdgeBox, colourButton);
        }

        #region Toolbar

        //Used to create a new graph
        private void New_Click(object sender, RoutedEventArgs e)
        {
            //User is asked if they would like to save first
            if (MessageBox.Show("Would you like to save your diagram?", "Save", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SaveFile();
            }
            NewGraph();
            MessageBox.Show("New graph created");
        }

        //Fired when the save button is clicked
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        //Fired when the open button is clicked
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog od = new OpenFileDialog();
            od.Title = "Open";
            od.Filter = "Force Directed Graph | *.fdg";
            if (od.ShowDialog() == true)
            {
                NewGraph();
                g.Load(od.FileName);
            }
        }

        //Fired when the change edge thickness button is clicked
        private void EdgeThick_Click(object sender, RoutedEventArgs e)
        {
            if (g.ClickedEdge != null)
            {
                g.ClickedEdge.ThicknessDialog();
            }
        }

        //Fired when the change node size button is clicked
        private void SizeThick_Click(object sender, RoutedEventArgs e)
        {
            if (g.ClickedNode != null)
            {
                g.ClickedNode.SizeDialog();
            }
        }

        //Fired when the change colour button is clicked
        private void ChangeColour_Click(object sender, RoutedEventArgs e)
        {
            if (g.ClickedNode != null)
            {
                g.ClickedNode.ColourDialog();
            }
            else if (g.ClickedEdge != null)
            {
                g.ClickedEdge.ColourDialog();
            }
        }

        //Creates a textbox on the canvas
        private void TBox_Click(object sender, RoutedEventArgs e)
        {
            g.CreateTextbox();
        }

        //Creates an image on the canvas
        private void Image_Click(object sender, RoutedEventArgs e)
        {
            g.CreateImage();
        }

        //Zooms in to the centre of the canvas by 1.1x
        private void ZIn_Click(object sender, RoutedEventArgs e)
        {
            ScaleTransform st = (ScaleTransform)c.RenderTransform;
            st.CenterX = c.ActualWidth / 2;
            st.CenterY = c.ActualHeight / 2;
            st.ScaleX *= 1.1;
            st.ScaleY *= 1.1;
        }

        //Zooms out of the centre of the canvas by 1.1x
        //It is limited at a 1x zoom value, so you cannot zoom out any more than the default of the canvas
        private void ZOut_Click(object sender, RoutedEventArgs e)
        {
            ScaleTransform st = (ScaleTransform)c.RenderTransform;
            st.CenterX = c.ActualWidth / 2;
            st.CenterY = c.ActualHeight / 2;
            if (st.ScaleY / 1.1 < 1)
            {
                st.ScaleX = 1;
                st.ScaleY = 1;
            }
            else
            {
                st.ScaleX /= 1.1;
                st.ScaleY /= 1.1;
            }
        }

        //Toggles force direction
        private void Direction_Click(object sender, RoutedEventArgs e)
        {
            ToggleDirection();
        }

        //Toggles snapping
        private void Snap_Click(object sender, RoutedEventArgs e)
        {
            snapping = !snapping;
        }

        //Exports the canvas to an image file
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            g.CreateBitmap();
        }

        //Fired when the text in the node size textbox is changed
        private void NodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            try
            {
                //Attempts to set the default node size and size of the ghost ellipse to what the user has written in the box
                int intSize = Convert.ToInt32(txt.Text);
                if (intSize >= 20 && intSize <= 150)
                {
                    ghostEllipse.Width = Convert.ToInt32(txt.Text);
                    ghostEllipse.Height = Convert.ToInt32(txt.Text);
                    NodeSize = Convert.ToInt32(txt.Text);
                    //If a node has been selected, its size will be changed to what the user has typed
                    if (g.ClickedNode != null)
                    {
                        g.ClickedNode.NodeSize = NodeSize;
                    }
                }
            }
            catch (NullReferenceException) { }
            catch (FormatException) { }
        }

        //Fires when the node size textbox loses focus
        private void NodeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            txt.Text = NodeSize.ToString();
        }

        private void EdgeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            try
            {
                double dText = Convert.ToDouble(txt.Text);
                //Ensures the edge box can't be a negative number
                if (dText >= 0.5 && dText <= 15)
                {
                    //Attempts to set the default edge size to what the user has typed
                    g.EdgeSize = dText;
                    //If an edge has been selected then its size will change to what the user has typed
                    if (g.ClickedEdge != null)
                    {
                        g.ClickedEdge.Thickness = g.EdgeSize;
                    }
                }
            }
            catch (NullReferenceException) { }
            catch (FormatException) { }
        }

        //Fires when the edge thickness textbox loses focus
        private void EdgeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            txt.Text = g.EdgeSize.ToString();
        }

        //Fired when the font button is clicked
        private void Font_Click(object sender, RoutedEventArgs e)
        {
            //Uses a font dialog and a CustomFont to assign a font in the graph class
            System.Windows.Forms.FontDialog fd = new System.Windows.Forms.FontDialog();
            if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.Drawing.Font f = fd.Font;
                CustomFont font = new CustomFont
                {
                    Family = f.FontFamily.Name,
                    Size = f.Size,
                    Bold = f.Bold,
                    Italic = f.Italic,
                    Underline = f.Underline
                };
                g.Font = font;
            }
        }

        //Toggles bold
        private void Bold_Click(object sender, RoutedEventArgs e)
        {
            g.ChangeBold();
        }

        //Toggles underline
        private void Underline_Click(object sender, RoutedEventArgs e)
        {
            g.ChangeUnderline();
        }

        //Toggles italics
        private void Italic_Click(object sender, RoutedEventArgs e)
        {
            g.ChangeItalic();
        }

        //Fired when the colour button is clicked
        private void Colour_Click(object sender, RoutedEventArgs e)
        {
            //Changes the colour of the ghost shapes and graph defaults
            Button colour = (Button)sender;
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SolidColorBrush sc = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));
                g.Brush = sc;
                ghostEllipse.Stroke = sc;
                ghostTemplate.Stroke = sc;
                colour.Background = sc;
                //Changes colour of any clicked edges or nodes
                if (g.ClickedEdge != null)
                {
                    g.ClickedEdge.LineStroke = sc;
                }
                else if (g.ClickedNode != null)
                {
                    g.ClickedNode.EllipseBrush = sc;
                }
            }
        }

        //Used when the button for force direction is clicked
        private void Direct_Click(object sender, RoutedEventArgs e)
        {
            Direction.IsChecked = !Direction.IsChecked;
            ToggleDirection();
        }

        //Toggles force direction on/off
        private void ToggleDirection()
        {
            //Disables placing of nodes
            noding = false;
            ghostEllipse.Opacity = 0;
            ghostTemplate.Opacity = 0;
            //if the timer is enabled it becomes disabled and the text on the button is changed
            if (Timer.IsEnabled)
            {
                Direction1.Content = "Direct";
            }
            //if the timer is disabled it becomes enabled and the text on the button is changed
            else
            {
                Direction1.Content = "Directing";
            }
            Timer.IsEnabled = !Timer.IsEnabled;
        }

        //Opens a word document which contains an explanation of how to use the program
        private void Doc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "WINWORD.EXE",
                    Arguments = "FDG_Documentation.docx"
                };
                Process.Start(startInfo);
            }
            catch
            {
                MessageBox.Show("Unable to load tutorial.");
            }
            
        }

        //Creates a traversal dialog when the button is clicked
        private void Traversal_Click(object sender, RoutedEventArgs e)
        {
            g.TraverseDialog();
        }

        #endregion
    }
}
