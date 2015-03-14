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
			public Dictionary<int, CoverageStatement> Statements;
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
				if (!Classes.ContainsKey (method.ClassName)) {
					klass = new CoverageClass {
						Assembly = assembly,
						Name = method.ClassName,
						ClassMethods = new List<CoverageMethod> (),
						ClassFiles = new Dictionary<string, CoverageFile> ()
					};

					Classes [method.ClassName] = klass;
				} else {
					klass = Classes [method.ClassName];
				}
				assembly.Classes [method.ClassName] = klass;

				klass.ClassMethods.Add (method);

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

					if (klass.ClassFiles.ContainsKey (statement.Filename)) {
						file = klass.ClassFiles [statement.Filename];
					} else {
						file = new CoverageFile {
							Filename = statement.Filename,
							Statements = new Dictionary<int, CoverageStatement> ()
						};
						klass.ClassFiles [statement.Filename] = file;
					}

					file.Statements[statement.Line] = statement;
				}

				Methods.Add (method);
			}
		}
	}
}

