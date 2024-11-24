using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Force_Directed_Graphs.UserControls;
using Force_Directed_Graphs.Dialogs;

namespace Force_Directed_Graphs
{
    class Graph
    {
        //List of nodes within the graph
        protected List<Node> nodes;
        //List of edges within the graph
        protected List<Edge> edges;
        //The clicked node and edge, used for editing (colour, text, etc.)
        protected Node clickedNode;
        protected Edge clickedEdge;
        //Main canvas
        protected Canvas c;
        //Line used for drawing edges
        protected Line l;
        //Determines if the user is placing a node
        protected bool placing;
        //Array of ellipses which appear when the user is drawing an edge
        protected Ellipse[] ellipses;
        //Used to create an edge
        protected Node edgeNode;
        //Nodes which do not move when directing
        protected List<Node> keyNodes;
        protected double edgeSize;
        protected CustomFont font;
        //Ellipse colour
        protected Brush ellipseBrush;
        //Textboxes from the main window
        protected TextBox nSize;
        protected TextBox eSize;
        //Button from the main window
        protected Button colourButton;
        //The extra textboxes and images that can be placed on the canvas
        protected List<TextBox> txtBoxes;
        protected List<Image> images;
        protected int textBoxId;
        //Used to allow a node to start moving when dragged by the user
        protected bool clickDrag;
        protected Dictionary<Node, bool> visited;
        protected List<Node> traversedOrder;

        public Graph(Canvas newCanvas, TextBox n, TextBox e, Button b)
        {
            images = new List<Image>();
            txtBoxes = new List<TextBox>();
            traversedOrder = new List<Node>();
            visited = new Dictionary<Node, bool>();
            clickDrag = false;
            colourButton = b;
            textBoxId = 0;
            ellipseBrush = Brushes.Black;
            font = new CustomFont
            {
                Family = "Arial",
                Size = 12,
                Bold = false,
                Italic = false,
                Underline = false
            };
            nSize = n;
            eSize = e;
            //Default edge size
            edgeSize = 1.5;
            keyNodes = new List<Node>();
            nodes = new List<Node>();
            edges = new List<Edge>();
            clickedNode = null;
            clickedEdge = null;
            //Canvas from the MainWindow is assigned to the canvas in the fields
            c = newCanvas;
            //Assigns events to the canvas
            c.MouseMove += C_MouseMove;
            c.MouseUp += C_MouseUp;
            c.PreviewMouseDown += C_MouseDown;
        }

        #region Properties

        //Property for the default font of a graph
        public CustomFont Font
        {
            get { return font; }
            set
            {
                font = value;
                for (int i = 0; i < nodes.Count; i++)
                {
                    //If the font is changed and one of the nodes is selected, it changes the font of the node as well
                    Node n = nodes[i];
                    if (n.Shape.txt.IsSelectionActive)
                    {
                        n.Font = value;
                    }
                }
                for (int i = 0; i < txtBoxes.Count; i++)
                {
                    //If the font is changed and one of the textboxes is selected, it changes the font of the textbox as well
                    TextBox txt = txtBoxes[i];
                    if (txt.IsSelectionActive)
                    {
                        font.ModifyMediaTxtBox(ref txt);
                    }
                }
            }
        }

        //Returns the clicked node field
        public Node ClickedNode
        {
            get
            {
                RemoveEllipses();
                return clickedNode;
            }
        }

        //Returns the clicked edge field
        public Edge ClickedEdge
        {
            get
            {
                RemoveEllipses();
                return clickedEdge;
            }
        }

        //Returns the ellipseBrush field
        public Brush Brush
        {
            get { return ellipseBrush; }
            set { ellipseBrush = value; }
        }

        //Returns the edgeSize field
        public double EdgeSize
        {
            get { return edgeSize; }
            set { edgeSize = value; }
        }

        //Takes a node as a parameter, and then returns a list of all nodes which are connected to it
        public List<Node> GetNeighbours(Node selectedNode)
        {
            List<Node> finalList = new List<Node>();
            //List of edges that are connected to the selected node
            List<Edge> edgeList = selectedNode.Edges;
            //Array of nodes that are connected to a given edge
            Node[] nodeArray;
            //Loops through each edge
            for (int i = 0; i < edgeList.Count; i++)
            {
                //Gets the nodes connected to each edge
                nodeArray = edgeList[i].Connections;
                //Any edge connected to a selected node will connect two nodes: the selected node and another. 
                //This if statement checks for the position in the array where the selected node is assigned, and then adds the node in the other position in the edge's array to the finalList list.
                if (nodeArray[0] == selectedNode)
                {
                    finalList.Add(nodeArray[1]);
                }
                else
                {
                    finalList.Add(nodeArray[0]);
                }
            }
            return finalList;
        }

        //Gets a node from its associated ID
        private Node GetNodeFromID(int ID)
        {
            foreach (Node n in nodes)
            {
                if (n.ID == ID)
                {
                    return n;
                }
            }
            return null;
        }

        //Gets a node from its associated label
        private Node GetNodeFromLabel(string label)
        {
            foreach (Node n in nodes)
            {
                if (n.Label == label)
                {
                    return n;
                }
            }
            return null;
        }

        //Gets an edge from its associated ID
        public Edge GetEdgeFromID(int ID)
        {
            foreach (Edge e in edges)
            {
                if (e.ID == ID)
                {
                    return e;
                }
            }
            return null;
        }

        #endregion

        #region Events

        //Used for removing edges between nodes
        private void deleteEdge_Click(object sender, RoutedEventArgs e)
        {
            MenuItem m = (MenuItem)sender;
            foreach (Edge ed in edges)
            {
                //finds the edge which the user has clicked on
                if (ed.ID == Convert.ToInt32(m.Header))
                {
                    //Removes edge from edges list
                    edges.Remove(ed);
                    //Edge is removed from canvas
                    ed.UndrawEdge();
                    Node[] ns = ed.Connections;
                    //Edge is removed from the 2 connected nodes
                    ns[0].RemoveEdge(ed);
                    ns[1].RemoveEdge(ed);
                    return;
                }
            }

        }

        //Deletes a node
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            RemoveEllipses();
            MenuItem m = (MenuItem)sender;
            List<Node> Neighbours;
            //Gets the node to be deleted
            Node deleted = GetNodeFromID(Convert.ToInt32(m.Tag));
            //Creates an array to store the edges to be deleted
            Edge[] delEdges = new Edge[deleted.Edges.Count];
            Neighbours = GetNeighbours(deleted);
            //Array now stores the edges to be deleted
            deleted.Edges.CopyTo(delEdges);
            foreach (Edge ed in delEdges)
            {
                foreach (Node n in Neighbours)
                {
                    //Removes the edge from all nodes it is connected to
                    n.RemoveEdge(ed);
                }
                //Removes the edge from the canvas
                ed.UndrawEdge();
            }
            //Removes the node from the canvas
            c.Children.Remove(deleted.Shape);
            //Removes the node from the nodes list
            nodes.Remove(deleted);
        }

        //Adds the clicked node to the key nodes list
        private void key_Checked(object sender, RoutedEventArgs e)
        {
            MenuItem m = (MenuItem)sender;
            keyNodes.Add(GetNodeFromID(Convert.ToInt32(m.Tag)));
        }

        //Removes the clicked node from the key nodes list
        private void key_Unchecked(object sender, RoutedEventArgs e)
        {
            MenuItem m = (MenuItem)sender;
            keyNodes.Remove(GetNodeFromID(Convert.ToInt32(m.Tag)));
        }

        //Fired when the mouse is released over an ellipse
        private void Circle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LabelledEllipse selected = (LabelledEllipse)sender;
            clickDrag = false;
            if (placing)
            {
                //If an edge is being drawn, the line following the cursor is removed from the canvas
                c.Children.Remove(l);
                //The ellipses used to draw the edge are removed from the canvas
                RemoveEllipses();
                placing = false;
                //An edge is created between the 2 nodes
                CreateEdge(edgeNode, GetNodeFromID(selected.ID));
                //The visibility of the text box within the ellipse is changed to visible
                foreach (Node n in nodes)
                {
                    n.Shape.TextVisibility = Visibility.Visible;
                }
            }
        }

        //Fired when the mouse is moved over an ellipse
        private void Circle_MouseMove(object sender, MouseEventArgs e)
        {
            LabelledEllipse le = (LabelledEllipse)sender;
            if (clickDrag && !le.txt.IsFocused && !placing)
            {
                //Moves the node's position according to the mouse position if the node has previously been clicked and an edge isnt being placed
                RemoveEllipses();
                LabelledEllipse circle = (LabelledEllipse)sender;
                Node n = GetNodeFromID(circle.ID);
                Vector v = new Vector(e.GetPosition(c).X - n.Position.X, e.GetPosition(c).Y - n.Position.Y);
                n.UpdatePosition(v);
            }
        }

        //Fired when an ellipse is left-clicked
        private void Circle_LeftClick(object sender, MouseButtonEventArgs e)
        {
            clickDrag = true;
            placing = false;
            LabelledEllipse selected = (LabelledEllipse)sender;
            clickedNode = GetNodeFromID(selected.ID);
            //Changes the text of the size textbox on the toolstrip to the size of the clicked node
            nSize.Text = selected.ActualWidth.ToString();
            //The edgeNode field used as the first node when drawing a n edge is set to the node clicked
            edgeNode = GetNodeFromID(selected.ID);
            //Creates 4 new ellipse objects and stores them within the ellipses array
            ellipses = new Ellipse[4];
            double x = Canvas.GetLeft(selected);
            double y = Canvas.GetTop(selected);
            for (int i = 0; i < 4; i++)
            {
                ellipses[i] = new Ellipse();
                ellipses[i].Height = selected.Height / 6;
                ellipses[i].Width = selected.Width / 6;
                ellipses[i].Stroke = Brushes.Black;
                ellipses[i].Fill = Brushes.White;
                ellipses[i].MouseDown += LineDrag;
            }
            //Positions the drawing ellipses around the node, sets their Z indexes to above the node and then adds them to the canvas
            Canvas.SetLeft(ellipses[0], x - ellipses[0].Width / 2);
            Canvas.SetTop(ellipses[0], y + selected.Height / 2 - ellipses[0].Height / 2);
            Canvas.SetZIndex(ellipses[0], 2);
            Canvas.SetLeft(ellipses[1], x + selected.Width / 2 - ellipses[0].Width / 2);
            Canvas.SetTop(ellipses[1], y - ellipses[0].Height / 2);
            Canvas.SetZIndex(ellipses[1], 2);
            Canvas.SetLeft(ellipses[2], x + selected.Width / 2 - ellipses[0].Width / 2);
            Canvas.SetTop(ellipses[2], y + selected.Height - ellipses[0].Height / 2);
            Canvas.SetZIndex(ellipses[2], 2);
            Canvas.SetLeft(ellipses[3], x + selected.Width - ellipses[0].Width / 2);
            Canvas.SetTop(ellipses[3], y + selected.Height / 2 - ellipses[0].Height / 2);
            Canvas.SetZIndex(ellipses[3], 2);
            for (int i = 0; i < 4; i++)
            {
                c.Children.Add(ellipses[i]);
            }
            selected.Focus();
        }

        //Fired when the mouse is clicked on top of a drawing ellipse
        private void LineDrag(object sender, MouseButtonEventArgs e)
        {
            placing = true;
            l = new Line();
            //Makes the text boxes of the labelled ellipse invisible
            foreach (Node n in nodes)
            {
                n.Shape.TextVisibility = Visibility.Hidden;
            }
            Ellipse selected = (Ellipse)sender;
            //Sets the first coordinate of the line to the midpoint of the clicked ellipse
            l.X1 = Canvas.GetLeft(selected) + selected.Width / 2;
            l.Y1 = Canvas.GetTop(selected) + selected.Height / 2;
            c.Children.Add(l);
        }

        //Fires when the mouse moves across the canvas
        private void C_MouseMove(object sender, MouseEventArgs e)
        {
            if (placing)
            {
                //if an edge is being placed, the line's 2nd coordinate follows the mouse
                l.X2 = e.GetPosition(c).X - 1;
                l.Y2 = e.GetPosition(c).Y - 5;
                l.Stroke = Brushes.Black;
                Canvas.SetZIndex(l, 3);
            }
        }

        //Fires when the mouse click is let go above the canvas
        private void C_MouseUp(object sender, MouseEventArgs e)
        {
            clickDrag = false;
            if (placing)
            {
                //Removes the drawing ellipses from the canvas
                RemoveEllipses();
                //Removes the drawing line from the canvas
                c.Children.Remove(l);
                //Makes the labelled ellipses's text boxes visible
                foreach (Node n in nodes)
                {
                    n.Shape.TextVisibility = Visibility.Visible;
                }
            }
        }

        //Focuses the canvas when it is clicked
        private void C_MouseDown(object sender, MouseButtonEventArgs e)
        {
            c.Focus();
        }

        //Fires when a labelled ellipse loses focus
        private void Circle_LostFocus(object sender, RoutedEventArgs e)
        {
            //Sets clickedNode to null and removes drawing ellipses around it
            clickedNode = null;
            RemoveEllipses();
        }

        //Fires when a key is pressed when an ellipse is focused
        private void Circle_KeyDown(object sender, KeyEventArgs e)
        {
            LabelledEllipse le = (LabelledEllipse)sender;
            if (e.Key == Key.Delete)
            {
                RemoveEllipses();
                List<Node> Neighbours;
                //Gets the node to be deleted
                Node deleted = GetNodeFromID(Convert.ToInt32(le.ID));
                //Creates an array to store the edges to be deleted
                Edge[] delEdges = new Edge[deleted.Edges.Count];
                Neighbours = GetNeighbours(deleted);
                //Array now stores the edges to be deleted
                deleted.Edges.CopyTo(delEdges);
                foreach (Edge ed in delEdges)
                {
                    foreach (Node n in Neighbours)
                    {
                        //Removes the edge from all nodes it is connected to
                        n.RemoveEdge(ed);
                    }
                    //Removes the edge from the canvas
                    ed.UndrawEdge();
                }
                //Removes the node from the canvas
                c.Children.Remove(deleted.Shape);
                //Removes the node from the nodes list
                nodes.Remove(deleted);
            }
            //Allows the user to use the common chords to make their textbox bold, italic or underlined
            if (le.txt.IsFocused)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    Node n = GetNodeFromID(le.ID);
                    if (Keyboard.IsKeyDown(Key.B))
                    {
                        n.Font.Bold = !n.Font.Bold;
                        n.Shape.TxtFont = n.Font;
                    }
                    else if (Keyboard.IsKeyDown(Key.I))
                    {
                        n.Font.Italic = !n.Font.Italic;
                        n.Shape.TxtFont = n.Font;
                    }
                    else if (Keyboard.IsKeyDown(Key.U))
                    {
                        n.Font.Underline = !n.Font.Underline;
                        n.Shape.TxtFont = n.Font;
                    }
                }
            }
        }

        //Fires when a line loses focus
        private void L_LostFocus(object sender, RoutedEventArgs e)
        {
            Line l = (Line)sender;
            //Turns it back to the colour it was before it was selected
            l.Stroke = GetEdgeFromID(Convert.ToInt32(l.Tag)).LineStroke;
            //Sets clickedEdge to null
            clickedEdge = null;
        }

        //Fires when a line is clicked
        private void L_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Line l = (Line)sender;
            //Focuses the line
            l.Focus();
            //Sets the clickedEdge field
            clickedEdge = GetEdgeFromID(Convert.ToInt32(l.Tag));
            //Sets the text of the edge size textbox to the thickness of the selected line
            eSize.Text = l.StrokeThickness.ToString();
            //Sets the colour of the colour button to the colour of the clicked line
            colourButton.Background = l.Stroke;
            //Turns the line blue to indicate that it has been clicked
            l.Stroke = Brushes.Blue;
        }

        //Fires when a key is pressed when a line is focused
        private void L_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                Line l = (Line)sender;
                l.LostFocus -= L_LostFocus;
                Edge ed = GetEdgeFromID(Convert.ToInt32(l.Tag));
                //Removes the edge from everything it is associated with
                edges.Remove(ed);
                ed.UndrawEdge();
                ed.Connections[0].RemoveEdge(ed);
                ed.Connections[1].RemoveEdge(ed);
            }
        }

        //Fires when the text of a labelled ellipse is changed
        private void Circle_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            //Sets the connected text of a node to the text written in the textbox of the labelled ellipse
            GetNodeFromID(Convert.ToInt32(txt.Tag)).Label = txt.Text;
        }

        //Fired when the Set Image menu item of a node is clicked
        private void SetImage_Click(object sender, RoutedEventArgs e)
        {
            MenuItem m = (MenuItem)sender;
            Node n = GetNodeFromID(Convert.ToInt32(m.Tag));
            n.Shape.SetImage();
        }

        //Fired when the Remove Image menu item of a node is clicked
        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            MenuItem m = (MenuItem)sender;
            Node n = GetNodeFromID(Convert.ToInt32(m.Tag));
            n.Shape.RemoveImage();
        }

        //Fired when the delete textbox menu item is clicked
        private void M_Click(object sender, RoutedEventArgs e)
        {
            MenuItem m = (MenuItem)sender;
            TextBox[] boxes = new TextBox[txtBoxes.Count];
            txtBoxes.CopyTo(boxes);
            //Removes the textbox from the canvas's children and the textbox list
            foreach (TextBox txt in boxes)
            {
                if (txt.Tag.ToString() == m.Tag.ToString())
                {
                    txtBoxes.Remove(txt);
                    c.Children.Remove(txt);
                }
            }
        }

        //Fired when the mouse moves over a textbox
        private void Txt_MouseMove(object sender, MouseEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            double x;
            double y;
            //If the mouse is held while it passes over the textbox, the textbox is moved to follow the mouse
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                x = e.GetPosition(c).X;
                y = e.GetPosition(c).Y;
                if (x + txt.ActualWidth / 2 > c.ActualWidth)
                {
                    x = c.ActualWidth - txt.ActualWidth / 2;
                }
                else if (x - txt.ActualWidth / 2 < 0)
                {
                    x = txt.ActualWidth / 2;
                }
                if (y + txt.ActualHeight / 2 > c.ActualHeight)
                {
                    y = c.ActualHeight - txt.ActualHeight / 2;
                }
                else if (y - txt.ActualHeight / 2 < 0)
                {
                    y = txt.ActualHeight / 2;
                }
                Canvas.SetLeft(txt, x - txt.ActualWidth / 2);
                Canvas.SetTop(txt, y - txt.ActualHeight / 2);
            }
        }

        //Fired when the mouse moves over an image
        private void I_MouseMove(object sender, MouseEventArgs e)
        {
            Image i = (Image)sender;
            double x;
            double y;
            //If the mouse is held while it passes over the image, the image moves to follow the mouse
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                x = e.GetPosition(c).X;
                y = e.GetPosition(c).Y;
                if (x + i.ActualWidth / 2 > c.ActualWidth)
                {
                    x = c.ActualWidth - i.ActualWidth / 2;
                }
                else if (x - i.ActualWidth / 2 < 0)
                {
                    x = i.ActualWidth / 2;
                }
                if (y + i.ActualHeight / 2 > c.ActualHeight)
                {
                    y = c.ActualHeight - i.ActualHeight / 2;
                }
                else if (y - i.ActualHeight / 2 < 0)
                {
                    y = i.ActualHeight / 2;
                }
                Canvas.SetLeft(i, x - i.ActualWidth / 2);
                Canvas.SetTop(i, y - i.ActualHeight / 2);
            }
        }

        //Focuses an image when it is clicked
        private void I_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Image i = (Image)sender;
            i.Focus();
        }

        //Fired when a key is pressed while the image is focused
        private void I_KeyDown(object sender, KeyEventArgs e)
        {
            Image i = (Image)sender;
            //If the delete key was pressed then the image is removed from the graph
            if (e.Key == Key.Delete)
            {
                c.Children.Remove(i);
                images.Remove(i);
            }
        }

        //Uses common chords to allow the user to toggle bold, italics or underlines with keyboard shortcuts
        private void Txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                TextBox txt = (TextBox)sender;
                if (Keyboard.IsKeyDown(Key.B))
                {
                    txt.FontWeight = (txt.FontWeight == FontWeights.Normal) ? FontWeights.Bold : FontWeights.Normal;
                }
                else if (Keyboard.IsKeyDown(Key.I))
                {
                    txt.FontStyle = (txt.FontStyle == FontStyles.Normal) ? FontStyles.Italic : FontStyles.Normal;
                }
                else if (Keyboard.IsKeyDown(Key.U))
                {
                    txt.TextDecorations = (txt.TextDecorations == TextDecorations.Baseline) ? TextDecorations.Underline : TextDecorations.Baseline;
                }
            }
        }

        #endregion

        #region Methods

        //Takes an x and a y ordinate and then instantiates a new node with them, adding it to the nodes list.
        public Node NodePlace(double x, double y, bool doubled, int size)
        {
            //Creates a labelled ellipse object
            LabelledEllipse circle;
            //Creates a new node and assigns its ellipse to circle
            Node n = new Node(x, y, size, c, doubled, font, ellipseBrush, out circle);
            //Adds node to node list
            nodes.Add(n);
            //Assigns events to the shape in the node
            circle.Focusable = true;
            circle.IsEnabled = true;
            circle.MouseLeftButtonDown += Circle_LeftClick;
            circle.MouseUp += Circle_MouseUp;
            circle.MouseMove += Circle_MouseMove;
            circle.TextChanged += Circle_TextChanged;
            //The menu item used for deleting an edge
            MenuItem deleteEdge = new MenuItem
            {
                Header = "Edge",
                FontSize = 12,
                Width = 200,
                Height = 25
            };
            //The menu item used to toggle a key node
            MenuItem key = new MenuItem
            {
                Header = "Key",
                FontSize = 12,
                Width = 200,
                Height = 25,
                Tag = circle.ID.ToString(),
                IsCheckable = true
            };
            //The menu item used for deleting a shape and its corresponding node
            MenuItem delete = new MenuItem
            {
                Header = "Delete",
                FontSize = 12,
                Width = 200,
                Height = 25,
                Tag = circle.ID.ToString()
            };
            //The menu item used for setting an image
            MenuItem setImage = new MenuItem
            {
                Header = "Add Image",
                FontSize = 12,
                Width = 200,
                Height = 25,
                Tag = circle.ID.ToString()
            };
            //The menu item used for removing an image
            MenuItem removeImage = new MenuItem
            {
                Header = "Remove Image",
                FontSize = 12,
                Width = 200,
                Height = 25,
                Tag = circle.ID.ToString()
            };
            //Assigns events and adds them to the context menu of the shape
            delete.Click += Delete_Click;
            key.Checked += key_Checked;
            key.Unchecked += key_Unchecked;
            setImage.Click += SetImage_Click;
            removeImage.Click += RemoveImage_Click;
            circle.KeyDown += Circle_KeyDown;
            circle.LostFocus += Circle_LostFocus;
            circle.ContextMenu.Items.Add(deleteEdge);
            circle.ContextMenu.Items.Add(delete);
            circle.ContextMenu.Items.Add(key);
            circle.ContextMenu.Items.Add(setImage);
            circle.ContextMenu.Items.Add(removeImage);
            return n;
        }

        //Used to change whether bold is enabled or not
        public void ChangeBold()
        {
            font.Bold = !font.Bold;
            //If a node is selected then the text's weight will be toggled
            foreach (Node n in nodes)
            {
                if (n.Shape.txt.IsSelectionActive)
                {
                    n.Font.Bold = !n.Font.Bold;
                    n.Shape.TxtFont = n.Font;
                }
            }
            //If a textbox is selected then the text's weight will be toggled
            foreach (TextBox txt in txtBoxes)
            {
                if (txt.IsSelectionActive)
                {
                    txt.FontWeight = (font.Bold == true) ? FontWeights.Bold : FontWeights.Normal;
                }
            }
        }

        //Used to change whether italics are enabled
        public void ChangeItalic()
        {
            font.Italic = !font.Italic;
            //If a node is selected then the text's style will be toggled
            foreach (Node n in nodes)
            {
                if (n.Shape.txt.IsSelectionActive)
                {
                    n.Font.Italic = !n.Font.Italic;
                    n.Shape.TxtFont = n.Font;
                }
            }
            //If a textbox is selected then the text's style will be toggled
            foreach (TextBox txt in txtBoxes)
            {
                if (txt.IsSelectionActive)
                {
                    txt.FontStyle = (font.Italic == true) ? FontStyles.Italic : FontStyles.Normal;
                }
            }
        }

        //Used to change whether an underline is enabled
        public void ChangeUnderline()
        {
            font.Underline = !font.Underline;
            //If a node is selected then the text's underline will be toggled
            foreach (Node n in nodes)
            {
                if (n.Shape.txt.IsSelectionActive)
                {
                    n.Font.Underline = !n.Font.Underline;
                    n.Shape.TxtFont = n.Font;
                }
            }
            //If a textbox is selected then the text's underline will be toggled
            foreach (TextBox txt in txtBoxes)
            {
                if (txt.IsSelectionActive)
                {
                    txt.TextDecorations = (font.Underline == true) ? TextDecorations.Underline : null;
                }
            }
        }

        //Takes two nodes and then instantiates a new edge, passing the nodes as parameters. It then adds it to the edges list.
        public void CreateEdge(Node node1, Node node2)
        {
            //Used to ensure that an edge cannot be drawn between 2 nodes twice
            Node[] comp = { node1, node2 };
            Line l;
            //Breaks if the 2 nodes are the same
            if (node1.ID == node2.ID)
            {
                return;
            }
            //Breaks if there is already an existing edge between the two nodes
            foreach (Edge ed in edges)
            {
                if (ed.Connections.SequenceEqual(comp) || ed.Connections.SequenceEqual(comp.Reverse()))
                {
                    return;
                }
            }
            //Instantiates a new edge and assigns its line object to l
            Edge e = new Edge(node1, node2, edgeSize, ellipseBrush, c, out l);
            l.IsEnabled = true;
            l.Focusable = true;
            l.MouseLeftButtonDown += L_MouseDown;
            l.KeyDown += L_KeyDown;
            l.LostFocus += L_LostFocus;
            //Adds the edge to the first node's edges
            node1.AddEdge(e);
            //Creates a new menu item and adds it to the context menu of the node's labelled ellipse
            MenuItem m1 = new MenuItem
            {
                Header = e.ID,
                FontSize = 12,
                Width = 110,
                Height = 25
            };
            m1.Click += deleteEdge_Click;
            foreach (MenuItem menu in node1.Shape.ContextMenu.Items)
            {
                if (menu.Header.ToString() == "Edge")
                {
                    menu.Items.Add(m1);
                }
            }
            //Adds the edge to the second node's edges
            node2.AddEdge(e);
            //Creates a new menu item and adds it to the context menu of the node's labelled ellipse
            MenuItem m2 = new MenuItem
            {
                Header = e.ID,
                FontSize = 12,
                Width = 110,
                Height = 25
            };
            m2.Click += deleteEdge_Click;
            foreach (MenuItem menu in node2.Shape.ContextMenu.Items)
            {
                if (menu.Header.ToString() == "Edge")
                {
                    menu.Items.Add(m2);
                }
            }
            //Adds the new edge to the edges list
            edges.Add(e);
        }

        //Used to remove drawing ellipses from a node
        private void RemoveEllipses()
        {
            if (ellipses != null)
            {
                foreach (Ellipse el in ellipses)
                {
                    c.Children.Remove(el);
                }
            }
        }

        //Returns the direction vector of a node to another
        public Vector DirectionVector(Node node1, Node node2)
        {
            Vector direction = new Vector();
            direction.X = node2.Position.X - node1.Position.X;
            direction.Y = node2.Position.Y - node1.Position.Y;
            return direction;
        }

        //Moves nodes according to forces
        public bool Direct()
        {
            //Removes drawing ellipses if they exist
            RemoveEllipses();
            //Total vector repulsion
            Vector totalRep;
            //Total vector attraction
            Vector totalAtt;
            Vector[] force = new Vector[nodes.Count];
            Vector direction;
            List<Node> nodeLis;
            //Loops through the node list
            for (int i = 0; i < nodes.Count; i++)
            {
                Node n = nodes[i];
                //If the key nodes list contains the current node then a velocity vector is not calculated
                if (!keyNodes.Contains(n))
                {
                    //Gets the neighbours of the current node
                    nodeLis = GetNeighbours(n);
                    //Sets the values of the two velocity vectors
                    totalAtt = new Vector(0, 0);
                    totalRep = new Vector(0, 0);
                    //Loops through neighbours of current node
                    foreach (Node no in nodeLis)
                    {
                        direction = DirectionVector(n, no);
                        //The attraction vector is calculated from the natural log of the distance between the two nodes, multiplied by the direction vector
                        totalAtt += Math.Log(direction.Length) * no.NodeSize * 0.02 * direction;
                    }
                    foreach (Node no in nodes)
                    {
                        //Ensures that a vector does not repel itself
                        if (n.ID != no.ID)
                        {
                            direction = DirectionVector(no, n);
                            //Repulsion vector is calculated by dividing the direction vector by the distance between the nodes, raised to a power
                            //Cubing the distance was chosen to enhance the "inverse square law" effect
                            //Ensures you cannot divide by 0
                            if (direction.Length > 0.1)
                            {
                                totalRep += no.NodeSize * 0.02 * 40000 * direction / Math.Pow(direction.Length, 3);
                            }
                        }
                    }
                    //The force applied to each node is the total attraction and total repulsion vectors summed, with some constants to acheive a good equilibrium
                    force[i] = (0.006 * totalAtt) + totalRep;
                }
            }
            //Updates the position of each node
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].UpdatePosition(0.1 * force[i]);
            }
            //Calculates if the total force applied to all nodes is less than 1. If it is then the force direction is disabled
            double total = 0;
            foreach (Vector v in force)
            {
                total += Math.Abs(v.X) + Math.Abs(v.Y);
            }
            if (total < 8)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Exports the contents of the canvas
        public void CreateBitmap()
        {
            List<UIElement> canChildren = new List<UIElement>();
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)c.ActualWidth, (int)c.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            //Unzooms canvas
            ScaleTransform st = (ScaleTransform)c.RenderTransform;
            st.ScaleX = 1;
            st.ScaleY = 1;
            //Adds all children of the canvas to the list
            foreach (UIElement ui in c.Children)
            {
                canChildren.Add(ui);
            }
            //Removes gridlines
            c.Children.RemoveRange(0, 28);
            //Renders the bitmap
            rtb.Render(c);
            //Clears the children of the canvas
            c.Children.Clear();
            //Adds all children of the canvas back
            foreach (UIElement ui in canChildren)
            {
                c.Children.Add(ui);
            }
            SaveFileDialog sd = new SaveFileDialog();
            sd.Title = "Save";
            sd.Filter = "JPEG Image (.jpeg)|*.jpeg|PNG Image (.png)|*.png|Bitmap Image (.bmp)|*.bmp";
            if (sd.ShowDialog() == true)
            {
                //Saves the bitmap as a jpeg to the file location determined by the save file dialog
                using (FileStream stream = File.Create(sd.FileName))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    encoder.Save(stream);
                }
            }
        }

        //Gets the string for saving the graph
        public string Save()
        {
            string save = "";
            //Adds the data from each node, separated by a line break
            foreach (Node n in nodes)
            {
                save += n.GetSaveString();
                save += "\n";
            }
            //Separates objects with a semicolon
            save += ";";
            //Adds the data from each edge, separated by a line break
            foreach (Edge e in edges)
            {
                save += e.GetSaveString();
                save += "\n";
            }
            save += ";";
            //Adds data from the graph
            save += edgeSize + ",";
            save += font.Family + ",";
            save += font.Size + ",";
            save += font.Bold + ",";
            save += font.Italic + ",";
            save += font.Underline + ",";
            save += Node.NextID + ",";
            save += Edge.NextID + ",";
            save += textBoxId + ",";
            save += ";";
            //Adds data from each textbox and image
            CustomFont cf = new CustomFont();
            foreach (TextBox txt in txtBoxes)
            {
                cf.BoldFromMedia(txt.FontWeight);
                cf.ItalicFromMedia(txt.FontStyle);
                cf.UnderlineFromMedia(txt.TextDecorations);
                save += txt.FontFamily.ToString() + ",";
                save += txt.FontSize + ",";
                save += cf.Bold + ",";
                save += cf.Italic + ",";
                save += cf.Underline + ",";
                save += Canvas.GetLeft(txt) + ",";
                save += Canvas.GetTop(txt) + ",";
                save += txt.ActualWidth + ",";
                save += txt.ActualHeight + ",";
                save += txt.Tag.ToString() + ",";
                string newLabel = "";
                //Replaces any instances of the split characters with greek letters
                foreach (char c in txt.Text)
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
                save += newLabel + "," + "\n";
            }
            save += ";";
            foreach (Image i in images)
            {
                save += Canvas.GetLeft(i) + ",";
                save += Canvas.GetTop(i) + ",";
                save += i.ActualWidth + ",";
                save += i.ActualHeight + ",";
                string newLabel = "";
                //Replaces any instances of the split characters with greek letters
                foreach (char c in i.Source.ToString())
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
                save += newLabel + "," + "\n";
            }
            return save;
        }

        //Loads data from a file
        public void Load(string FileName)
        {
            try
            {
                //Reads text from the file and stores it in loadString
                string loadString = File.ReadAllText(FileName);
                //Splits loadstring by semicolons
                string[] splitSemiC = loadString.Split(';');
                string[] splitLineB;
                string[] splitCommas;
                //Node
                //Splits the semicolon split by linebreaks
                splitLineB = splitSemiC[0].Split('\n');
                for (int i = 0; i < splitLineB.Length - 1; i++)
                {
                    //Splits the linebreak split by commas
                    splitCommas = splitLineB[i].Split(',');
                    string newLabel = "";
                    //Replaces any instances of greek letters with the split characters
                    foreach (char c in splitCommas[13])
                    {
                        if (c == 'α')
                        {
                            newLabel += ",";
                        }
                        else if (c == 'β')
                        {
                            newLabel += "\n";
                        }
                        else if (c == 'γ')
                        {
                            newLabel += ";";
                        }
                        else
                        {
                            newLabel += c;
                        }
                    }
                    //Creates a new labelled ellipse
                    LabelledEllipse circle;
                    //Creates the font from the file
                    CustomFont cf = new CustomFont
                    {
                        Family = splitCommas[4],
                        Size = Convert.ToDouble(splitCommas[5]),
                        Bold = Convert.ToBoolean(splitCommas[6]),
                        Italic = Convert.ToBoolean(splitCommas[7]),
                        Underline = Convert.ToBoolean(splitCommas[8])
                    };
                    //Creates a colour from the file
                    SolidColorBrush sb = new SolidColorBrush
                    {
                        Color = Color.FromArgb(Convert.ToByte(splitCommas[9]), Convert.ToByte(splitCommas[10]), Convert.ToByte(splitCommas[11]), Convert.ToByte(splitCommas[12]))
                    };
                    //Creates a new node
                    nodes.Add(new Node(Convert.ToDouble(splitCommas[1]), Convert.ToDouble(splitCommas[2]), Convert.ToInt32(splitCommas[0]), Convert.ToInt32(splitCommas[3]), c, Convert.ToBoolean(splitCommas[14]), cf, sb, newLabel, out circle));
                    //Adds events and context menu items to the new node
                    circle.Focusable = true;
                    circle.IsEnabled = true;
                    circle.MouseLeftButtonDown += Circle_LeftClick;
                    circle.MouseUp += Circle_MouseUp;
                    circle.MouseMove += Circle_MouseMove;
                    circle.TextChanged += Circle_TextChanged;
                    //The menu item used for deleting an edge
                    MenuItem deleteEdge = new MenuItem
                    {
                        Header = "Edge",
                        FontSize = 12,
                        Width = 200,
                        Height = 25
                    };
                    //The menu item used to toggle a key node
                    MenuItem key = new MenuItem
                    {
                        Header = "Key",
                        FontSize = 12,
                        Width = 200,
                        Height = 25,
                        Tag = circle.ID.ToString(),
                        IsCheckable = true
                    };
                    //The menu item used for deleting a shape and its corresponding node
                    MenuItem delete = new MenuItem
                    {
                        Header = "Delete",
                        FontSize = 12,
                        Width = 200,
                        Height = 25,
                        Tag = circle.ID.ToString()
                    };
                    //The menu item used for adding an image to a node
                    MenuItem setImage = new MenuItem
                    {
                        Header = "Add Image",
                        FontSize = 12,
                        Width = 200,
                        Height = 25,
                        Tag = circle.ID.ToString()
                    };
                    //The menu item used to remove an image from a node
                    MenuItem removeImage = new MenuItem
                    {
                        Header = "Remove Image",
                        FontSize = 12,
                        Width = 200,
                        Height = 25,
                        Tag = circle.ID.ToString()
                    };
                    //Assigns events and adds them to the context menu of the shape
                    delete.Click += Delete_Click;
                    key.Checked += key_Checked;
                    key.Unchecked += key_Unchecked;
                    setImage.Click += SetImage_Click;
                    removeImage.Click += RemoveImage_Click;
                    circle.KeyDown += Circle_KeyDown;
                    circle.LostFocus += Circle_LostFocus;
                    circle.ContextMenu.Items.Add(deleteEdge);
                    circle.ContextMenu.Items.Add(delete);
                    circle.ContextMenu.Items.Add(key);
                    circle.ContextMenu.Items.Add(setImage);
                    circle.ContextMenu.Items.Add(removeImage);
                }
                //Edge
                //Splits semicolon split by linebreaks
                splitLineB = splitSemiC[1].Split('\n');
                for (int i = 0; i < splitLineB.Length - 1; i++)
                {
                    //Splits linebreak split by commas
                    splitCommas = splitLineB[i].Split(',');
                    //Finds connected nodes through ids
                    Node node1 = GetNodeFromID(Convert.ToInt32(splitCommas[1]));
                    Node node2 = GetNodeFromID(Convert.ToInt32(splitCommas[2]));
                    //Creates the colour of the edge through the file
                    SolidColorBrush sb = new SolidColorBrush(Color.FromArgb(Convert.ToByte(splitCommas[4]), Convert.ToByte(splitCommas[5]), Convert.ToByte(splitCommas[6]), Convert.ToByte(splitCommas[7])));
                    Line l;
                    string newLabel = "";
                    //Replaces any instances of greek letters with the split characters
                    foreach (char c in splitCommas[8])
                    {
                        if (c == 'α')
                        {
                            newLabel += ",";
                        }
                        else if (c == 'β')
                        {
                            newLabel += "\n";
                        }
                        else if (c == 'γ')
                        {
                            newLabel += ";";
                        }
                        else
                        {
                            newLabel += c;
                        }
                    }
                    //Creates a new edge
                    Edge e = new Edge(node1, node2, Convert.ToInt32(splitCommas[0]), Convert.ToDouble(splitCommas[3]), sb, c, newLabel, splitCommas[9], out l);
                    //Assigns events to the new edge
                    l.IsEnabled = true;
                    l.Focusable = true;
                    l.MouseLeftButtonDown += L_MouseDown;
                    l.KeyDown += L_KeyDown;
                    l.LostFocus += L_LostFocus;
                    //Adds the edge to the first node's edges
                    node1.AddEdge(e);
                    //Creates a new menu item and adds it to the context menu of the node's labelled ellipse
                    MenuItem m1 = new MenuItem
                    {
                        Header = e.ID,
                        FontSize = 12,
                        Width = 110,
                        Height = 25
                    };
                    m1.Click += deleteEdge_Click;
                    foreach (MenuItem menu in node1.Shape.ContextMenu.Items)
                    {
                        if (menu.Header.ToString() == "Edge")
                        {
                            menu.Items.Add(m1);
                        }
                    }
                    //Adds the edge to the second node's edges
                    node2.AddEdge(e);
                    //Creates a new menu item and adds it to the context menu of the node's labelled ellipse
                    MenuItem m2 = new MenuItem
                    {
                        Header = e.ID,
                        FontSize = 12,
                        Width = 110,
                        Height = 25
                    };
                    m2.Click += deleteEdge_Click;
                    foreach (MenuItem menu in node2.Shape.ContextMenu.Items)
                    {
                        if (menu.Header.ToString() == "Edge")
                        {
                            menu.Items.Add(m2);
                        }
                    }
                    //Adds the new edge to the edges list
                    edges.Add(e);
                }
                //Graph
                //Splits semicolon split by commas
                splitCommas = splitSemiC[2].Split(',');
                edgeSize = Convert.ToDouble(splitCommas[0]);
                font.Family = splitCommas[1];
                font.Size = Convert.ToDouble(splitCommas[2]);
                font.Bold = Convert.ToBoolean(splitCommas[3]);
                font.Italic = Convert.ToBoolean(splitCommas[4]);
                font.Underline = Convert.ToBoolean(splitCommas[5]);
                Node.NextID = Convert.ToInt32(splitCommas[6]);
                Edge.NextID = Convert.ToInt32(splitCommas[7]);
                textBoxId = Convert.ToInt32(splitCommas[8]);
                //Text boxes
                //Splits semicolon split by linebreaks
                splitLineB = splitSemiC[3].Split('\n');
                for (int i = 0; i < splitLineB.Length - 1; i++)
                {
                    //Splits linebreak split by commas
                    splitCommas = splitLineB[i].Split(',');
                    //Makes the font of the textbox through the file
                    CustomFont cf = new CustomFont
                    {
                        Family = splitCommas[0],
                        Size = Convert.ToDouble(splitCommas[1]),
                        Bold = Convert.ToBoolean(splitCommas[2]),
                        Italic = Convert.ToBoolean(splitCommas[3]),
                        Underline = Convert.ToBoolean(splitCommas[4])
                    };
                    //Creates new textbox
                    TextBox txt = new TextBox
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(2),
                        Width = Convert.ToDouble(splitCommas[7]),
                        Height = Convert.ToDouble(splitCommas[8]),
                        Tag = splitCommas[9]
                    };
                    //Adds textbox to the list of textboxes
                    txtBoxes.Add(txt);
                    cf.ModifyMediaTxtBox(ref txt);
                    //Positions textbox
                    Canvas.SetLeft(txt, Convert.ToDouble(splitCommas[5]));
                    Canvas.SetTop(txt, Convert.ToDouble(splitCommas[6]));
                    string newLabel = "";
                    //Replaces any instances of greek letters with the split characters
                    foreach (char c in splitCommas[10])
                    {
                        if (c == 'α')
                        {
                            newLabel += ",";
                        }
                        else if (c == 'β')
                        {
                            newLabel += "\n";
                        }
                        else if (c == 'γ')
                        {
                            newLabel += ";";
                        }
                        else
                        {
                            newLabel += c;
                        }
                    }
                    //Menu item used to delete a textbox
                    MenuItem m = new MenuItem
                    {
                        Header = "Delete",
                        FontSize = 12,
                        Width = 120,
                        Height = 25,
                        Tag = txt.Tag.ToString()
                    };
                    //Assigns events to textbox
                    m.Click += M_Click;
                    txt.ContextMenu = new ContextMenu();
                    txt.ContextMenu.Items.Add(m);
                    txt.MouseMove += Txt_MouseMove;
                    txt.Text = newLabel;
                    //Adds textbox to canvas's children
                    c.Children.Add(txt);
                }
                //Images
                //Splits semicolon split by linebreaks
                splitLineB = splitSemiC[4].Split('\n');
                for (int i = 0; i < splitLineB.Length - 1; i++)
                {
                    //Splits lineberak split by commas
                    splitCommas = splitLineB[i].Split(',');
                    //Creates a new image
                    Image im = new Image
                    {
                        Width = Convert.ToDouble(splitCommas[2]),
                        Height = Convert.ToDouble(splitCommas[3]),
                        Focusable = true,
                        IsEnabled = true
                    };
                    //Adds it to the images list
                    images.Add(im);
                    //Positions the image
                    Canvas.SetLeft(im, Convert.ToDouble(splitCommas[0]));
                    Canvas.SetTop(im, Convert.ToDouble(splitCommas[1]));
                    string newLabel = "";
                    //Replaces any instances of greek letters with the split characters
                    foreach (char c in splitCommas[4])
                    {
                        if (c == 'α')
                        {
                            newLabel += ",";
                        }
                        else if (c == 'β')
                        {
                            newLabel += "\n";
                        }
                        else if (c == 'γ')
                        {
                            newLabel += ";";
                        }
                        else
                        {
                            newLabel += c;
                        }
                    }
                    //Sets the image, assigns events and adds it to the children of the canvas
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(newLabel);
                    bmp.DecodePixelWidth = Convert.ToInt32(im.ActualWidth);
                    bmp.EndInit();
                    im.Source = bmp;
                    im.MouseDown += I_MouseDown;
                    im.KeyDown += I_KeyDown;
                    im.MouseMove += I_MouseMove;
                    c.Children.Add(im);
                }
                MessageBox.Show("Load Successful");
            }
            catch (Exception)
            {
                MessageBox.Show("Load Unsuccessful");
            }
        }

        //Used to add a new textbox to the canvas
        public void CreateTextbox()
        {
            //Creates the new textbox
            TextBox txt = new TextBox
            {
                Height = 60,
                Width = 100,
                IsEnabled = true,
                Focusable = true,
                Tag = textBoxId,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(4)
            };
            //Adds it to the txtBoxes field
            txtBoxes.Add(txt);
            //Menu item used for deleting a textbox
            MenuItem m = new MenuItem
            {
                Header = "Delete",
                FontSize = 12,
                Width = 120,
                Height = 25,
                Tag = textBoxId
            };
            //Increments the textBoxId field
            textBoxId++;
            //Gives the menu item its click event
            m.Click += M_Click;
            txt.ContextMenu = new ContextMenu();
            txt.ContextMenu.Items.Add(m);
            txt.MouseMove += Txt_MouseMove;
            txt.KeyDown += Txt_KeyDown;
            //Adds textbox to the canvas's children
            c.Children.Add(txt);
        }

        //Used to add a new image to the canvas
        public void CreateImage()
        {
            //Creates the new image
            Image i = new Image();
            i.IsEnabled = true;
            i.Focusable = true;
            //Assigns events to the image
            i.MouseDown += I_MouseDown;
            i.KeyDown += I_KeyDown;
            i.MouseMove += I_MouseMove;
            //Starts a dialog to open a file to be displayed in the image
            OpenFileDialog od = new OpenFileDialog();
            od.Title = "Open";
            od.Filter = "Image Files (*.bmp, *.jpeg, *.png)|*.bmp;*.jpg;*.png";
            if (od.ShowDialog() == true)
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(od.FileName);
                bmp.DecodePixelWidth = 100;
                bmp.EndInit();
                i.Source = bmp;
                c.Children.Add(i);
                images.Add(i);
            }
        }

        //Used to check whether traversal is possible, and then creates the dialog
        public void TraverseDialog()
        {
            //Checks if there are nodes on the graph
            if (!nodes.Any())
            {
                MessageBox.Show("There are no nodes.");
                return;
            }
            //Checks if there are any unconnected nodes
            foreach (Node n in nodes)
            {
                if (!GetNeighbours(n).Any())
                {
                    MessageBox.Show("Not all nodes have connections.");
                    return;
                }
            }
            bool breadthFirst;
            //Creates the dialog and calls the traverse method
            SearchDialog sd = new SearchDialog(nodes);
            if (sd.ShowDialog() == true)
            {
                breadthFirst = sd.BreadthFirst;
                if (sd.isID)
                {
                    Traverse(GetNodeFromID(Convert.ToInt32(sd.selectedNode)), breadthFirst, sd.isID);
                }
                else
                {
                    Traverse(GetNodeFromLabel(sd.selectedNode), breadthFirst, sd.isID);
                }
            }
        }

        //Used to call the different traversal algorithms
        private void Traverse(Node start, bool breadthFirst, bool isID)
        {
            //List containing the strings of ids/labels of nodes in traversal order
            List<string> traversedString = new List<string>();
            if (breadthFirst)
            {
                //Calls the breadth first method with the selected start node
                Breadth_First(start);
                //If IDs were used then the resulting window will display IDs, otherwise it will display labels
                if (isID)
                {
                    foreach (Node n in traversedOrder)
                    {
                        traversedString.Add(n.ID.ToString());
                    }
                }
                else
                {
                    foreach (Node n in traversedOrder)
                    {
                        traversedString.Add(n.Label);
                    }
                }
            }
            else
            {
                //Calls the depth first method with the selected start node
                Depth_First(start);
                //If IDs were used then the resulting window will display IDs, otherwise it will display labels
                if (isID)
                {
                    foreach (Node n in traversedOrder)
                    {
                        traversedString.Add(n.ID.ToString());
                    }
                }
                else
                {
                    foreach (Node n in traversedOrder)
                    {
                        traversedString.Add(n.Label);
                    }
                }
            }
            //Shows the traversal output window
            TraversalOutput to = new TraversalOutput(traversedString);
            to.ShowDialog();
            //Clears the dictionary and traversed order list
            visited.Clear();
            traversedOrder.Clear();
        }

        //Recursive method used to create the depth first algorithm
        private void Depth_First(Node currentNode)
        {
            //Adds the current node to the visited list
            visited.Add(currentNode, true);
            //Adds the current node to the traversal list
            traversedOrder.Add(currentNode);
            //If a connection to the current node has not been visited, call Depth_First with the connected node as the current node
            foreach (Node n in GetNeighbours(currentNode))
            {
                if (!visited.ContainsKey(n))
                {
                    Depth_First(n);
                }
            }
        }

        //Breadth first algorithm
        private void Breadth_First(Node startNode)
        {
            //Creates a queue structure for exploring nodes
            Queue<Node> visitQ = new Queue<Node>();
            //Enqueues the starting node
            visitQ.Enqueue(startNode);
            //Adds the start node to visited
            visited.Add(startNode, true);
            Node currentNode;
            //While visited has anything queued
            while (visitQ.Any())
            {
                //Dequeues the first item in the queue
                currentNode = visitQ.Dequeue();
                traversedOrder.Add(currentNode);
                //Loops through neighbours to the current node. If they have not been visited they are added to the queue and visited dictionary
                foreach (Node n in GetNeighbours(currentNode))
                {
                    if (!visited.ContainsKey(n))
                    {
                        visitQ.Enqueue(n);
                        visited.Add(n, true);
                    }
                }
            }
        }

        #endregion
    }
}
