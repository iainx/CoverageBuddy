using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Gtk;
using Glade;

using CoverageBuddy;
using IgeMacIntegration;

public partial class MainWindow: Gtk.Window
{
	private TreeStore coverageAsTreeModel;
	private CoverageModel model;
	public CoverageModel Model { 
		get { return model; }
		set { 
			model = value;

			coverageAsTreeModel = new TreeStore (typeof (string), typeof (string), typeof (string), typeof (string));
			foreach (var assemblyPair in model.Assemblies) {
				CoverageModel.CoverageAssembly assembly = assemblyPair.Value;

				int percentage = ((assembly.FullyCovered + assembly.PartiallyCovered) * 100) / assembly.NumberOfMethods;
				TreeIter iter = coverageAsTreeModel.AppendValues (String.Format ("{0} {1}% covered", assembly.Name, percentage), assembly, null, null);

				if (model.Methods == null) {
					continue;
				}

				List<KeyValuePair<string, CoverageModel.CoverageClass>> classList = assembly.Classes.ToList ();
				classList.Sort ((firstPair, secondPair) => {
					return firstPair.Value.Name.CompareTo (secondPair.Value.Name);
				});

				foreach (var classPair in classList) {
					CoverageModel.CoverageClass klass = classPair.Value;
					coverageAsTreeModel.AppendValues (iter, klass.Name, null, klass.Name, null);

					/*
					var classMethods = model.Methods.Where (m => m.ClassName == method.ClassName);
					foreach (CoverageModel.CoverageMethod classMethod in classMethods) {
						coverageAsTreeModel.AppendValues (classIter, classMethod.MethodName, null, null, classMethod.MethodName);
					}
					*/
				}
			}

			coverageTreeView.Model = coverageAsTreeModel;
		}
	}

	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();

		Title = "CoverageBuddy";

		SetupTreeView ();

		SetupMacIntegration ();

	}

	void SetupMacIntegration ()
	{
		IgeMacMenu.GlobalKeyHandlerEnabled = true;
		IgeMacMenu.MenuBar = mainMenuBar;

		mainMenuBar.Hide ();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	enum CoverageType {
		NotProfiled,
		Covered,
		NotCovered
	};

	void AddLineToBuffer (string line, int lineNumber, CoverageType type) {
		TextBuffer buffer = fileTextView.Buffer;
		TextIter iter = buffer.GetIterAtLine (lineNumber);

		switch (type) {
		case CoverageType.NotProfiled:
			buffer.Insert (ref iter, line + '\n');
			break;

		case CoverageType.Covered:
			buffer.InsertWithTagsByName (ref iter, line + '\n', "green");
			break;

		case CoverageType.NotCovered:
			buffer.InsertWithTagsByName (ref iter, line + '\n', "red");
			break;
		}
	}

	private void SetupTreeView () {
		TextBuffer buffer = fileTextView.Buffer;
		TextTag redTag = new TextTag ("red");
		TextTag greenTag = new TextTag ("green");

		redTag.Foreground = "red";
		buffer.TagTable.Add (redTag);

		greenTag.Foreground = "green";
		buffer.TagTable.Add (greenTag);

		TreeViewColumn assemblyColumn = new TreeViewColumn ();
		assemblyColumn.Title = "Assemblies";

		coverageTreeView.AppendColumn ("Assemblies", new CellRendererText (), "text", 0);

		TreeSelection selection = coverageTreeView.Selection;
		selection.Changed += (o, e) => {
			TreeIter iter;

			if (selection.GetSelected(out iter)) {
				string className = coverageAsTreeModel.GetValue (iter, 2) as string;
				if (className == null) {
					return;
				}

				CoverageModel.CoverageClass klass = model.Classes[className];

				var pair = klass.ClassFiles.FirstOrDefault();
				CoverageModel.CoverageFile file = pair.Value;
				string filename = file.Filename;
				if (string.IsNullOrEmpty (filename)) {
					return;
				}

				titleLabel.Text = filename;

				fileTextView.Buffer.Text = "";
				string[] fileContents = System.IO.File.ReadAllLines (filename);

				for (int i = 0; i < fileContents.Length; i++) {
					CoverageType type = CoverageType.NotProfiled;

					// Statement line numbers start at 1
					bool covered;
					if (file.LinesHit.TryGetValue (i + 1, out covered)) {
						if (covered) {
							type = CoverageType.Covered;
						} else {
							type = CoverageType.NotCovered;
						}
					}

					AddLineToBuffer (fileContents[i], i, type);
				}
			}
		};
	}

	protected void OnOpen (object sender, EventArgs e)
	{
		FileChooserDialog dialog = new FileChooserDialog ("Select coverage file", this, FileChooserAction.Open, 
			"Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

		if (dialog.Run () == (int) ResponseType.Accept) {
			CoverageModel model = new CoverageModel (dialog.Filename);
			Model = model;
		}

		dialog.Destroy ();
	}

	protected void OnQuit (object sender, EventArgs e)
	{
		Application.Quit ();
	}
}
