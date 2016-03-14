using System;
using System.Collections.Generic;

namespace arookas {
	class jmpNamesLUT {
		Dictionary<jmpHash, string> mNames;

		public int Count {
			get { return mNames.Count; }
		}

		public string this[jmpHash hash] {
			get {
				string name;
				if (!mNames.TryGetValue(hash, out name)) {
					return null;
				}
				return name;
			}
		}

		public jmpNamesLUT() {
			mNames = new Dictionary<jmpHash, string>();
		}

		public void add(string name) {
			if (name == null) {
				throw new ArgumentNullException("name");
			}
			mNames[name] = name; // let the jmpHash operator hash for me because I'm a lazy bitch
		}
		public void addRange(IEnumerable<string> names) {
			if (names == null) {
				throw new ArgumentNullException("names");
			}
			foreach (var name in names) {
				add(name);
			}
		}
		public void clear() {
			mNames.Clear();
		}
	}
}
