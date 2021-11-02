using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace InfoCaster.Umbraco.UrlTracker.Extensions
{
	public static class NameValueCollectionExtension
	{
		public static bool CollectionEquals(this NameValueCollection nameValueCollection1, NameValueCollection nameValueCollection2)
		{
			return nameValueCollection1.ToKeyValue().SequenceEqual(nameValueCollection2.ToKeyValue());
		}

		public static NameValueCollection Clone(NameValueCollection collection)
		{
			return new NameValueCollection(collection);
		}

		public static void Append(this NameValueCollection nameValueCollection1, NameValueCollection nameValueCollection2)
		{
			if (nameValueCollection1 == null)
			{
				throw new ArgumentNullException("first");
			}
			if (nameValueCollection2 != null)
			{
				for (int i = 0; i < nameValueCollection2.Count; i++)
				{
					nameValueCollection1.Set(nameValueCollection2.GetKey(i), nameValueCollection2.Get(i));
				}
			}
		}

		public static NameValueCollection Merge(this NameValueCollection nameValueCollection1, NameValueCollection nameValueCollection2)
		{
			if (nameValueCollection1 == null && nameValueCollection2 == null)
			{
				return null;
			}
			if (nameValueCollection1 != null && nameValueCollection2 == null)
			{
				return Clone(nameValueCollection1);
			}
			if (nameValueCollection1 == null && nameValueCollection2 != null)
			{
				return Clone(nameValueCollection2);
			}
			NameValueCollection nameValueCollection3 = Clone(nameValueCollection1);
			nameValueCollection3.Append(nameValueCollection2);
			return nameValueCollection3;
		}

		public static string ToQueryString(this NameValueCollection nameValueCollection)
		{
			List<string> list = new List<string>();
			foreach (string item in nameValueCollection)
			{
				list.Add(item + "=" + HttpUtility.UrlEncode(nameValueCollection[item]));
			}
			return string.Join("&", list.ToArray());
		}

		private static IEnumerable<object> ToKeyValue(this NameValueCollection nameValueCollection)
		{
			return from x in nameValueCollection.AllKeys
				orderby x
				select new
				{
					Key = x,
					Value = nameValueCollection[x]
				};
		}
	}
}
