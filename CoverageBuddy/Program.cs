using System;
using Gtk;

namespace CoverageBuddy
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();

			//CoverageModel model = new CoverageModel ("/Users/iain/Projects/xamarin/hello/test.xml");
			MainWindow win = new MainWindow ();

			win.Show ();
			Application.Run ();
		}
	}
}
