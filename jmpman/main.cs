using arookas.IO.Binary;
using arookas.Xml;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace arookas {
	static class jmpman {
		static jmp sJmp;
		static jmpNamesLUT sNames;

		static int Main(string[] args) {
			message("jmpman arookas\n");
			if (args.Length < 2) {
				message("usage: jmpman.exe <xml|jmp> \"input file\" [\"outfile file\"]\n");
				return 1;
			}
			loadNamesTxt();
			var informat = IoFormat.JMP;
			var outformat = IoFormat.XML;
			switch (args[0].ToLowerInvariant()) {
				case "xml": {
					informat = IoFormat.XML;
					outformat = IoFormat.JMP;
					break;
				}
				case "jmp": {
					informat = IoFormat.JMP;
					outformat = IoFormat.XML;
					break;
				}
			}
			var inpath = args[1];
			var outpath = String.Concat(args[1], ".", outformat);
			if (args.Length > 2) {
				outpath = args[2];
			}
			var jmp = loadJmp(inpath, informat);
			convertJmp(jmp, outpath, outformat);
			return 0;
		}

		static jmp loadJmp(string path, IoFormat format) {
			var jmp = new jmp();
			jmp.SetNameResolver(lookupName);
			using (var file = File.OpenRead(path)) {
				switch (format) {
					case IoFormat.JMP: {
						var reader = new aBinaryReader(file, Endianness.Big, Encoding.GetEncoding(932));
						jmp.load(reader);
						break;
					}
					case IoFormat.XML: {
						var document = new xDocument(file);
						jmp.load(document.Root);
						break;
					}
				}
			}
			return jmp;
		}
		static void convertJmp(jmp jmp, string path, IoFormat format) {
			using (var file = File.Create(path)) {
				switch (format) {
					case IoFormat.JMP: {
						var writer = new aBinaryWriter(file, Endianness.Big, Encoding.GetEncoding(932));
						jmp.save(writer);
						break;
					}
					case IoFormat.XML: {
						var settings = new XmlWriterSettings() {
							OmitXmlDeclaration = true,
							Encoding = Encoding.UTF8,
							NewLineChars = "\n",
							IndentChars = "\t",
							Indent = true,
						};
						var writer = XmlWriter.Create(file, settings);
						jmp.save(writer);
						writer.Close();
						break;
					}
				}
			}
		}

		static void loadNamesTxt() {
			message("loading names...\r");
			if (sNames == null) {
				sNames = new jmpNamesLUT();
			}
			if (!File.Exists("names.txt")) {
				message("could not find names. all fields in output will be unnamed.\n");
				return;
			}
			using (var file = File.OpenRead("names.txt")) {
				var reader = new StreamReader(file);
				while (!reader.EndOfStream) {
					var name = reader.ReadLine();
					if (String.IsNullOrWhiteSpace(name) || name.StartsWith("#")) {
						continue;
					}
					sNames.add(name);
				}
			}
			message("names loaded successfully ({0} total).\n", sNames.Count);
		}
		static bool lookupName(uint hash, out string name) {
			name = sNames[hash];
			if (name == null) {
				return false;
			}
			return true;
		}

		static void message(string msg) {
			message("{0}", msg);
		}
		static void message(string format, params object[] args) {
			Console.WriteLine(format, args);
		}

		enum IoFormat {
			XML,
			JMP,
		}
	}
}
