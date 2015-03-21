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
            public int NumberOfMethods;
            public int FullyCovered;
            public int PartiallyCovered;

			public List<CoverageMethod> ClassMethods;
			public Dictionary<string, CoverageFile> ClassFiles;
		};
			
		public class CoverageStatement {
			public int Offset;
			public int Counter;
			public int Line;
			public int Column;
		};

		public class CoverageMethod {
			public CoverageAssembly Assembly;
            public CoverageClass ParentClass;
			public string ClassName;
			public string MethodName;
            public string Filename;
			public Dictionary<int, CoverageStatement> Statements;
		};

		public Dictionary<string, CoverageAssembly> Assemblies { get; private set; }
		public List<CoverageMethod> Methods { get; private set; }
		public Dictionary<string, CoverageClass> Classes { get; private set; }

		public CoverageModel (string filename)
		{
			XDocument document = XDocument.Load (filename);

			Classes = new Dictionary<string, CoverageClass> ();

            Assemblies = new Dictionary<string, CoverageAssembly> ();
            var assemblies = document.Descendants ("assembly");
            foreach (var a in assemblies) {
                CoverageAssembly assembly = new CoverageAssembly {
                    Name = (string)a.Attribute ("name"),
                    Guid = (string)a.Attribute ("guid"),
                    Filename = (string)a.Attribute ("filename"),
                    NumberOfMethods = (int)a.Attribute ("method-count"),
                    FullyCovered = (int)a.Attribute ("full"),
                    PartiallyCovered = (int)a.Attribute ("partial"),
                    Classes = new Dictionary<string, CoverageClass> ()
                };

                Assemblies [assembly.Name] = assembly;

                var classes = a.Descendants ("class");
                foreach (var c in classes) {
                    CoverageClass klass = new CoverageClass {
                        Assembly = assembly,
                        Name = (string)c.Attribute ("name"),
                        NumberOfMethods = (int)c.Attribute ("method-count"),
                        FullyCovered = (int)c.Attribute ("full"),
                        PartiallyCovered = (int)c.Attribute ("partial"),
                        ClassMethods = new List<CoverageMethod> (),
                        ClassFiles = new Dictionary<string, CoverageFile> ()
                    };

                    assembly.Classes[klass.Name] = klass;
                    Classes [klass.Name] = klass;
                }
            }

			Methods = new List<CoverageMethod> ();

			var ms = document.Descendants ("method");
			foreach (var m in ms) {
				CoverageAssembly assembly = Assemblies [(string)m.Attribute ("assembly")];

				CoverageMethod method = new CoverageMethod {
					Assembly = assembly,
					ClassName = (string)m.Attribute ("class"),
					MethodName = (string)m.Attribute ("name"),
                    Filename = (string)m.Attribute ("filename"),
					Statements = new Dictionary<int, CoverageStatement> ()
				};
                    
				CoverageClass klass;
                CoverageFile file = null;

				if (!Classes.TryGetValue (method.ClassName, out klass)) {
                    Console.WriteLine ("Unknown class: " + method.ClassName);
                } else {
                    klass.ClassMethods.Add (method);
                    method.ParentClass = klass;

                    if (!klass.ClassFiles.TryGetValue (method.Filename, out file)) {
                        file = new CoverageFile {
                            Filename = method.Filename,
                            LinesHit = new Dictionary<int, bool> ()
                        };
                        klass.ClassFiles [method.Filename] = file;
                    }
                }

				var s = from statement in m.Descendants ("statement")
					select new CoverageStatement {
						Offset = (int)statement.Attribute("offset"),
						Counter = (int)statement.Attribute("counter"),
						Line = (int)statement.Attribute("line"),
						Column = (int)statement.Attribute("column")
				};

				foreach (var statement in s) {
					method.Statements [statement.Line] = statement;

                    if (file == null) {
                        continue;
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

