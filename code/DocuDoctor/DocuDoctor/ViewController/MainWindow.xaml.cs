using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using DocuDoctor.Model;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
namespace DocuDoctor.ViewController
{
    /// <summary>
    /// Controller for DocuDoctor
    /// </summary>
    public partial class MainWindow : Window
    {
        private Data m_data;
        private bool m_clicked;
        private SKPoint m_lastMousePos;
        private SKPoint m_initialMousePos;
        private float m_initialTransformX;
        private float m_initialTransformY;
        private bool m_ctrlClicked;

        public MainWindow()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: MainWindow : MainWindow                               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 9/28/2024                                            ::
        :: 4. Purpose: Creator                                              ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            InitializeComponent();
            OnStartup();
            AddEvents();
            BindElements();
        }

        private void BindElements() {
            PropertyGrid.ItemsSource = m_data.PropertyTable.DefaultView;
            MethodGrid.ItemsSource = m_data.MethodTable.DefaultView;
            ParameterGrid.ItemsSource = m_data.ParameterTable.DefaultView;
        }

        private void AddEvents()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddEvents : MainWindow                                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Attaches events on initialization                    ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            skCanvas.MouseDown += SkCanvas_MouseDown;
            skCanvas.MouseMove += SkCanvas_MouseMove;
            skCanvas.MouseUp += SkCanvas_MouseUp;
            skCanvas.MouseWheel += SkCanvas_MouseWheel;
            KeyDown += Screen_KeyDown;
            KeyUp += Screen_KeyUp;
        }

        private void Screen_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Screen_KeyDown : MainWindow                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: handles events when you click keys in window         ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object that called this            ::
        ::                          e - input arguments for the key press   ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) m_ctrlClicked = true;
        }

        private void Screen_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Screen_KeyUp : MainWindow                             ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: handles events when you release keys in window       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object that called this            ::
        ::                          e - input arguments for the key press   ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) m_ctrlClicked = false;
        }

        private void SkCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseWheel : MainWindow                      ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: handles events when you when you scroll in window    ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object that called this            ::
        ::                          e - input arguments for the scroll      ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            float scaleFactor = 1.25f;
            if (!m_ctrlClicked) return;
            System.Windows.Point curPos = e.GetPosition(skCanvas);
            float oldScale = m_data.Scale;
            // Zoom in
            if (e.Delta > 0)
            {
                m_data.Scale *= scaleFactor;
            }
            // Zoom out
            else 
            {
                m_data.Scale /= scaleFactor;
            }
            m_data.TranslationX = (float)(curPos.X - (curPos.X - m_data.TranslationX) * (m_data.Scale / oldScale));
            m_data.TranslationY = (float)(curPos.Y - (curPos.Y - m_data.TranslationY) * (m_data.Scale / oldScale));
            m_initialMousePos = new SKPoint((float)(curPos.X),(float)curPos.Y);
            m_initialTransformX = m_data.TranslationX; m_initialTransformY = m_data.TranslationY;
            skCanvas.InvalidateVisual();
        }

        private void SkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseUp : MainWindow                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: handles events when you release mouse in skcanvas    ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object that called this            ::
        ::                          e - input arguments for the release     ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_data.MovedBox = null;
            m_clicked = false;
        }

        private void SkCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseMove : MainWindow                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: handles events when you move mouse in skcanvas       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object that called this            ::
        ::                          e - input argument for mouse move       ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (!m_clicked) return;
            else if (m_clicked && e.MouseDevice.LeftButton == MouseButtonState.Released) { m_clicked = false; return; }
            System.Windows.Point mPos = e.GetPosition(skCanvas);
            mPos.X -= m_data.TranslationX; mPos.X /= m_data.Scale;
            mPos.Y -= m_data.TranslationY; mPos.Y /= m_data.Scale;
            SKPoint curMousePos = new SKPoint((float)mPos.X, (float)mPos.Y);
            float deltaX = curMousePos.X - m_lastMousePos.X;
            float deltaY = curMousePos.Y - m_lastMousePos.Y;
            if (!m_data.MoveBox(m_lastMousePos.X, m_lastMousePos.Y, deltaX, deltaY) && m_ctrlClicked) {
                System.Windows.Point curPos = e.GetPosition(skCanvas);
                m_data.TranslationX = m_initialTransformX + ((float)curPos.X-m_initialMousePos.X) / m_data.Scale;
                m_data.TranslationY = m_initialTransformY + ((float)curPos.Y-m_initialMousePos.Y) /m_data.Scale;
            }
            UpdateProperties();
            skCanvas.InvalidateVisual();
            m_lastMousePos = curMousePos;
        }

        private void SkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseDown : MainWindow                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: handles events when you click mouse in skcanvas      ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object that called this            ::
        ::                          e - input arguments for the click       ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            System.Windows.Point mPos = e.GetPosition(skCanvas); 
            mPos.X -= m_data.TranslationX; mPos.X /= m_data.Scale;
            mPos.Y -= m_data.TranslationY; mPos.Y /= m_data.Scale;
            SKPoint internalPos = new((float)mPos.X, (float)mPos.Y);
            if (e.RightButton == MouseButtonState.Pressed) {
                if (m_ctrlClicked) m_data.RemoveBox(internalPos);
                else m_data.AddBox(internalPos); 
                skCanvas.InvalidateVisual();
                UpdateProperties();
                return; 
            }
            if (e.LeftButton == MouseButtonState.Released) return;
            m_lastMousePos = internalPos;
            m_initialMousePos = internalPos;
            m_initialTransformX = m_data.TranslationX; m_initialTransformY = m_data.TranslationY;
            m_clicked = true;
        }
        private void UpdateProperties()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: UpdateProperties : MainWindow                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/23/2025                                            ::
        :: 4. Purpose: Update properties panel with currently selected box  ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            UmlBox cur = m_data.SelectedForProperties;
            m_data.PropertyTable.Rows.Clear();
            m_data.MethodTable.Rows.Clear();
            m_data.ParameterTable.Rows.Clear();
            if (cur == null) { boxName.Text = ""; return; }
            boxName.Text = cur.Name;
            for (int i = 0; i < cur.Variables.Count; i++) {
                m_data.PropertyTable.Rows.Add(cur.Variables[i].Protection, cur.Variables[i].Type, cur.Variables[i].Name);
            }
            for (int i = 0; i < cur.Methods.Count; i++)
            {
                m_data.MethodTable.Rows.Add(cur.Methods[i].Protection, cur.Methods[i].Name, cur.Methods[i].Name);
            }
        }

        private void OnStartup()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: OnStartup : MainWindow                                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 9/28/2024                                            ::
        :: 4. Purpose: Actions to occur on startup of main window           ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_data = new Data();
            m_clicked = false;
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            m_ctrlClicked = false;
            ResetProperties();
        }

        private void ResetProperties() {
            boxName.Text = "";
            m_data.PropertyTable.Columns.Clear();
            m_data.PropertyTable.Rows.Clear();
            string[] ids = { "Protection","Type","Name" };
            for (int i = 0; i < 3; i++) m_data.PropertyTable.Columns.Add(ids[i], typeof(string));
            m_data.PropertyTable.RowChanged += PropertyTable_SyncChanges;
            m_data.PropertyTable.RowDeleted += PropertyTable_SyncChanges;
            m_data.PropertyTable.TableNewRow += PropertyTable_SyncChanges;

            m_data.MethodTable.Columns.Clear();
            m_data.MethodTable.Rows.Clear();
            ids = ["Protection", "Return Type", "Name"];
            for (int i = 0; i < 3; i++) m_data.MethodTable.Columns.Add(ids[i], typeof(string));
            m_data.MethodTable.RowChanged += MethodTable_SyncChanges;
            m_data.MethodTable.RowDeleted += MethodTable_SyncChanges;
            m_data.MethodTable.TableNewRow += MethodTable_SyncChanges;
            MethodGrid.SelectedCellsChanged += MethodTable_ChangeSelected;

            m_data.ParameterTable.Columns.Clear();
            m_data.ParameterTable.Rows.Clear();
            ids = ["Type", "Name"];
            for (int i = 0; i < 2; i++) m_data.ParameterTable.Columns.Add(ids[i], typeof(string));
            m_data.ParameterTable.RowChanged += ParameterTable_SyncChanges;
            m_data.ParameterTable.RowDeleted += ParameterTable_SyncChanges;
            m_data.ParameterTable.TableNewRow += ParameterTable_SyncChanges;

            boxName.TextChanged += Name_SyncChanges;
        }

        private void Name_SyncChanges(object sender, TextChangedEventArgs e)
        {
            if (m_data.SelectedForProperties == null) return;
            m_data.SelectedForProperties.Name = boxName.Text;
            m_data.CalculateWidthHeight(m_data.SelectedForProperties);
            skCanvas.InvalidateVisual();
        }

        private void PropertyTable_SyncChanges(object sender, DataTableNewRowEventArgs e)
        {
            PropertyTable_SyncChanges(sender, new DataRowChangeEventArgs(null, new DataRowAction()));
        }

        private void PropertyTable_SyncChanges(object sender, DataRowChangeEventArgs e) {
            if (m_data.SelectedForProperties == null) {
                // Add a new box if nothing is selected when you start writing
                UmlBox b = m_data.AddBox(new SKPoint(0, 0));
                m_data.SelectedForProperties = b;
            }
            List<UmlVariable> l = m_data.SelectedForProperties.Variables;
            DataTable t = m_data.PropertyTable;
            if (t.Columns.Count != 3) return;
            l.Clear();
            for (int i = 0; i < t.Rows.Count; i++) {
                string[] values = new string[3];
                try {
                    for (int j = 0; j < 3; j++) values[j] = (string)t.Rows[i][j];
                    l.Add(new UmlVariable(values[0], values[1], values[2]));
                }
                catch {
                    // Break out and dont add the item if the row isnt completely filled out
                    // Just go to the next row like nothing happened
                    // Console.WriteLine("Row # " + i.ToString() + " not filled out");
                }
            }
            for (int i=0; i<PropertyGrid.Items.Count; i++) {
                DataGridRow row = (DataGridRow)PropertyGrid.ItemContainerGenerator.ContainerFromItem(PropertyGrid.Items[i]);
                if (row != null) {
                    bool hasEmptyCell = false;
                    // Check each column in the row for empty cells
                    for (int j=0; j < PropertyGrid.Columns.Count; j++) {
                        DataGridColumn column = PropertyGrid.Columns[j];
                        TextBlock? cellContent = column.GetCellContent(PropertyGrid.Items[i]) as TextBlock;
                        if (cellContent == null || string.IsNullOrWhiteSpace(cellContent.Text)) {
                            hasEmptyCell = true;
                            break;
                        }
                    }
                    // Update row background color
                    if (hasEmptyCell && i != PropertyGrid.Items.Count-1) {
                        row.Background = new SolidColorBrush(Colors.OrangeRed);
                    } else {
                        row.Background = new SolidColorBrush(Colors.White);
                    }
                }
            }
            m_data.CalculateWidthHeight(m_data.SelectedForProperties);
            skCanvas.InvalidateVisual();
        }

        private void MethodTable_ChangeSelected(object sender, SelectedCellsChangedEventArgs e)
        {
            // Lock monitoring of parameter table until this method finishes
            m_data.methodSwitchDone = false;
            // Get the current method's inputs
            DataTable t = m_data.ParameterTable;
            t.Rows.Clear();
            if (MethodGrid.SelectedItem is DataRowView && MethodGrid.SelectedIndex < m_data.SelectedForProperties.Methods.Count) {
                DataRow row = ((DataRowView)MethodGrid.SelectedItem).Row;
                if (row[0] is DBNull || row[1] is DBNull || row[2] is DBNull) return;
                UmlMethod selectedMethod = m_data.SelectedForProperties.Methods[MethodGrid.SelectedIndex];
                List<UmlVariable> l = selectedMethod.Parameters;
                for (int i = 0; i < l.Count; i++)
                {
                    t.Rows.Add(l[i].Type, l[i].Name);
                }
            }
            m_data.methodSwitchDone = true;
        }

        private void MethodTable_SyncChanges(object sender, DataTableNewRowEventArgs e)
        {
            MethodTable_SyncChanges(sender, new DataRowChangeEventArgs(null, new DataRowAction()));
        }

        private void MethodTable_SyncChanges(object sender, DataRowChangeEventArgs e)
        {
            if (m_data.SelectedForProperties == null)
            {
                // Add a new box if nothing is selected when you start writing
                UmlBox b = m_data.AddBox(new SKPoint(0, 0));
                m_data.SelectedForProperties = b;
            }
            List<UmlMethod> l = m_data.SelectedForProperties.Methods;
            DataTable t = m_data.MethodTable;

            // Data cant simply be wiped and regenerated since input variables are stored separately
            // We can use the fact that there are only 3 changes that can be made to the table
            // Addition, deletion, and modifications to a single row (not multiple rows)

            // Get the number of rows that are fully filled out, to check what modification was made
            int numFilled = 0;
            for (int r = 0; r<t.Rows.Count; r++) {
                bool hasEmptyCell = false;
                // Check each column in the row for empty cells
                for (int c = 0; c < 3; c++) {
                    if (t.Rows[r][c] is DBNull || (string)t.Rows[r][c] == "") { hasEmptyCell = true; break; }
                }
                if (!hasEmptyCell) numFilled++;
            }
            int lIndex = 0;
            if (numFilled < l.Count) {
                bool actionCompleted = false;
                // If a row is deleted, or one is created incorrectly
                for (int r = 0; r < t.Rows.Count; r++)
                {
                    bool hasEmptyCell = false;
                    // Check each column in the row for empty cells
                    for (int c = 0; c < 3; c++)
                    {
                        if (t.Rows[r][c] is DBNull || (string)t.Rows[r][c] == "") { hasEmptyCell = true; break; }
                    }
                    if (hasEmptyCell) continue;
                    // If the row has NOT been changed, move onto next index
                    if (l[lIndex].Protection == t.Rows[r][0].ToString()
                        && l[lIndex].Type == t.Rows[r][1].ToString()
                        && l[lIndex].Name == t.Rows[r][2].ToString()) { lIndex++; continue; }
                    l.RemoveAt(lIndex);
                    actionCompleted = true;
                    break;
                    // Since we removed an index, incrementing lIndex would skip over an index, so dont
                }
                if (!actionCompleted) l.RemoveAt(l.Count - 1);
            } else if (numFilled > l.Count) {
                // If a row is created, or an incorrect one is fixed
                for (int r = 0; r < t.Rows.Count; r++)
                {
                    bool hasEmptyCell = false;
                    // Check each column in the row for empty cells
                    for (int c = 0; c < 3; c++)
                    {
                        if (t.Rows[r][c] is DBNull || (string)t.Rows[r][c] == "") { hasEmptyCell = true; break; }
                    }
                    if (hasEmptyCell) continue;
                    // If the row has NOT been changed, move onto next index
                    if (lIndex < l.Count
                        && l[lIndex].Protection == t.Rows[r][0].ToString()
                        && l[lIndex].Type == t.Rows[r][1].ToString()
                        && l[lIndex].Name == t.Rows[r][2].ToString()) { lIndex++; continue; }
                    l.Insert(lIndex, new UmlMethod(t.Rows[r][0].ToString() ?? "", t.Rows[r][1].ToString() ?? "", t.Rows[r][2].ToString() ?? "", []));
                    lIndex++;
                }
            } else {
                // If a row is modified
                for (int r = 0; r < t.Rows.Count; r++) {
                    bool hasEmptyCell = false;
                    // Check each column in the row for empty cells
                    for (int c = 0; c < 3; c++) {
                        if (t.Rows[r][c] is DBNull ||  (string)t.Rows[r][c] == "") { hasEmptyCell = true; break; }
                    }
                    if (hasEmptyCell) continue;
                    l[lIndex].Protection = t.Rows[r][0].ToString() ?? "";
                    l[lIndex].Type = t.Rows[r][1].ToString() ?? "";
                    l[lIndex].Name = t.Rows[r][2].ToString() ?? "";
                    lIndex++;
                }
            }
            for (int i = 0; i < MethodGrid.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)MethodGrid.ItemContainerGenerator.ContainerFromItem(MethodGrid.Items[i]);
                if (row != null)
                {
                    bool hasEmptyCell = false;
                    // Check each column in the row for empty cells
                    for (int j = 0; j < MethodGrid.Columns.Count; j++)
                    {
                        DataGridColumn column = MethodGrid.Columns[j];
                        TextBlock? cellContent = column.GetCellContent(MethodGrid.Items[i]) as TextBlock;
                        if (cellContent == null || string.IsNullOrWhiteSpace(cellContent.Text))
                        {
                            hasEmptyCell = true;
                            break;
                        }
                    }
                    // Update row background color
                    if (hasEmptyCell && i != MethodGrid.Items.Count - 1)
                    {
                        row.Background = new SolidColorBrush(Colors.OrangeRed);
                    }
                    else
                    {
                        row.Background = new SolidColorBrush(Colors.White);
                    }
                }
            }
            m_data.CalculateWidthHeight(m_data.SelectedForProperties);
            skCanvas.InvalidateVisual();
        }

        private void ParameterTable_SyncChanges(object sender, DataTableNewRowEventArgs e)
        {
            ParameterTable_SyncChanges(sender, new DataRowChangeEventArgs(null, new DataRowAction()));
        }

        private void ParameterTable_SyncChanges(object sender, DataRowChangeEventArgs e)
        {
            // Events for switching methods and the syncing of changes can happen simulatenously, causing some issues...
            // Wait until the switch is 100% complete before starting to monitor changes in parameter table
            if (!m_data.methodSwitchDone) return;
            if (m_data.SelectedForProperties == null || MethodGrid.SelectedIndex == -1) {
                m_data.ParameterTable.Clear();
                return;
            }
            List<UmlVariable> l = m_data.SelectedForProperties.Methods[MethodGrid.SelectedIndex].Parameters;
            DataTable t = m_data.ParameterTable;
            if (t.Columns.Count != 2) return;
            l.Clear();
            for (int i = 0; i < t.Rows.Count; i++)
            {
                string[] values = new string[2];
                try
                {
                    for (int j = 0; j < 2; j++) values[j] = (string)t.Rows[i][j];
                    l.Add(new UmlVariable("", values[0], values[1]));
                }
                catch
                {
                    // Break out and dont add the item if the row isnt completely filled out
                    // Just go to the next row like nothing happened
                    // Console.WriteLine("Row # " + i.ToString() + " not filled out");
                }
            }
            for (int i = 0; i < ParameterGrid.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)ParameterGrid.ItemContainerGenerator.ContainerFromItem(ParameterGrid.Items[i]);
                if (row != null)
                {
                    bool hasEmptyCell = false;
                    // Check each column in the row for empty cells
                    for (int j = 0; j < ParameterGrid.Columns.Count; j++)
                    {
                        DataGridColumn column = ParameterGrid.Columns[j];
                        TextBlock? cellContent = column.GetCellContent(ParameterGrid.Items[i]) as TextBlock;
                        if (cellContent == null || string.IsNullOrWhiteSpace(cellContent.Text))
                        {
                            hasEmptyCell = true;
                            break;
                        }
                    }
                    // Update row background color
                    if (hasEmptyCell && i != ParameterGrid.Items.Count - 1)
                    {
                        row.Background = new SolidColorBrush(Colors.OrangeRed);
                    }
                    else
                    {
                        row.Background = new SolidColorBrush(Colors.White);
                    }
                }
            }
            m_data.CalculateWidthHeight(m_data.SelectedForProperties);
            skCanvas.InvalidateVisual();
        }

        private void buttonMinimize_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonMinimize_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 9/28/2024                                            ::
        :: 4. Purpose: Action for the minimize button                       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object sending the action          ::
        ::                      e - routed event arguments                  ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            this.WindowState = WindowState.Minimized;
        }

        private void buttonMaximize_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonMaximize_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 9/28/2024                                            ::
        :: 4. Purpose: Action for the maximize button                       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object sending the action          ::
        ::                      e - routed event arguments                  ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                windowedButton.Source = new BitmapImage(new Uri("pack://application:,,,/assets/maximize.png"));
                return;
            }
            windowedButton.Source = new BitmapImage(new Uri("pack://application:,,,/assets/windowed.png"));
            this.WindowState = WindowState.Maximized;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonClose_Click : MainWindow                        ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 9/28/2024                                            ::
        :: 4. Purpose: Action for the close button                          ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object sending the action          ::
        ::                      e - routed event arguments                  ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Window_MouseLeftButtonDown : MainWindow               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 9/28/2024                                            ::
        :: 4. Purpose: Action for when left clicking on anywhere in window  ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object sending the action          ::
        ::                      e - mouse button  event arguments           ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications:                                                ::
        :: DD10: Windowing top bar through dragging changes windowed button ::
        :: DD11: Mutiscreen support                                         ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (e.GetPosition(this).Y < 40) {
                if (this.WindowState == WindowState.Maximized) {
                    // Multi monitor support with drag to windowed mode
                    nint windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    Screen currentScreen = Screen.FromHandle(windowHandle);
                    double amountCovered = e.GetPosition(this).X/this.Width;
                    // Since it is full screen at this point, position relative to window is relative to screen
                    double screenX = e.GetPosition(this).X;
                    this.WindowState = WindowState.Normal;
                    this.Top = currentScreen.WorkingArea.Top;
                    // Set x position to be so that the mouse is the same percentage across after not fullscreen
                    this.Left = currentScreen.WorkingArea.Left + screenX - this.Width * amountCovered;
                    // Adjust the windowed button to change to the fullscreen button, don't need to do other way around
                    windowedButton.Source = new BitmapImage(new Uri("pack://application:,,,/assets/maximize.png"));
                }
                DragMove();
            }
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: OnPaintSurface : MainWindow                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Redraw the center canvas                             ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: sender - object sending the action          ::
        ::                      e - arguments for painting surface          ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            e.Surface.Canvas.Clear();
            e.Surface.Canvas.Translate(m_data.TranslationX, m_data.TranslationY);
            e.Surface.Canvas.Scale(m_data.Scale);
            m_data.RedrawAllBoxes(e.Surface.Canvas);
        }
    }
}