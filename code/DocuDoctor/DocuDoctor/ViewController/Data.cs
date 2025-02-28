using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using DocuDoctor.Model;
using SkiaSharp;

namespace DocuDoctor.ViewController
{
    /// <summary>
    /// Holds all stored data for main window
    /// </summary>
    internal class Data
    {
        protected List<UmlBox> m_boxes;
        public List<UmlBox> Boxes { get { return m_boxes; } }

        private UmlBox m_movedBox;
        public UmlBox MovedBox { get { return m_movedBox; } set { m_movedBox = value; } }

        private float m_scale;
        public float Scale { get { return m_scale; } set { m_scale = value; } }
        private float m_translationX;
        public float TranslationX { get { return m_translationX; } set { m_translationX = value; } }
        private float m_translationY;
        public float TranslationY { get { return m_translationY; } set { m_translationY = value; } }

        private UmlBox m_selectedForProperties;
        public UmlBox SelectedForProperties { get { return m_selectedForProperties; } set { 
                m_selectedForProperties = value; } }

        private DataTable m_propertyTable;
        public DataTable PropertyTable { get { return m_propertyTable; } set { m_propertyTable = value; } }
        private DataTable m_methodTable;
        public DataTable MethodTable { get { return m_methodTable; } set { m_methodTable = value; } }
        private DataTable m_parameterTable;
        public DataTable ParameterTable { get { return m_parameterTable; } set { m_parameterTable = value; } }

        public bool methodSwitchDone;

        public int toolbarSelection;

        public Data()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Data : Data                                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Initializer for backend data handling                ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_boxes = new List<UmlBox>();
            m_movedBox = null;
            m_selectedForProperties = null;
            m_scale = (float)Screen.PrimaryScreen.Bounds.Width/1920; m_translationX = 0; m_translationY = 0;
            m_propertyTable = new DataTable();
            m_methodTable = new DataTable();
            m_parameterTable = new DataTable();
            methodSwitchDone = true;
            toolbarSelection = 0;
        }

        public UmlBox AddBox(SKPoint pos)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddBox : Data                                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Adds a new box at the point specified                ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: pos - position defining the box             ::
        :: 6. Output Parameters: box - box that was just created            ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: 1/28/25 - Added output parameter for box       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            UmlBox box = new UmlBox("Class", "NewClass", (int)pos.X, (int)pos.Y);
            m_selectedForProperties = box;
            m_boxes.Add(box);
            CalculateWidthHeight(box);
            return box;
        }

        public UmlBox AddBox(SKPoint pos, string type) {
            UmlBox box = new UmlBox(type, "NewClass", (int)pos.X, (int)pos.Y);
            m_selectedForProperties = box;
            m_boxes.Add(box);
            CalculateWidthHeight(box);
            return box;
        }

        public UmlBox? FindBoxAtCoords(float x, float y) {
            // If you are already moving a box, keep moving that one
            // Find the box you are trying to move based on the x y coordinates
            UmlBox selectedBox = m_movedBox;
            if (selectedBox == null)
            {
                // Search backwards, so you move the topmost box (since topmost is inherently drawn last aka on top)
                for (int i = m_boxes.Count - 1; i >= 0; i--)
                {
                    UmlBox b = m_boxes[i];
                    if (b.X <= x && x < b.X + b.Width && b.Y <= y && y < b.Y + b.Height)
                    {
                        selectedBox = b;
                        // Move the current box to the end of the boxlist so its drawn on top
                        m_boxes.RemoveAt(i); m_boxes.Add(b);
                        break;
                    }
                }
            }
            return selectedBox;
        }

        public void DrawArrow(SKCanvas canvas, (float, float) boxOneCoords, (float, float) boxTwoCoords, bool isDotted) {
            SKPaint arrowPaint = new SKPaint {
                Color = SKColors.White,
                StrokeWidth = 3,
                IsAntialias = true
            };
            SKPaint arrowPaintDotted = new SKPaint {
                Color = SKColors.White,
                StrokeWidth = 3,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 10, 10 }, 0) // Dotted pattern (10px on, 10px off)
            };
            // Draw the line
            if (isDotted) canvas.DrawLine(new SKPoint(boxOneCoords.Item1, boxOneCoords.Item2), new SKPoint(boxTwoCoords.Item1, boxTwoCoords.Item2), arrowPaintDotted);
            else canvas.DrawLine(new SKPoint(boxOneCoords.Item1, boxOneCoords.Item2), new SKPoint(boxTwoCoords.Item1, boxTwoCoords.Item2), arrowPaint);
            // Draw the arrowhead
            // Calculate the direction vector as atan2 value
            float angle = (float)Math.Atan2(boxTwoCoords.Item2-boxOneCoords.Item2, boxTwoCoords.Item1-boxOneCoords.Item1);
            float arrowAngleOne = angle+(float)Math.PI/6;
            float arrowAngleTwo = angle-(float)Math.PI/6;
            float arrowheadSize = 20;
            // Calculate arrowhead points
            SKPoint arrowPointOne = new SKPoint(
                boxTwoCoords.Item1 - arrowheadSize * (float)Math.Cos(arrowAngleOne),
                boxTwoCoords.Item2 - arrowheadSize * (float)Math.Sin(arrowAngleOne)
            );
            SKPoint arrowPointTwo = new SKPoint(
                boxTwoCoords.Item1 - arrowheadSize * (float)Math.Cos(arrowAngleTwo),
                boxTwoCoords.Item2 - arrowheadSize * (float)Math.Sin(arrowAngleTwo)
            );
            canvas.DrawLine(arrowPointOne, new SKPoint(boxTwoCoords.Item1, boxTwoCoords.Item2), arrowPaint);
            canvas.DrawLine(arrowPointTwo, new SKPoint(boxTwoCoords.Item1, boxTwoCoords.Item2), arrowPaint);
        }

        public void RedrawAllArrows(SKCanvas canvas) {
            // Get each box into a hashmap of id, box pairs
            Dictionary<long, UmlBox> d = new Dictionary<long, UmlBox>();
            foreach (UmlBox box in m_boxes) d.Add(box.ID, box);
            foreach (UmlBox box in m_boxes) {
                for (int j = box.Arrows.Count-1; j >= 0; j--) {
                    long i = box.Arrows[j].Item1;
                    int t = box.Arrows[j].Item2;
                    if (d.TryGetValue(i, out UmlBox arrowEnd)) DrawArrow(canvas, (box.X, box.Y), (arrowEnd.X, arrowEnd.Y), t==1);
                    // The else block being hit means the arrows end pos does not exist anymore
                    // Therefore it should be removed from the list of arrows
                    else box.Arrows.RemoveAt(j);
                }
            }
        }

        public void RedrawAllBoxes(SKCanvas canvas)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: RedrawAllBoxes : Data                                 ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Refreshes screen by redrawing all boxes              ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: canvas - canvas object of the main area     ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            foreach (UmlBox b in m_boxes) DisplayBox(b, canvas);
        }

        public void AddArrow(float x, float y, int arrowType) {
            // Return if no first box selected
            if (m_selectedForProperties == null) return;
            UmlBox? selectedBox = FindBoxAtCoords(x, y);
            // Return if no second box selected
            if (selectedBox == null) return;
            // Return if first and second box are the same
            if (selectedBox.ID == SelectedForProperties.ID) return;
            SelectedForProperties.AddArrow(selectedBox.ID, arrowType);
        }

        public bool MoveBox(float x, float y, float deltaX, float deltaY)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: MoveBox : Data                                        ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Moves box at given x and y by deltaX and deltaY      ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: x,y - position defining the original pos    ::
        ::            deltaX,deltaY - change in x and y                     ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            UmlBox? selectedBox = FindBoxAtCoords(x, y);
            if (selectedBox == null) return false;
            selectedBox.X += (float)1.5*deltaX; selectedBox.Y += (float)1.5* deltaY;
            m_movedBox = selectedBox;
            SelectedForProperties = selectedBox;
            return true;
        }

        public void RemoveBox(SKPoint mPos)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: RemoveBox : Data                                      ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: removes a box from the screen at defined point       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: pos - position defining the box             ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            float x = mPos.X; float y = mPos.Y;
            // Search backwards, so you move the topmost box (since topmost is inherently drawn last aka on top)
            for (int i = m_boxes.Count - 1; i >= 0; i--)
            {
                UmlBox b = m_boxes[i];
                if (b.X <= x && x < b.X + b.Width && b.Y <= y && y < b.Y + b.Height)
                {
                    // Delete the topmost box at that position, aka, what is being acted on
                    m_boxes.RemoveAt(i);
                    m_selectedForProperties = null;
                    return;
                }
            }
        }

        public void CalculateWidthHeight(UmlBox box)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: CalculateWidthHeight : Data                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Given a box, set its width and height properties     ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: pos - position defining the box             ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications:                                                ::
        :: Added a line between variables and methods                       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            SKPaint textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 24,
                IsAntialias = true
            };
            float maxWidth = 0; float totalHeight = 0;
            float lineHeight = textPaint.TextSize + 10;
            // Header for the box itself
            float textWidth = textPaint.MeasureText(box.ToString());
            if (textWidth > maxWidth) maxWidth = textWidth;
            totalHeight += lineHeight;
            // All the variables
            foreach (UmlVariable v in box.Variables)
            {
                textWidth = textPaint.MeasureText(v.ToString());
                if (textWidth > maxWidth) maxWidth = textWidth;
                totalHeight += lineHeight;
            }
            // All the methods
            foreach (UmlMethod m in box.Methods)
            {
                textWidth = textPaint.MeasureText(m.ToString());
                if (textWidth > maxWidth) maxWidth = textWidth;
                totalHeight += lineHeight;
            }
            if (box.Variables.Count > 0 && box.Methods.Count > 0) totalHeight += lineHeight / 2;
            box.Width = maxWidth + 20; box.Height = totalHeight + 20;
        }

        private void DisplayBox(UmlBox box, SKCanvas canvas)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: DisplayBox : Data                                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Draws background, and text for a given box           ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: box - box in question being drawn           ::
        ::                   canvas - canvas to be drawn on                 ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications:                                                ::
        :: Added a line between variables and methods                       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // Create variables to store the x and y (may improve lookup times EVER so slightly not fully sure)
            float x = box.X; float y = box.Y;
            SKPaint textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                IsAntialias = true
            };
            float lineHeight = textPaint.TextSize + 10;
            // Draw The Box
            SKPaint boxPaint = new SKPaint
            {
                Color = SKColors.LightGray,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            if (box.BoxType == "Class") boxPaint.Color = new SKColor(236, 248, 255);
            else if (box.BoxType == "Interface") boxPaint.Color = new SKColor(238, 255, 225);
            else if (box.BoxType == "Template") boxPaint.Color = new SKColor(255, 216, 201);
            canvas.DrawRect(new SKRect(x, y, x + box.Width, y + box.Height), boxPaint);
            // Draw The Border
            SKPaint borderPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawRect(x, y, box.Width, box.Height, borderPaint);
            // Clip the canvas
            canvas.Save();
            canvas.ClipRect(new SKRect(x,y,x+box.Width,y+box.Height));
            float textX = x + 10; float textY = y + 10 + textPaint.TextSize;
            // Display the box header
            canvas.DrawText(box.ToString(), textX, textY, textPaint);
            textY += lineHeight;
            // Display the variables
            foreach (UmlVariable v in box.Variables) {
                canvas.DrawText(v.ToString(), textX, textY, textPaint);
                textY += lineHeight;
            }
            // Draw the dividing line
            if (box.Variables.Count > 0 && box.Methods.Count > 0) {
                textY -= lineHeight / 2;
                canvas.DrawLine(new SKPoint(textX, textY), new SKPoint(textX + box.Width - 20, textY), borderPaint);
                textY += lineHeight;
            }
            // Display the methods
            foreach (UmlMethod m in box.Methods)
            {
                canvas.DrawText(m.ToString(), textX, textY, textPaint);
                textY += lineHeight;
            }
            canvas.Restore();
        }
    }
}
