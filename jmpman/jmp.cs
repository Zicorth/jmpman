using arookas.IO.Binary;
using arookas.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace arookas {
	class jmp {
		jmpField[] mFields;
		jmpValue[,] mEntries; // BRING ON THE GOATS SPACEY
		jmpNameResolver mResolver;

		static string[] sTypeNames = {
			"int", "str", "flt",
		};

		public int this[jmpHash hash] {
			get {
				for (var i = 0; i < mFields.Length; ++i) {
					if (mFields[i].hash == hash) {
						return i;
					}
				}
				return -1;
			}
		}
		public jmpValue this[int index, jmpHash hash] {
			get {
				return this[index, this[hash]];
			}
		}
		public jmpValue this[int index, int field] {
			get {
				return mEntries[index, field];
			}
		}

		public int FieldCount {
			get { return mFields.Length; }
		}
		public int EntryCount {
			get { return mEntries.Length / FieldCount; }
		}

		public jmp() {
			mFields = new jmpField[0];
			mEntries = new jmpValue[0, 0];
			mResolver = null;
		}

		public jmpNameResolver SetNameResolver(jmpNameResolver resolver) {
			var old = mResolver;
			mResolver = resolver;
			return old;
		}

		public void load(aBinaryReader reader) {
			if (reader == null) {
				throw new ArgumentNullException("reader");
			}
			reader.PushAnchor();
			var entryCount = reader.ReadS32();
			var fieldCount = reader.ReadS32();
			var entryOffset = reader.Read32();
			var entrySize = reader.ReadS32();
			mFields = new jmpField[fieldCount];
			for (var i = 0; i < fieldCount; ++i) {
				mFields[i].hash = reader.Read32();
				mFields[i].bitmask = reader.Read32();
				mFields[i].start = reader.Read16();
				mFields[i].shift = reader.Read8();
				mFields[i].type = (jmpValueType)reader.Read8();
			}
			mEntries = new jmpValue[entryCount, fieldCount];
			for (var entry = 0; entry < entryCount; ++entry) {
				for (var field = 0; field < fieldCount; ++field) {
					reader.Goto(entryOffset + (entrySize * entry) + mFields[field].start);
					switch (mFields[field].type) {
						case jmpValueType.INTEGER: mEntries[entry, field] = (int)((reader.ReadS32() & mFields[field].bitmask) >> mFields[field].shift); break;
						case jmpValueType.FLOAT: mEntries[entry, field] = reader.ReadF32(); break;
						case jmpValueType.STRING: mEntries[entry, field] = reader.ReadString<aCSTR>(0x20); break;
					}
				}
			}
			reader.PopAnchor();
		}
		public void load(xElement element) {
			if (element == null) {
				throw new ArgumentNullException("element");
			}
			loadFields(element.Elements("field"));
			loadEntries(element.Elements("entry"));
		}
		void loadFields(IEnumerable<xElement> elements) {
			if (elements == null) {
				mFields = new jmpField[0];
				return;
			}
			mFields = new jmpField[elements.Count()];
			var index = 0;
			foreach (var element in elements) {
				mFields[index].hash = loadHash(element);
				mFields[index].bitmask = parseU32(element.Attribute("bitmask").Value);
				mFields[index].start = (ushort)parseU32(element.Attribute("start").Value);
				mFields[index].shift = (byte)parseU32(element.Attribute("shift").Value);
				mFields[index].type = parseType(element.Attribute("type"));
				++index;
			}
		}
		uint loadHash(xElement element) {
			if (element != null) {
				if (element.Attribute("hash")) {
					return parseU32(element.Attribute("element"));
				}
				else if (element.Attribute("name") != null) {
					return hash.calculate(element.Attribute("name"));
				}
			}
			return 0u;
		}
		void loadEntries(IEnumerable<xElement> elements) {
			if (elements == null) {
				mEntries = new jmpValue[0, FieldCount];
				return;
			}
			mEntries = new jmpValue[elements.Count(), FieldCount];
			var index = 0;
			foreach (var element in elements) {
				var field = 0;
				foreach (var child in element.Elements()) {
					if (field >= FieldCount) {
						break;
					}
					switch (mFields[field].type) {
						case jmpValueType.INTEGER: {
							mEntries[index, field] = readIntegerField(child);
							break;
						}
						case jmpValueType.FLOAT: {
							mEntries[index, field] = readFloatField(child);
							break;
						}
						case jmpValueType.STRING: {
							mEntries[index, field] = readStringField(child);
							break;
						}
					}
					++field;
				}
				while (field < FieldCount) {
					mEntries[index, field] = jmpValue.getDefault(mFields[field].type);
					++field;
				}
				++index;
			}
		}
		
		public void save(aBinaryWriter writer) {
			if (writer == null) {
				throw new ArgumentNullException("writer");
			}
			// calculate entry shit
			var entryOffset = calculateEntryOffset(FieldCount);
			var entrySize = calculateEntrySize(mFields);
			// write header
			writer.WriteS32(EntryCount);
			writer.WriteS32(FieldCount);
			writer.Write32(entryOffset);
			writer.WriteS32(entrySize);
			// write field LUT
			foreach (var field in mFields) {
				writer.Write32(field.hash);
				writer.Write32(field.bitmask);
				writer.Write16(field.start);
				writer.Write8(field.shift);
				writer.Write8((byte)field.type);
			}
			// since the stream is write-only, we must write packed integer fields to an intermediate, R/W buffer
			var buffer = new Dictionary<ushort, uint>(FieldCount);
			for (var entry = 0; entry < EntryCount; ++entry) {
				buffer.Clear();
				for (var field = 0; field < FieldCount; ++field) {
					writer.Goto(entryOffset + (entrySize * entry) + mFields[field].start);
					switch (mFields[field].type) {
						case jmpValueType.INTEGER: {
							if (mFields[field].bitmask == 0xFFFFFFFFu) {
								// field is unpacked; write directly to stream
								writer.WriteS32(mEntries[entry, field]);
							}
							else {
								// field is packed; write to intermediate buffer
								if (!buffer.ContainsKey(mFields[field].start)) {
									buffer[mFields[field].start] = 0u; // if there's no key yet, create one
								}
								buffer[mFields[field].start] |= ((uint)mEntries[entry, field] << mFields[field].shift) & mFields[field].bitmask;
							}
							break;
						}
						case jmpValueType.FLOAT: {
							writer.WriteF32(mEntries[entry, field]);
							break;
						}
						case jmpValueType.STRING: {
							writer.WriteString<aCSTR>(mEntries[entry, field], 0x20);
							break;
						}
					}
				}
				// flush intermediate buffer
				foreach (var point in buffer) {
					writer.Goto(entryOffset + (entrySize * entry) + point.Key);
					writer.Write32(point.Value);
				}
			}
		}
		public void save(XmlWriter writer) {
			if (writer == null) {
				throw new ArgumentNullException("writer");
			}
			string name;
			writer.WriteStartElement("jmp");
			// write entries
			for (var entry = 0; entry < EntryCount; ++entry) {
				writer.WriteStartElement("entry");
				for (var field = 0; field < FieldCount; ++field) {
					if (tryResolveName(mFields[field].hash, out name)) {
						writer.WriteStartElement(name);
					}
					else {
						writer.WriteStartElement("field");
					}
					writer.WriteValue(mEntries[entry, field].ToString());
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}
			// write field LUT
			foreach (var field in mFields) {
				writer.WriteStartElement("field");
				if (tryResolveName(field.hash, out name)) {
					writer.WriteAttributeString("name", name);
				}
				else {
					writer.WriteAttributeString("hash", String.Format("${0:X8}", (uint)field.hash));
				}
				writer.WriteAttributeString("type", sTypeNames[(int)field.type]);
				writer.WriteAttributeString("start", field.start.ToString());
				writer.WriteAttributeString("bitmask", String.Format("${0:X8}", field.bitmask));
				writer.WriteAttributeString("shift", field.shift.ToString());
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		bool tryResolveName(jmpHash hash, out string name) {
			name = null;
			if (mResolver == null) {
				return false;
			}
			return mResolver(hash, out name);
		}

		static uint calculateEntryOffset(int fieldCount) {
			return (uint)(16 + (12 * fieldCount));
		}
		static int calculateEntrySize(IEnumerable<jmpField> fields) {
			if (fields == null) {
				return 0;
			}
			var max = 0;
			foreach (var field in fields) {
				var cur = field.start + calculateFieldSize(field);
				if (cur > max) {
					max = cur;
				}
			}
			return max;
		}
		static int calculateFieldSize(jmpField field) {
			switch (field.type) {
				case jmpValueType.INTEGER: return 4;
				case jmpValueType.FLOAT: return 4;
				case jmpValueType.STRING: return 32;
			}
			return 0;
		}

		static uint parseU32(string value) {
			return parseU32(value, 0);
		}
		static uint parseU32(string value, uint def) {
			if (value == null || value.Length == 0) {
				return def;
			}
			var style = NumberStyles.None;
			var res = 0u;
			if (value[0] == '$') {
				value = value.Substring(1);
				style = NumberStyles.AllowHexSpecifier;
			}
			if (!UInt32.TryParse(value, style, null, out res)) {
				return 0;
			}
			return res;
		}
		static jmpValueType parseType(xAttribute attribute) {
			if (attribute == null) {
				return jmpValueType.STRING;
			}
			switch (attribute.Value) {
				case "i":
				case "int":
				case "integer": {
					return jmpValueType.INTEGER;
				}
				case "f":
				case "flt":
				case "float": {
					return jmpValueType.FLOAT;
				}
				case "s":
				case "str":
				case "string": {
					return jmpValueType.STRING;
				}
			}
			return jmpValueType.STRING;
		}

		static int readIntegerField(xElement element) {
			if (element == null) {
				return 0;
			}
			var match = Regex.Match(element.Value, @"^\s*-?\d+\s*$");
			if (!match.Success) {
				return 0;
			}
			return Int32.Parse(element.Value);
		}
		static float readFloatField(xElement element) {
			if (element == null) {
				return 0.0f;
			}
			var match = Regex.Match(element.Value, @"^\s*-?\d+(\.\d+\s*)?$");
			if (!match.Success) {
				return 0.0f;
			}
			return Single.Parse(element.Value, CultureInfo.InvariantCulture);
		}
		static string readStringField(xElement element) {
			if (element == null) {
				return null;
			}
			return element.Value;
		}

		struct jmpField {
			public jmpHash hash;
			public uint bitmask;
			public ushort start;
			public byte shift;
			public jmpValueType type;
		}
	}

	delegate bool jmpNameResolver(uint hash, out string value);

	enum jmpValueType : byte {
		INTEGER,
		STRING,
		FLOAT,
	}

	struct jmpHash {
		uint mHash;
		
		public jmpHash(uint hash) {
			mHash = hash;
		}
		public jmpHash(string value) {
			if (value == null) {
				throw new ArgumentNullException("value");
			}
			mHash = hash.calculate(value);
		}

		public static implicit operator jmpHash(uint hash) {
			return new jmpHash(hash);
		}
		public static implicit operator jmpHash(string value) {
			return new jmpHash(value);
		}

		public static implicit operator uint(jmpHash hash) {
			return hash.mHash;
		}

		public static bool operator ==(jmpHash a, jmpHash b) {
			return a.mHash == b.mHash;
		}
		public static bool operator !=(jmpHash a, jmpHash b) {
			return !(a == b);
		}

		public override int GetHashCode() {
			return (int)mHash;
		}
		public override string ToString() {
			return mHash.ToString();
		}
	}

	struct jmpValue {
		jmpValueType mType;
		int mIntValue;
		float mFloatValue;
		string mStringValue;

		public jmpValueType Type {
			get { return mType; }
		}

		public jmpValue(int value)
			: this() {
			mType = jmpValueType.INTEGER;
			mIntValue = value;
		}
		public jmpValue(float value)
			: this() {
			mType = jmpValueType.FLOAT;
			mFloatValue = value;
		}
		public jmpValue(string value)
			: this() {
			mType = jmpValueType.STRING;
			mStringValue = value;
		}

		public static jmpValue getDefault(jmpValueType type) {
			switch (type) {
				case jmpValueType.INTEGER: return default(int);
				case jmpValueType.FLOAT: return default(float);
				case jmpValueType.STRING: return default(string);
			}
			return default(jmpValue);
		}

		public static implicit operator int(jmpValue value) {
			switch (value.mType) {
				case jmpValueType.INTEGER: return value.mIntValue;
				case jmpValueType.FLOAT: return (int)value.mFloatValue;
			}
			return 0;
		}
		public static implicit operator float(jmpValue value) {
			switch (value.mType) {
				case jmpValueType.INTEGER: return (float)value.mIntValue;
				case jmpValueType.FLOAT: return value.mFloatValue;
			}
			return 0;
		}
		public static implicit operator string(jmpValue value) {
			if (value.mType != jmpValueType.STRING) {
				return null;
			}
			return value.mStringValue;
		}

		public static implicit operator jmpValue(int value) {
			return new jmpValue(value);
		}
		public static implicit operator jmpValue(float value) {
			return new jmpValue(value);
		}
		public static implicit operator jmpValue(string value) {
			return new jmpValue(value);
		}

		public override string ToString() {
			switch (mType) {
				case jmpValueType.INTEGER: {
					return mIntValue.ToString();
				}
				case jmpValueType.FLOAT: {
					return mFloatValue.ToString(CultureInfo.InvariantCulture);
				}
				case jmpValueType.STRING: {
					if (mStringValue == null) {
						return "(null)";
					}
					return mStringValue;
				}
			}
			return base.ToString();
		}
	}
}
