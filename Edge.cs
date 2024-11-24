using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Force_Directed_Graphs.Dialogs;

namespace Force_Directed_Graphs
{
    public class Edge
    {
        //The nodes that the edge is connected to
        protected Node[] connections = new Node[2];
        //ID of the edge, which is generated from a static field
        protected int edgeID;
        protected static int nextID = 1;
        //Label and weight of the edge
        protected string label;
        protected string weight;
        //Line drawn for the edge
        protected Line l;
        //Labels for the text and weights associated with an edge
        protected Label lblLabel;
        protected Label lblWeight;
        //Main canvas
        protected Canvas c;
        //Thickness of an edge
        protected double edgeThickness;
        //Colour of an edge
        protected Brush lineBrush;

        public Edge(Node node1, Node node2, double thick, Brush b, Canvas can, out Line li)
        {
            //Adds nodes to the connections array
            connections[0] = node1;
            connections[1] = node2;
            //The ID becomes the value of the nextID variable
            edgeID = nextID;
            //Increments the nextID static field
            nextID++;
            edgeThickness = thick;
            lineBrush = b;
            //Instantiates the line to be drawn on the canvas
            l = new Line
            {
                Stroke = b,
                StrokeThickness = thick,
                Tag = edgeID.ToString()
            };
            //Creates the line's context menu and adds multiple menu items to the menu
            l.ContextMenu = new ContextMenu();
            l.ContextMenu.Width = 150;
            //Used to change thickness of an edge
            MenuItem thickness = new MenuItem
            {
                Header = "Change Thickness",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(thickness);
            thickness.Click += Thickness_Click;
            //Used to change colour of an edge
            MenuItem colour = new MenuItem
            {
                Header = "Change Colour",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(colour);
            colour.Click += Colour_Click;
            //Used to change the label of an edge
            MenuItem changeText = new MenuItem
            {
                Header = "Change Text",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(changeText);
            changeText.Click += ChangeText_Click;
            //Used to change the weight of an edge
            MenuItem changeWeight = new MenuItem
            {
                Header = "Change Weight",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(changeWeight);
            changeWeight.Click += ChangeWeight_Click;
            //Sets that line to the out variable
            li = l;
            //Sets the canvas field
            c = can;
            //Creates new labels and adds them to the children of the canvas
            lblLabel = new Label();
            Label = "";
            lblWeight = new Label();
            Weight = "";
            c.Children.Add(lblWeight);
            c.Children.Add(lblLabel);
            DrawEdge();
        }

        public Edge(Node node1, Node node2, int id, double thick, Brush b, Canvas can, string edgeLabel, string edgeWeight, out Line li)
        {
            //Adds nodes to the connections array
            connections[0] = node1;
            connections[1] = node2;
            //Assigns the edge id to the presaved ID
            edgeID = id;
            edgeThickness = thick;
            lineBrush = b;
            //Instantiates the line to be drawn on the canvas
            l = new Line
            {
                Stroke = b,
                StrokeThickness = thick,
                Tag = edgeID.ToString()
            };
            //Creates the line's context menu and adds multiple menu items to the menu
            l.ContextMenu = new ContextMenu();
            l.ContextMenu.Width = 150;
            //Used to change thickness of an edge
            MenuItem thickness = new MenuItem
            {
                Header = "Change Thickness",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(thickness);
            thickness.Click += Thickness_Click;
            //Used to change colour of an edge
            MenuItem colour = new MenuItem
            {
                Header = "Change Colour",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(colour);
            colour.Click += Colour_Click;
            //Used to change the label of an edge
            MenuItem changeText = new MenuItem
            {
                Header = "Change Text",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(changeText);
            changeText.Click += ChangeText_Click;
            //Used to change the weight of an edge
            MenuItem changeWeight = new MenuItem
            {
                Header = "Change Weight",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            l.ContextMenu.Items.Add(changeWeight);
            changeWeight.Click += ChangeWeight_Click;
            //Sets that line to the out variable
            li = l;
            //Sets the canvas field
            c = can;
            //Creates new labels and adds them to the children of the canvas
            lblLabel = new Label();
            Label = edgeLabel;
            lblWeight = new Label();
            Weight = edgeWeight;
            c.Children.Add(lblLabel);
            c.Children.Add(lblWeight);
            DrawEdge();
        }

        #region Properties

        //Used to get or set an edge's weight
        public string Weight
        {
            get { return weight; }
            set
            {
                weight = value;
                lblWeight.Content = value;
            }
        }

        //Used to get or set the nextid property of an edge (used for saving/loading)
        public static int NextID
        {
            get { return nextID; }
            set { nextID = value; }
        }

        //Used to get or set an edge's label
        public string Label
        {
            get { return label; }
            set
            {
                label = value;
                lblLabel.Content = value;
            }
        }

        //Returns an edge's ID
        public int ID
        {
            get { return edgeID; }
        }

        //Returns an array of the edge's connected nodes
        public Node[] Connections
        {
            get { return connections; }
        }

        //Used to get or set the thickness of an edge
        public double Thickness
        {
            get { return edgeThickness; }
            set
            {
                edgeThickness = value;
                l.StrokeThickness = value;
            }
        }

        //Used to get or set the colour of an edge
        public Brush LineStroke
        {
            get { return lineBrush; }
            set
            {
                l.Stroke = value;
                lineBrush = value;
            }
        }
        #endregion

        #region Methods

        //Returns the direction vector of a node to another
        private Vector DirectionVector()
        {
            Vector direction = new Vector();
            direction.X = connections[1].Position.X - connections[0].Position.X;
            direction.Y = connections[1].Position.Y - connections[0].Position.Y;
            return direction;
        }

        //Draws an edge between the two nodes on the canvas that has been passed to it
        public void DrawEdge()
        {
            //Gets the direction vector between the two connection nodes
            Vector normalised = DirectionVector();
            double length = normalised.Length;
            normalised.Normalize();
            c.Children.Remove(l);
            //Sets the start and end points of the line to the midpoints of the connected nodes
            l.X1 = connections[0].Position.X;
            l.Y1 = connections[0].Position.Y;
            l.X2 = connections[1].Position.X;
            l.Y2 = connections[1].Position.Y;
            //Used to position the label and weight labels around the line
            if (normalised.Y >= 0 && normalised.X >= 0)
            {
                Canvas.SetLeft(lblLabel, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 - lblLabel.ActualWidth);
                Canvas.SetTop(lblLabel, connections[0].Position.Y + (normalised.Y * 0.5 * length) + 20 - lblLabel.ActualHeight);
            }
            else if (normalised.Y >= 0 && normalised.X < 0)
            {
                Canvas.SetLeft(lblLabel, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 - lblLabel.ActualWidth);
                Canvas.SetTop(lblLabel, connections[0].Position.Y + (normalised.Y * 0.5 * length) - 20 - lblLabel.ActualHeight);
            }
            else if (normalised.Y < 0 && normalised.X >= 0)
            {
                Canvas.SetLeft(lblLabel, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 + lblLabel.ActualWidth);
                Canvas.SetTop(lblLabel, connections[0].Position.Y + (normalised.Y * 0.5 * length) - 20 - lblLabel.ActualHeight);
            }
            else if (normalised.Y < 0 && normalised.X < 0)
            {
                Canvas.SetLeft(lblLabel, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 - lblLabel.ActualWidth);
                Canvas.SetTop(lblLabel, connections[0].Position.Y + (normalised.Y * 0.5 * length) + 20 - lblLabel.ActualHeight);
            }
            if (normalised.Y >= 0 && normalised.X >= 0)
            {
                Canvas.SetLeft(lblWeight, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 - lblWeight.ActualWidth);
                Canvas.SetTop(lblWeight, connections[0].Position.Y + (normalised.Y * 0.5 * length) - 20 - lblWeight.ActualHeight);
            }
            else if (normalised.Y >= 0 && normalised.X < 0)
            {
                Canvas.SetLeft(lblWeight, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 - lblWeight.ActualWidth);
                Canvas.SetTop(lblWeight, connections[0].Position.Y + (normalised.Y * 0.5 * length) + 20 - lblWeight.ActualHeight);
            }
            else if (normalised.Y < 0 && normalised.X >= 0)
            {
                Canvas.SetLeft(lblWeight, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 + lblWeight.ActualWidth);
                Canvas.SetTop(lblWeight, connections[0].Position.Y + (normalised.Y * 0.5 * length) + 20 - lblWeight.ActualHeight);
            }
            else if (normalised.Y < 0 && normalised.X < 0)
            {
                Canvas.SetLeft(lblWeight, connections[0].Position.X + (normalised.X * 0.5 * length) - 15 - lblWeight.ActualWidth);
                Canvas.SetTop(lblWeight, connections[0].Position.Y + (normalised.Y * 0.5 * length) - 20 - lblWeight.ActualHeight);
            }
            //Adds the line to the canvas's children
            c.Children.Add(l);
            //Sets the ZIndex of the line to 0, which means that the line is essentially "sent to back" 
            Canvas.SetZIndex(l, 0);
        }

        //Removes the drawn edge from the canvas's children
        public void UndrawEdge()
        {
            if (c.Children.Contains(l))
            {
                c.Children.Remove(l);
            }
            c.Children.Remove(lblLabel);
            c.Children.Remove(lblWeight);
        }

        //Gets the string used to save the edge's data
        public string GetSaveString()
        {
            string save = "";
            save += edgeID + ",";
            save += connections[0].ID + ",";
            save += connections[1].ID + ",";
            save += edgeThickness + ",";
            SolidColorBrush sb = (SolidColorBrush)lineBrush;
            save += sb.Color.A + ",";
            save += sb.Color.R + ",";
            save += sb.Color.G + ",";
            save += sb.Color.B + ",";
            string newLabel = "";
            //Replaces any instances of the split characters with greek letters
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
            save += weight;
            return save;
        }

        //Dialog pops which allows the user to change the weight of the node
        private void ChangeWeight_Click(object sender, RoutedEventArgs e)
        {
            SizeDialog sd = new SizeDialog("Enter the new Weight", weight);
            if (sd.ShowDialog() == true)
            {
                Weight = sd.AnswerWeight.ToString();
            }
        }

        //Dialog pops which allows the user to change the text of the node
        private void ChangeText_Click(object sender, RoutedEventArgs e)
        {
            SizeDialog sd = new SizeDialog("Enter the new Text", label);
            if (sd.ShowDialog() == true)
            {
                Label = sd.AnswerLabel;
            }
        }

        //Calls the colour dialog which is used to assign a brush to the edge
        private void Colour_Click(object sender, RoutedEventArgs e)
        {
            ColourDialog();
        }

        //Calls the thickness dialog which is used to change the thickness of the edge
        private void Thickness_Click(object sender, RoutedEventArgs e)
        {
            ThicknessDialog();
        }

        //Sets the brush of the edge according to the result of a colour dialog
        public void ColourDialog()
        {
            System.Windows.Forms.ColorDialog cd = new System.Windows.Forms.ColorDialog();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SolidColorBrush sc = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));
                LineStroke = sc;
            }
        }

        //Sets the thickness of the edge according to the result of a custom dialog
        public void ThicknessDialog()
        {
            SizeDialog sd = new SizeDialog("Enter the Edge Thickness (must be between 0.5 and 15)", edgeThickness.ToString());
            if (sd.ShowDialog() == true)
            {
                Thickness = sd.AnswerEdge;
            }
        }

        #endregion
    }
}
