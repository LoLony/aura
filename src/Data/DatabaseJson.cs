﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MsgPack.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aura.Data
{
	/// <summary>
	/// Base class for JSON databases
	/// </summary>
	/// <typeparam name="TList"></typeparam>
	/// <typeparam name="TInfo"></typeparam>
	public abstract class DatabaseJsonBase<TList, TInfo> : DatabaseBase<TList, TInfo>
		where TInfo : class, new()
		where TList : ICollection, new()
	{
		private string[] _mandatoryKeys;

		public DatabaseJsonBase()
		{
			var attr = this.GetType().GetMethod("ReadEntry", BindingFlags.NonPublic | BindingFlags.Instance).GetCustomAttributes(typeof(MandatoryAttribute), true);
			if (attr.Length > 0)
				_mandatoryKeys = (attr[0] as MandatoryAttribute).Keys;
		}

		protected override void LoadFromFile(string path)
		{
			using (var fs = new StreamReader(path))
			using (var reader = new JsonTextReader(fs))
			{
				try
				{
					var array = JArray.Load(reader);

					foreach (var entry in array)
					{
						var obj = entry as JObject;
						if (obj == null) continue;

						try
						{
							if (_mandatoryKeys != null)
							{
								var missing = false;
								foreach (var key in _mandatoryKeys)
								{
									if (obj[key] == null)
									{
										this.Warnings.Add(new MandatoryValueException(path, key, obj));
										missing = true;
										continue;
									}
								}
								if (missing)
									continue;
							}

							this.ReadEntry(obj);
						}
						catch (MandatoryValueException ex)
						{
							this.Warnings.Add(new MandatoryValueException(path, ex.Key, obj));
							continue;
						}
						catch (DatabaseWarningException ex)
						{
							this.Warnings.Add(new DatabaseWarningException(ex.Message + ", in " + Environment.NewLine + obj.ToString(), path));
							continue;
						}
						catch (OverflowException ex)
						{
							this.Warnings.Add(new DatabaseWarningException("Number to big or too small for variable, in " + Environment.NewLine + obj.ToString(), path));
							continue;
						}
					}
				}
				catch (JsonReaderException ex)
				{
					// Throw to stop the server, databases depend on each
					// other, skipping one could lead to problems.
					throw new DatabaseErrorException(ex.Message, path);
				}
			}
		}

		protected abstract void ReadEntry(JObject entry);
	}

	/// <summary>
	/// JSON database holding a data list
	/// </summary>
	/// <typeparam name="TInfo">Data type</typeparam>
	public abstract class DatabaseJson<TInfo> : DatabaseJsonBase<List<TInfo>, TInfo> where TInfo : class, new()
	{
		public override IEnumerator<TInfo> GetEnumerator()
		{
			foreach (var entry in this.Entries)
				yield return entry;
		}

		public override void Clear()
		{
			this.Entries.Clear();
		}
	}

	/// <summary>
	/// JSON database holding a data dictionary
	/// </summary>
	/// <typeparam name="TIndex">Type of the dictionary key</typeparam>
	/// <typeparam name="TInfo">Data type</typeparam>
	public abstract class DatabaseJsonIndexed<TIndex, TInfo> : DatabaseJsonBase<Dictionary<TIndex, TInfo>, TInfo> where TInfo : class, new()
	{
		public override IEnumerator<TInfo> GetEnumerator()
		{
			foreach (var entry in this.Entries.Values)
				yield return entry;
		}

		public TInfo Find(TIndex key)
		{
			return this.Entries.GetValueOrDefault(key);
		}

		public bool Exists(TIndex key)
		{
			return this.Entries.ContainsKey(key);
		}

		public override void Clear()
		{
			this.Entries.Clear();
		}
	}

	public class MandatoryAttribute : Attribute
	{
		public string[] Keys { get; protected set; }

		public MandatoryAttribute(params string[] keys)
		{
			this.Keys = keys;
		}
	}

	public class MandatoryValueException : DatabaseWarningException
	{
		public string Key { get; private set; }

		public MandatoryValueException(string path, string key, JObject obj)
			: base("Missing mandatory value '" + key + "', in " + Environment.NewLine + obj.ToString(), path)
		{
			this.Key = key;
		}
	}

	internal static class JsonExtensions
	{
		internal static bool ReadBool(this JObject obj, string key, bool def = false) { return (bool)(obj[key] ?? def); }
		internal static byte ReadByte(this JObject obj, string key, byte def = 0) { return (byte)(obj[key] ?? def); }
		internal static sbyte ReadSByte(this JObject obj, string key, sbyte def = 0) { return (sbyte)(obj[key] ?? def); }
		internal static short ReadShort(this JObject obj, string key, short def = 0) { return (short)(obj[key] ?? def); }
		internal static ushort ReadUShort(this JObject obj, string key, ushort def = 0) { return (ushort)(obj[key] ?? def); }
		internal static int ReadInt(this JObject obj, string key, int def = 0) { return (int)(obj[key] ?? def); }
		internal static uint ReadUInt(this JObject obj, string key, uint def = 0) { return (uint)(obj[key] ?? def); }
		internal static float ReadFloat(this JObject obj, string key, float def = 0) { return (float)(obj[key] ?? def); }
		internal static double ReadDouble(this JObject obj, string key, double def = 0) { return (double)(obj[key] ?? def); }
		internal static string ReadString(this JObject obj, string key, string def = "") { return (string)(obj[key] ?? def); }

		internal static bool ContainsKeys(this JObject obj, params string[] keys)
		{
			if (keys.Length == 1)
				return (obj[keys[0]] != null);

			foreach (var key in keys)
			{
				if (obj[key] == null)
					return false;
			}

			return true;
		}

		internal static void AssertNotMissing(this JObject obj, params string[] keys)
		{
			foreach (var key in keys)
			{
				if (obj[key] == null)
					throw new MandatoryValueException(null, key, obj);
			}
		}
	}
}
