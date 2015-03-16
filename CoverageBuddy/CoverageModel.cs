using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CoverageBuddy
{
	public class CoverageModel
	{
		public class CoverageAssembly {
			public string Name;
			public string Guid;
			public string Filename;
			public int NumberOfMethods;
			public int FullyCovered;
			public int PartiallyCovered;

			public Dictionary<string, CoverageClass> Classes;
		};

		public class CoverageFile {
			public string Filename;
			public Dictionary<int, bool> LinesHit;
		};

		public class CoverageClass {
			public CoverageAssembly Assembly;
			public string Name;
			public List<CoverageMethod> ClassMethods;
			public Dictionary<string, CoverageFile> ClassFiles;
		};
			
		public class CoverageStatement {
			public int Offset;
			public int Counter;
			public string Filename;
			public int Line;
			public int Column;
		};

		public class CoverageMethod {
			public CoverageAssembly Assembly;
            public CoverageClass ParentClass;
			public string ClassName;
			public string MethodName;
			public Dictionary<int, CoverageStatement> Statements;
		};

		public Dictionary<string, CoverageAssembly> Assemblies { get; private set; }
		public List<CoverageMethod> Methods { get; private set; }
		public Dictionary<string, CoverageClass> Classes { get; private set; }

		public CoverageModel (string filename)
		{
			XDocument document = XDocument.Load (filename);

			Classes = new Dictionary<string, CoverageClass> ();

			var a = from assembly in document.Descendants ("assembly")
			        select new CoverageAssembly {
				Name = (string)assembly.Attribute ("name"),
				Guid = (string)assembly.Attribute ("guid"),
				Filename = (string)assembly.Attribute ("filename"),
				NumberOfMethods = (int)assembly.Attribute ("method-count"),
				FullyCovered = (int)assembly.Attribute ("full"),
				PartiallyCovered = (int)assembly.Attribute ("partial"),
				Classes = new Dictionary<string, CoverageClass> ()
			};

			Assemblies = new Dictionary<string, CoverageAssembly> ();
			foreach (var assembly in a) {
				Assemblies[assembly.Name] = assembly;
			}

			Methods = new List<CoverageMethod> ();

			var ms = document.Descendants ("method");
			foreach (var m in ms) {
				CoverageAssembly assembly = Assemblies [(string)m.Attribute ("assembly")];

				CoverageMethod method = new CoverageMethod {
					Assembly = assembly,
					ClassName = (string)m.Attribute ("class"),
					MethodName = (string)m.Attribute ("name"),
					Statements = new Dictionary<int, CoverageStatement> ()
				};

				CoverageClass klass;
				if (!Classes.TryGetValue (method.ClassName, out klass)) {
					klass = new CoverageClass {
						Assembly = assembly,
						Name = method.ClassName,
						ClassMethods = new List<CoverageMethod> (),
						ClassFiles = new Dictionary<string, CoverageFile> ()
					};

					Classes [method.ClassName] = klass;
				}
				assembly.Classes [method.ClassName] = klass;

				klass.ClassMethods.Add (method);
                method.ParentClass = klass;

				var s = from statement in m.Descendants ("statement")
					select new CoverageStatement {
						Offset = (int)statement.Attribute("offset"),
						Counter = (int)statement.Attribute("counter"),
						Filename = (string)statement.Attribute("filename"),
						Line = (int)statement.Attribute("line"),
						Column = (int)statement.Attribute("column")
				};

				foreach (var statement in s) {
					method.Statements [statement.Line] = statement;

					CoverageFile file;

					if (!klass.ClassFiles.TryGetValue (statement.Filename, out file)) {
						file = new CoverageFile {
							Filename = statement.Filename,
							LinesHit = new Dictionary<int, bool> ()
						};
						klass.ClassFiles [statement.Filename] = file;
					}

					bool covered;
					if (file.LinesHit.TryGetValue (statement.Line, out covered)) {
						// If a line has been covered, we don't want to undo it
						// if we come across the same line that hasn't been hit.
						if (covered == false) {
							file.LinesHit [statement.Line] = (statement.Counter == 1);
						}
					} else {
						file.LinesHit [statement.Line] = (statement.Counter == 1);
					}
				}

				Methods.Add (method);
			}
		}
	}
}

