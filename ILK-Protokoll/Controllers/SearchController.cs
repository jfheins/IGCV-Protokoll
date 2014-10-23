﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ILK_Protokoll.Models;
using ILK_Protokoll.util;
using ILK_Protokoll.ViewModels;
using StackExchange.Profiling;

namespace ILK_Protokoll.Controllers
{
	public class SearchController : BaseController
	{
		// GET: Results
		// Einfache Suche über die Navbar
		public ActionResult Results(string searchterm)
		{
			if (string.IsNullOrWhiteSpace(searchterm))
				return RedirectToAction("Index");

			var sw = new Stopwatch();
			sw.Start();
			IQueryable<Topic> query = db.Topics
				.Include(t => t.Assignments)
				.Include(t => t.Comments)
				.Include(t => t.Decision)
				.Include(t => t.Documents)
				.Include(t => t.Tags);

			ILookup<string, string> tokens = Tokenize(searchterm);
			RestrictToAllTags(ref query, tokens["hasTag"]);


			if (tokens["has"].Contains("Decision", StringComparer.InvariantCultureIgnoreCase))
				query = query.Where(t => t.Decision != null);

			Regex[] searchTerms = MakePatterns(tokens[""]).Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray();
			var results = new SearchResultList();

			var profiler = MiniProfiler.Current;

			using (profiler.Step("Suche in Themen"))
			{
				foreach (Topic topic in query)
				{
					SearchDecision(topic, searchTerms, results);
					SearchTopic(topic, searchTerms, results);
					SearchComments(topic, searchTerms, results);
					SearchAssignments(topic, searchTerms, results);
					SearchAttachments(topic, searchTerms, results);
				}
			}
			using (profiler.Step("Suche in Listen"))
			{
				SearchLists(searchTerms, results);
			}

			ViewBag.ElapsedMilliseconds = sw.ElapsedMilliseconds;
			ViewBag.SearchTerm = searchterm;
			ViewBag.SearchPatterns = searchTerms;
			results.Sort(); // Absteigend sortieren nach Score
			return View(results);
		}

		// GET: /Search
		// Erweiterte Suche 
		public ActionResult Index(ExtendedSearchVM input)
		{
			if (string.IsNullOrWhiteSpace(input.Searchterm))
				return View("SearchMask", input);
			else
				return ExtendendResults(input);
		}

		private static ILookup<string, string> Tokenize(string str)
		{
			return Regex.Matches(str, @"(?<=^|\s)((?<disc>\w+):)?((?<token>[^\s""]+)|""(?<token>[^""]+)"")")
				.Cast<Match>()
				.ToLookup(match => match.Groups["disc"].ToString(),
					match => match.Groups["token"].ToString(),
					StringComparer.InvariantCultureIgnoreCase);
		}

		private void RestrictToAllTags(ref IQueryable<Topic> query, IEnumerable<string> tags)
		{
			string[] names = tags.ToArray();
			int[] tagIDs = db.Tags.Where(t => names.Contains(t.Name)).Select(t => t.ID).ToArray();

			query = query.Where(topic => (from tt in topic.Tags
				where tagIDs.Contains(tt.TagID)
				select tt).Count() == tagIDs.Count());
		}

		private void RestrictToAnyTag(ref IQueryable<Topic> query, IEnumerable<string> tags)
		{
			string[] names = tags.ToArray();
			int[] tagIDs = db.Tags.Where(t => names.Contains(t.Name)).Select(t => t.ID).ToArray();

			query = query.Where(topic => (from tt in topic.Tags
				where tagIDs.Contains(tt.TagID)
				select tt).Any());
		}

		/// <summary>
		///    Generiert aus einer Menge von Tokens eine Menge von Patterns. Die Tokens werden nach "or" durchsucht,
		///    und die benachbarten Tokens werden schließlich in einem kombinierten Regex zurückgegeben. Die Tokens
		///    werden für den Regex passend escapet.
		/// </summary>
		/// <param name="items">Die Token, aus denen die Patterns erzeugt werden sollen.</param>
		/// <returns>Patterns, die sich aus den Tokens ergeben.</returns>
		private static IEnumerable<string> MakePatterns(IEnumerable<string> items)
		{
			const string delimiter = "or";
			Func<IEnumerable<string>, string> escapeAndJoin = x => x.Select(Regex.Escape).Aggregate((a, b) => a + "|" + b);

			var orItems = new List<string>();
			string lastToken = null;
			foreach (string item in items)
			{
				if (string.Equals(item, delimiter, StringComparison.OrdinalIgnoreCase))
				{
					if (lastToken != null)
						orItems.Add(lastToken);
					lastToken = null;
				}
				else
				{
					if (lastToken != null)
					{
						if (orItems.Count > 0)
						{
							orItems.Add(lastToken);
							yield return @"\b(" + escapeAndJoin(orItems) + ")";
							orItems.Clear();
						}
						else
							yield return @"\b" + lastToken;
					}
					lastToken = item;
				}
			}

			if (orItems.Count > 0)
			{
				if (lastToken != null)
					orItems.Add(lastToken);

				yield return @"\b(" + escapeAndJoin(orItems) + ")";
			}
			else if (lastToken != null)
				yield return @"\b" + lastToken;
		}

		private ActionResult ExtendendResults(ExtendedSearchVM input)
		{
			//var sw = new Stopwatch();
			//sw.Start();

			//var tokens = Tokenize(input.Searchterm).ToList();
			//var searchpatterns = MakePatterns(tokens, "AND");
			//var regexes = searchpatterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase));

			//var sets = new List<HashSet<SearchResult>>();

			//foreach (var regex in regexes)
			//{
			//	var currentset = new HashSet<SearchResult>();
			//	sets.Add(currentset);

			//	if (input.SearchTopics)
			//		SearchTopics(regex, currentset);
			//	if (input.SearchComments)
			//		SearchComments(regex, currentset);
			//	if (input.SearchAssignments)
			//		SearchAssignments(regex, currentset);
			//	if (input.SearchAttachments)
			//		SearchAttachments(regex, currentset);
			//	if (input.SearchDecisions)
			//		SearchDecisions(regex, currentset);
			//	if (input.SearchLists)
			//		SearchLists(regex, currentset);
			//}

			//var results = sets.Aggregate((a, b) =>
			//{
			//	a.IntersectWith(b);
			//	return a;
			//}).ToList();


			//ViewBag.ElapsedMilliseconds = sw.ElapsedMilliseconds;
			//ViewBag.SearchTerm = input.Searchterm;
			//ViewBag.SearchPattern = @"\b(" + tokens.Where(x => x != "AND").Aggregate((a, b) => a + "|" + b) + ")";
			//results.Sort((a, b) => b.Score.CompareTo(a.Score)); // Absteigend sortieren

			return View("Results", null);
		}

		private void SearchDecision(Topic topic, Regex[] searchterms, SearchResultList resultlist)
		{
			Decision decision = topic.Decision;
			if (decision == null)
				return;

			float score = decision.Type == DecisionType.Resolution ? 0.0f : -5;
			var hitlist = new HashSet<Hit>(new HitPropertyComparer());

			foreach (Regex pattern in searchterms)
			{
				float oldScore = score;

				MatchCollection m = pattern.Matches(decision.OriginTopic.Title);
				if (m.Count > 0)
				{
					score += ScoreMult(21, m.Count);
					hitlist.Add(new Hit("Titel", decision.OriginTopic.Title));
				}

				m = pattern.Matches(decision.Text);
				if (m.Count > 0)
				{
					score += 16;
					hitlist.Add(new Hit("Beschlusstext", decision.Text));
				}

				if (score <= oldScore)
					return;
			}

			if (score > 0)
			{
				resultlist.Add(topic.ID, new SearchResult
				{
					ID = decision.ID,
					Score = score,
					EntityType = decision.Type.DisplayName(),
					Title = decision.OriginTopic.Title,
					ActionURL = Url.Action("Details", "Topics", new {id = decision.OriginTopic.ID}),
					Timestamp = decision.Report.End,
					Hits = hitlist.ToList(),
					Tags = topic.Tags.Select(tt => tt.Tag).ToArray()
				});
			}
		}

		private void SearchTopic(Topic topic, Regex[] searchterms, SearchResultList resultlist)
		{
			float score = topic.IsReadOnly ? -5 : 0.0f;
			var hitlist = new HashSet<Hit>(new HitPropertyComparer());

			foreach (Regex pattern in searchterms)
			{
				float oldScore = score;

				MatchCollection m;
				if (!resultlist.Contains(topic.ID)) // Duplikat Beschlussvorschlag/Beschlusstext vermeiden
				{
					m = pattern.Matches(topic.Title);
					if (m.Count > 0)
					{
						score += ScoreMult(20, m.Count);
						hitlist.Add(new Hit("Titel", topic.Title));
					}

					m = pattern.Matches(topic.Proposal);
					if (m.Count > 0)
					{
						score += ScoreMult(8, m.Count);
						hitlist.Add(new Hit("Beschlusstext", topic.Proposal));
					}
				}

				m = pattern.Matches(topic.Description);
				if (m.Count > 0)
				{
					score += ScoreMult(6, m.Count);
					hitlist.Add(new Hit("Beschreibung", topic.Description));
				}
				if (score <= oldScore)
					return;
			}
			if (score <= 0)
				return;

			if (resultlist.Contains(topic.ID))
				resultlist.Amend(topic.ID, score, hitlist);
			else
			{
				resultlist.Add(topic.ID, new SearchResult
				{
					ID = topic.ID,
					Score = score,
					EntityType = topic.HasDecision() ? topic.Decision.Type.DisplayName() : "Diskussion",
					Title = topic.Title,
					ActionURL = Url.Action("Details", "Topics", new {id = topic.ID}),
					Timestamp = topic.Created,
					Hits = hitlist.ToList(),
					Tags = topic.Tags.Select(tt => tt.Tag).ToArray()
				});
			}
		}

		private void SearchComments(Topic topic, Regex[] searchterms, SearchResultList resultlist)
		{
			foreach (Comment comment in topic.Comments)
			{
				float score = 0.0f;
				foreach (Regex pattern in searchterms)
				{
					MatchCollection m = pattern.Matches(comment.Content);
					if (m.Count > 0)
						score += ScoreMult(2, m.Count);
					else
						score = float.NaN;
				}
				if (float.IsNaN(score))
					continue;

				if (resultlist.Contains(topic.ID))
					resultlist.Amend(topic.ID, score, new Hit("Kommentar", comment.Content));
				else
				{
					resultlist.Add(new SearchResult(comment.Content)
					{
						ID = comment.ID,
						Score = score,
						EntityType = "Kommentar",
						Title = topic.Title,
						ActionURL = Url.Action("Details", "Topics", new {id = topic.ID}),
						Timestamp = comment.Created,
						Tags = topic.Tags.Select(tt => tt.Tag).ToArray()
					});
				}
			}
		}

		private void SearchAssignments(Topic topic, Regex[] searchterms, SearchResultList resultlist)
		{
			var hitlist = new HashSet<Hit>(new HitPropertyComparer());
			foreach (Assignment assignment in topic.Assignments)
			{
				float score = 0.0f;
				foreach (Regex pattern in searchterms)
				{
					MatchCollection m = pattern.Matches(assignment.Title);
					if (m.Count > 0)
					{
						score += ScoreMult(9, m.Count);
						hitlist.Add(new Hit("Aufgabentitel", assignment.Title));
						continue;
					}

					m = pattern.Matches(assignment.Description);
					if (m.Count > 0)
					{
						score += ScoreMult(7, m.Count);
						hitlist.Add(new Hit("Aufgabentext", assignment.Title));
						continue;
					}
					score = float.NaN;
				}
				if (float.IsNaN(score))
					continue;

				if (resultlist.Contains(topic.ID))
					resultlist.Amend(topic.ID, score, hitlist);
				else
				{
					resultlist.Add(new SearchResult("Aufgabe", assignment.Description)
					{
						ID = assignment.ID,
						Score = score,
						EntityType = "Aufgabe",
						Title = assignment.Description,
						ActionURL = Url.Action("Details", "Assignments", new {id = assignment.ID}),
						Timestamp = assignment.DueDate,
						Hits = hitlist.ToList(),
						Tags = topic.Tags.Select(tt => tt.Tag).ToArray()
					});
				}
				hitlist.Clear();
			}
		}

		private void SearchAttachments(Topic topic, Regex[] searchterms, SearchResultList resultlist)
		{
			foreach (Document attachment in topic.Documents)
			{
				float score = 0.0f;
				foreach (Regex pattern in searchterms)
				{
					MatchCollection m = pattern.Matches(attachment.DisplayName);
					if (m.Count > 0)
						score += ScoreMult(9, m.Count);
					else
						score = float.NaN;
				}
				if (float.IsNaN(score))
					continue;

				if (resultlist.Contains(topic.ID))
					resultlist.Amend(topic.ID, score, new Hit("Dateiname", attachment.DisplayName));
				else
				{
					resultlist.Add(new SearchResult("Dateiname", attachment.DisplayName)
					{
						ID = attachment.ID,
						Score = score,
						EntityType = "Dokument",
						Title = attachment.DisplayName,
						ActionURL = Url.Action("Details", "Attachments", new {id = attachment.ID}),
						Timestamp = attachment.Created,
						Tags = topic.Tags.Select(tt => tt.Tag).ToArray()
					});
				}
			}
		}

		private void SearchLists(Regex[] searchterms, SearchResultList resultlist)
		{
			foreach (var item in db.LEvents)
			{
				float score = 0.0f;
				foreach (Regex pattern in searchterms)
				{
					var m = pattern.Matches(item.Description);
					if (m.Count > 0)
					{
						score += 7;
						continue;
					}
					m = pattern.Matches(item.Place);
					if (m.Count > 0)
					{
						score += 5;
						continue;
					}
					score = float.NaN;
				}
				if (!float.IsNaN(score))
				{
					resultlist.Add(new SearchResult(item.Description)
					{
						ID = item.ID,
						Score = score,
						EntityType = "Listeneintrag",
						Title = "Termin",
						ActionURL = Url.Content("~/ViewLists#event_table"),
						Timestamp = item.StartDate
					});
				}
			}

			foreach (var item in db.LConferences)
			{
				if (searchterms.All(pattern => pattern.IsMatch(item.Description)))
				{
					resultlist.Add(new SearchResult(item.Description)
					{
						ID = item.ID,
						Score = 7,
						EntityType = "Listeneintrag",
						Title = "Auslandskonferenz",
						ActionURL = Url.Content("~/ViewLists#conference_table"),
						Timestamp = item.Created
					});
				}
			}
			
			foreach (var item in db.LExtensions)
			{
				if (searchterms.All(pattern => pattern.IsMatch(item.Comment)))
				{
					resultlist.Add(new SearchResult(item.Comment)
					{
						ID = item.ID,
						Score = 7,
						EntityType = "Listeneintrag",
						Title = "Vertragsverlängerung",
						ActionURL = Url.Content("~/ViewLists#conference_table"),
						Timestamp = item.Created
					});
				}
			}

			//-----------------------------------------------------------------------------------------------------
			foreach (var item in db.LEmployeePresentations)
			{
				if (searchterms.Any(pattern => pattern.IsMatch(item.Employee)))
				{
					resultlist.Add(new SearchResult("Mitarbeiter", item.Employee)
					{
						ID = item.ID,
						Score = 7,
						EntityType = "Listeneintrag",
						Title = "Mitarbeiterpräsentation",
						ActionURL = Url.Content("~/ViewLists#conference_table"),
						Timestamp = item.Created
					});
				}
			}
			foreach (var doc in db.Documents.Where(doc => doc.EmployeePresentationID != null))
			{
				if (searchterms.All(pattern => pattern.IsMatch(doc.DisplayName)))
				{
					resultlist.Add(new SearchResult("Dateiname", doc.DisplayName)
					{
						ID = doc.ID,
						Score = 7,
						EntityType = "Dokument",
						Title = doc.DisplayName,
						ActionURL = Url.Action("Details", "Attachments", new { id = doc.ID }),
						Timestamp = doc.Created
					});
				}
			}
			//-----------------------------------------------------------------------------------------------------

			foreach (var item in db.LIlkDays)
			{
				if (searchterms.All(pattern => pattern.IsMatch(item.Topics)))
				{
					resultlist.Add(new SearchResult(item.Topics)
					{
						ID = item.ID,
						Score = 7,
						EntityType = "Listeneintrag",
						Title = "ILK-Tag",
						ActionURL = Url.Content("~/ViewLists#ilkDay_table"),
						Timestamp = item.Created
					});
				}
			}

			foreach (var item in db.LIlkMeetings)
			{
				if (searchterms.All(pattern => pattern.IsMatch(item.Comments)))
				{
					resultlist.Add(new SearchResult(item.Comments)
					{
						ID = item.ID,
						Score = 7,
						EntityType = "Listeneintrag",
						Title = "ILK-Regeltermin",
						ActionURL = Url.Content("~/ViewLists#ilkMeeting_table"),
						Timestamp = item.Created
					});
				}
			}

			foreach (var item in db.LOpenings)
			{
				if (searchterms.All(pattern => pattern.IsMatch(item.Description)))
				{
					resultlist.Add(new SearchResult(item.Description)
					{
						ID = item.ID,
						Score = 6.5f,
						EntityType = "Listeneintrag",
						Title = "Vakante Stelle",
						ActionURL = Url.Content("~/ViewLists#opening_table"),
						Timestamp = item.Created
					});
				}
			}
		}

		private static float ScoreMult(int baseScore, int count)
		{
			if (baseScore == 0 || count == 0)
				return 0;
			else
				return baseScore * (float)(5 - 4 * Math.Pow(0.8, count - 1));
		}
	}

	public class HitPropertyComparer : IEqualityComparer<Hit>
	{
		public bool Equals(Hit x, Hit y)
		{
			return string.Equals(x.Property, y.Property);
		}

		public int GetHashCode(Hit obj)
		{
			return obj.Property.GetHashCode();
		}
	}

	public class SREntityComparer : IEqualityComparer<SearchResult>
	{
		private readonly string _entity;

		public SREntityComparer(string entity)
		{
			_entity = entity;
		}

		public bool Equals(SearchResult x, SearchResult y)
		{
			return x.EntityType == _entity && y.EntityType == _entity && x.ID == y.ID;
		}

		public int GetHashCode(SearchResult obj)
		{
			return obj.ID;
		}
	}
}