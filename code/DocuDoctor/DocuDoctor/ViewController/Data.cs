using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using DocuDoctor.Model;
using SkiaSharp;

namespace DocuDoctor.ViewController
{
    [Serializable]
    /// <summary>
    /// Holds all stored data for main window
    /// </summary>
    public class Data
    {
        // Main list where uml boxes are stored in
        protected List<UmlBox> m_boxes;
        public List<UmlBox> Boxes { get { return m_boxes; } }

        protected List<string> m_files;
        public List<string> Files { get { return m_files; } }

        // The box being currently moved (used when you click and drag a box)
        private UmlBox m_movedBox;
        public UmlBox MovedBox { get { return m_movedBox; } set { m_movedBox = value; } }

        // Current scale of the SKCanvas area
        private float m_scale;
        public float Scale { get { return m_scale; } set { m_scale = value; } }
        // Current translation from 0,0 of the SKCanvas area
        private float m_translationX;
        public float TranslationX { get { return m_translationX; } set { m_translationX = value; } }
        private float m_translationY;
        
        public float TranslationY { get { return m_translationY; } set { m_translationY = value; } }
        // Current box being displayed on the propreties pages
        private UmlBox m_selectedForProperties;
        public UmlBox? SelectedForProperties { get { return m_selectedForProperties; } set { m_selectedForProperties = value; } }
        // DataTables that are bound to the property tables, allows for easier manipulation into the property pages
        private DataTable m_propertyTable;
        public DataTable PropertyTable { get { return m_propertyTable; } set { m_propertyTable = value; } }
        private DataTable m_methodTable;
        public DataTable MethodTable { get { return m_methodTable; } set { m_methodTable = value; } }
        private DataTable m_parameterTable;
        public DataTable ParameterTable { get { return m_parameterTable; } set { m_parameterTable = value; } }
        // Avoids issues with property pages being updated simulatenously
        public bool methodSwitchDone;
        // Current button selected on toolbar, kept as an id 0-x
        public ToolState toolbarSelection;

        // Boolean used for frontend to determine when to print
        private bool m_exportPhoto;
        public bool ExportPhoto { get { return m_exportPhoto; } set { m_exportPhoto = value; } }

        private string m_filePath;
        public string FilePath { get { return m_filePath; } set { m_filePath = value; } }

        public Data()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Data : Data                                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Initializer for backend data handling                ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_boxes = new List<UmlBox>();
            m_movedBox = null;
            m_selectedForProperties = null;
            m_scale = 1; 
            m_translationX = 0; m_translationY = 0;
            m_propertyTable = new DataTable("PropertyTable");
            m_methodTable = new DataTable("MethodTable");
            m_parameterTable = new DataTable("ParameterTable");
            methodSwitchDone = true;
            toolbarSelection = 0;
            m_exportPhoto = false;
            m_filePath = "";
        }

        public UmlBox AddBox(SKPoint pos)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddBox : Data                                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Adds a new box at the point specified                ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            UmlBox box = new UmlBox("Class", "NewClass", (int)pos.X, (int)pos.Y);
            m_selectedForProperties = box;
            m_boxes.Add(box);
            CalculateWidthHeight(box);
            return box;
        }



        public bool ReadFiles(string[] fileNames)
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
            bool updatedBoxes = false;
            List<UmlBox> boxes = new List<UmlBox>();
            Queue<KeyValuePair<long, string>> subtypePairs = new();
            foreach(string fileName in fileNames) {
                Parser parser = new Parser(fileName);
                boxes.AddRange(parser.ParseFile(subtypePairs));
            }
            
            float xOffset = 0;
            foreach(UmlBox box in boxes) {
                CalculateWidthHeight(box);
                box.X = xOffset+5;
                xOffset += box.Width;
                m_boxes.Add(box);

                updatedBoxes = true;
                m_selectedForProperties = box;
            }
            while(subtypePairs.Count > 0) {
                KeyValuePair<long, string> nameIdPair = subtypePairs.Dequeue();
                UmlBox? source = FindBoxFromID(BoxNameToBoxID(nameIdPair.Value));
                if(source != null) {
                    source.AddArrow(nameIdPair.Key, 0);
                }
            }
            return updatedBoxes;
        }

        
        public UmlBox AddBox(SKPoint pos, string type)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddBox : Data                                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Adds a new box at the point specified, with the type ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            UmlBox box = new UmlBox(type, "NewClass", (int)pos.X, (int)pos.Y);
            m_selectedForProperties = box;
            m_boxes.Add(box);
            CalculateWidthHeight(box);
            return box;
        }

        public UmlBox? FindBoxAtCoords(float x, float y)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: FindBoxAtCoords : Data                                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Searches through boxes and finds the one at (x,y)    ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
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
                        m_boxes.RemoveAt(i);
                        m_boxes.Add(b);
                        break;
                    }
                }
            }

            return selectedBox;
        }

        public void DrawArrow(SKCanvas canvas, (float, float) boxOneCoords, (float, float) boxTwoCoords, bool isDotted)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: DrawArrow : Data                                      ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Draws an arrow between two points                    ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
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

        public Tuple<UmlBox, long> FindArrowAtCoords(float x, float y)
        {
            const float TOLERANCE = 5.0f; // Max distance to consider the point on the arrow
            Dictionary<long, UmlBox> d = new Dictionary<long, UmlBox>();

            foreach (UmlBox box in m_boxes)
                d.Add(box.ID, box);

            foreach (UmlBox box in m_boxes)
            {
                for (int j = box.Arrows.Count - 1; j >= 0; j--)
                {
                    long i = box.Arrows[j].Item1;
                    int t = box.Arrows[j].Item2;
                    if (d.TryGetValue(i, out UmlBox arrowEnd))
                    {
                        Vector[] startPoints = box.ArrowPoints;
                        Vector[] endPoints = arrowEnd.ArrowPoints;

                        Vector start = startPoints[0];
                        Vector end = endPoints[0];
                        double length = (start - end).Length;

                        // Find the shortest arrow between the two boxes
                        for (int k = 0; k < startPoints.Length; k++)
                        {
                            Vector iStart = startPoints[k];
                            for (int k2 = 0; k2 < endPoints.Length; k2++)
                            {
                                Vector iEnd = endPoints[k2];
                                if ((iStart - iEnd).Length < length)
                                {
                                    start = iStart;
                                    end = iEnd;
                                    length = (iEnd - iStart).Length;
                                }
                            }
                        }

                        // Check if (x, y) is on this arrow
                        if (IsPointOnLineSegment(start, end, new Vector(x, y), TOLERANCE))
                        {
                            return Tuple.Create(box, i); // Return the arrow ID if found
                        }
                    }
                    else
                    {
                        // The arrow end position does not exist anymore, remove it
                        box.Arrows.RemoveAt(j);
                    }
                }
            }
            return Tuple.Create<UmlBox, long>(null, -1); // No arrow found at the given coordinates
        }

        // Checks if a point is on a line segment within a tolerance
        private bool IsPointOnLineSegment(Vector A, Vector B, Vector P, float tolerance)
        {
            Vector AB = B - A;
            Vector AP = P - A;
            Vector BP = P - B;

            // Projection scalar t of P onto line AB
            double t = (AP * AB) / (AB * AB);

            // Clamp t between 0 and 1 to stay within segment
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            // Closest point on the line segment to P
            Vector closest = A + t * AB;

            // Check if distance from P to closest point is within tolerance
            return (P - closest).Length <= tolerance;
        }
        public void RedrawAllArrows(SKCanvas canvas)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: RedrawAllArrows : Data                                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Draws all arrows to the screen                       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // Get each box into a hashmap of id, box pairs
            Dictionary<long, UmlBox> d = new Dictionary<long, UmlBox>();
            foreach (UmlBox box in m_boxes) d.Add(box.ID, box);
            foreach (UmlBox box in m_boxes) {
                for (int j = box.Arrows.Count-1; j >= 0; j--) {
                    long i = box.Arrows[j].Item1;
                    int t = box.Arrows[j].Item2;
                    if(d.TryGetValue(i, out UmlBox arrowEnd)) {
                        
                        // Find the shortest arrow between the two boxs
                        Vector[] startPoints = box.ArrowPoints;
                        Vector[] endPoints = arrowEnd.ArrowPoints;
                        Vector start = startPoints[0];
                        Vector end = endPoints[0];
                        double length = (start-end).Length;
                        for (int k = 0; k < startPoints.Length; k++) {
                            Vector iStart = startPoints[k];
                            for (int k2 = 0; k2 < endPoints.Length; k2++) {
                                Vector iEnd = endPoints[k2];
                                if ((iStart - iEnd).Length < length) {
                                    start = iStart;
                                    end = iEnd;
                                    length = (iEnd - iStart).Length;
                                }
                            }
                        }

                        DrawArrow(canvas, ((float, float))(start.X, start.Y), ((float, float))(end.X, end.Y), t == 1);
                    }
                    // The else block being hit means the arrows end pos does not exist anymore
                    // Therefore it should be removed from the list of arrows
                    else
                        box.Arrows.RemoveAt(j);
                }
            }
        }

        public void RedrawAllBoxes(SKCanvas canvas)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: RedrawAllBoxes : Data                                 ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Refreshes screen by redrawing all boxes              ::
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

        public long BoxNameToBoxID(string boxName)
           /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
           :: 1. Method: BoxNameToBoxID : Data                                 ::
           :: ---------------------------------------------------------------- ::
           :: 2. Author: Riley Horling                                         ::
           :: 3. Purpose: Finds a if a box with the inputed name exists returns::
           ::    -1 if it doesnt                                               ::
           ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            foreach(UmlBox b in m_boxes) {
                if(b.Name.ToLower() == boxName.ToLower()) return b.ID;
            }
            return -1;
        }

        private UmlBox? FindBoxFromID(long id) {
            foreach(UmlBox b in m_boxes) {
                if(b.ID == id) return b;
            }
            return null;
        }

        public bool MoveBox(float x, float y, float deltaX, float deltaY)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: MoveBox : Data                                        ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Moves box at given x and y by deltaX and deltaY      ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            UmlBox? selectedBox = FindBoxAtCoords(x, y);
            if (selectedBox == null) return false;
            selectedBox.X += deltaX; selectedBox.Y += deltaY;
            m_movedBox = selectedBox;
            SelectedForProperties = selectedBox;
            return true;
        }

        public void RemoveBox(SKPoint mPos)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: RemoveBox : Data                                      ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: removes a box from the screen at defined point       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            float x = mPos.X;
            float y = mPos.Y;
            Tuple<UmlBox, long> arrowID = FindArrowAtCoords(x, y);
            Debug.WriteLine(arrowID);
            if (arrowID.Item1 != null)
            {
                arrowID.Item1.RemoveArrow(arrowID.Item2);
                return;
            }

            // Search backwards, so you move the topmost box (since topmost is inherently drawn last aka on top)
            for (int i = m_boxes.Count - 1; i >= 0; i--)
            {
                UmlBox b = m_boxes[i];
                if ((b.X <= x) && (x < (b.X + b.Width)) && (b.Y <= y) && (y < (b.Y + b.Height)))
                {
                    // Delete the topmost box at that position, aka, what is being acted on
                    m_boxes.RemoveAt(i);
                    m_selectedForProperties = null;
                    return;
                }
            }


        }

        public void RemoveBox(UmlBox b) {
            m_boxes.Remove(b);
        }

        public void CalculateWidthHeight(UmlBox box)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: CalculateWidthHeight : Data                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Given a box, set its width and height properties     ::
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

            if (box.Variables.Count > 0 && box.Methods.Count > 0) totalHeight += lineHeight / 2;
            box.Width = maxWidth + 20; box.Height = totalHeight + 20;
        }

        public void SelectBox(SKPoint mPos)
        {
            float x = mPos.X;
            float y = mPos.Y;
            // Search backwards, so you select the topmost box (since topmost is inherently drawn last aka on top)
            for (int i = m_boxes.Count - 1; i >= 0; i--)
            {
                UmlBox b = m_boxes[i];
                if ((b.X <= x) && (x < (b.X + b.Width)) && (b.Y <= y) && (y < (b.Y + b.Height)))
                {
                    // Move selected box to top
                    m_boxes.RemoveAt(i);
                    m_boxes.Add(b);
                    // Select the topmost box at that position, aka, what is being acted on
                    m_selectedForProperties = b;
                    SelectedForProperties = b;
                    return;
                }
            }
        }

        private void DisplayBox(UmlBox box, SKCanvas canvas)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: DisplayBox : Data                                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Draws background, and text for a given box           ::
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
            SKPaint boxPaint = new SKPaint {
                Color = SKColors.LightGray,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            if (box.BoxType == "Class") boxPaint.Color = new SKColor(236, 248, 255);
            else if (box.BoxType == "Interface") boxPaint.Color = new SKColor(238, 255, 225);
            else if (box.BoxType == "Template") boxPaint.Color = new SKColor(255, 216, 201);
            canvas.DrawRect(new SKRect(x, y, x + box.Width, y + box.Height), boxPaint);
            // Draw The Border
            SKPaint borderPaint = new SKPaint {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            // Highlight box if seleted
            if (SelectedForProperties != null && box == SelectedForProperties) {
                borderPaint.Color = SKColors.Yellow;
                borderPaint.StrokeWidth = 5;
            }
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
            // Draw the dividing line
            if (box.Variables.Count > 0 && box.Methods.Count > 0) {
                textY -= lineHeight / 2;
                canvas.DrawLine(new SKPoint(textX, textY), new SKPoint(textX + box.Width - 20, textY), borderPaint);
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
    public enum ToolState {
        Select, AddClass, AddTemplate, AddInterface, RemoveBox, AddArrow, AddDashedArrow
    }

}
