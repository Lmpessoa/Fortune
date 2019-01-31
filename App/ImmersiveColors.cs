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
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Lmpessoa.Fortune {

   class ImmersiveColors {

      [DllImport("uxtheme.dll", EntryPoint = "#95")]
      static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);

      [DllImport("uxtheme.dll", EntryPoint = "#96")]
      static extern uint GetImmersiveColorTypeFromName(IntPtr pName);

      [DllImport("uxtheme.dll", EntryPoint = "#98")]
      static extern int GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

      static Color GetImmersiveColour(string colour) {
         uint cluser = (uint) GetImmersiveUserColorSetPreference(false, false);
         uint cltype = GetImmersiveColorTypeFromName(Marshal.StringToHGlobalUni(colour));
         uint cl = GetImmersiveColorFromColorSetEx(cluser, cltype, false, 0);
         return Color.FromArgb((byte) ((0xFF000000 & cl) >> 24), (byte) (0x000000FF & cl),
             (byte) ((0x0000FF00 & cl) >> 8), (byte) ((0x00FF0000 & cl) >> 16));
      }

      public static Brush ImmersiveToastBackgroundBrush =>
         new LinearGradientBrush(GetImmersiveColour("ImmersiveStartHoverBackground"), GetImmersiveColour("ImmersiveSaturatedSelectionBackground"), 90);

      public static Brush ImmersiveStartHoverPrimaryTextBrush =>
         new SolidColorBrush(GetImmersiveColour("ImmersiveStartHoverPrimaryText"));
   }
}
