using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
namespace DocuDoctor.ViewController {
    /// <summary>
    /// Controller for DocuDoctor
    /// </summary>
    public partial class MainWindow : Window {
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
            fileButton.Click += FileButton_OnClick;

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
            if(e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                m_ctrlClicked = true;
        }

        private void FileButton_OnClick(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            m_data.ReadFile(openFileDialog.FileName);
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
            if(e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                m_ctrlClicked = false;
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
            if(!m_ctrlClicked)
                return;
            System.Windows.Point curPos = e.GetPosition(skCanvas);
            float oldScale = m_data.Scale;
            // Zoom in
            if(e.Delta > 0) {
                m_data.Scale *= scaleFactor;
            }
            // Zoom out
            else {
                m_data.Scale /= scaleFactor;
            }
            m_data.TranslationX = (float)(curPos.X - (curPos.X - m_data.TranslationX) * (m_data.Scale / oldScale));
            m_data.TranslationY = (float)(curPos.Y - (curPos.Y - m_data.TranslationY) * (m_data.Scale / oldScale));
            m_initialMousePos = new SKPoint((float)(curPos.X), (float)curPos.Y);
            m_initialTransformX = m_data.TranslationX;
            m_initialTransformY = m_data.TranslationY;
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
            if(!m_clicked)
                return;
            else if(m_clicked && e.MouseDevice.LeftButton == MouseButtonState.Released) { m_clicked = false; return; }
            System.Windows.Point mPos = e.GetPosition(skCanvas);
            mPos.X -= m_data.TranslationX;
            mPos.X /= m_data.Scale;
            mPos.Y -= m_data.TranslationY;
            mPos.Y /= m_data.Scale;
            SKPoint curMousePos = new SKPoint((float)mPos.X, (float)mPos.Y);
            float deltaX = curMousePos.X - m_lastMousePos.X;
            float deltaY = curMousePos.Y - m_lastMousePos.Y;
            if(!m_data.MoveBox(m_lastMousePos.X, m_lastMousePos.Y, deltaX, deltaY) && m_ctrlClicked) {
                System.Windows.Point curPos = e.GetPosition(skCanvas);
                m_data.TranslationX = m_initialTransformX + ((float)curPos.X - m_initialMousePos.X) / m_data.Scale;
                m_data.TranslationY = m_initialTransformY + ((float)curPos.Y - m_initialMousePos.Y) / m_data.Scale;
            }
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
            mPos.X -= m_data.TranslationX;
            mPos.X /= m_data.Scale;
            mPos.Y -= m_data.TranslationY;
            mPos.Y /= m_data.Scale;
            SKPoint internalPos = new((float)mPos.X, (float)mPos.Y);
            if(e.RightButton == MouseButtonState.Pressed) { m_data.AddBox(internalPos); skCanvas.InvalidateVisual(); return; }
            if(e.MiddleButton == MouseButtonState.Pressed) { m_data.RemoveBox(internalPos); skCanvas.InvalidateVisual(); return; }
            if(e.LeftButton == MouseButtonState.Released)
                return;
            m_lastMousePos = internalPos;
            m_initialMousePos = internalPos;
            m_initialTransformX = m_data.TranslationX;
            m_initialTransformY = m_data.TranslationY;
            m_clicked = true;
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