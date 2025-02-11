using DocuDoctor.Model;
using SkiaSharp;

namespace DocuDoctor.ViewController {
    /// <summary>
    /// Holds all stored data for main window
    /// </summary>
    internal class Data {
        protected List<UmlBox> m_boxes;
        public List<UmlBox> Boxes { get { return m_boxes; } }

        protected List<string> m_files;
        public List<string> Files { get { return m_files; } }

        private UmlBox m_movedBox;
        public UmlBox MovedBox { get { return m_movedBox; } set { m_movedBox = value; } }

        private float m_scale;
        public float Scale { get { return m_scale; } set { m_scale = value; } }
        private float m_translationX;
        public float TranslationX { get { return m_translationX; } set { m_translationX = value; } }
        private float m_translationY;
        public float TranslationY { get { return m_translationY; } set { m_translationY = value; } }

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
            m_scale = 1;
            m_translationX = 0;
            m_translationY = 0;
        }

        public void AddBox(SKPoint pos)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddBox : Data                                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Adds a new box at the point specified                ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: pos - position defining the box             ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            UmlBox box = new UmlBox("Class", "TestClass", (int)pos.X, (int)pos.Y);
            m_boxes.Add(box);
            box.AddMethod("public", "int", "TestMethod", [["int", "testOne"], ["double", "testTwo"]]);
            box.AddVariable("public", "float", "TestVariable");
            CalculateWidthHeight(box);
        }



        public bool ReadFile(string fileName)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ReadFile : Data                                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Riley Horling                                         ::
        :: 3. Created: 1/28/2025                                            ::
        :: 4. Purpose: Takes in a file name and reads it and                ::
        ::    adds any registered boxes                                     ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: fileName - path to the file to be read      ::
        :: 6. Output Parameters: bool, did the file contain any classes     ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            Parser parser = new Parser(fileName);
            foreach(UmlBox box in parser.ParseFile()) {
                CalculateWidthHeight(box);
                m_boxes.Add(box);

            }
            return true;
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
            foreach(UmlBox b in m_boxes)
                DisplayBox(b, canvas);
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
            // If you are already moving a box, keep moving that one
            // Find the box you are trying to move based on the x y coordinates
            UmlBox selectedBox = m_movedBox;
            if(selectedBox == null) {
                // Search backwards, so you move the topmost box (since topmost is inherently drawn last aka on top)
                for(int i = m_boxes.Count - 1; i >= 0; i--) {
                    UmlBox b = m_boxes[i];
                    if(b.X <= x && x < b.X + b.Width && b.Y <= y && y < b.Y + b.Height) {
                        selectedBox = b;
                        // Move the current box to the end of the boxlist so its drawn on top
                        m_boxes.RemoveAt(i);
                        m_boxes.Add(b);
                        break;
                    }
                }
            }
            if(selectedBox == null)
                return false;
            selectedBox.X += deltaX;
            selectedBox.Y += deltaY;
            m_movedBox = selectedBox;
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
            float x = mPos.X;
            float y = mPos.Y;
            // Search backwards, so you move the topmost box (since topmost is inherently drawn last aka on top)
            for(int i = m_boxes.Count - 1; i >= 0; i--) {
                UmlBox b = m_boxes[i];
                if(b.X <= x && x < b.X + b.Width && b.Y <= y && y < b.Y + b.Height) {
                    // Delete the topmost box at that position, aka, what is being acted on
                    m_boxes.RemoveAt(i);
                    return;
                }
            }
        }

        private void CalculateWidthHeight(UmlBox box)
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
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            SKPaint textPaint = new SKPaint {
                Color = SKColors.White,
                TextSize = 24,
                IsAntialias = true
            };
            float maxWidth = 0;
            float totalHeight = 0;
            float lineHeight = textPaint.TextSize + 10;
            // Header for the box itself
            float textWidth = textPaint.MeasureText(box.ToString());
            if(textWidth > maxWidth)
                maxWidth = textWidth;
            totalHeight += lineHeight;
            // All the variables
            foreach(UmlVariable v in box.Variables) {
                textWidth = textPaint.MeasureText(v.ToString());
                if(textWidth > maxWidth)
                    maxWidth = textWidth;
                totalHeight += lineHeight;
            }
            // All the methods
            foreach(UmlMethod m in box.Methods) {
                textWidth = textPaint.MeasureText(m.ToString());
                if(textWidth > maxWidth)
                    maxWidth = textWidth;
                totalHeight += lineHeight;
            }
            box.Width = maxWidth + 20;
            box.Height = totalHeight + 20;
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
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // Create variables to store the x and y (may improve lookup times EVER so slightly not fully sure)
            float x = box.X;
            float y = box.Y;
            SKPaint textPaint = new SKPaint {
                Color = SKColors.White,
                TextSize = 24,
                IsAntialias = true
            };
            float lineHeight = textPaint.TextSize + 10;
            // Draw The Box
            SKPaint boxPaint = new SKPaint {
                Color = SKColors.LightGray,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(new SKRect(x, y, x + box.Width, y + box.Height), boxPaint);
            // Draw The Border
            SKPaint borderPaint = new SKPaint {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawRect(x, y, box.Width, box.Height, borderPaint);
            // Clip the canvas
            canvas.Save();
            canvas.ClipRect(new SKRect(x, y, x + box.Width, y + box.Height));
            float textX = x + 10;
            float textY = y + 10 + textPaint.TextSize;
            // Display the box header
            canvas.DrawText(box.ToString(), textX, textY, textPaint);
            textY += lineHeight;
            // Display the variables
            foreach(UmlVariable v in box.Variables) {
                canvas.DrawText(v.ToString(), textX, textY, textPaint);
                textY += lineHeight;
            }
            // Display the methods
            foreach(UmlMethod m in box.Methods) {
                canvas.DrawText(m.ToString(), textX, textY, textPaint);
                textY += lineHeight;
            }
            canvas.Restore();
        }
    }
}
