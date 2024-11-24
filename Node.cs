using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Force_Directed_Graphs.UserControls;
using Force_Directed_Graphs.Dialogs;

namespace Force_Directed_Graphs
{
    public class Node
    {
        //Position vector of the midpoint of a node
        protected Vector position;
        //List of all the edges connected to a node
        protected List<Edge> edges;
        //Used to generate an ID for a node
        protected static int nextID = 1;
        protected int nodeID;
        //Text associated with a node
        protected string label;
        //Size of a node
        protected int nodeSize;
        //The shape object which is connected to the node
        protected LabelledEllipse circle;
        //Main canvas
        protected Canvas c;
        //Font of the node
        protected CustomFont font;
        //Colour of the node
        protected Brush ellipseBrush;
        //Determines whether it has an inner ellipse
        protected bool isDouble;

        public Node(double x, double y, int s, Canvas can, bool doubled, CustomFont f, Brush circleBrush, out LabelledEllipse el)
        {
            label = "";
            edges = new List<Edge>();
            position = new Vector(x, y);
            //The node ID becomes the value of the nextID field
            nodeID = nextID;
            //Increments the static ID field
            nextID++;
            //Sets whether the node has an inner circle or not
            isDouble = doubled;
            nodeSize = s;
            font = new CustomFont();
            font.Family = f.Family;
            font.Size = f.Size;
            font.Bold = f.Bold;
            font.Italic = f.Italic;
            font.Underline = f.Underline;
            ellipseBrush = circleBrush;
            //Creates and sets the properties of a labelled ellipse object, which is drawn later
            circle = new LabelledEllipse
            {
                Width = nodeSize,
                Height = nodeSize,
                Stroke = circleBrush,
                ID = nodeID,
                Fill = Brushes.White,
                TxtFont = f
            };
            if (doubled)
            {
                circle.InnerVisibility = Visibility.Visible;
            }
            //Circle is assigned to the out variable 
            el = circle;
            //Assigns a new context menu to the one stored in this labelled ellipse
            circle.ContextMenu = new ContextMenu();
            circle.ContextMenu.Width = 150;
            MenuItem size = new MenuItem
            {
                Header = "Change Size",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            circle.ContextMenu.Items.Add(size);
            size.Click += Size_Click;
            MenuItem colour = new MenuItem
            {
                Header = "Change Colour",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            circle.ContextMenu.Items.Add(colour);
            colour.Click += Colour_Click;
            c = can;
            DrawNode();
        }

        //Constructor for when the node is loaded
        public Node(double x, double y, int id, int s, Canvas can, bool doubled, CustomFont f, SolidColorBrush sb, string newLabel, out LabelledEllipse el)
        {
            edges = new List<Edge>();
            position = new Vector(x, y);
            nodeID = id;
            //Sets whether the node has an inner circle or not
            isDouble = doubled;
            nodeSize = s;
            font = new CustomFont();
            font.Family = f.Family;
            font.Size = f.Size;
            font.Bold = f.Bold;
            font.Italic = f.Italic;
            font.Underline = f.Underline;
            ellipseBrush = sb;
            label = newLabel;
            //Creates and sets the properties of a labelled ellipse object, which is drawn later
            circle = new LabelledEllipse
            {
                Width = nodeSize,
                Height = nodeSize,
                Stroke = sb,
                ID = nodeID,
                Fill = Brushes.White,
                TxtFont = f
            };
            circle.Text = newLabel;
            if (doubled)
            {
                circle.InnerVisibility = Visibility.Visible;
            }
            //Circle is assigned to the out variable 
            el = circle;
            //Assigns a new context menu to the one stored in this labelled ellipse
            circle.ContextMenu = new ContextMenu();
            circle.ContextMenu.Width = 150;
            MenuItem size = new MenuItem
            {
                Header = "Change Size",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            circle.ContextMenu.Items.Add(size);
            size.Click += Size_Click;
            MenuItem colour = new MenuItem
            {
                Header = "Change Colour",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            circle.ContextMenu.Items.Add(colour);
            colour.Click += Colour_Click;
            c = can;
            DrawNode();
        }

        #region Properties

        //Size property, gets or sets the size of a node and its ellipse
        public int NodeSize
        {
            get { return nodeSize; }
            set
            {
                circle.Width = value;
                circle.Height = value;
                nodeSize = value;
                //Redraws node
                DrawNode();
            }
        }

        //Used to get or set the nextid property of a node (used for saving/loading)
        public static int NextID
        {
            get { return nextID; }
            set { nextID = value; }
        }

        //Returns a node's ID
        public int ID
        {
            get { return nodeID; }
        }

        //Returns the position vector of a node
        public Vector Position
        {
            get { return position; }
        }

        //Returns the list of edges connected to the node
        public List<Edge> Edges
        {
            get { return edges; }
        }

        //Returns the shape of the node that is drawn
        public LabelledEllipse Shape
        {
            get { return circle; }
        }

        //Returns the font of the node
        public CustomFont Font
        {
            get { return font; }
            set
            {
                font = value;
                circle.TxtFont = font;
            }
        }

        //Returns the stored text within the node
        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        //Sets the colour of the node
        public Brush EllipseBrush
        {
            set
            {
                ellipseBrush = value;
                circle.Stroke = value;
            }
        }

        #endregion

        #region Methods

        //Draws the node's associated shape
        public void DrawNode()
        {
            //Sets the position of the object on the canvas
            Canvas.SetLeft(circle, position.X - circle.Width / 2);
            Canvas.SetTop(circle, position.Y - circle.Height / 2);
            //Adds the shape to the canvas's children  
            c.Children.Remove(circle);
            c.Children.Add(circle);
            //Changes the Z Index of the shape to ensure that it is drawn over the edges
            Canvas.SetZIndex((UIElement)circle, 2);
            if (edges.Count > 0)
            {
                foreach (Edge e in edges)
                {
                    e.DrawEdge();
                }
            }
        }

        //Used to update the position of the node's midpoint
        public void UpdatePosition(Vector v)
        {
            position += v;
            //This ensures a node's position cannot leave the bounds of the canvas
            if (position.X + NodeSize / 2 > c.ActualWidth)
            {
                position.X = c.ActualWidth - nodeSize / 2;
            }
            else if (position.X - nodeSize / 2 < 0)
            {
                position.X = nodeSize / 2;
            }
            if (position.Y + nodeSize / 2 > c.ActualHeight)
            {
                position.Y = c.ActualHeight - nodeSize / 2;
            }
            else if (position.Y - nodeSize / 2 < 0)
            {
                position.Y = nodeSize / 2;
            }
            DrawNode();
        }

        //Adds an edge connection to the node
        public void AddEdge(Edge e)
        {
            edges.Add(e);
        }

        //Removes an edge object from the edge list and removes the edge from the shape's context menu
        public void RemoveEdge(Edge e)
        {
            if (edges.Contains(e))
            {
                edges.Remove(e);
                foreach (MenuItem item1 in circle.ContextMenu.Items)
                {
                    if (item1.Header.ToString() == "Edge")
                    {
                        foreach (MenuItem item2 in item1.Items)
                        {
                            if (Convert.ToInt32(item2.Header) == e.ID)
                            {
                                item1.Items.Remove(item2);
                                return;
                            }
                        }
                    }
                }
            }
        }

        //Returns the string used for saving a node
        public string GetSaveString()
        {
            string save = "";
            save += nodeID + ",";
            save += position.X + ",";
            save += position.Y + ",";
            save += nodeSize + ",";
            save += font.Family + ",";
            save += font.Size + ",";
            save += font.Bold + ",";
            save += font.Italic + ",";
            save += font.Underline + ",";
            SolidColorBrush sb = (SolidColorBrush)ellipseBrush;
            save += sb.Color.A + ",";
            save += sb.Color.R + ",";
            save += sb.Color.G + ",";
            save += sb.Color.B + ",";
            //Sanitising the contents of label to ensure it doesn't mess with loading
            string newLabel = "";
            foreach (char c in label)
            {
                if (c == ',')
                {
                    newLabel += "α";
                }
                else if (c == '\n')
                {
                    newLabel += "β";
                }
                else if (c == ';')
                {
                    newLabel += "γ";
                }
                else
                {
                    newLabel += c;
                }
            }
            save += newLabel + ",";
            save += isDouble;
            return save;
        }

        //Calls the colour dialog when the menu item in the context menu is clicked
        private void Colour_Click(object sender, RoutedEventArgs e)
        {
            ColourDialog();
        }

        //Dialog to change the colour of the node
        public void ColourDialog()
        {
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SolidColorBrush sc = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));
                EllipseBrush = sc;
            }
        }

        //Calls the size dialog when the menu item in the context menu is clicked
        private void Size_Click(object sender, RoutedEventArgs e)
        {
            SizeDialog();
        }

        //Dialog to change the size of the node
        public void SizeDialog()
        {
            SizeDialog sd = new SizeDialog("Enter the Node Size (must be between 20 and 150)", nodeSize.ToString());
            if (sd.ShowDialog() == true)
            {
                NodeSize = sd.AnswerNode;
            }
        }

        #endregion
    }
}
