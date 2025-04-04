
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using DocuDoctor.Model;
using Microsoft.VisualBasic.Devices;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Mouse = System.Windows.Input.Mouse;
namespace DocuDoctor.ViewController
{
    /// <summary>
    /// Controller for DocuDoctor
    /// </summary>
    public partial class MainWindow : Window {
        // Data object for MVP structure
        private Data m_data;
        // Sudo mutex to prevent out of order actions on synchronous action
        private bool m_updatingTable;

        // Not actual data, but performance monitors so kept in frontend
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private DispatcherTimer timer;
        private ulong totalRam;

        public MainWindow()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: MainWindow : MainWindow                               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Creator                                              ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            InitializeComponent();
            OnStartup();
            AddEvents();
            BindElements();
        }

        // The Following couple methods are meant to drastically simplify the code elsewhere
        // They are helper functions which increase code clarity and reduce dependencies on arbitrary variables
        //
        // TLDR the values generated in these methods should not really be wrong
        //      But if you still find troubles, look elsewhere
        //
        private bool IsControlPressed()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: IsControlPressed : MainWindow                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Helper method which returns if controll is pressed   ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            return System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private (int,int) GetAbsoluteMousePos()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: GetMousePos : MainWindow                              ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Gets absolute mouse position over entire system      ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            System.Drawing.Point screenPos = System.Windows.Forms.Cursor.Position;
            return (screenPos.X, screenPos.Y);
        }

        private (int, int) GetMousePosRelWindow()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: GetMousePos : MainWindow                              ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Gets relative mouse position over DocumentationDoctor::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            System.Windows.Point screenPos = Mouse.GetPosition(System.Windows.Application.Current.MainWindow);
            return ((int)screenPos.X, (int)screenPos.Y);
        }

        private (float, float) GetMousePosRelSkia()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: GetMousePos : MainWindow                              ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Gets relative mouse position over SkiaSharp          ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            System.Windows.Point screenPos = Mouse.GetPosition(skCanvas);
            return ((float)screenPos.X, (float)screenPos.Y);
        }

        private (float, float) GetDPI()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: GetDPI : MainWindow                                   ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Returns the individual x,y dpi (should be the same)  ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            float x = (float)(skCanvas.CanvasSize.Width / skCanvas.ActualWidth);
            float y = (float)(skCanvas.CanvasSize.Height / skCanvas.ActualHeight);
            return (x, y);
        }

        private (float, float) ScaleAdjustedWithDPI()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ScaleAdjustedWithDPI : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Gets the true scale of the skia window with DPI      ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            (float, float) dpi = GetDPI();
            float x = dpi.Item1 * m_data.Scale;
            float y = dpi.Item2 * m_data.Scale;
            return (x, y);
        }

        private (float, float) GetMousePosInSkiaCoords()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: GetMousePos : MainWindow                              ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Gets mouse position in skia coordinates              ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            (float, float) mousePos = GetMousePosRelSkia();
            (float, float) dpi = GetDPI();

            // Final formula for adjusting scale is (ab-c)/(bd)
            // a = mousePos, b = DPI, c = translation, d = Scale
            // Dont ask me why, but it works

            // 1. Adjust mousePosition with DPI
            mousePos.Item1 *= dpi.Item1;
            mousePos.Item2 *= dpi.Item2;
            // 2. Reverse transformations made on draw
            float skiaX = (mousePos.Item1 - m_data.TranslationX) / m_data.Scale;
            float skiaY = (mousePos.Item2 - m_data.TranslationY) / m_data.Scale;
            // 3. Readjust based on DPI to screen coordinates
            skiaX /= dpi.Item1;
            skiaY /= dpi.Item2;
            return (skiaX, skiaY);
        }

        private (float, float) GetDeltaSkia(float x1, float y1, float x2, float y2)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: GetDeltaSkia : MainWindow                             ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Gets skia distance between two mouse positions       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // Based on the formula for translating mouse position
            // relatve to skia to skia coords
            (float, float) dpi = GetDPI();

            // Final formula for adjusting scale is (ab)/(bd)
            // a = deltaPos, b = DPI, c = translation, d = Scale
            // Dont ask me why, but it works

            // 1. Adjust deltaPosition with DPI
            (float, float) deltaPos = (x2 - x1, y2 - y1);
            deltaPos.Item1 *= dpi.Item1;
            deltaPos.Item2 *= dpi.Item2;
            // 2. Reverse transformations made on draw
            float skiaX = (deltaPos.Item1) / m_data.Scale;
            float skiaY = (deltaPos.Item2) / m_data.Scale;
            // 3. Readjust based on DPI to screen coordinates
            skiaX /= dpi.Item1;
            skiaY /= dpi.Item2;
            return (skiaX, skiaY);
        }

        // END OF HELPER METHODS

        private void BindElements()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: BindElements : MainWindow                             ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Binds tables to grids for easier table manip         ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            PropertyGrid.ItemsSource = m_data.PropertyTable.DefaultView;
            MethodGrid.ItemsSource = m_data.MethodTable.DefaultView;
            ParameterGrid.ItemsSource = m_data.ParameterTable.DefaultView;
        }

        private void AddEvents()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddEvents : MainWindow                                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Attaches events on initialization                    ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            skCanvas.MouseDown += SkCanvas_MouseDown;
            skCanvas.MouseMove += SkCanvas_MouseMove;
            skCanvas.MouseUp += SkCanvas_MouseUp;
            skCanvas.MouseWheel += SkCanvas_MouseWheel;
            KeyDown += Screen_KeyDown;

        }

        private void Screen_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Screen_KeyDown : MainWindow                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: handles events when you click keys in window         ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (IsControlPressed() && System.Windows.Input.Keyboard.IsKeyDown(Key.P))
                ExportProject_Click(sender, new RoutedEventArgs());
            if (IsControlPressed() && System.Windows.Input.Keyboard.IsKeyDown(Key.A) && System.Windows.Input.Keyboard.IsKeyDown(Key.F)) {
                OpenBrowser();
            }
         }

        private void OpenBrowser()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: OpenBrowser : MainWindow                              ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Huzzah! An april fools joke                          ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            Process.Start(new ProcessStartInfo {
                FileName = "chrome",
                Arguments = "--new-window https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                UseShellExecute = true
            });
        }

        private void FileButton_OnClick(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.ShowDialog();
            bool newBoxes = m_data.ReadFiles(openFileDialog.FileNames);
            if (newBoxes) {
                UpdateProperties();
                skCanvas.InvalidateVisual();
            }
        }

        private void SkCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseWheel : MainWindow                      ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: handles events when you when you scroll in window    ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            float scaleFactor = 1.25f;

            if(!IsControlPressed())
                return;

            // Inherintely, the problem boils down to this
            // Position of mouse in skia coordinates MUST be the same before and after the zoom in or out
            // The solution? Its very very simple thanks to the helper methods
            // 1. Get mouse position before in skia coordinates
            // 2. Scale to whatever you want
            // 3. Get mouse position after in skia coordinates
            // Change in translationX and translationY ais relative to both difference in positions and the current scale

            (float, float) posBefore = GetMousePosInSkiaCoords();
            // Zoom in
            if (e.Delta > 0) m_data.Scale *= (scaleFactor);
            // Zoom out
            else m_data.Scale /= (scaleFactor);
            (float, float) posAfter = GetMousePosInSkiaCoords();
            (float, float) actualScale = ScaleAdjustedWithDPI();
            m_data.TranslationX += (float)(posAfter.Item1 - posBefore.Item1) * actualScale.Item1;
            m_data.TranslationY += (float)(posAfter.Item2 - posBefore.Item2) * actualScale.Item2;
            skCanvas.InvalidateVisual();
        }

        private void SkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseUp : MainWindow                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: handles events when you release mouse in skcanvas    ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_data.MovedBox = null;
        }

        private void SkCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseMove : MainWindow                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: handles events when you move mouse in skcanvas       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // We only perform actions here if the mouse button is being pressed
            // We also only perform actions if the current selection is the select/mouse tool
            if (Mouse.LeftButton != MouseButtonState.Pressed || m_data.toolbarSelection != ToolState.Select) {
                return;
            }
            (float, float) curMousePos = GetMousePosRelSkia();
            (float, float) delta = GetDeltaSkia(m_data.InitialMousePos.Item1, m_data.InitialMousePos.Item2, curMousePos.Item1, curMousePos.Item2);
            // Move entire skia sharp window
            if (IsControlPressed()) {
                m_data.TranslationX = m_data.InitialTranslation.Item1 + delta.Item1*ScaleAdjustedWithDPI().Item1;
                m_data.TranslationY = m_data.InitialTranslation.Item2 + delta.Item2*ScaleAdjustedWithDPI().Item2;
            // Move selected box
            } else {
                // No action or screen refresh needed if nothing is selected
                if (m_data.SelectedForProperties == null) return;
                m_data.SelectedForProperties.X = m_data.InitialSelectedPos.Item1 + delta.Item1;
                m_data.SelectedForProperties.Y = m_data.InitialSelectedPos.Item2 + delta.Item2;
            }
            skCanvas.InvalidateVisual();
        }

        private void SkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SkCanvas_MouseDown : MainWindow                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: handles events when you click mouse in skcanvas      ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            (float, float) mPos = GetMousePosInSkiaCoords();
            SKPoint mPosSkia = new SKPoint(mPos.Item1, mPos.Item2);
            m_data.InitialMousePos = GetMousePosRelSkia();
            m_data.InitialTranslation = (m_data.TranslationX, m_data.TranslationY);
            if (m_data.SelectedForProperties != null) {
                m_data.InitialSelectedPos = (m_data.SelectedForProperties.X, m_data.SelectedForProperties.Y);
            }
            if (e.LeftButton == MouseButtonState.Pressed) {
                switch (m_data.toolbarSelection) {
                    case ToolState.Select:
                        m_data.SelectBox(mPosSkia);
                        skCanvas.InvalidateVisual();
                        UpdateProperties();
                        break;
                    case ToolState.RemoveBox:
                        m_data.RemoveBox(mPosSkia);
                        skCanvas.InvalidateVisual();
                        UpdateProperties();
                        break;
                    case ToolState.AddClass:
                        m_data.AddBox(mPosSkia, "Class");
                        skCanvas.InvalidateVisual();
                        UpdateProperties();
                        break;
                    case ToolState.AddInterface:
                        m_data.AddBox(mPosSkia, "Interface");
                        skCanvas.InvalidateVisual();
                        UpdateProperties();
                        break;
                    case ToolState.AddTemplate:
                        m_data.AddBox(mPosSkia, "Template");
                        skCanvas.InvalidateVisual();
                        UpdateProperties();
                        break;
                    case ToolState.AddArrow:
                        m_data.AddArrow((float)mPosSkia.X, (float)mPosSkia.Y, 0);
                        skCanvas.InvalidateVisual();
                        break;
                    case ToolState.AddDashedArrow:
                        m_data.AddArrow((float)mPosSkia.X, (float)mPosSkia.Y, 1);
                        skCanvas.InvalidateVisual();
                        break;
                }
            }
        }
        private void UpdateProperties()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: UpdateProperties : MainWindow                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Update properties panel with currently selected box  ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // Prevents syncing incomplete tables to the box
            m_updatingTable = true;
            UmlBox cur = m_data.SelectedForProperties;
            m_data.PropertyTable.Rows.Clear();
            m_data.MethodTable.Rows.Clear();
            m_data.ParameterTable.Rows.Clear();
            if (cur == null) { boxName.Text = ""; boxTypeComboBox.SelectedValue = null; return; }
            boxName.Text = cur.Name;
            boxTypeComboBox.SelectedValue = cur.BoxType;
            for (int i = 0; i < cur.Variables.Count; i++) {
                m_data.PropertyTable.Rows.Add(cur.Variables[i].Protection, cur.Variables[i].Type, cur.Variables[i].Name);
            }
            for (int i = 0; i < cur.Methods.Count; i++)
            {
                m_data.MethodTable.Rows.Add(cur.Methods[i].Protection, cur.Methods[i].Name, cur.Methods[i].Name);
            }
            m_updatingTable=false;
        }

        private void OnStartup()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: OnStartup : MainWindow                                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Actions to occur on startup of main window           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_data = new Data();
            m_updatingTable = false;
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            ResetProperties();
            InitResourceMonitors();
        }

        private void InitResourceMonitors()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: InitResourceMonitors : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Creates the resource monitors in status bar          ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            totalRam = GetTotalMemoryInBytes()/1000000;
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += UpdateResourceUsage;
            timer.Start();
        }

        private void UpdateResourceUsage(object sender, EventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: UpdateResourceUsage : MainWindow                      ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Updates resource monitors every few seconds          ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            float cpuUsage = cpuCounter.NextValue();
            float ramAvailable = ramCounter.NextValue();
            float ramUsed = totalRam - ramAvailable;
            float ramUsagePercent = (ramUsed / totalRam) * 100;
            CpuUsageBar.Value = cpuUsage;
            CpuUsageText.Text = $"CPU Usage: {cpuUsage:F1}%";
            RamUsageBar.Value = ramUsagePercent;
            RamUsageText.Text = $"RAM: {ramUsagePercent:F1}%";
        }

        static ulong GetTotalMemoryInBytes()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: GetTotalMemoryInBytes : MainWindow                    ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Helper function for estimating current ram usage     ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
        }

        private void ResetProperties()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ResetProperties : MainWindow                          ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Resets the various property windows to default       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
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
            boxTypeComboBox.SelectionChanged += Type_SyncChanges;

        }

        private void Name_SyncChanges(object sender, TextChangedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Name_SyncChanges : MainWindow                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Syncs name bar and actual name of box                ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (m_data.SelectedForProperties == null) return;
            m_data.SelectedForProperties.Name = boxName.Text;
            m_data.CalculateWidthHeight(m_data.SelectedForProperties);
            skCanvas.InvalidateVisual();
        }

        private void Type_SyncChanges(object sender, SelectionChangedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Name_SyncChanges : MainWindow                         ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Syncs type bar and actual type of box                ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (m_data.SelectedForProperties == null) return;
            Debug.WriteLine(boxTypeComboBox.SelectedValue.ToString());
            m_data.SelectedForProperties.BoxType = boxTypeComboBox.SelectedValue.ToString();
            m_data.CalculateWidthHeight(m_data.SelectedForProperties);
            skCanvas.InvalidateVisual();
        }

        private void PropertyTable_SyncChanges(object sender, DataTableNewRowEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: PropertyTable_SyncChanges : MainWindow                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Alternative call for syncing propery table           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            PropertyTable_SyncChanges(sender, new DataRowChangeEventArgs(null, new DataRowAction()));
        }

        private void PropertyTable_SyncChanges(object sender, DataRowChangeEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: PropertyTable_SyncChanges : MainWindow                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Syncs property table to backend                      ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if(m_updatingTable) return;
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
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: MethodTable_ChangeSelected : MainWindow               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Wipes parameter table                                ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
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
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: MethodTable_ChangeSelected : MainWindow               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Syncs method table to backend                        ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            MethodTable_SyncChanges(sender, new DataRowChangeEventArgs(null, new DataRowAction()));
        }

        private void MethodTable_SyncChanges(object sender, DataRowChangeEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: MethodTable_ChangeSelected : MainWindow               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Syncs method table to backend                        ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if(m_updatingTable)
                return;
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
                    l.Insert(lIndex, new UmlMethod(t.Rows[r][0].ToString() ?? "", t.Rows[r][1].ToString() ?? "", t.Rows[r][2].ToString() ?? "", new List<List<string>>()));
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
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ParameterTable_ChangeSelected : MainWindow            ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Syncs parameter table to backend                     ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ParameterTable_SyncChanges(sender, new DataRowChangeEventArgs(null, new DataRowAction()));
        }

        private void ParameterTable_SyncChanges(object sender, DataRowChangeEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ParameterTable_ChangeSelected : MainWindow            ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Syncs parameter table to backend                     ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
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

        private void ProjectFileButton_Click(object sender, RoutedEventArgs e) {
            System.Windows.Controls.Button? button = sender as System.Windows.Controls.Button;
            if (button != null && button.ContextMenu != null) {
                button.ContextMenu.PlacementTarget = button; // Ensure the menu is positioned correctly
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom; // Place below button
                button.ContextMenu.IsOpen = true; // Open the dropdown menu
            }
        }

        private void LoadProject_Click(object sender, RoutedEventArgs e) {
            // Create the file dialog
            System.Windows.Forms.OpenFileDialog loadFileDialog = new System.Windows.Forms.OpenFileDialog();
            loadFileDialog.Filter = "DD Files (*.dd)|*.dd";  // Filters to only show .dd files
            loadFileDialog.DefaultExt = "dd";  // Default file extension
            loadFileDialog.AddExtension = true; // Automatically add .dd extension if none is provided

            // Show the save file dialog and check if the user selected a file
            if (loadFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                string filePath = loadFileDialog.FileName;
                // Check if the file exists
                if (File.Exists(filePath)) {
                    XmlSerializer serializer = new XmlSerializer(typeof(Data));
                    // Read and deserialize the XML data
                    using (StreamReader reader = new StreamReader(filePath)) {
                        m_data = (Data)serializer.Deserialize(reader);
                    }
                    skCanvas.InvalidateVisual();
                } else {
                    System.Windows.Forms.MessageBox.Show("No saved data found!", "Load", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SaveProject_Click : MainWindow                        ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Action for saving project                            ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if (m_data.FilePath == "") SaveAs_Click(sender, e);
            // Create an XmlSerializer instance
            XmlSerializer serializer = new XmlSerializer(typeof(Data));

            // Save the object to the file
            using (StreamWriter writer = new StreamWriter(m_data.FilePath)) {
                serializer.Serialize(writer, m_data);
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: SaveAs_Click : MainWindow                             ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: when requested or on first save, ask for subdirectory::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // Create the file dialog
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "DD Files (*.dd)|*.dd";  // Filters to only show .dd files
            saveFileDialog.DefaultExt = "dd";  // Default file extension
            saveFileDialog.AddExtension = true; // Automatically add .dd extension if none is provided

            // Show the save file dialog and check if the user selected a file
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                m_data.FilePath = saveFileDialog.FileName;
                SaveProject_Click(sender, e);
            }
        }

        private void ExportProject_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ExportProject_Click : MainWindow                      ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Starts up snapshot of skia sharp canvas              ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_data.ExportPhoto = true;
            skCanvas.InvalidateVisual();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ImportButton_Click : MainWindow                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Riley Horling                                         ::
        :: 3. Purpose: Imports file and parses into program                 ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.ShowDialog();
            bool newBoxes = m_data.ReadFiles(openFileDialog.FileNames);
            if (newBoxes) {
                UpdateProperties();
                skCanvas.InvalidateVisual();
            }
        }

        private void buttonMinimize_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonMinimize_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Action for the minimize button                       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            this.WindowState = WindowState.Minimized;
        }

        private void buttonMaximize_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonMaximize_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Action for the maximize button                       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if(this.WindowState == WindowState.Maximized) {
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
        :: 3. Purpose: Action for the close button                          ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            this.Close();
        }

        private void ClearAllToolbarButtons()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ClearAllToolbarButtons : MainWindow                   ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Makes background for buttons transparent             ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            Deselect.Background = new SolidColorBrush(Colors.Transparent);
            Delete.Background = new SolidColorBrush(Colors.Transparent);
            AddClass.Background = new SolidColorBrush(Colors.Transparent);
            AddInterface.Background = new SolidColorBrush(Colors.Transparent);
            AddTemplate.Background = new SolidColorBrush(Colors.Transparent);
            AddArrow.Background = new SolidColorBrush(Colors.Transparent);
            AddDottedArrow.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void buttonDeselect_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonDeselect_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: When you click deselect                              ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ClearAllToolbarButtons();
            Deselect.Background = new SolidColorBrush(Colors.Yellow);
            m_data.toolbarSelection = 0;
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonDelete_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: When you click delete                              ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ClearAllToolbarButtons();
            Delete.Background = new SolidColorBrush(Colors.Yellow);
            m_data.toolbarSelection = ViewController.ToolState.RemoveBox;
        }

        private void buttonAddClass_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonAddClass_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: When you click add class                             ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ClearAllToolbarButtons();
            AddClass.Background = new SolidColorBrush(Colors.Yellow);
            m_data.toolbarSelection = ViewController.ToolState.AddClass;
        }

        private void buttonAddInterface_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonAddInterface_Click : MainWindow                 ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: When you click add interface                         ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ClearAllToolbarButtons();
            AddInterface.Background = new SolidColorBrush(Colors.Yellow);
            m_data.toolbarSelection = ViewController.ToolState.AddInterface;
        }

        private void buttonAddTemplate_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonAddTemplate_Click : MainWindow                  ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: When you click add template                          ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ClearAllToolbarButtons();
            AddTemplate.Background = new SolidColorBrush(Colors.Yellow);
            m_data.toolbarSelection = ToolState.AddTemplate;
        }

        private void buttonAddArrow_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonAddArrow_Click : MainWindow                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: When you click add solid arrow                       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ClearAllToolbarButtons();
            AddArrow.Background = new SolidColorBrush(Colors.Yellow);
            m_data.toolbarSelection = ToolState.AddArrow;
        }

        private void buttonAddDottedArrow_Click(object sender, RoutedEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: buttonAddDottedArrow_Click : MainWindow               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: When you click add dotted arrow                      ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            ClearAllToolbarButtons();
            AddDottedArrow.Background = new SolidColorBrush(Colors.Yellow);
            m_data.toolbarSelection = ToolState.AddDashedArrow;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Window_MouseLeftButtonDown : MainWindow               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Action for when left clicking on anywhere in window  ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if(e.GetPosition(this).Y < 40) {
                if(this.WindowState == WindowState.Maximized) {
                    // Multi monitor support with drag to windowed mode
                    nint windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    Screen currentScreen = Screen.FromHandle(windowHandle);
                    double amountCovered = e.GetPosition(this).X / this.Width;
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

        private void PrintPhoto(SKSurface s)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: PrintPhoto : MainWindow                               ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Prints the current look of the canvas                ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_data.ExportPhoto = false;
            SKImage image = s.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog {
                    Filter = "PNG Image|*.png",
                    Title = "Save PNG File",
                    FileName = "output.png"
                };
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    using (FileStream stream = File.OpenWrite(saveFileDialog.FileName)) {
                        data.SaveTo(stream);
                    }
                }
            });
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: OnPaintSurface : MainWindow                           ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Redraw the center canvas                             ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            e.Surface.Canvas.Clear();
            e.Surface.Canvas.Translate(m_data.TranslationX, m_data.TranslationY);
            e.Surface.Canvas.Scale(ScaleAdjustedWithDPI().Item1);
            m_data.RedrawAllBoxes(e.Surface.Canvas);
            m_data.RedrawAllArrows(e.Surface.Canvas);
            if (m_data.ExportPhoto) PrintPhoto(e.Surface);
        }
    }
}