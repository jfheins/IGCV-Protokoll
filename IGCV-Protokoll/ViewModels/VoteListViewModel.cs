﻿using System.Collections.Generic;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.ViewModels
{
	public class VoteListViewModel
	{
		public VoteListViewModel()
		{
			OtherVotes = new List<Vote>();
		}

		public int TopicID { get; set; }
		public Vote OwnVote { get; set; }
		public VoteLinkLevel LinkLevel { get; set; }
		public IEnumerable<Vote> OtherVotes { get; set; }
	}

	/// <summary>
	///    Bestimmt, welche Icons verlinkt werden, um eine Stimmabgabe zu ermöglichen
	/// </summary>
	public enum VoteLinkLevel
	{
		/// <summary>
		///    Es werden keine Links generiert
		/// </summary>
		None,

		/// <summary>
		///    Es wird nur ein Link zur Abgabe der eigenen Stimme generiert
		/// </summary>
		OnlyMine,

		/// <summary>
		///    Es wird für jeden Stimmberechtigten ein Link generiert
		/// </summary>
		Everyone
	}
}