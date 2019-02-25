/*
 * Copyright (c) 2019 Leonardo Pessoa
 * https://lmpessoa.com
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace Lmpessoa.Fortune {

   public partial class MainWindow : Window {

      public MainWindow() {
         InitializeComponent();
         string path = Path.GetFullPath(ConfigurationManager.AppSettings["PathToFortunes"] ?? ".\\Data");
         Fortune fortune = Fortune.Get(path);
         Text.Text = fortune.Text;
         Attribution.Text = fortune.Author != "" ? $"\u2014  {fortune.Author}" : "";
         TextBlock calc = new TextBlock() {
            TextWrapping = Text.TextWrapping,
            FontFamily = Text.FontFamily,
            FontStyle = Text.FontStyle,
            FontWeight = Text.FontWeight,
            FontStretch = Text.FontStretch,
            FontSize = Text.FontSize,
            LineHeight = Text.LineHeight,
            Text = Text.Text
         };
         Text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
         Text.Arrange(new Rect(Text.DesiredSize));

         Height = Text.ActualHeight + 40;
         if (fortune.Author != "") {
            Height += 30;
         }
         Width = Text.ActualWidth + 50;
      }

      private void CloseButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => CloseButton.Opacity = .8;

      private void CloseButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => CloseButton.Opacity = .2;

      private void CloseButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
         Point pos = e.GetPosition(CloseButton);
         if (e.LeftButton == System.Windows.Input.MouseButtonState.Released && pos.X >= 0 && pos.X < CloseButton.Width && pos.Y >= 0 && pos.Y < CloseButton.Height) {
            Close();
         }
      }

      private void Window_Loaded(object sender, EventArgs e) {
         WindowInteropHelper helper = new WindowInteropHelper(this);
         int exStyle = (int) GetWindowLong(helper.Handle, (int) GetWindowLongFields.GWL_EXSTYLE);
         exStyle |= (int) ExtendedWindowStyles.WS_EX_TOOLWINDOW;
         SetWindowLong(helper.Handle, (int) GetWindowLongFields.GWL_EXSTYLE, (IntPtr) exStyle);

         double dpiFactor = System.Windows.PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
         System.Drawing.Rectangle screen = Screen.PrimaryScreen.WorkingArea;
         CloseButton.Margin = new Thickness(Width - 20, 0, 0, 0);
         Left = ((screen.Width / dpiFactor) - Width) / 2;
         DoubleAnimation animation = new DoubleAnimation() {
            From = -Height,
            To = 12,
            Duration = new Duration(TimeSpan.FromMilliseconds(700 - Height)),
            EasingFunction = new CubicEase(),
         };
         BeginAnimation(Window.TopProperty, animation);
         SystemSounds.Exclamation.Play();
      }

      private void Window_Closing(object sender, CancelEventArgs e) {
         Closing -= Window_Closing;
         e.Cancel = true;
         DoubleAnimation animation = new DoubleAnimation() {
            From = 12,
            To = -Height,
            Duration = new Duration(TimeSpan.FromMilliseconds(700 - Height)),
            EasingFunction = new CubicEase(),
         };
         animation.Completed += (s, _) => Close();
         BeginAnimation(Window.TopProperty, animation);

      }

      [Flags]
      public enum ExtendedWindowStyles {
         // ...
         WS_EX_TOOLWINDOW = 0x00000080,
         // ...
      }

      public enum GetWindowLongFields {
         // ...
         GWL_EXSTYLE = (-20),
         // ...
      }

      [DllImport("user32.dll")]
      public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

      public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
         int error = 0;
         IntPtr result = IntPtr.Zero;
         // Win32 SetWindowLong doesn't clear error on success
         SetLastError(0);
         if (IntPtr.Size == 4) {
            // use SetWindowLong
            Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
            error = Marshal.GetLastWin32Error();
            result = new IntPtr(tempResult);
         } else {
            // use SetWindowLongPtr
            result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
            error = Marshal.GetLastWin32Error();
         }
         if ((result == IntPtr.Zero) && (error != 0)) {
            throw new System.ComponentModel.Win32Exception(error);
         }

         return result;
      }

      [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
      private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

      [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
      private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

      private static int IntPtrToInt32(IntPtr intPtr) {
         return unchecked((int) intPtr.ToInt64());
      }

      [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
      public static extern void SetLastError(int dwErrorCode);
   }
}
