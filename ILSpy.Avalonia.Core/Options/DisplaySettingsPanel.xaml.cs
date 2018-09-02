﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;
using System.Xml.Linq;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace ICSharpCode.ILSpy.Options
{
	/// <summary>
	/// Interaction logic for DisplaySettingsPanel.xaml
	/// </summary>
	[ExportOptionPage(Title = "Display", Order = 1)]
	public partial class DisplaySettingsPanel : UserControl, IOptionPage
	{
		internal DropDown fontSelector;

		public DisplaySettingsPanel()
		{
			InitializeComponent();

			Task<FontFamily[]> task = new Task<FontFamily[]>(FontLoader);
			task.Start();
			task.ContinueWith(
				delegate(Task continuation) {
					Dispatcher.UIThread.InvokeAsync(
						(Action)(
							async () => {
								fontSelector.Items = task.Result;
								if (continuation.Exception != null) {
									foreach (var ex in continuation.Exception.InnerExceptions) {
										await MessageBox.Show(ex.ToString());
									}
								}
							}),
						DispatcherPriority.Normal
					);
				}
			);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			fontSelector = this.FindControl<DropDown>("fontSelector");
		}

		public void Load(ILSpySettings settings)
		{
			this.DataContext = LoadDisplaySettings(settings);
		}
		
		static DisplaySettings currentDisplaySettings;
		
		public static DisplaySettings CurrentDisplaySettings {
			get {
				return currentDisplaySettings ?? (currentDisplaySettings = LoadDisplaySettings(ILSpySettings.Load()));
			}
		}
		
		//static bool IsSymbolFont(FontFamily fontFamily)
		//{
		//	foreach (var tf in fontFamily.GetTypefaces()) {
		//		GlyphTypeface glyph;
		//		try {
		//			if (tf.TryGetGlyphTypeface(out glyph))
		//				return glyph.Symbol;
		//		} catch (Exception) {
		//			return true;
		//		}
		//	}
		//	return false;
		//}
		
		static FontFamily[] FontLoader()
		{
			//TODO: font families
			//return (from ff in Fonts.SystemFontFamilies
			//		where !IsSymbolFont(ff)
			//		orderby ff.Source
			//		select ff).ToArray();
			return new FontFamily[]{ FontFamily.Parse("Arial"), FontFamily.Parse("Segoe UI") };
		}

		public static DisplaySettings LoadDisplaySettings(ILSpySettings settings)
		{
			XElement e = settings["DisplaySettings"];
			var s = new DisplaySettings();
			s.SelectedFont = new FontFamily((string)e.Attribute("Font") ?? "Consolas");
			s.SelectedFontSize = (double?)e.Attribute("FontSize") ?? 10.0 * 4 / 3;
			s.ShowLineNumbers = (bool?)e.Attribute("ShowLineNumbers") ?? false;
			s.ShowMetadataTokens = (bool?) e.Attribute("ShowMetadataTokens") ?? false;
		    s.EnableWordWrap = (bool?)e.Attribute("EnableWordWrap") ?? false;
			s.SortResults = (bool?)e.Attribute("SortResults") ?? true;

			return s;
		}
		
		public void Save(XElement root)
		{
			var s = (DisplaySettings)this.DataContext;
			
			var section = new XElement("DisplaySettings");
			section.SetAttributeValue("Font", s.SelectedFont.Name);
			section.SetAttributeValue("FontSize", s.SelectedFontSize);
			section.SetAttributeValue("ShowLineNumbers", s.ShowLineNumbers);
			section.SetAttributeValue("ShowMetadataTokens", s.ShowMetadataTokens);
			section.SetAttributeValue("EnableWordWrap", s.EnableWordWrap);
			section.SetAttributeValue("SortResults", s.SortResults);

			XElement existingElement = root.Element("DisplaySettings");
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);

			if (currentDisplaySettings != null)
				currentDisplaySettings.CopyValues(s);
		}

	}
	

	public class FontSizeConverter : IValueConverter
	{
		public static readonly FontSizeConverter Instance = new FontSizeConverter();

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is double) {
				return Math.Round((double)value / 4 * 3);
			}
			
			throw new NotImplementedException();
		}
		
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is IContentControl context)
			{
				value = context.Content;
			}
			if (value is string) {
				double d;
				if (double.TryParse((string)value, out d))
					return d * 4 / 3;
				return 11 * 4 / 3;
			}
			if(value == null)
			{
				return 11 * 4 / 3;
			}
			
			throw new NotImplementedException();
		}
	}
}