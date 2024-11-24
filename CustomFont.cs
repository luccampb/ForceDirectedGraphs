using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Force_Directed_Graphs
{
    public class CustomFont
    {
        //Font family
        private string family;
        //Font size
        private double size;
        //Whether it is bold or not
        private bool bold;
        //Whether it is in italics or not
        private bool italic;
        //Whether it is underlined or not
        private bool underline;

        public CustomFont() { }

        #region Properties

        //Properties for each field
        public string Family
        {
            get { return family; }
            set { family = value; }
        }
        public double Size
        {
            get { return size; }
            set { size = value; }
        }
        public bool Bold
        {
            get { return bold; }
            set { bold = value; }
        }
        public bool Italic
        {
            get { return italic; }
            set { italic = value; }
        }
        public bool Underline
        {
            get { return underline; }
            set { underline = value; }
        }

        #endregion

        #region Methods

        //Sets the bold field from a System.Windows.Media FontWeight
        public void BoldFromMedia(FontWeight weight)
        {
            if(weight == FontWeights.Bold)
            {
                bold = true;
            }
            else
            {
                bold = false;
            }
        }

        //Sets the italic field from a System.Windows.Media FontStyle
        public void ItalicFromMedia(FontStyle style)
        {
            if (style == FontStyles.Italic)
            {
                italic = true;
            }
            else
            {
                italic = false;
            }
        }

        //Sets the underline field from a System.Windows.Media TextDecoration
        public void UnderlineFromMedia(TextDecorationCollection deco)
        {
            if (deco == TextDecorations.Underline)
            {
                underline = true;
            }
            else
            {
                underline = false;
            }
        }

        //Takes a textbox by reference as a parameter, and sets its text properties according to the fields of the custom font class
        public void ModifyMediaTxtBox(ref TextBox txt)
        {
            txt.FontFamily = new FontFamily(family);
            txt.FontSize = size;
            txt.FontWeight = (bold == true) ? FontWeights.Bold : FontWeights.Normal;
            txt.FontStyle = (italic == true) ? FontStyles.Italic : FontStyles.Normal;
            txt.TextDecorations = (underline == true) ? TextDecorations.Underline : null;
        }

        #endregion
    }
}
