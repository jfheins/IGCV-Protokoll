using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace IGCV_Protokoll.ViewModels
{
	public class StatisticsViewModels : IEnumerable<Stat>
	{
		public List<Stat> Items { get; set; } = new List<Stat>();

		public void Add(string name, int value, string tpl = null)
		{
			Items.Add(new Stat { Name = name, Value = value, DisplayTemplate = tpl});
		}

		public IEnumerator<Stat> GetEnumerator()
		{
			return ((IEnumerable<Stat>)Items).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Stat>)Items).GetEnumerator();
		}
	}
	public class Stat
	{
		public string Name { get; set; }
		public int Value { get; set; }
		public string DisplayTemplate { get; set; }
	}
}